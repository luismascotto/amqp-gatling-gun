using ConsoleWebAppDemo.Components;
using Microsoft.Extensions.Hosting;
using RazorConsole.Core;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<LoginForm>();
IHost host = hostBuilder.Build();
await host.RunAsync();