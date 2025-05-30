using System.Net.Http.Headers;
using Newtonsoft.Json;
using WebApp.Data;

public class WebApiExecuter : IWebApiExecuter
{
    private const int MaxApiConcurrency = 5;

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
        await _apiCallLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("GET: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName).ConfigureAwait(false);
            var response = await client.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            await HandlePotentialError(response).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task<T?> InvokePost<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("POST: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName).ConfigureAwait(false);
            var response = await client.PostAsJsonAsync(relativeUrl, obj, cancellationToken).ConfigureAwait(false);
            await HandlePotentialError(response).ConfigureAwait(false);
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task InvokePut<T>(string relativeUrl, T obj, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("PUT: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName).ConfigureAwait(false);
            var response = await client.PutAsJsonAsync(relativeUrl, obj, cancellationToken).ConfigureAwait(false);
            await HandlePotentialError(response).ConfigureAwait(false);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    public async Task InvokeDelete(string relativeUrl, CancellationToken cancellationToken = default)
    {
        await _apiCallLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("DELETE: {Url}", relativeUrl);
            var client = await PrepareClientAsync(Constants.ShirtsApiName).ConfigureAwait(false);
            var response = await client.DeleteAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
            await HandlePotentialError(response).ConfigureAwait(false);
        }
        finally
        {
            _apiCallLock.Release();
        }
    }

    private async Task<HttpClient> PrepareClientAsync(string clientName)
    {
        var client = _httpClientFactory.CreateClient(clientName);
        await AddJwtToHeader(client).ConfigureAwait(false);
        return client;
    }

    private async Task HandlePotentialError(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            _logger.LogWarning("API error: {StatusCode} - {Content}", response.StatusCode, errorJson);
            throw new WebApiException(errorJson);
        }
    }

    private async Task AddJwtToHeader(HttpClient client)
    {
        var token = await GetOrRefreshToken().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            _logger.LogError("JWT token is null or empty.");
            throw new WebApiException("JWT token is null or empty.");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
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
            _logger.LogError(ex, "Failed to deserialize JWT.");
            return null;
        }
    }

    private bool IsTokenValid(JwtToken? token)
    {
        return token != null && token.AccessTokenExpiresAt > DateTime.UtcNow;
    }

    private async Task<JwtToken?> GetOrRefreshToken()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return null;

        string? tokenJson = session.GetString(Constants.SessionTokenKey);
        JwtToken? token = DeserializeToken(tokenJson);

        if (IsTokenValid(token))
            return token;

        await _authLock.WaitAsync().ConfigureAwait(false);
        try
        {
            tokenJson = session.GetString(Constants.SessionTokenKey);
            token = DeserializeToken(tokenJson);

            if (IsTokenValid(token))
                return token;

            // Use refresh token if valid
            if (token != null
                && !string.IsNullOrEmpty(token.RefreshToken)
                && token.RefreshTokenExpiresAt > DateTime.UtcNow)
            {
                var refreshClient = _httpClientFactory.CreateClient(Constants.AuthorityApiName);

                var refreshPayload = new
                {
                    clientId = _configuration["ClientId"],
                    refreshToken = token.RefreshToken
                };

                var refreshResponse = await refreshClient.PostAsJsonAsync("auth/refresh", refreshPayload).ConfigureAwait(false);

                if (!refreshResponse.IsSuccessStatusCode)
                {
                    var error = await refreshResponse.Content.ReadAsStringAsync();
                    throw new WebApiException($"Refresh failed: {refreshResponse.StatusCode} - {error}");
                }

                var refreshResponseText = await refreshResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                var refreshed = JsonConvert.DeserializeObject<JwtToken>(refreshResponseText);

                if (refreshed == null)
                    throw new WebApiException("Failed to parse refresh response");

                refreshed.IssuedAt = DateTime.UtcNow;
                session.SetString(Constants.SessionTokenKey, JsonConvert.SerializeObject(refreshed));
                return refreshed;
            }

            // Refresh token expired or not present — full client credentials flow
            var clientId = _configuration["ClientId"];
            var secret = _configuration["Secret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("Missing client credentials.");

            var authClient = _httpClientFactory.CreateClient(Constants.AuthorityApiName);

            var authResponse = await authClient.PostAsJsonAsync("auth", new AppCredential
            {
                ClientId = clientId,
                Secret = secret
            }).ConfigureAwait(false);

            if (!authResponse.IsSuccessStatusCode)
            {
                var error = await authResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new WebApiException($"Authentication failed: {authResponse.StatusCode} - {error}");
            }

            var authResponseText = await authResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            var newToken = JsonConvert.DeserializeObject<JwtToken>(authResponseText);

            if (newToken == null)
                throw new WebApiException("Failed to deserialize new token");

            newToken.IssuedAt = DateTime.UtcNow;
            session.SetString(Constants.SessionTokenKey, JsonConvert.SerializeObject(newToken));

            return newToken;
        }
        finally
        {
            _authLock.Release();
        }
    }
}
