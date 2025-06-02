using System;
using System.Linq.Expressions;
using webapi.Interfaces;

namespace webapi.Services;

public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobWrapper _jobWrapper;

    public BackgroundJobService(IBackgroundJobWrapper jobWrapper)
    {
        _jobWrapper = jobWrapper;
    }

    public void EnqueueJob()
    {
        // Enqueue a background job using the wrapper
        _jobWrapper.Enqueue(() => Console.WriteLine("Background job executed!"));
    }
}
