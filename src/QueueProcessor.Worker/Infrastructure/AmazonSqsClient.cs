using System.Runtime.CompilerServices;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using QueueProcessor.Worker.Abstractions;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Extension;

namespace QueueProcessor.Worker.Infrastructure;

public sealed class AmazonSqsClient : IMessageQueueClient
{
    private readonly ILogger<AmazonSqsClient> _logger;
    private readonly QueueOptions _queue;

    private readonly IAmazonSQS _sqsClient;

    public AmazonSqsClient(ILogger<AmazonSqsClient> logger, IOptions<QueueOptions> options, IAmazonSQS sqsClient)
    {
        _logger = logger;
        _queue = options.Value;
        _sqsClient = sqsClient;
    }


    public async IAsyncEnumerable<QueueMessage> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var receiveReq = new ReceiveMessageRequest
        {
            QueueUrl = $"{_queue.ConnectionString}",
            MaxNumberOfMessages = 3,
            WaitTimeSeconds = 20
        };
        while (!cancellationToken.IsCancellationRequested)
        {
            var receiveResponse = await _sqsClient.ReceiveMessageAsync(receiveReq, cancellationToken);
            if (receiveResponse.Messages.CountSafe() == 0)
            {
                _logger.LogInformation("No Messages Returned. Waiting a little");
#if DEBUG
                Console.WriteLine($"[AmazonSqsClient] No Messages Received...");
#endif
                // No messages, wait before polling again
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                continue;
            }
            _logger.LogInformation("Received {Count} messages ", receiveResponse.Messages.Count);
#if DEBUG
            Console.WriteLine($"[AmazonSqsClient] Received {receiveResponse.Messages.Count} messages");
#endif
            int countBatch = 0;
            foreach (var sqsMessage in receiveResponse.Messages)
            {
                if (countBatch > 0)
                {
                    // Wait a little between messages in the batch
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                }
                _logger.LogInformation("Materializing message {MessageId}", sqsMessage.MessageId);
#if DEBUG
                Console.WriteLine($"[AmazonSqsClient] Materializing message {sqsMessage.MessageId}");
#endif

                var body = System.Text.Encoding.UTF8.GetBytes(sqsMessage.Body ?? string.Empty);

                var headers = new Dictionary<string, string>();

                if (sqsMessage.MessageAttributes is not null)
                {
                    foreach (KeyValuePair<string, MessageAttributeValue> kvp in sqsMessage.MessageAttributes)
                    {
                        headers.Add(kvp.Key, kvp.Value.StringValue ?? string.Empty);
                    }
                }
                yield return new QueueMessage(sqsMessage.MessageId, sqsMessage.ReceiptHandle, body, headers);
                countBatch++;
            }
            // Wait a little between receives
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }


    public async Task<bool> AcceptMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AmazonSQSClient] Accepting message {MessageId}", message.Id);
        await _sqsClient.DeleteMessageAsync(_queue.ConnectionString, message.ReceiptId, cancellationToken);
        return true;
    }
    public async Task<bool> SkipMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[AmazonSQSClient] Skipping message {MessageId}", message.Id);
        await _sqsClient.ChangeMessageVisibilityAsync(_queue.ConnectionString, message.ReceiptId, 30, cancellationToken);
        return true;
    }
}


