using System;
using System.Linq.Expressions;
using Hangfire;
using webapi.Interfaces;

namespace webapi.Services;

public class BackgroundJobWrapper : IBackgroundJobWrapper
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public BackgroundJobWrapper(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        // Call the Hangfire extension method here
        return _backgroundJobClient.Enqueue(methodCall);
    }
}
