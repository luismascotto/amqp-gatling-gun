using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Infrastructure;
using QueueProcessor.Worker.Services;
using QueueProcessor.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection("Aws"));
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection("Queue"));
builder.Services.Configure<BufferingOptions>(builder.Configuration.GetSection("Buffering"));

var user = builder.Configuration.GetSection("Aws").GetValue<string>("AccessKeyId") ?? "";
var pass = builder.Configuration.GetSection("Aws").GetValue<string>("SecretAccessKey") ?? "";
var region = builder.Configuration.GetSection("Aws").GetValue<string>("Region") ?? "";

var sqsConfig = new AmazonSQSConfig
{
    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
};

builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(new BasicAWSCredentials(user, pass), sqsConfig));

builder.Services.AddSingleton<IMessageProcessor, MessageProcessor>();
//builder.Services.AddSingleton<IMessageQueueClient, NullQueueClient>();
builder.Services.AddSingleton<IMessageQueueClient, AmazonSqsClient>();

builder.Services.AddSingleton<PartitionedBuffer>();
builder.Services.AddHostedService<BufferPopulator>();
builder.Services.AddHostedService<PartitionedDispatcher>();

// builder.Services.AddHostedService<Worker>();

//builder.Services.AddLogging(configure => configure.AddSimpleConsole(options =>
//{
//    options.IncludeScopes = false;
//    options.TimestampFormat = "[HH:mm:ss] ";
//}));

var host = builder.Build();
host.Run();
