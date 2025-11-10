using Microsoft.Extensions.Options;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Models;

namespace QueueProcessor.Worker.Infrastructure;

public sealed class TenantAwareBuffer
{
    private readonly object _sync = new();
    private readonly Dictionary<int, Queue<BufferedQueueMessage>> _tenantQueues = new();
    private readonly List<int> _activeTenants = new();
    private int _roundRobinIndex;
    private int _totalCount;

    private readonly int _maxTotal;
    private readonly int _maxPerTenant;

    private readonly SemaphoreSlim _itemsAvailable = new(0);
    private readonly SemaphoreSlim _spaceReleased = new(0);
    private readonly SemaphoreSlim _eligibilityChanged = new(0);

    private readonly ILogger<TenantAwareBuffer> _logger;

    public TenantAwareBuffer(IOptions<BufferingOptions> options, ILogger<TenantAwareBuffer> logger)
    {
        _logger = logger;
        var opts = options.Value;
        _maxTotal = Math.Max(1, opts.MaxBufferedMessages);
        _maxPerTenant = Math.Max(1, opts.MaxBufferedMessagesPerTenant);
    }

    public async Task EnqueueAsync(BufferedQueueMessage message, CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (_sync)
            {
                if (_totalCount < _maxTotal)
                {
                    if (!_tenantQueues.TryGetValue(message.TenantId, out var queue))
                    {
                        queue = new Queue<BufferedQueueMessage>();
                        _tenantQueues[message.TenantId] = queue;
                    }

                    if (queue.Count < _maxPerTenant)
                    {
                        bool wasEmpty = queue.Count == 0;
                        queue.Enqueue(message);
                        _totalCount++;
                        if (wasEmpty)
                        {
                            _activeTenants.Add(message.TenantId);
                        }
                        _itemsAvailable.Release();

                        Console.WriteLine($"[TenantAwareBuffer] Enqueued message for tenant {message.TenantId} (total: {_totalCount}/{_maxTotal}, tenant {_tenantQueues[message.TenantId].Count}/{_maxPerTenant})");

                        return;
                    }
                }
            }
            Console.WriteLine($"[TenantAwareBuffer] Buffer full (total: {_totalCount}/{_maxTotal}, tenant {message.TenantId}: {_tenantQueues[message.TenantId].Count}/{_maxPerTenant}), waiting for space...");

            await _spaceReleased.WaitAsync(cancellationToken);
        }
    }

    public async Task<BufferedQueueMessage> DequeueNextEligibleAsync(Func<int, bool> isTenantEligible, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (TryDequeueNextEligible(isTenantEligible, out var message))
            {
                return message!;
            }

            // Wait until a new item arrives or eligibility changes (e.g., in-flight reduced)
            var waitItem = _itemsAvailable.WaitAsync(cancellationToken);
            var waitEligibility = _eligibilityChanged.WaitAsync(cancellationToken);
            await Task.WhenAny(waitItem, waitEligibility);
        }
    }

    public void NotifyEligibilityChanged()
    {
        _eligibilityChanged.Release();
    }

    private bool TryDequeueNextEligible(Func<int, bool> isTenantEligible, out BufferedQueueMessage? message)
    {
        lock (_sync)
        {
            message = null;
            if (_activeTenants.Count == 0)
            {
                return false;
            }

            int scanned = 0;
            int count = _activeTenants.Count;
            while (scanned < count)
            {
                if (_activeTenants.Count == 0)
                {
                    return false;
                }
                if (_roundRobinIndex >= _activeTenants.Count)
                {
                    _roundRobinIndex = 0;
                }
                int tenantId = _activeTenants[_roundRobinIndex];
                if (_tenantQueues.TryGetValue(tenantId, out var queue) && queue.Count > 0 && isTenantEligible(tenantId))
                {
                    var dequeued = queue.Dequeue();
                    _totalCount--;
                    if (queue.Count == 0)
                    {
                        _activeTenants.RemoveAt(_roundRobinIndex);
                        if (_roundRobinIndex >= _activeTenants.Count)
                        {
                            _roundRobinIndex = 0;
                        }
                    }
                    else
                    {
                        _roundRobinIndex = (_roundRobinIndex + 1) % _activeTenants.Count;
                    }
                    message = dequeued;
                    _spaceReleased.Release();
                    return true;
                }
                else
                {
                    _roundRobinIndex = (_roundRobinIndex + 1) % _activeTenants.Count;
                    scanned++;
                }
            }

            return false;
        }
    }
}


