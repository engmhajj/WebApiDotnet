using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using webapi.Services;
using Xunit;

public class CacheServiceTests
{
    [Fact]
    public async Task SetCacheAsync_Should_Call_SetAsync_With_Correct_Parameters()
    {
        var cacheMock = new Mock<IDistributedCache>();
        var service = new CacheService(cacheMock.Object);
        var key = "test-key";
        var value = "test-value";
        var bytes = Encoding.UTF8.GetBytes(value);

        await service.SetCacheAsync(key, value);

        cacheMock.Verify(
            m =>
                m.SetAsync(
                    key,
                    It.Is<byte[]>(b => b.SequenceEqual(bytes)),
                    It.Is<DistributedCacheEntryOptions>(opts =>
                        opts.AbsoluteExpirationRelativeToNow.HasValue
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetCacheAsync_Should_Return_Value_If_Exists()
    {
        var key = "test-key";
        var expectedValue = "cached-value";
        var bytes = Encoding.UTF8.GetBytes(expectedValue);
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);
        var service = new CacheService(cacheMock.Object);

        var result = await service.GetCacheAsync(key);

        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetCacheAsync_Should_Return_Null_If_Key_Not_Found()
    {
        var key = "missing-key";
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock
            .Setup(m => m.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        var service = new CacheService(cacheMock.Object);

        var result = await service.GetCacheAsync(key);

        Assert.Null(result);
    }
}
