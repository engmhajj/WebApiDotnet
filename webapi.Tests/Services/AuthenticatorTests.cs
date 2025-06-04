using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using webapi.Authority;
using webapi.Data;
using webapi.Models;
using Xunit;

public class AuthenticatorTests
{
    private readonly Mock<ApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<Authenticator>> _loggerMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IOptions<JwtOptions>> _jwtOptionsMock;
    private readonly Authenticator _authenticator;

    public AuthenticatorTests()
    {
        _dbContextMock = new Mock<ApplicationDbContext>();
        _loggerMock = new Mock<ILogger<Authenticator>>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _jwtOptionsMock = new Mock<IOptions<JwtOptions>>();
        _authenticator = new Authenticator(
            _dbContextMock.Object,
            _loggerMock.Object,
            _refreshTokenServiceMock.Object,
            _jwtOptionsMock.Object
        );
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var clientId = "validClientId";
        var secret = "validSecret";
        var application = new Application
        {
            ClientId = clientId,
            SecretHash = "hashedSecret",
            SecretSalt = "salt",
        };
        _dbContextMock
            .Setup(db => db.Applications.FirstOrDefaultAsync(It.IsAny<Func<Application, bool>>()))
            .ReturnsAsync(application);

        // Act
        var result = await _authenticator.AuthenticateAsync(clientId, secret);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidCredentials_ReturnsFalse()
    {
        // Arrange
        var clientId = "invalidClientId";
        var secret = "invalidSecret";
        _dbContextMock
            .Setup(db => db.Applications.FirstOrDefaultAsync(It.IsAny<Func<Application, bool>>()))
            .ReturnsAsync((Application)null);

        // Act
        var result = await _authenticator.AuthenticateAsync(clientId, secret);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_ValidToken_ReturnsClaims()
    {
        // Arrange
        var token = "validToken";
        var expectedClaims = new List<Claim> { new Claim("sub", "userId") };
        _jwtOptionsMock.Setup(options => options.Value.SecretKey).Returns("secretKey");

        // Act
        var claims = _authenticator.VerifyToken(token);

        // Assert
        Assert.Equal(expectedClaims, claims);
    }

    [Fact]
    public void VerifyToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalidToken";
        _jwtOptionsMock.Setup(options => options.Value.SecretKey).Returns("secretKey");

        // Act
        var claims = _authenticator.VerifyToken(token);

        // Assert
        Assert.Null(claims);
    }
}
