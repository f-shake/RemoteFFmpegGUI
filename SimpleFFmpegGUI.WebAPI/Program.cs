using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;
using SimpleFFmpegGUI.WebAPI.Controllers;
using SimpleFFmpegGUI.WebAPI.Converter;

// 公共属性
bool webApp = false;
string pipeName = null;
WebApplication app = null;


CreateWebApplication(args);


void CreateWebApplication(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureServices(builder);

    app = builder.Build();
    ConfigureMiddleware(app);

    app.Run();
}

void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<TaskManager>();
    builder.Services.AddSingleton<QueueManager>();
    builder.Services.AddSingleton<PresetManager>();
    builder.Services.AddSingleton<ConfigManager>();
    builder.Services.AddSingleton<LogManager>();
    builder.Services.AddKeyedSingleton<FtpService>(FileController.InputFtpKey);
    builder.Services.AddKeyedSingleton<FtpService>(FileController.OutputFtpKey);
    builder.Services.AddHealthChecks();
    // 添加控制器
    builder.Services.AddControllers(options =>
    {
         options.Filters.Add(new TokenFilter(builder.Configuration));
    })
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
    });

    // 配置表单选项
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = int.MaxValue;
    });

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

    if (webApp)
    {
        app.UseStaticFiles();
    }

    app.UseRouting();
    app.UseCors("AllowAll");
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.MapGet("/", () => "SimpleFFmpegGUI API is running!");
}