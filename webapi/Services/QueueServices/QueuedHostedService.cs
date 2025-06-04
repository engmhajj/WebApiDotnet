// (*^▽^*)═══════════════════════════════════════(^▽^*)
// ❖     Implement a Background Worker     ❖
// (^▽^*)═══════════════════════════════════════(*^▽^*)
namespace webapi.Services.QueueServices
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly ITaskQueue _taskQueue;

        public QueuedHostedService(ITaskQueue taskQueue, ILogger<QueuedHostedService> logger)
        {
            _taskQueue = taskQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var task = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    _logger.LogInformation("Executing task: {TaskName}", task.TaskName);
                    await task.ExecuteAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task {TaskName}", task.TaskName);
                }
            }
        }
    }
}
