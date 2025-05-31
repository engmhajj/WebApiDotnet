using Newtonsoft.Json;

namespace WebApp.Data;

public class AppCredential
{
    [JsonProperty("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonProperty("secret")]
    public string Secret { get; set; } = string.Empty;
}
