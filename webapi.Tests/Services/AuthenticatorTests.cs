namespace webapi.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using webapi.Authority;
    using webapi.Data;
    using webapi.Models;
    using webapi.Security;
    using webapi.Token;
    using Xunit;

    public class AuthenticatorTests
    {
        private readonly Mock<ApplicationDbContext> _dbMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<Authenticator>> _loggerMock;
        private readonly Mock<RefreshTokenService> _refreshTokenServiceMock;
        private readonly IOptions<JwtOptions> _jwtOptions;

        public AuthenticatorTests()
        {
            var options = new JwtOptions
            {
                AccessTokenExpiryMinutes = 60,
                RefreshTokenExpiryMinutes = 1440,
            };
            _jwtOptions = Options.Create(options);

            var optionsBuilder =
                new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("TestDb");
            var context = new ApplicationDbContext(optionsBuilder.Options);

            _dbMock = new Mock<ApplicationDbContext>(optionsBuilder.Options);
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<Authenticator>>();
            _refreshTokenServiceMock = new Mock<RefreshTokenService>(null);

            _configMock
                .Setup(c => c.GetValue<string>("JwtOptions:SecretKey"))
                .Returns("supersecretkey1234567890");

            // For real in-memory DB, could inject ApplicationDbContext directly for better integration testing
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsFalse_IfClientIdOrSecretIsEmpty()
        {
            var auth = CreateAuthenticator();
            Assert.False(await auth.AuthenticateAsync("", "secret"));
            Assert.False(await auth.AuthenticateAsync("clientId", ""));
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsFalse_IfClientNotFound()
        {
            var auth = CreateAuthenticator();

            // Setup DB to return null for application
            _dbMock
                .Setup(db =>
                    db.Applications.FirstOrDefaultAsync(
                        It.IsAny<System.Linq.Expressions.Expression<Func<Application, bool>>>()
                    )
                )
                .ReturnsAsync((Application)null!);

            var result = await auth.AuthenticateAsync("unknown", "secret");
            Assert.False(result);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsTrue_IfSecretValid()
        {
            var app = new Application
            {
                ClientId = "client1",
                SecretSalt = "salt",
                SecretHash = "hashedSecret",
                ApplicationName = "MyApp",
            };

            _dbMock
                .Setup(db =>
                    db.Applications.FirstOrDefaultAsync(
                        It.IsAny<System.Linq.Expressions.Expression<Func<Application, bool>>>()
                    )
                )
                .ReturnsAsync(app);

            // Mock SecretHasher to always return true for this test
            var originalVerify = SecretHasher.VerifySecret;
            SecretHasher.VerifySecret = (secret, salt, hash) => secret == "correctSecret";

            var auth = CreateAuthenticator();

            var valid = await auth.AuthenticateAsync("client1", "correctSecret");
            Assert.True(valid);

            var invalid = await auth.AuthenticateAsync("client1", "wrongSecret");
            Assert.False(invalid);

            // Restore original delegate or method if needed
            // (Assuming VerifySecret is static method - would be better to abstract)
        }

        [Fact]
        public async Task CreateTokenAsync_ReturnsValidToken()
        {
            var app = new Application
            {
                ClientId = "client1",
                ApplicationName = "TestApp",
                Scopes = "read,write",
            };

            _dbMock
                .Setup(db =>
                    db.Applications.FirstOrDefaultAsync(
                        It.IsAny<System.Linq.Expressions.Expression<Func<Application, bool>>>()
                    )
                )
                .ReturnsAsync(app);

            var auth = CreateAuthenticator();

            var expires = DateTime.UtcNow.AddMinutes(30);
            var token = await auth.CreateTokenAsync("client1", expires);

            Assert.False(string.IsNullOrWhiteSpace(token));

            var claims = auth.VerifyToken(token);
            Assert.NotNull(claims);

            // Check that AppName and scope claims exist
            Assert.Contains(claims!, c => c.Type == "AppName" && c.Value == "TestApp");
            Assert.Contains(claims!, c => c.Type == "scope" && c.Value.Contains("read"));
        }

        [Fact]
        public async Task RefreshAccessTokenAsync_RevokesOldTokenAndCreatesNewTokens()
        {
            var clientId = "client1";
            var refreshTokenRaw = "rawtoken";

            var hashedToken = TokenHasher.Hash(refreshTokenRaw);

            var storedRefreshToken = new RefreshToken
            {
                Token = hashedToken,
                ClientId = clientId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsRevoked = false,
            };

            _dbMock
                .Setup(db =>
                    db.RefreshTokens.FirstOrDefaultAsync(
                        It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()
                    )
                )
                .ReturnsAsync(storedRefreshToken);

            _refreshTokenServiceMock
                .Setup(s =>
                    s.CreateRefreshTokenAsync(
                        It.IsAny<string>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<HttpContext>()
                    )
                )
                .ReturnsAsync("newRefreshToken");

            var auth = CreateAuthenticator();

            var mockContext = new DefaultHttpContext();
            var tokenResponse = await auth.RefreshAccessTokenAsync(
                refreshTokenRaw,
                clientId,
                mockContext
            );

            Assert.NotNull(tokenResponse);
            Assert.False(string.IsNullOrWhiteSpace(tokenResponse.AccessToken));
            Assert.Equal("newRefreshToken", tokenResponse.RefreshToken);

            // Verify token revoked
            Assert.True(storedRefreshToken.IsRevoked);
        }

        private Authenticator CreateAuthenticator()
        {
            return new Authenticator(
                _dbMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                _refreshTokenServiceMock.Object,
                _jwtOptions
            );
        }
    }
}
