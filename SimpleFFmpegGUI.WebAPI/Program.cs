using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using SimpleFFmpegGUI;
using SimpleFFmpegGUI.Caching;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Controllers;
using SimpleFFmpegGUI.WebAPI.Realtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Compatibility;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Converters;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Models;
using Log = Serilog.Log;

WebApplication app = null;

FzLib.Application.UnhandledExceptionCatcher.WithCatcher(() => { CreateWebApplication(args); })
    .Catch((ex, source) => { Log.Fatal(ex, "程序发生未捕获的异常"); })
    .Run();


/// <summary>
/// 初始化 Serilog 文件日志。须在 MigrateDb 等可能失败的步骤之前调用（P3-7）。
/// </summary>
static void InitializeFileLogger()
{
    int processId = Process.GetCurrentProcess().Id;
    // 生产（非 Development）用 Information，避免 EF Core 的 Debug 查询日志刷屏；Development 保留 Debug 便于排障。
    var isDevelopment = string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)
        // 抑制 ASP.NET Core / System 框架的 Debug（以及逐请求的 Information）日志刷屏，仅保留应用自身日志。
        // 应用日志（SimpleFFmpegGUI.*、顶层 Log.Information 等）不受此覆盖影响，仍按上面的全局级别
        // （开发 Debug / 生产 Information）输出，便于排障。
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.WithProperty("ProcessId", processId)
        .WriteTo.File("logs/logs.txt",
            outputTemplate:
            "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] [PID:{ProcessId}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console() // 恒用 console sink，便于控制台查看启动/运行日志
        .CreateLogger();
    Log.Information("程序启动");
}

static void InitializeLogs(IServiceProvider services)
{
    //数据库日志
    services.GetRequiredService<DbLoggerService>().Log += Logger_Log;
    services.GetRequiredService<DbLoggerService>().LogSaveFailed += Logger_LogSaveFailed;

    void Logger_Log(object sender, LogEventArgs e)
    {
        switch (e.Log.Type)
        {
            case 'E': Log.Error(e.Log.Message); break;
            case 'W': Log.Warning(e.Log.Message); break;
            case 'I': Log.Information(e.Log.Message); break;
        }
    }

    void Logger_LogSaveFailed(object sender, ExceptionEventArgs e)
    {
        Log.Error(e.Exception.Message, e.Exception);
    }
}

void CreateWebApplication(string[] args)
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    // 提前初始化文件日志，使 MigrateDb 等早期步骤的异常也能落盘（P3-7）
    InitializeFileLogger();
    var builder = WebApplication.CreateBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    ConfigureAppsettings(builder);
    ConfigureServices(builder);
    app = builder.Build();
    ConfigureMiddleware(app);
    // 先确保 schema（EnsureCreated），再执行迁移（可能打标基线/升级到当前版本）。
    // EnsureCreated 仅在"无任何表"时建表，遇到已有表（含 v1 旧库）是 no-op。
    InitializeDatabase(app);
    MigrateDb(app.Configuration);
    InitializeLogs(app.Services);
    app.Run();
}

void InitializeDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FFmpegDbContext>>();
    using var context = contextFactory.CreateDbContext();
    context.Database.EnsureCreated();
}

void ConfigureAppsettings(WebApplicationBuilder builder)
{
    builder.Configuration.SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
}

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.Configure<AppSettings>(builder.Configuration);
    builder.Services.AddFFmpegServices();
    builder.Services.AddKeyedSingleton<FtpService>(FileController.InputFtpKey);
    builder.Services.AddKeyedSingleton<FtpService>(FileController.OutputFtpKey);
    builder.Services.AddHealthChecks();
    builder.Services.AddWindowsService();
    // 生产环境 UseExceptionHandler()（无参）会生成 ProblemDetails 响应（P3-7 统一异常处理）。
    // 必须注册 IProblemDetailsService，否则非 Development 环境下启动即抛
    // "Either the 'ExceptionHandlingPath' or the 'ExceptionHandler' property must be set"。
    builder.Services.AddProblemDetails();
    // 添加控制器
    builder.Services.AddControllers(options =>
        {
            options.Filters.Add<AppActionFilter>();
            // 所有控制器挂到 /api 下，与根路径的 history 前端路由隔离（见 ApiRoutePrefixConvention）。
            options.Conventions.Add(new ApiRoutePrefixConvention());
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DoubleConverter());
            options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
        });

    // 实时推送（SignalR）。Hub 挂在 /api/queue-hub，前端只走 WebSocket（skipNegotiation），
    // 连不上时前端自行降级为 HTTP 轮询，因此不需要配置 SignalR 的传输层回退。
    builder.Services.AddSignalR().AddJsonProtocol(options =>
    {
        // 必须与 MVC 的 JSON 选项（上面的 AddJsonOptions）保持一致：camelCase + TimeSpan 转秒数 +
        // NaN/Infinity 转 null。否则前端拿到的 status.time 会变成 "00:01:23" 字符串，
        // 状态栏里"用播放时间点配合快照"的数值比较会失效。
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new DoubleConverter());
        options.PayloadSerializerOptions.Converters.Add(new TimeSpanConverter());
    });

    // 队列状态的唯一出处（推送与 HTTP 兜底共用，内部维护推送序号），必须是单例
    builder.Services.AddSingleton<QueueStateProvider>();
    builder.Services.AddHostedService<RealtimeBroadcaster>();
    // 快照缓存（全站最贵的请求：每次都要起一个 ffmpeg 进程截图）
    builder.Services.AddSingleton<SnapshotCache>();

    // 只关闭"引用类型字符串字段被隐式判为 [Required]"（Nullable=disable 时 DTO 的
    // Size/AspectRatio/PixelFormat/Extra/Format 等合法 null 字段会被误拒为 400）。
    // 不改用 SuppressModelStateInvalidFilter，以保留值类型参数（seconds/priority）绑定失败的自动 400。
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    // 添加API探索器和Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SimpleFFmpegGUI API",
            Version = "v1"
        });

        // 配置认证类型
        c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme."
        });

        c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("bearer", document)] = []
        });
    });


    // 配置表单选项
    builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = int.MaxValue; });

    // 配置CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });
}

void ConfigureMiddleware(WebApplication app)
{
    // 应用订阅在子路径（与前端 base 一致，如 /ffmpeg）：从请求路径剥掉该前缀，使控制器、静态资源、
    // SPA 回退都按根路径处理。前缀从 appsettings 的 PathBase 读取，留空则应用在根路径。
    // 经 nginx 部署时（proxy_pass http://127.0.0.1:5001/; 剥前缀）此步近乎 no-op；
    // 本地直接访问 http://localhost:5001/ffmpeg/ 时由它定位。
    // 注：不能写成 Urls http://0.0.0.0:5001/ffmpeg——Kestrel 不接受地址含路径（会抛
    // "A path base can only be configured using IApplicationBuilder.UsePathBase()"）。
    // 规范化 PathBase：补前导 /、去尾斜杠；值为根路径(/)或空则等效于不挂子路径，不调用 UsePathBase。
    // 这样配置值只需写到一处，前端注入 <base> 与 UsePathBase 使用同一规范化结果。
    var rawPathBase = app.Configuration["PathBase"];
    var pathBase = string.IsNullOrWhiteSpace(rawPathBase) ? null : rawPathBase.Trim();
    if (pathBase != null && !pathBase.StartsWith("/")) pathBase = "/" + pathBase;
    if (pathBase != null && pathBase.Length > 1) pathBase = pathBase.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(pathBase) && pathBase != "/")
    {
        app.UsePathBase(pathBase);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // 生产环境统一异常处理（P3-7）
        app.UseExceptionHandler();
    }

    // 托管前端（Vue 构建产物位于 wwwroot）。返回 index.html 时动态注入 <base href="PathBase/">，
    // 前端仅在 src/config.ts 的 getBase() 一处从这个 <base> 读取部署基址，因此 /ffmpeg 只在
    // appsettings 的 PathBase 配置一次，改前缀无需动前端。
    var indexFile = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");

    // 注入 <base> 的逻辑见 HtmlBaseInjector（关键：<base> 必须注入在 <head> 起始处，排在 <script>/<link> 之前）。
    // 前缀只在此路径 + appsettings 的 PathBase 出现一次，前端零硬编码。

    // 显式访问 /index.html：静态文件中间件会直接返回原始文件（缺 <base>，导致 history 路由基址与
    // cookie 路径错误、SPA 渲染空白），故先在此拦下并注入 <base>。其余 /assets/... 等仍走静态文件中间件。
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
            && File.Exists(indexFile))
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            var html = File.ReadAllText(indexFile);
            await context.Response.WriteAsync(HtmlBaseInjector.InjectBaseHref(html, pathBase));
            return;
        }
        await next();
    });

    app.UseStaticFiles();

    // 实时通道（Hub）的鉴权：Hub 是 SignalR 自管的路由，MVC 的 AppActionFilter 管不到它，
    // 只能在这里校验。规则与 AppActionFilter 完全一致（Token 为空则不校验，否则要求凭据匹配）。
    // 必须放在 UseRouting 之前、且在升级连接之前就拒绝：否则连接建立后才断开，前端只能看到"连上又断"。
    app.Use(async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments("/api/queue-hub"))
        {
            await next();
            return;
        }

        var token = app.Configuration["Token"];
        if (!string.IsNullOrEmpty(token))
        {
            // 浏览器发 WebSocket 时无法自定义请求头，只能把凭据放在 Cookie 或 URL 里：
            // 优先 Cookie（前端 api.ts 已把 token 存在名为 token 的 cookie 中，同源握手自动携带、不进访问日志），
            // 其次 access_token 查询参数（跨域/dev 以及将来 WebView 场景的兜底），
            // 最后 Authorization 头（非 WebSocket 传输，例如改用 LongPolling 的客户端只能靠它）
            var provided = context.Request.Cookies["token"];
            var source = "Cookie";
            if (string.IsNullOrEmpty(provided))
            {
                provided = context.Request.Query["access_token"];
                source = "Query";
            }
            if (string.IsNullOrEmpty(provided))
            {
                provided = context.Request.Headers.Authorization;
                source = "Header";
            }

            provided = provided?.Trim();
            // 先记下"到底有没有带凭据"，再裁剪 Bearer 前缀：否则客户端发一个字面量 "Bearer "（空凭据）
            // 会被记成"没带"，排查时分不清"没登录"与"凭据写错"
            var hasCredential = !string.IsNullOrEmpty(provided);
            if (provided?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                provided = provided["Bearer ".Length..];
            }

            if (provided != token)
            {
                // 必须留日志（带来源）：否则握手静默 401，前端只表现为"实时连不上"，无从排查
                Log.Warning("实时通道鉴权失败：IP={Ip} Path={Path} Source={Source} 是否带凭据={HasCredential}",
                    context.Connection.RemoteIpAddress, context.Request.Path, source, hasCredential);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next();
    });

    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthorization();

    app.MapControllers();
    // 实时推送 Hub。必须挂在 /api 下：MapFallback 只对 /api、/health 前缀（以及末段含点的路径）返回 404，
    // 其它路径的 GET 会被回退成 200 + index.html —— 对 SignalR 客户端就是"拿到一份 HTML"的隐蔽失败
    app.MapHub<QueueHub>("/api/queue-hub");
    app.MapHealthChecks("/health");

    // 根路径入口：有前端时返回（注入 <base> 的）index.html（SPA 入口）；无前端（裸 API/测试宿主）时返回横幅。
    app.MapGet("/", () =>
    {
        if (!File.Exists(indexFile))
        {
            return Results.Text("SimpleFFmpegGUI API is running!");
        }
        var html = File.ReadAllText(indexFile);
        return Results.Text(HtmlBaseInjector.InjectBaseHref(html, pathBase), "text/html; charset=utf-8");
    });

    // 前端采用 history 路由：对未匹配的前端 GET/HEAD 统一回退到（注入 <base> 的）index.html，使刷新/直链
    // history 地址也能正确返回 SPA 入口（最低优先级端点，不遮蔽控制器与静态文件）。
    // 但对 /api/**、/health**、缺失的静态文件（末段含 "."）及非 GET/HEAD 一律 404，避免把 API/资产错误
    // 伪装成 200（掩盖后端故障或残缺部署）。
    app.MapFallback(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var isGetOrHead = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
        var isApiOrHealth = path.Equals("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health/", StringComparison.OrdinalIgnoreCase);
        var lastSegment = path.Substring(path.LastIndexOf('/') + 1);
        var isFileLike = lastSegment.Contains('.');
        if (!isGetOrHead || isApiOrHealth || isFileLike || !File.Exists(indexFile))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        var html = File.ReadAllText(indexFile);
        return context.Response.WriteAsync(HtmlBaseInjector.InjectBaseHref(html, pathBase));
    });
}

static void MigrateDb(IConfiguration configuration)
{
    try
    {
        var connStr = configuration.GetConnectionString(DependencyInjectionExtension.LocalSqliteConnectionStringKey);
        // 迁移出的用户配置要写入 ConfigService 实际读取的位置：它用相对 cwd 的 "config.json"。
        // 故这里也解析为 cwd 下的 config.json，与运行时行为一致（不能写成 BaseDirectory）。
        var configJsonPath = Path.Combine(Environment.CurrentDirectory, "config.json");
        if (MigrationRunner.Upgrade(connStr, configJsonPath))
        {
            Console.WriteLine("数据库迁移完成");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "数据库迁移失败");
        Environment.Exit(-1);
    }
}


public partial class Program
{
}