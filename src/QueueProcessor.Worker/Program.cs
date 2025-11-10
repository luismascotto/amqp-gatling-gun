using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Services;
using QueueProcessor.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("Queue"));
builder.Services.Configure<BufferingOptions>(builder.Configuration.GetSection("Buffering"));

builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();
builder.Services.AddSingleton<IMessageQueueClient, NullQueueClient>();

builder.Services.AddSingleton<TenantAwareBuffer>();
builder.Services.AddHostedService<TenantBufferPopulator>();
builder.Services.AddHostedService<TenantAwareDispatcher>();

// builder.Services.AddHostedService<Worker>();

//builder.Services.AddLogging(configure => configure.AddSimpleConsole(options =>
//{
//    options.IncludeScopes = false;
//    options.TimestampFormat = "[HH:mm:ss] ";
//}));

var host = builder.Build();
host.Run();
