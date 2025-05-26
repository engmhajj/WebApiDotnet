namespace webapi.Authority;

public static class AppRepository
{
    private static readonly List<Application> _applications = new()
    {
        new Application
        {
            ApplicationId = 1,
            ApplicationName = "MVCWebApp",
            ClientId = "53D3C1E6-5487-8C6E-A8E4BD59940E",
            Secret = "0673FC70-0514-4011-CCA3-DF9BC03201BC",
            Scopes = "read,write,delete"
        }
    };

    /// <summary>
    /// Returns an Application object matching the provided ClientId.
    /// </summary>
    /// <param name="clientId">The client ID to match.</param>
    /// <returns>An Application instance if found; otherwise, null.</returns>
    public static Application? GetApplicationByClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        return _applications.FirstOrDefault(app =>
            app.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Optionally expose all applications for diagnostics.
    /// </summary>
    public static IReadOnlyList<Application> GetAllApplications() => _applications.AsReadOnly();
}
