using QueueProcessor.Worker;
using Microsoft.Extensions.Options;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("Queue"));

builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();
builder.Services.AddSingleton<IMessageQueueClient, NullQueueClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
