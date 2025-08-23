# CQRS and Mediator Usage Examples

This document provides examples of how to use the CQRS and Mediator components of the Idevs.Foundation framework.

## Setup with Autofac

First, let's set up the application to use Autofac with the Foundation module:

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Idevs.Foundation.Autofac.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((context, builder) =>
            {
                // Register Foundation components
                builder.RegisterFoundation(Assembly.GetExecutingAssembly());
                
                // Register your application components
                builder.RegisterType<ApplicationDbContext>().AsSelf().InstancePerLifetimeScope();
                builder.RegisterType<ProductRepository>().As<IProductRepository>().InstancePerLifetimeScope();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
            })
            .Build();

        await host.RunAsync();
    }
}
```

## Command Handlers

### Create Command and Handler

First, define your commands and handlers:

```csharp
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Results;
using Idevs.Foundation.EntityFramework.Entities;

// Command definition
public record CreateProductCommand(string Name, decimal Price) : ICommand<Result<ProductDto>>;

// Command handler
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public CreateProductCommandHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<ProductDto>> HandleAsync(
        CreateProductCommand command, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create entity
            var product = new Product
            {
                Name = command.Name,
                Price = command.Price,
                IsActive = true
            };

            // Add to repository
            var result = await _repository.AddAsync(product, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            // Map to DTO and return
            var productDto = _mapper.Map<ProductDto>(result);
            return Result<ProductDto>.Success(productDto);
        }
        catch (Exception ex)
        {
            return Result<ProductDto>.Failure($"Failed to create product: {ex.Message}");
        }
    }
}
```

### Command Validation Behavior

You can add a validation behavior to validate commands before they reach the handler:

```csharp
using FluentValidation;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Cqrs.Results;
using Microsoft.Extensions.Logging;

// Validator for the command
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// Validation behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        _logger.LogDebug("Validating {RequestType}", typeof(TRequest).Name);
        
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors", 
            typeof(TRequest).Name, failures.Count);
            
        // Convert failures to dictionary
        var errors = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        // If the response is a Result type, return a failure result
        if (typeof(TResponse).IsGenericType && 
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var method = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod("Failure", new[] { typeof(string), typeof(Dictionary<string, string[]>) });
                
            return (TResponse)method.Invoke(null, new object[] 
            { 
                "Validation failed", 
                errors 
            });
        }

        throw new ValidationException(failures);
    }
}
```

## Query Handlers

### Create Query and Handler

```csharp
using Idevs.Foundation.Cqrs.Queries;

// Query definition
public record GetProductByIdQuery(int Id) : IQuery<ProductDto?>;

// Query handler
public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> HandleAsync(
        GetProductByIdQuery query, 
        CancellationToken cancellationToken = default)
    {
        var product = await _repository.RetrieveAsync(query.Id, cancellationToken);
        return product != null ? _mapper.Map<ProductDto>(product) : null;
    }
}
```

### Caching Behavior

You can add a caching behavior to cache query results:

```csharp
using Idevs.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private readonly TimeSpan _cacheDuration;

    public CachingBehavior(
        IMemoryCache cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger,
        TimeSpan? cacheDuration = null)
    {
        _cache = cache;
        _logger = logger;
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Only cache queries, not commands
        if (!typeof(IQuery<>).IsAssignableFrom(typeof(TRequest).GetGenericTypeDefinition()))
            return await next();

        var cacheKey = $"{typeof(TRequest).Name}_{JsonSerializer.Serialize(request)}";

        if (_cache.TryGetValue(cacheKey, out TResponse cachedResponse))
        {
            _logger.LogDebug("Returning cached response for {RequestType}", typeof(TRequest).Name);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for {RequestType}", typeof(TRequest).Name);
        var response = await next();
        
        _cache.Set(cacheKey, response, _cacheDuration);
        
        return response;
    }
}
```

## Using the Mediator

Now you can use the mediator to send commands and queries:

```csharp
using Idevs.Foundation.Cqrs.Results;
using Idevs.Foundation.Mediator.Core;

public class ProductService
{
    private readonly IMediator _mediator;

    public ProductService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<ProductDto>> CreateProductAsync(string name, decimal price)
    {
        var command = new CreateProductCommand(name, price);
        return await _mediator.SendAsync<CreateProductCommand, Result<ProductDto>>(command);
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var query = new GetProductByIdQuery(id);
        return await _mediator.QueryAsync<GetProductByIdQuery, ProductDto?>(query);
    }
}
```

## Using ServiceBase for Enhanced Logging

ServiceBase provides built-in structured logging and error handling:

```csharp
using Idevs.Foundation.Services;
using Idevs.Foundation.Cqrs.Results;
using Idevs.Foundation.Mediator.Core;
using Microsoft.Extensions.Logging;

public class ProductService : ServiceBase
{
    public ProductService(IMediator mediator, ILogger<ProductService> logger)
        : base(mediator, logger)
    {
    }

    public async Task<Result<ProductDto>> CreateProductWithLoggingAsync(string name, decimal price)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var command = new CreateProductCommand(name, price);
                var result = await SendCommandAsync<CreateProductCommand, Result<ProductDto>>(command);
                return result;
            },
            "CreateProduct",
            new { name, price }
        );
    }

    public async Task<ProductDto?> GetProductWithLoggingAsync(int id)
    {
        // SendQueryAsync automatically logs the query execution
        var query = new GetProductByIdQuery(id);
        return await SendQueryAsync<GetProductByIdQuery, ProductDto?>(query);
    }

    public async Task<Result<bool>> ValidateAndCreateProductAsync(string name, decimal price)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                // Custom validation logic with logging
                LogInformation("Validating product data for {ProductName} with price {Price}", name, price);
                
                if (string.IsNullOrWhiteSpace(name))
                {
                    LogWarning("Product creation failed: Name is required");
                    return false;
                }
                
                if (price <= 0)
                {
                    LogWarning("Product creation failed: Price must be positive");
                    return false;
                }

                var command = new CreateProductCommand(name, price);
                var result = await SendCommandAsync<CreateProductCommand, Result<ProductDto>>(command);
                
                if (result.IsSuccess)
                {
                    LogInformation("Product {ProductId} created successfully", result.Value?.Id);
                    return true;
                }
                else
                {
                    LogError("Product creation failed: {ErrorMessage}", result.ErrorMessage);
                    return false;
                }
            },
            "ValidateAndCreateProduct",
            new { name, price }
        );
    }
}
```

## CQRS with Generic Service

You can also use the CqrsGenericService for common operations:

```csharp
using Idevs.Foundation.Services;

public class ProductService : CqrsGenericService<Product, int>
{
    public ProductService(IMediator mediator) : base(mediator)
    {
    }

    // Additional methods specific to your domain
    public async Task<List<Product>> GetActiveProductsAsync()
    {
        return await GetAsync(p => p.IsActive && !p.IsDeleted);
    }
}
```

## Benefits of This Approach

1. **Separation of Concerns**: Commands and queries are separated, making the code easier to understand and maintain.

2. **Single Responsibility**: Each handler has a single responsibility, making them simple and testable.

3. **Pipeline Behaviors**: Cross-cutting concerns like validation, logging, and caching are handled through pipeline behaviors.

4. **Testability**: Commands, queries, and handlers are all easily testable in isolation.

5. **Extensibility**: New behaviors can be added without modifying existing code.

6. **Clean Architecture**: This approach aligns well with clean architecture principles, separating application logic from infrastructure concerns.

## Important Considerations

1. **Performance**: The mediator pattern adds a small overhead, which is usually negligible but something to be aware of for high-performance applications.

2. **Complexity**: For small applications, this approach might introduce unnecessary complexity.

3. **Learning Curve**: There's a learning curve for developers who are not familiar with CQRS or the mediator pattern.

4. **Consistency**: Be consistent in how you structure your commands and queries across the application.

## Conclusion

The CQRS and Mediator components of the Idevs.Foundation framework provide a structured way to handle your application's business logic. They help separate read and write operations, making your code more maintainable and testable. The pipeline behaviors allow you to add cross-cutting concerns like validation, logging, and caching in a clean and reusable way.
