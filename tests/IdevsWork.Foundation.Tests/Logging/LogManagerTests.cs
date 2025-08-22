using IdevsWork.Foundation.Abstractions.Logging;
using IdevsWork.Foundation.Services.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdevsWork.Foundation.Tests.Logging;

public class LogManagerTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogManager _logManager;

    public LogManagerTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logManager = new LogManager(_loggerFactory);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LogManager(null!));
    }

    [Fact]
    public void GetLogger_Generic_ReturnsTypedLogger()
    {
        // Act
        var logger = _logManager.GetLogger<LogManagerTests>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger<LogManagerTests>>(logger);
    }

    [Fact]
    public void GetLogger_WithString_ReturnsLogger()
    {
        // Arrange
        const string categoryName = "TestCategory";

        // Act
        var logger = _logManager.GetLogger(categoryName);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void GetLogger_WithType_ReturnsLogger()
    {
        // Arrange
        var type = typeof(LogManagerTests);

        // Act
        var logger = _logManager.GetLogger(type);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void GetLogger_WithNullString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _logManager.GetLogger((string)null!));
    }

    [Fact]
    public void GetLogger_WithNullType_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _logManager.GetLogger((Type)null!));
    }
}
