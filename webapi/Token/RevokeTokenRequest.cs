namespace webapi.Token
{
    public class RevokeTokenRequest
    {
        public string RefreshToken { get; set; } = default!;
    }
}
