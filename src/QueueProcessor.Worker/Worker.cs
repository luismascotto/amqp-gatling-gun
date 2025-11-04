namespace QueueProcessor.Worker;

using QueueProcessor.Worker.Abstractions;

public class Worker : BackgroundService
{
	private readonly ILogger<Worker> _logger;
	private readonly IMessageQueueClient _queueClient;
	private readonly IMessageProcessor _messageProcessor;

	public Worker(ILogger<Worker> logger, IMessageQueueClient queueClient, IMessageProcessor messageProcessor)
	{
		_logger = logger;
		_queueClient = queueClient;
		_messageProcessor = messageProcessor;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Message worker starting...");
		await foreach (var message in _queueClient.ReadMessagesAsync(stoppingToken))
		{
			try
			{
				await _messageProcessor.ProcessAsync(message, stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed processing message {MessageId}", message.Id);
			}
		}
		_logger.LogInformation("Message worker stopped.");
	}
}
