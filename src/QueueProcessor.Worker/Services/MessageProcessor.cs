using System.Text.Json;
using QueueProcessor.Worker.Abstractions;

namespace QueueProcessor.Worker.Services;

public sealed class MessageProcessor : IMessageProcessor
{
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(ILogger<MessageProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        //string bodyText = Encoding.UTF8.GetString(message.Body);
        int waitMilliseconds = Random.Shared.Next(500, 1500);
        int tenantId = ExtractTenantId(message);
        if(tenantId == 4)
        {
            waitMilliseconds += 5000;
        }
        await Task.Delay(waitMilliseconds, cancellationToken);
        _logger.LogDebug("Processed message {MessageId} successfully", message.Id);
    }
    private static int ExtractTenantId(QueueMessage message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message.Body);
            var root = doc.RootElement;
            if (TryGetInt(root, "Tenant_ID", out var id) ||
                TryGetInt(root, "tenant_id", out id) ||
                TryGetInt(root, "TenantId", out id) ||
                TryGetInt(root, "tenantId", out id))
            {
                return id;
            }
        }
        catch
        {
            // Not JSON or no tenant id - fall through
        }
        return 0;
    }
    private static bool TryGetInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return false;
        }
        return prop.ValueKind switch
        {
            JsonValueKind.Number => prop.TryGetInt32(out value),
            JsonValueKind.String => int.TryParse(prop.GetString(), out value),
            _ => false,
        };
    }
}


