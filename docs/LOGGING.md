# IdevsWork Foundation - Logging

The Foundation framework provides comprehensive logging support through the `ILogManager` interface and static `Log` class, making it easy to get loggers throughout your application.

## Features

- **Centralized logger creation** via `ILogManager`
- **Static access** through the `Log` class for easy usage anywhere
- **Type-safe logging** with generic logger support
- **Flexible initialization** via dependency injection or manual setup
- **Integration** with Microsoft.Extensions.Logging ecosystem

## Basic Usage

### 1. Dependency Injection Setup

```csharp
// In your Program.cs or Startup.cs
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.AddDebug();
});

// Add Foundation logging
services.AddFoundationLogging();

// OR with static access initialization
services.AddFoundationLoggingWithStaticAccess();
```

### 2. Using ILogManager in Services

```csharp
public class UserService : ServiceBase
{
    // Constructor using LogManager (recommended)
    public UserService(IMediator mediator, ILogManager logManager) 
        : base(mediator, logManager)
    {
    }
    
    // Or using explicit ILogger
    public UserService(IMediator mediator, ILogger<UserService> logger) 
        : base(mediator, logger)
    {
    }
    
    public async Task<User> GetUserAsync(int userId)
    {
        Logger.LogInformation("Getting user with ID {UserId}", userId);
        
        // Your business logic here
        
        return user;
    }
}
```

### 3. Using Static Log Class

```csharp
public class AnyClass
{
    public void DoSomething()
    {
        // Get logger for current type
        var logger = Log.GetLogger<AnyClass>();
        logger.LogInformation("Doing something important");
        
        // Get logger by category name
        var categoryLogger = Log.GetLogger("CustomCategory");
        categoryLogger.LogWarning("This is a warning");
        
        // Get logger for calling type automatically
        var currentLogger = Log.GetCurrentLogger();
        currentLogger.LogDebug("Debug information");
    }
}
```

## Advanced Usage

### Custom Service with LogManager

```csharp
public interface IProductService
{
    Task<Product> CreateProductAsync(Product product);
    Task<List<Product>> GetProductsAsync();
}

public class ProductService : IProductService
{
    private readonly ILogger _logger;
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository, ILogManager logManager)
    {
        _repository = repository;
        _logger = logManager.GetLogger<ProductService>();
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        _logger.LogInformation("Creating product: {@Product}", product);
        
        try
        {
            var result = await _repository.CreateAsync(product);
            _logger.LogInformation("Successfully created product with ID {ProductId}", result.Id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create product: {@Product}", product);
            throw;
        }
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        _logger.LogDebug("Retrieving all products");
        
        var products = await _repository.GetAllAsync();
        
        _logger.LogInformation("Retrieved {ProductCount} products", products.Count);
        return products;
    }
}
```

### Manual Initialization (Without DI)

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        // Create logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Create and initialize log manager
        var logManager = new LogManager(loggerFactory);
        Log.Initialize(logManager);

        // Now you can use static Log class anywhere
        var logger = Log.GetLogger<Program>();
        logger.LogInformation("Application starting...");

        // Your application logic here
    }
}
```

### Using in Static Methods

```csharp
public static class UtilityClass
{
    public static string ProcessData(string data)
    {
        // Get logger for current type
        var logger = Log.GetCurrentLogger();
        
        logger.LogDebug("Processing data: {Data}", data);
        
        try
        {
            // Process data
            var result = data.ToUpper();
            
            logger.LogInformation("Data processed successfully");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process data: {Data}", data);
            throw;
        }
    }
}
```

## Integration with Serilog

```csharp
// In Program.cs
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog();
});

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Add Foundation logging with static access
services.AddFoundationLoggingWithStaticAccess();
```

## Best Practices

### 1. Use Dependency Injection When Possible

```csharp
// Preferred - testable and follows DI principles
public class MyService
{
    private readonly ILogger _logger;
    
    public MyService(ILogManager logManager)
    {
        _logger = logManager.GetLogger<MyService>();
    }
}
```

### 2. Use Static Log Class for Utilities

```csharp
// Good for static classes and utilities
public static class Extensions
{
    public static void ProcessItems<T>(this IEnumerable<T> items)
    {
        var logger = Log.GetLogger(typeof(Extensions));
        logger.LogInformation("Processing {Count} items", items.Count());
    }
}
```

### 3. Structured Logging

```csharp
// Use structured logging with properties
logger.LogInformation("User {UserId} performed action {Action} at {Timestamp}", 
    userId, action, DateTime.UtcNow);

// Use object destructuring for complex objects
logger.LogInformation("Processing order: {@Order}", order);
```

### 4. Proper Exception Logging

```csharp
try
{
    // risky operation
}
catch (Exception ex)
{
    // Log exception with context
    logger.LogError(ex, "Operation failed for user {UserId} with parameters {@Parameters}", 
        userId, parameters);
    
    // Re-throw or handle appropriately
    throw;
}
```

## Testing

```csharp
public class ServiceTests
{
    [Fact]
    public void TestWithMockLogManager()
    {
        // Arrange
        var mockLogManager = new Mock<ILogManager>();
        var mockLogger = new Mock<ILogger>();
        
        mockLogManager.Setup(x => x.GetLogger<MyService>())
                     .Returns(mockLogger.Object);

        var service = new MyService(mockLogManager.Object);

        // Act & Assert
        // Your test logic here
        
        // Verify logging occurred
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("expected message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

## Configuration

### appsettings.json Example

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "IdevsWork.Foundation": "Debug"
    }
  }
}
```

This logging system provides a flexible and powerful way to handle logging throughout your Foundation-based applications while maintaining clean code and testability.
