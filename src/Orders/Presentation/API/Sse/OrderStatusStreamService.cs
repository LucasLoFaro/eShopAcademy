using System.Threading.Channels;

namespace API.Sse;

public sealed class OrderStatusStreamService
{
    // One channel per order ID, each holding serialized JSON payloads
    private readonly Dictionary<Guid, List<Channel<string>>> _subscriptions = new();
    private readonly Lock _lock = new();

    public ChannelReader<string> Subscribe(Guid orderId)
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(32)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(orderId, out var list))
            {
                list = [];
                _subscriptions[orderId] = list;
            }

            list.Add(channel);
        }

        return channel.Reader;
    }

    public void Unsubscribe(Guid orderId, ChannelReader<string> reader)
    {
        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(orderId, out var list))
                return;

            list.RemoveAll(ch => ch.Reader == reader);

            if (list.Count == 0)
                _subscriptions.Remove(orderId);
        }
    }

    public void Publish(Guid orderId, string json)
    {
        List<Channel<string>>? snapshot;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(orderId, out var list))
                return;

            snapshot = [.. list];
        }

        foreach (var channel in snapshot)
        {
            channel.Writer.TryWrite(json);
        }
    }
}
