using Idevs.Foundation.Abstractions.Logging;
using Idevs.Foundation.Services.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Idevs.Foundation.Tests.Logging;

public class StaticLogTests
{
    private readonly ILogManager _logManager;

    public StaticLogTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logManager = new LogManager(loggerFactory);
        
        // Reset before each test
        Log.Reset();
    }

    [Fact]
    public void IsInitialized_BeforeInitialization_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(Log.IsInitialized);
    }

    [Fact]
    public void Initialize_WithValidLogManager_SetsIsInitializedToTrue()
    {
        // Act
        Log.Initialize(_logManager);

        // Assert
        Assert.True(Log.IsInitialized);
    }

    [Fact]
    public void Initialize_WithNullLogManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Log.Initialize(null!));
    }

    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        Log.Initialize(_logManager);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Log.Initialize(_logManager));
    }

    [Fact]
    public void GetLogger_Generic_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Log.GetLogger<StaticLogTests>());
    }

    [Fact]
    public void GetLogger_WithString_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Log.GetLogger("TestCategory"));
    }

    [Fact]
    public void GetLogger_WithType_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Log.GetLogger(typeof(StaticLogTests)));
    }

    [Fact]
    public void GetCurrentLogger_BeforeInitialization_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Log.GetCurrentLogger());
    }

    [Fact]
    public void GetLogger_Generic_AfterInitialization_ReturnsLogger()
    {
        // Arrange
        Log.Initialize(_logManager);

        // Act
        var logger = Log.GetLogger<StaticLogTests>();

        // Assert
        Assert.NotNull(logger);
        Assert.IsAssignableFrom<ILogger<StaticLogTests>>(logger);
    }

    [Fact]
    public void GetLogger_WithString_AfterInitialization_ReturnsLogger()
    {
        // Arrange
        Log.Initialize(_logManager);
        const string categoryName = "TestCategory";

        // Act
        var logger = Log.GetLogger(categoryName);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void GetLogger_WithType_AfterInitialization_ReturnsLogger()
    {
        // Arrange
        Log.Initialize(_logManager);
        var type = typeof(StaticLogTests);

        // Act
        var logger = Log.GetLogger(type);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void GetCurrentLogger_AfterInitialization_ReturnsLogger()
    {
        // Arrange
        Log.Initialize(_logManager);

        // Act
        var logger = Log.GetCurrentLogger();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void Reset_AfterInitialization_SetsIsInitializedToFalse()
    {
        // Arrange
        Log.Initialize(_logManager);
        Assert.True(Log.IsInitialized);

        // Act
        Log.Reset();

        // Assert
        Assert.False(Log.IsInitialized);
    }
}
