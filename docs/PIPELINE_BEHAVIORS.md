# Pipeline Behaviors Guide

This guide covers the pipeline behaviors available in Idevs.Foundation for implementing cross-cutting concerns in your CQRS applications.

## Overview

Pipeline behaviors provide a way to implement cross-cutting concerns such as caching, validation, retry logic, and transaction management that apply to multiple commands and queries without cluttering your business logic.

## Available Behaviors

### 1. Caching Behavior

The `CachingBehavior<TRequest, TResponse>` automatically caches responses for requests that implement `ICacheable`.

#### Interface

```csharp
public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? CacheExpiration { get; }
}
```

#### Example Usage

```csharp
public record GetUserQuery(int UserId) : IQuery<User>, ICacheable
{
    public string CacheKey => $"user:{UserId}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(15);
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<User> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        // This will be cached automatically
        return await _userRepository.GetByIdAsync(query.UserId);
    }
}
```

#### Features

- Automatic caching and cache retrieval
- Configurable expiration times
- Memory usage estimation
- Comprehensive logging
- Thread-safe operations

### 2. Validation Behavior

The `ValidationBehavior<TRequest, TResponse>` provides automatic validation for requests that implement `IValidatable`.

#### Interface

```csharp
public interface IValidatable
{
    ValidationResult Validate();
}

public record ValidationResult(bool IsValid, IReadOnlyCollection<ValidationError> Errors);
public record ValidationError(string PropertyName, string ErrorMessage);
```

#### Example Usage

```csharp
public record CreateUserCommand(string Email, string Name) : ICommand<User>, IValidatable
{
    public ValidationResult Validate()
    {
        var errors = new List<ValidationError>();
        
        if (string.IsNullOrWhiteSpace(Email))
            errors.Add(new ValidationError(nameof(Email), "Email is required"));
        
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError(nameof(Name), "Name is required"));
        
        if (!Email.Contains("@"))
            errors.Add(new ValidationError(nameof(Email), "Invalid email format"));
        
        return errors.Count == 0 
            ? ValidationResult.Success 
            : ValidationResult.Failure(errors);
    }
}
```

#### Features

- Automatic validation before handler execution
- Rich validation error information
- Custom `ValidationException` with structured errors
- Comprehensive error logging

### 3. Retry Behavior

The `RetryBehavior<TRequest, TResponse>` provides configurable retry logic for requests that implement `IRetryable`.

#### Interface

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    TimeSpan BaseDelay { get; }
    RetryPolicy RetryPolicy { get; }
    bool UseJitter { get; }
    bool ShouldRetry(Exception exception);
}

public enum RetryPolicy
{
    FixedDelay,
    ExponentialBackoff,
    LinearBackoff
}
```

#### Example Usage

```csharp
public record SendEmailCommand(string To, string Subject, string Body) 
    : ICommand, IRetryable
{
    public int MaxRetryAttempts => 3;
    public TimeSpan BaseDelay => TimeSpan.FromSeconds(1);
    public RetryPolicy RetryPolicy => RetryPolicy.ExponentialBackoff;
    public bool UseJitter => true;

    public bool ShouldRetry(Exception exception)
    {
        // Retry on transient exceptions, not on validation errors
        return exception is HttpRequestException or TimeoutException;
    }
}
```

#### Features

- Multiple retry policies (Fixed, Exponential, Linear)
- Configurable retry conditions
- Jitter support for exponential backoff
- Detailed retry logging
- Smart exception handling

### 4. Transaction Behavior

The `TransactionBehavior<TRequest, TResponse>` provides automatic transaction management for requests that implement `ITransactional`.

#### Interface

```csharp
public interface ITransactional
{
    IsolationLevel? IsolationLevel { get; }
    TimeSpan? Timeout { get; }
}
```

#### Example Usage

```csharp
public record CreateOrderCommand(int CustomerId, List<OrderItem> Items) 
    : ICommand<Order>, ITransactional
{
    public IsolationLevel? IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
    public TimeSpan? Timeout => TimeSpan.FromMinutes(2);
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Order>
{
    public async Task<Order> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // This entire operation will run in a transaction
        var order = await _orderRepository.CreateAsync(command.CustomerId);
        
        foreach (var item in command.Items)
        {
            await _orderRepository.AddItemAsync(order.Id, item);
            await _inventoryService.ReserveAsync(item.ProductId, item.Quantity);
        }
        
        await _notificationService.SendOrderConfirmationAsync(order);
        
        return order;
        // Transaction will be committed automatically if successful
        // or rolled back if any exception occurs
    }
}
```

#### Features

- Automatic transaction creation and management
- Configurable isolation levels
- Custom timeout support
- Automatic rollback on exceptions
- Nested transaction detection
- Comprehensive transaction logging

## Behavior Registration

### Autofac Registration

```csharp
public class BehaviorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register behaviors in the order you want them to execute
        builder.RegisterGeneric(typeof(ValidationBehavior<,>))
            .As(typeof(IPipelineBehavior<,>));

        builder.RegisterGeneric(typeof(CachingBehavior<,>))
            .As(typeof(IPipelineBehavior<,>));

        builder.RegisterGeneric(typeof(RetryBehavior<,>))
            .As(typeof(IPipelineBehavior<,>));

        builder.RegisterGeneric(typeof(TransactionBehavior<,>))
            .As(typeof(IPipelineBehavior<,>));

        builder.RegisterGeneric(typeof(LoggingBehavior<,>))
            .As(typeof(IPipelineBehavior<,>));
    }
}
```

### .NET DI Registration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Register memory cache for caching behavior
    services.AddMemoryCache();

    // Register behaviors
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
}
```

## Execution Order

Pipeline behaviors execute in the order they are registered. The typical recommended order is:

1. **ValidationBehavior** - Validate early to fail fast
2. **CachingBehavior** - Check cache before doing work
3. **RetryBehavior** - Handle transient failures
4. **TransactionBehavior** - Manage database transactions
5. **LoggingBehavior** - Log the actual execution

## Combining Behaviors

You can implement multiple behavior interfaces on a single request:

```csharp
public record GetUserProfileQuery(int UserId) 
    : IQuery<UserProfile>, ICacheable, IRetryable
{
    // Caching properties
    public string CacheKey => $"user-profile:{UserId}";
    public TimeSpan? CacheExpiration => TimeSpan.FromMinutes(10);

    // Retry properties
    public int MaxRetryAttempts => 2;
    public TimeSpan BaseDelay => TimeSpan.FromMilliseconds(500);
    public RetryPolicy RetryPolicy => RetryPolicy.FixedDelay;
    public bool UseJitter => false;

    public bool ShouldRetry(Exception exception) => 
        exception is TimeoutException or HttpRequestException;
}
```

## Best Practices

### Caching

- Use meaningful cache keys that include relevant parameters
- Set appropriate expiration times based on data volatility
- Consider cache invalidation strategies for write operations
- Monitor memory usage in production

### Validation

- Validate early and fail fast
- Provide clear, actionable error messages
- Use property names that match your API contracts
- Consider async validation for complex rules

### Retry

- Only retry on transient exceptions
- Use exponential backoff with jitter for external services
- Set reasonable maximum retry attempts
- Log retry attempts for monitoring

### Transactions

- Use appropriate isolation levels
- Set reasonable timeouts
- Consider the scope of your transactions
- Be aware of deadlock potential with complex operations

## Error Handling

Each behavior provides specific error handling:

- **ValidationBehavior**: Throws `ValidationException` with structured errors
- **CachingBehavior**: Gracefully degrades if caching fails
- **RetryBehavior**: Throws the last exception after all retries exhausted
- **TransactionBehavior**: Automatically rolls back on any exception

## Performance Considerations

- Behaviors add overhead - only use what you need
- Caching behavior can significantly improve read performance
- Retry behavior adds latency - tune delays carefully
- Transaction behavior can impact concurrency - use appropriate isolation levels

## Testing

All behaviors are fully testable. The test examples in the `tests/Behaviors/` folder show how to:

- Mock dependencies
- Verify behavior execution
- Test error conditions
- Validate configuration handling

## Migration from Existing Code

When migrating existing code to use pipeline behaviors:

1. Identify cross-cutting concerns in your handlers
2. Implement appropriate behavior interfaces on your requests
3. Remove the cross-cutting logic from your handlers
4. Register the behaviors in your DI container
5. Test thoroughly to ensure behavior order is correct