using System.Text.Json;

using WebApp.Data;

public class WebApiException : Exception
{
    public ErrorResponse? ErrorResponse { get; }

    public WebApiException(string errorJson)
        : base("API Error")
    {
        ErrorResponse = ParseError(errorJson);
    }

    public WebApiException()
        : base("API Error")
    {
    }

    public WebApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private static ErrorResponse ParseError(string errorJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(errorJson) ?? new ErrorResponse
            {
                Errors = new Dictionary<string, List<string>>
                {
                    { "General", new List<string> { "Unable to parse API error." } }
                }
            };
        }
        catch (JsonException)
        {
            // Fallback to a basic error structure if deserialization fails
            return new ErrorResponse
            {
                Errors = new Dictionary<string, List<string>>
                {
                    { "General", new List<string> { errorJson } }
                }
            };
        }
    }
}
