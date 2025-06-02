using System;
using System.Linq.Expressions;
using Moq;
using webapi.Interfaces;
using webapi.Services;
using Xunit;

public class BackgroundJobServiceTests
{
    [Fact]
    public void EnqueueJob_ShouldEnqueueJob()
    {
        // Arrange
        var mockWrapper = new Mock<IBackgroundJobWrapper>();
        var service = new BackgroundJobService(mockWrapper.Object);

        // Act
        service.EnqueueJob();

        // Assert
        mockWrapper.Verify(m => m.Enqueue(It.IsAny<Expression<Action>>()), Times.Once);
    }
}
