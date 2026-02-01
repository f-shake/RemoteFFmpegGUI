using JKang.IpcServiceFramework;
using JKang.IpcServiceFramework.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI;

public class PipeClient
{
    private IIpcClient<IPipeService> client;
    private readonly string hostName;
    private readonly string hostPath;
    private readonly string pipeName;
    private readonly IConfiguration config;

    public PipeClient(IConfiguration config)
    {
        pipeName = config.GetValue<string>("PipeName") ?? throw new Exception("不存在PipeName配置项");

        ServiceProvider serviceProvider = new ServiceCollection()
            .AddNamedPipeIpcClient<IPipeService>("m", pipeName: pipeName)
            .BuildServiceProvider();

        // resolve IPC client factory
        IIpcClientFactory<IPipeService> clientFactory = serviceProvider
            .GetRequiredService<IIpcClientFactory<IPipeService>>();

        // create client
        client = clientFactory.CreateClient("m");
        this.config = config;
    }

    public async Task<TResult> InvokeAsync<TResult>(Expression<Func<IPipeService, TResult>> exp)
    {
        try
        {
            return await client.InvokeAsync(exp);
        }
        catch (IpcFaultException ex)
        {
            throw (ex.InnerException ?? ex).InnerException ?? ex.InnerException ?? ex;
        }
    }

    public async Task InvokeAsync(Expression<Action<IPipeService>> exp)
    {
        try
        {
            await client.InvokeAsync(exp);
        }
        catch (IpcFaultException ex)
        {
            throw (ex.InnerException ?? ex).InnerException ?? ex.InnerException ?? ex;
        }
    }

    public async Task<TResult> InvokeAsync<TResult>(Expression<Func<IPipeService, Task<TResult>>> exp)
    {
        try
        {
            return await client.InvokeAsync(exp);
        }
        catch (IpcFaultException ex)
        {
            throw (ex.InnerException ?? ex).InnerException ?? ex.InnerException ?? ex;
        }
    }

    public async Task InvokeAsync(Expression<Func<IPipeService, Task>> exp)
    {
        try
        {
            await client.InvokeAsync(exp);
        }
        catch (IpcFaultException ex)
        {
            throw (ex.InnerException ?? ex).InnerException ?? ex.InnerException ?? ex;
        }
    }
}