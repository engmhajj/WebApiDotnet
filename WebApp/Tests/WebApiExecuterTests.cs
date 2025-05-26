using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using WebApp.Data;
using Xunit;

public class WebApiExecuterTests
{
    private const string SessionTokenKey = "access_token";

    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger<WebApiExecuter>> _logger = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly MockHttpSession _session = new();
    private readonly WebApiExecuter _executer;

    public WebApiExecuterTests()
    {
        // Setup HttpContext and Session mocks
        var httpContext = new DefaultHttpContext();
        httpContext.Session = _session;
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        // Setup configuration for ClientId and Secret
        var clientIdSection = new Mock<IConfigurationSection>();
        clientIdSection.Setup(s => s.Value).Returns("fakeClientId");
        _configuration.Setup(c => c.GetSection("ClientId")).Returns(clientIdSection.Object);

        var secretSection = new Mock<IConfigurationSection>();
        secretSection.Setup(s => s.Value).Returns("fakeSecret");
        _configuration.Setup(c => c.GetSection("Secret")).Returns(secretSection.Object);

        _executer = new WebApiExecuter(
            _httpClientFactory.Object,
            _configuration.Object,
            _logger.Object,
            _httpContextAccessor.Object);
    }

    #region Helpers

    private HttpClient CreateMockHttpClient(HttpResponseMessage responseMessage)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(responseMessage)
           .Verifiable();

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost/")
        };
    }

    private void SetupHttpClientFactory(string clientName, HttpClient client)
    {
        _httpClientFactory.Setup(f => f.CreateClient(clientName)).Returns(client);
    }

    private string CreateValidTokenJson(DateTime? expiresAt = null)
    {
        var token = new JwtToken
        {
            AccessToken = "valid_token",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1)
        };
        return JsonConvert.SerializeObject(token);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task AddJwtToHeader_WithValidTokenInSession_SetsAuthorizationHeader()
    {
        // Arrange
        var tokenJson = CreateValidTokenJson();
        _session.SetString(SessionTokenKey, tokenJson);

        var client = new HttpClient();

        // Act
        await InvokeAddJwtToHeader(client);

        // Assert
        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal("valid_token", client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task AddJwtToHeader_WithExpiredToken_AuthenticatesAndSetsToken()
    {
        // Arrange expired token in session
        var expiredTokenJson = CreateValidTokenJson(DateTime.UtcNow.AddMinutes(-5));
        _session.SetString(SessionTokenKey, expiredTokenJson);

        // Setup Authority API client to respond with new token
        var newToken = new JwtToken
        {
            AccessToken = "new_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(newToken))
        };

        var authClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("AuthorityApi", authClient);

        var client = new HttpClient();

        // Act
        await InvokeAddJwtToHeader(client);

        // Assert
        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal("new_token", client.DefaultRequestHeaders.Authorization.Parameter);

        // Also verify new token saved to session
        var savedTokenJson = _session.GetString(SessionTokenKey);
        Assert.Contains("new_token", savedTokenJson);
    }

    [Fact]
    public async Task AddJwtToHeader_WithoutTokenInSession_AuthenticatesSuccessfully()
    {
        // Arrange no token in session

        var newToken = new JwtToken
        {
            AccessToken = "fresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(newToken))
        };

        var authClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("AuthorityApi", authClient);

        var client = new HttpClient();

        // Act
        await InvokeAddJwtToHeader(client);

        // Assert
        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal("fresh_token", client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task AddJwtToHeader_AuthenticationFails_ThrowsWebApiException()
    {
        // Arrange no token in session

        var responseMessage = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };

        var authClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("AuthorityApi", authClient);

        var client = new HttpClient();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebApiException>(() => InvokeAddJwtToHeader(client));
        Assert.Contains("Authentication failed", ex.Message);
    }

    #endregion

    #region InvokeGet Tests

    [Fact]
    public async Task InvokeGet_Success_ReturnsDeserializedObject()
    {
        // Arrange
        var expectedObject = new TestData { Id = 123, Name = "Test" };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(expectedObject))
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Setup AddJwtToHeader to not throw
        var client = new HttpClient();
        SetupHttpClientFactory("ShirtsApi", client);

        // Act
        var result = await _executer.InvokeGet<TestData>("dummy");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedObject.Id, result.Id);
        Assert.Equal(expectedObject.Name, result.Name);
    }

    [Fact]
    public async Task InvokeGet_ApiReturnsError_ThrowsWebApiException()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebApiException>(() => _executer.InvokeGet<TestData>("dummy"));
        Assert.Contains("Bad Request", ex.Message);
    }

    #endregion

    #region InvokePost Tests

    [Fact]
    public async Task InvokePost_Success_ReturnsDeserializedObject()
    {
        // Arrange
        var postData = new TestData { Id = 1, Name = "Post" };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(postData))
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Act
        var result = await _executer.InvokePost("dummy", postData);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(postData.Id, result.Id);
        Assert.Equal(postData.Name, result.Name);
    }

    [Fact]
    public async Task InvokePost_ApiReturnsError_ThrowsWebApiException()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        var postData = new TestData { Id = 1, Name = "Post" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebApiException>(() => _executer.InvokePost("dummy", postData));
        Assert.Contains("Server error", ex.Message);
    }

    #endregion

    #region InvokePut Tests

    [Fact]
    public async Task InvokePut_Success_CompletesWithoutException()
    {
        // Arrange
        var putData = new TestData { Id = 5, Name = "Put" };
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);

        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Act
        await _executer.InvokePut("dummy", putData);

        // Assert: No exception thrown means pass
    }

    [Fact]
    public async Task InvokePut_ApiReturnsError_ThrowsWebApiException()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid data")
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        var putData = new TestData { Id = 5, Name = "Put" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebApiException>(() => _executer.InvokePut("dummy", putData));
        Assert.Contains("Invalid data", ex.Message);
    }

    #endregion

    #region InvokeDelete Tests

    [Fact]
    public async Task InvokeDelete_Success_CompletesWithoutException()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Act
        await _executer.InvokeDelete("dummy");

        // Assert: No exception thrown means pass
    }

    [Fact]
    public async Task InvokeDelete_ApiReturnsError_ThrowsWebApiException()
    {
        // Arrange
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not Found")
        };
        var apiClient = CreateMockHttpClient(responseMessage);
        SetupHttpClientFactory("ShirtsApi", apiClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WebApiException>(() => _executer.InvokeDelete("dummy"));
        Assert.Contains("Not Found", ex.Message);
    }

    #endregion

    #region Private helper to invoke internal AddJwtToHeader

    private async Task InvokeAddJwtToHeader(HttpClient client)
    {
        // Use reflection because AddJwtToHeader is private

        var method = typeof(WebApiExecuter).GetMethod("AddJwtToHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException("AddJwtToHeader method not found");
        }

        var task = (Task)method.Invoke(_executer, new object[] { client });
        await task.ConfigureAwait(false);
    }

    #endregion

    #region Test DTO classes

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class JwtToken
    {
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    #endregion
}

/// <summary>
/// Simple mock session implementation for tests.
/// </summary>
public class MockHttpSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new();

    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public string Id => Guid.NewGuid().ToString();

    public bool IsAvailable => true;

    public void Clear() => _sessionStorage.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) => _sessionStorage.Remove(key);

    public void Set(string key, byte[] value) => _sessionStorage[key] = value;

    public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);

    // Helpers to get/set string values easily
    public string GetString(string key)
    {
        if (_sessionStorage.TryGetValue(key, out var data))
        {
            return System.Text.Encoding.UTF8.GetString(data);
        }
        return null;
    }

    public void SetString(string key, string value)
    {
        _sessionStorage[key] = System.Text.Encoding.UTF8.GetBytes(value);
    }
}
