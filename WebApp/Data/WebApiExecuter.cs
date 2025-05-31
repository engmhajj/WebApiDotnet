using System.Net.Http.Headers;
using Newtonsoft.Json;
using WebApp.Data;
using WebApp.Models;

public class WebApiExecuter : IWebApiExecuter
{
    private const int MaxApiConcurrency = 5;
    private const int TokenExpiryBufferSeconds = 30;
    private const int MaxTokenRefreshRetries = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebApiExecuter> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly SemaphoreSlim _apiCallLock = new(MaxApiConcurrency, MaxApiConcurrency);
    private static readonly SemaphoreSlim _authLock = new(1, 1);

    public WebApiExecuter(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebApiExecuter> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<T?> InvokeGet<T>(string relativeUrl, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("GET: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName);
            var response = await client.GetAsync(relativeUrl, cancellationToken);
            await EnsureSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task<T?> InvokePost<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("POST: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName);
            var response = await client.PostAsJsonAsync(relativeUrl, obj, cancellationToken);
            await EnsureSuccessStatusCode(response);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task InvokePut<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("PUT: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName);
            var response = await client.PutAsJsonAsync(relativeUrl, obj, cancellationToken);
            await EnsureSuccessStatusCode(response);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task InvokeDelete(string relativeUrl, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("DELETE: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName);
            var response = await client.DeleteAsync(relativeUrl, cancellationToken);
            await EnsureSuccessStatusCode(response);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    private async Task<HttpClient> PrepareClientAsync(string clientName)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        var token = await GetValidTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    private async Task EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("API error {StatusCode}: {Content}", response.StatusCode, content);
            throw new WebApiException(content);
        }
    }

    private JwtToken? DeserializeToken(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<JwtToken>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize JWT token.");
            return null;
        }
    }

    private bool IsTokenValid(JwtToken? token)
    {
        return token != null && token.AccessTokenExpiresAt > DateTime.UtcNow.AddSeconds(TokenExpiryBufferSeconds);
    }

    private async Task<JwtToken> GetValidTokenAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("Session is not available.");

        var tokenJson = session.GetString(Constants.SessionTokenKey);
        var token = DeserializeToken(tokenJson);

        if (IsTokenValid(token))
            return token!;

        await _authLock.WaitAsync();
        try
        {
            // Double-check inside lock
            tokenJson = session.GetString(Constants.SessionTokenKey);
            token = DeserializeToken(tokenJson);
            if (IsTokenValid(token))
                return token!;

            int retries = session.GetInt32("TokenRefreshRetryCount") ?? 0;
            if (retries >= MaxTokenRefreshRetries)
            {
                session.Remove("TokenRefreshRetryCount");
                throw new WebApiException("Exceeded maximum token refresh retries.");
            }

            session.SetInt32("TokenRefreshRetryCount", retries + 1);

            if (token?.RefreshToken != null && token.RefreshTokenExpiresAt > DateTime.UtcNow)
            {
                var refreshedToken = await TryRefreshTokenAsync(token.RefreshToken);
                if (refreshedToken != null)
                {
                    session.Remove("TokenRefreshRetryCount");
                    return refreshedToken;
                }
            }

            // Fallback to client credentials auth
            var newToken = await AuthenticateClientCredentialsAsync();
            session.Remove("TokenRefreshRetryCount");
            return newToken;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task<JwtToken?> TryRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var refreshClient = _httpClientFactory.CreateClient(Constants.AuthorityApiName);
            var refreshPayload = new
            {
                clientId = _configuration["ClientId"],
                refreshToken
            };

            var response = await refreshClient.PostAsJsonAsync("auth/refresh", refreshPayload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refresh token request failed with status {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);

            if (tokenResponse == null)
                return null;

            var now = DateTime.UtcNow;
            var newToken = new JwtToken
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                IssuedAt = now,
                AccessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresInSeconds),
                RefreshTokenExpiresAt = now.AddSeconds(tokenResponse.RefreshTokenExpiresInSeconds)
            };

            _httpContextAccessor.HttpContext?.Session.SetString(Constants.SessionTokenKey, JsonConvert.SerializeObject(newToken));
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during refresh token request.");
            return null;
        }
    }

    private async Task<JwtToken> AuthenticateClientCredentialsAsync()
    {
        var clientId = _configuration["ClientId"];
        var secret = _configuration["Secret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("ClientId or Secret is missing in configuration.");

        var authClient = _httpClientFactory.CreateClient(Constants.AuthorityApiName);
        var payload = new AppCredential { ClientId = clientId, Secret = secret };

        var response = await authClient.PostAsJsonAsync("auth", payload).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Authentication failed with status {StatusCode}: {ResponseContent}", response.StatusCode, responseContent);
            throw new WebApiException($"Authentication failed: {response.StatusCode} - {responseContent}");
        }

        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
        if (tokenResponse == null)
        {
            _logger.LogError("Failed to parse authentication response: {ResponseContent}", responseContent);
            throw new WebApiException("Failed to parse authentication response.");
        }

        var now = DateTime.UtcNow;
        var newToken = new JwtToken
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            IssuedAt = now,
            AccessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresInSeconds),
            RefreshTokenExpiresAt = now.AddSeconds(tokenResponse.RefreshTokenExpiresInSeconds)
        };

        SaveTokenToSession(newToken);

        return newToken;
    }

    private void SaveTokenToSession(JwtToken token)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session != null)
        {
            session.SetString(Constants.SessionTokenKey, JsonConvert.SerializeObject(token));
        }
    }

}
