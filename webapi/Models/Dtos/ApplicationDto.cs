namespace webapi.Models.Dtos;

public class ApplicationDto
{
    public int ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;

    // from Application -> ApplicationDto
    public static ApplicationDto ToDto(Application app)
    {
        return new ApplicationDto
        {
            ApplicationId = app.ApplicationId,
            ApplicationName = app.ApplicationName!,
            ClientId = app.ClientId!,
            Scopes = app.Scopes!,
        };
    }
}
