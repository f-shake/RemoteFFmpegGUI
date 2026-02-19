using FFMpegCore;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Serilog;
using SimpleFFmpegGUI;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Controllers;
using SimpleFFmpegGUI.WebAPI.Converter;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Log = Serilog.Log;

WebApplication app = null;

FzLib.Application.UnhandledExceptionCatcher.WithCatcher(() => { CreateWebApplication(args); })
    .Catch((ex, source) => { Log.Fatal(ex, "程序发生未捕获的异常"); })
    .Run();


static void InitializeLogs(IServiceProvider services)
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
    MigrateDb();
    var builder = WebApplication.CreateBuilder(args);
    ConfigureServices(builder);
    app = builder.Build();
    ConfigureMiddleware(app);
    InitializeDatabase(app);
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

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddFFmpegServices(builder.Configuration);
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

static void MigrateDb()
{
    try
    {
        // FFmpegDbContext.Migrate();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "数据库迁移失败");
        Environment.Exit(-1);
    }
}


public partial class Program
{
}