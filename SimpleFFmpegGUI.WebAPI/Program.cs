using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Serilog;
using SimpleFFmpegGUI;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Controllers;
using System;
using System.Diagnostics;
using System.IO;
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
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Enrich.WithProperty("ProcessId", processId)
        .WriteTo.File("logs/logs.txt",
            outputTemplate:
            "{Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} [{Level:u3}] [PID:{ProcessId}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day)
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
    // 添加控制器
    builder.Services.AddControllers(options => { options.Filters.Add<AppActionFilter>(); })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new DoubleConverter());
            options.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
        });

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

    app.UseHttpsRedirection();

    //if (webApp)
    //{
    //    app.UseStaticFiles();
    //}

    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.MapGet("/", () => "SimpleFFmpegGUI API is running!");
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