namespace webapi.Tests.Services;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using webapi.Authority;
using webapi.Data;
using webapi.Interfaces;
using webapi.Models;
using webapi.Services;
using webapi.Token;
using Xunit;

public class RefreshTokenServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokenService _service;
    private readonly Mock<ILogger<RefreshTokenService>> _loggerMock;

    public RefreshTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
            .Options;

        _dbContext = new ApplicationDbContext(options);
        // Inside your test class constructor:
        _loggerMock = new Mock<ILogger<RefreshTokenService>>();
        _service = new RefreshTokenService(_dbContext);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_ShouldCreateTokenAndPersistHashedToken()
    {
        // Arrange
        string clientId = "client1";
        DateTime expiresAt = DateTime.UtcNow.AddDays(1);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        context.Request.Headers["User-Agent"] = "UnitTestAgent";

        // Act
        string rawToken = await _service.CreateRefreshTokenAsync(clientId, expiresAt, context);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        string hashed = TokenHasher.Hash(rawToken);
        var storedToken = await _dbContext.RefreshTokens.SingleOrDefaultAsync(t =>
            t.Token == hashed
        );

        Assert.NotNull(storedToken);
        Assert.Equal(clientId, storedToken.ClientId);
        Assert.Equal(expiresAt, storedToken.ExpiresAt, TimeSpan.FromSeconds(1)); // allow small timing difference
        Assert.False(storedToken.IsRevoked);
        Assert.Equal("127.0.0.1", storedToken.CreatedFromIp);
        Assert.Equal("UnitTestAgent", storedToken.DeviceInfo);
    }

    [Fact]
    public async Task RevokeAsync_ShouldMarkTokenAsRevoked()
    {
        // Arrange
        string rawToken = "rawTokenToRevoke";
        string hashed = TokenHasher.Hash(rawToken);

        var refreshToken = new RefreshToken
        {
            Token = hashed,
            ClientId = "client1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedFromIp = "127.0.0.1",
            DeviceInfo = "TestAgent",
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.RevokeAsync(rawToken);

        // Assert
        var storedToken = await _dbContext.RefreshTokens.SingleAsync(t => t.Token == hashed);
        Assert.True(storedToken.IsRevoked);
    }

    [Fact]
    public void GenerateSecureToken_ShouldReturnBase64String_OfCorrectLength()
    {
        // Use reflection to access private method GenerateSecureToken(int)
        var methodInfo = typeof(RefreshTokenService).GetMethod(
            "GenerateSecureToken",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );

        Assert.NotNull(methodInfo);

        string token = (string)methodInfo.Invoke(null, new object[] { 64 })!;
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Base64 string length is ~ (4 * ceil(n/3)), so token length should be greater than input size
        Assert.True(token.Length > 64);
    }

    [Fact]
    public void GetClientIp_ShouldReturnForwardedIp_IfHeaderExists()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var methodInfo = typeof(RefreshTokenService).GetMethod(
            "GetClientIp",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        Assert.NotNull(methodInfo);

        string? ip = (string?)methodInfo.Invoke(null, new object[] { context });
        Assert.Equal("203.0.113.195", ip);
    }

    [Fact]
    public void GetClientIp_ShouldReturnRemoteIp_IfNoForwardedHeader()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var methodInfo = typeof(RefreshTokenService).GetMethod(
            "GetClientIp",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        Assert.NotNull(methodInfo);

        string? ip = (string?)methodInfo.Invoke(null, new object[] { context });
        Assert.Equal("127.0.0.1", ip);
    }
}
