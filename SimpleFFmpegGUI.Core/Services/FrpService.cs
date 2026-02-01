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
    private readonly IFtpServerHost ftpServerHost;

    private readonly ServiceProvider serviceProvider;

    这里融合，不要单独FTP服务了
    internal FtpService(string path, int port)
    {
        if (port <= 0)
        {
            port = FreeTcpPort();
        }
        var services = new ServiceCollection();

        services.Configure<DotNetFileSystemOptions>(opt => opt
            .RootPath = path);

        services.AddFtpServer(builder => builder
            .UseDotNetFileSystem()
            .EnableAnonymousAuthentication());

        services.Configure<FtpServerOptions>(opt => opt.Port = port);

        serviceProvider = services.BuildServiceProvider();
        ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();
        Path = path;
        Port = port;
    }
    ~FtpService()
    {
        Dispose();
    }

    public string Path { get; }
    public int Port { get; }

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

    public Task StartAsync()
    {
        if (ftpServerHost == null)
        {
            throw new NullReferenceException("请先初始化");
        }
        return ftpServerHost.StartAsync(CancellationToken.None);
    }

    public Task StopAsync()
    {
        if (ftpServerHost == null)
        {
            throw new NullReferenceException("请先初始化");
        }
        return ftpServerHost.StopAsync(CancellationToken.None);
    }

    public bool IsRunning()
    {
        if (ftpServerHost == null)
        {
            throw new NullReferenceException("请先初始化");
        }
        return ftpServerHost.;
    }
}