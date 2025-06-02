namespace webapi.Exceptions;

public class WebApiException : Exception
{
    public WebApiException() { }

    public WebApiException(string message)
        : base(message) { }

    public WebApiException(string message, Exception inner)
        : base(message, inner) { }
}
