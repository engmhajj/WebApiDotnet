namespace webapi.Services.QueueServices
{
    public class QueueTaskModel
    {
        public string TaskName { get; set; }
        public Func<CancellationToken, Task> ExecuteAsync { get; set; }
    }
}
