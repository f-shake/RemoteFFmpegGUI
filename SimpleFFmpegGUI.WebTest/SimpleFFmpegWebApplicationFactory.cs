using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SimpleFFmpegGUI.WebTest;

public class SimpleFFmpegWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 这里的配置会对该 Factory 创建的所有实例生效
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Token"] = "Test_Token_123",
                ["Database:ConnectionString"] = "DataSource=:memory:"
            });
        });

        // 如果你以后需要 Mock 掉某个单例服务，可以在这里处理：
        // builder.ConfigureServices(services => { ... });
    }
}