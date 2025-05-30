namespace webapi.Token
{
    public class RefreshRequest
    {
        public string ClientId { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
