using Microsoft.Extensions.Options;
using QueueProcessor.Worker.Configuration;
using QueueProcessor.Worker.Models;

namespace QueueProcessor.Worker.Infrastructure;

public class PartitionedBuffer
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
    private readonly BufferingOptions _options;
    private readonly ILogger _logger;


    public PartitionedBuffer(IOptions<BufferingOptions> options, ILogger logger)
    {
        _options = options.Value;
        _logger = logger;

        _maxTotal = Math.Max(1, _options.MaxBufferedMessages);
        _maxPerTenant = Math.Max(1, _options.MaxBufferedMessagesPerTenant);
    }

    public async Task<EnqueueStatus> EnqueueAsync(BufferedQueueMessage message, CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (_sync)
            {
                if (_totalCount < _maxTotal)
                {
                    if (!_tenantQueues.TryGetValue(message.TenantId, out var tenantQueue))
                    {
                        tenantQueue = new Queue<BufferedQueueMessage>();
                        _tenantQueues[message.TenantId] = tenantQueue;
                    }

                    if (tenantQueue.Count < _maxPerTenant)
                    {
                        bool wasEmpty = tenantQueue.Count == 0;
                        tenantQueue.Enqueue(message);
                        _totalCount++;
                        if (wasEmpty)
                        {
                            _activeTenants.Add(message.TenantId);
                        }
                        _itemsAvailable.Release();

#if DEBUG
                        Console.WriteLine($"[PartitionedBuffer] Enqueued message for tenant {message.TenantId} ({_tenantQueues[message.TenantId].Count}/{_maxPerTenant}) (Total: {_totalCount}/{_maxTotal}");
#endif
                        return EnqueueStatus.Enqueued;
                    }

                    switch (_options.OnTenantCapacity)
                    {
                        case OnCapacityBehavior.Accept:
                            return EnqueueStatus.NotEnqueuedAccept;

                        case OnCapacityBehavior.Skip:
                            return EnqueueStatus.NotEnqueuedSkip;

                        default:
                            // Fallback to waiting if an unknown enum value is supplied
                            break;
                    }
                }
#if DEBUG
                Console.WriteLine($"[PartitionedBuffer] Buffer total: {_totalCount}/{_maxTotal}, Tenants full: {string.Join(", ", _activeTenants.Where(t => _tenantQueues[t].Count >= _maxPerTenant))}, waiting for space...");
#endif
            }

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



