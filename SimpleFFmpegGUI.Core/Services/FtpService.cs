using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Services;


public class FtpService : IDisposable
{
    private IFtpServerHost ftpServerHost;

    private ServiceProvider serviceProvider;

    ~FtpService()
    {
        Dispose();
    }

    public string Path { get; private set; }
    public int Port { get; private set; }

    public static int FreeTcpPort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public Task StartAsync(string path, int port)
    {
        if (ftpServerHost != null)
        {
            throw new InvalidOperationException("Ftp服务已启动");
        }
        if (port <= 0)
        {
            port = FreeTcpPort();
        }
        var services = new ServiceCollection();

        services.Configure<DotNetFileSystemOptions>(opt => opt.RootPath = path);
        services.Configure<FtpServerOptions>(opt => opt.Port = port);

        services.AddFtpServer(builder => builder
            .UseDotNetFileSystem()
            .EnableAnonymousAuthentication());

        serviceProvider = services.BuildServiceProvider();
        ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();
        Path = path;
        Port = port;
        return ftpServerHost.StartAsync(CancellationToken.None);
    }

    public Task StopAsync()
    {
        if (ftpServerHost == null)
        {
            throw new NullReferenceException("请先初始化");
        }
        var server = ftpServerHost;
        ftpServerHost = null;
        return server.StopAsync(CancellationToken.None);
    }

    public bool IsRunning => ftpServerHost != null;
}