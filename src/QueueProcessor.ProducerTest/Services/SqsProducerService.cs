using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueueProcessor.ProducerTest.Configuration;
using System.Text.Json;

namespace QueueProcessor.ProducerTest.Services;

public sealed class SqsProducerService : BackgroundService
{
    private readonly ILogger<SqsProducerService> _logger;
    private readonly AwsOptions _aws;
    private readonly QueueOptions _queue;
    private readonly ProducerOptions _producer;

    public SqsProducerService(
        ILogger<SqsProducerService> logger,
        IOptions<AwsOptions> aws,
        IOptions<QueueOptions> queue,
        IOptions<ProducerOptions> producer)
    {
        _logger = logger;
        _aws = aws.Value;
        _queue = queue.Value;
        _producer = producer.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Producer starting...");

        if (string.IsNullOrWhiteSpace(_queue.QueueUrl) && string.IsNullOrWhiteSpace(_queue.QueueName))
        {
            _logger.LogError("QueueUrl or QueueName must be provided in configuration.");
            return;
        }

        using var client = CreateClient();
        var queueUrl = await ResolveQueueUrlAsync(client, stoppingToken);
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            _logger.LogError("Could not resolve queue URL. Check configuration.");
            return;
        }

        var delayMin = Math.Max(0, _producer.DelayMsMin);
        var delayMax = Math.Max(delayMin, _producer.DelayMsMax);

        int sent = 0;
        int? total = _producer.Messages is > 0 ? _producer.Messages : null;

        while (!stoppingToken.IsCancellationRequested && (total is null || sent < total.Value))
        {
            var tenantId = Random.Shared.Next(Math.Min(_producer.TenantMin, _producer.TenantMax), Math.Max(_producer.TenantMin, _producer.TenantMax) + 1);
            var payload = new
            {
                Tenant_ID = tenantId,
                message = $"Random message at {DateTimeOffset.UtcNow:O}",
                uuid = Guid.NewGuid().ToString("n")
            };
            string body = JsonSerializer.Serialize(payload);

            var request = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = body,
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["x-provider"] = new MessageAttributeValue { DataType = "String", StringValue = "sqs" },
                    ["tenant-id"] = new MessageAttributeValue { DataType = "Number", StringValue = tenantId.ToString() }
                }
            };

            try
            {
                var response = await client.SendMessageAsync(request, stoppingToken);
                if (response.HttpStatusCode is System.Net.HttpStatusCode.OK)
                {
                    sent++;
                    _logger.LogInformation("Sent message {Count} (MessageId={MessageId})", sent, response.MessageId);
                }
                else
                {
                    _logger.LogWarning("Non-OK response sending message: {Status}", response.HttpStatusCode);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpErrorResponseException aex)
            {
                _logger.LogError(aex, "AWS service error sending message: {Message}", aex.Message);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message");
            }

            try
            {
                var delay = Random.Shared.Next(delayMin, delayMax + 1);
                await Task.Delay(delay, stoppingToken);
            }
            catch
            {
            }
        }

        _logger.LogInformation("SQS Producer stopped. Total sent: {Count}", sent);
    }

    private IAmazonSQS CreateClient()
    {
        AmazonSQSConfig config = new();

        if (!string.IsNullOrWhiteSpace(_aws.ServiceURL))
        {
            config.ServiceURL = _aws.ServiceURL;
        }

        if (!string.IsNullOrWhiteSpace(_aws.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(_aws.Region);
        }

        if (!_aws.UseDefaultCredentials && !string.IsNullOrWhiteSpace(_aws.AccessKeyId) && !string.IsNullOrWhiteSpace(_aws.SecretAccessKey))
        {
            AWSCredentials creds = string.IsNullOrWhiteSpace(_aws.SessionToken)
                ? new BasicAWSCredentials(_aws.AccessKeyId, _aws.SecretAccessKey)
                : new SessionAWSCredentials(_aws.AccessKeyId, _aws.SecretAccessKey, _aws.SessionToken);

            return new AmazonSQSClient(creds, config);
        }

        return new AmazonSQSClient(config);
    }

    private async Task<string?> ResolveQueueUrlAsync(IAmazonSQS client, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_queue.QueueUrl))
        {
            return _queue.QueueUrl;
        }

        if (!string.IsNullOrWhiteSpace(_queue.QueueName))
        {
            try
            {
                var url = await client.GetQueueUrlAsync(new GetQueueUrlRequest
                {
                    QueueName = _queue.QueueName
                }, ct);
                return url.QueueUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve QueueUrl from QueueName={QueueName}", _queue.QueueName);
                return null;
            }
        }

        return null;
    }
}


