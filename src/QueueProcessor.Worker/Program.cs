using QueueProcessor.Worker;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("Queue"));

builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();
builder.Services.AddSingleton<IMessageQueueClient, NullQueueClient>();

builder.Services.AddHostedService<Worker>();

//builder.Services.AddLogging(configure => configure.AddSimpleConsole(options =>
//{
//    options.IncludeScopes = false;
//    options.TimestampFormat = "[HH:mm:ss] ";
//}));

var host = builder.Build();
host.Run();
