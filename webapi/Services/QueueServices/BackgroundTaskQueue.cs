// (*^▽^*)════════════════════════════════════════════(^▽^*)
// ❖     Create a Queue Manager / Processor     ❖
// (^▽^*)════════════════════════════════════════════(*^▽^*)
using System.Threading.Channels;

namespace webapi.Services.QueueServices;

public interface ITaskQueue
{
    public void Enqueue(QueueTaskModel task);
    public Task<QueueTaskModel> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : ITaskQueue
{
    private readonly Channel<QueueTaskModel> _queue;

    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<QueueTaskModel>();
    }

    public void Enqueue(QueueTaskModel task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _queue.Writer.TryWrite(task);
    }

    public async Task<QueueTaskModel> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
