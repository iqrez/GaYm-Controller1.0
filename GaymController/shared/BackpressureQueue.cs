using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace GaymController.Shared;

/// <summary>
/// Simple bounded channel that applies backpressure to writers when full.
/// </summary>
public class BackpressureQueue<T>
{
    private readonly Channel<T> _channel;

    public BackpressureQueue(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<T>(options);
    }

    /// <summary>
    /// Enqueues an item. If the queue is full this call awaits until space is available.
    /// </summary>
    public ValueTask EnqueueAsync(T item, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(item, ct);

    /// <summary>
    /// Dequeues the next available item, awaiting if the queue is empty.
    /// </summary>
    public ValueTask<T> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);

    public void Complete() => _channel.Writer.Complete();
}
