namespace WebApp.Data;

public interface IWebApiExecuter
{
    Task InvokeDelete(string relativeUrl, CancellationToken cancellationToken = default);
    Task<T?> InvokeGet<T>(string relativeUrl, CancellationToken cancellationToken = default);
    Task<T?> InvokePost<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default);
    Task InvokePut<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default);
}
