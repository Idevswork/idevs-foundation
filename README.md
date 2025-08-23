# Idevs Foundation

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/badge/NuGet-Available-brightgreen.svg)](https://www.nuget.org/)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen.svg)](https://github.com/Idevswork/idevs-foundation)

A comprehensive .NET foundation framework that provides essential building blocks for modern applications. Built with CQRS patterns, Entity Framework integration, centralized logging, and dependency injection abstractions.

## Overview

Idevs.Foundation provides a set of reusable abstractions and implementations for common data access patterns in .NET applications. It follows the Repository and Unit of Work patterns with support for audit trails, soft deletion, and extensible querying.

## ✨ New Features

### 📦 Consolidated Package (Recommended)

Get all Foundation components in a single package:

```bash
dotnet add package Idevs.Foundation
```

This includes all individual components with guaranteed compatibility and simplified dependency management.

### 📝 LogManager (NEW)

Centralized logging management with both static and DI access:

```csharp
// Setup
services.AddFoundationLoggingWithStaticAccess();

// Use in services (recommended)
public class MyService : ServiceBase
{
    public MyService(IMediator mediator, ILogManager logManager) 
        : base(mediator, logManager) { }
}

// Static access anywhere (convenient)
var logger = Log.GetLogger<MyClass>();
logger.LogInformation("Easy logging anywhere!");
```

See [Logging Documentation](docs/LOGGING.md) for comprehensive usage examples.

## 📦 Individual Packages

### Idevs.Foundation.Abstractions
Core interfaces and contracts that define the foundation's API.

```bash
dotnet add package Idevs.Foundation.Abstractions
```

### Idevs.Foundation.EntityFramework  
Entity Framework Core implementation of the foundation abstractions.

```bash
dotnet add package Idevs.Foundation.EntityFramework
```

### Idevs.Foundation.Cqrs
CQRS (Command Query Responsibility Segregation) abstractions and base classes.

```bash
dotnet add package Idevs.Foundation.Cqrs
```

### Idevs.Foundation.Mediator
Mediator pattern implementation with pipeline behaviors for cross-cutting concerns.

```bash
dotnet add package Idevs.Foundation.Mediator
```

### Idevs.Foundation.Services
Generic service layer implementations with CQRS support.

```bash
dotnet add package Idevs.Foundation.Services
```

### Idevs.Foundation.Autofac
Autofac integration with automatic registration of handlers and behaviors.

```bash
dotnet add package Idevs.Foundation.Autofac
```

### Idevs.Foundation.Serilog
Serilog integration with Foundation-specific logging configurations and extensions.

```bash
dotnet add package Idevs.Foundation.Serilog
```

## Quick Start

### 1. Define Your Entity

```csharp
using Idevs.Foundation.EntityFramework.Entities;

public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
```

### 2. Create Repository Interface

```csharp
using Idevs.Foundation.Abstractions.Repositories;

public interface IProductRepository : IRepositoryBase<Product, int>
{
    Task<List<Product>> GetActiveProductsAsync();
}
```

### 3. Implement Repository

```csharp
using Idevs.Foundation.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext context, ILogger<ProductRepository> logger) 
        : base(context, logger) { }
    
    public async Task<List<Product>> GetActiveProductsAsync()
    {
        return await QueryAsync(p => p.IsActive && !p.IsDeleted);
    }
}
```

### 4. Use in Service

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;
    
    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<Product> CreateProductAsync(string name, decimal price)
    {
        var product = new Product { Name = name, Price = price, IsActive = true };
        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        return product;
    }
}
```

## Features

### ✨ Core Features

- **Generic Repository Pattern**: Type-safe CRUD operations
- **Unit of Work**: Transaction management and change tracking
- **Audit Trail**: Automatic creation and update timestamp tracking
- **Soft Delete**: Logical deletion with timestamp tracking
- **Expression-based Querying**: Strongly-typed query building

### 🏗️ Base Entity Classes

- `Entity<TId>`: Basic entity with identifier
- `AuditableEntity<TId>`: Entity with creation and update tracking
- `SoftDeletableEntity<TId>`: Full-featured entity with soft delete capability

### 📝 Audit Interfaces

- `IHasId<TId>`: Entity identification
- `IHasCreatedLog`: Creation timestamp tracking  
- `IHasUpdatedLog`: Update timestamp tracking
- `IHasDeletedLog`: Soft delete functionality
- `IAuditableEntity<TId>`: Composite interface for full audit support

### 🔄 Repository Operations

- **Retrieval**: `RetrieveAsync`, `ListAsync`, `QueryAsync`, `GetAllAsync`
- **Commands**: `AddAsync`, `UpdateAsync`, `DeleteAsync` (with bulk operations)
- **Querying**: `Query()`, `QueryNoTracking()`, `ExistsAsync`
- **JSON Support**: Extensible JSON column querying (provider-specific)

## Entity Framework Integration

The package integrates seamlessly with Entity Framework Core:

```csharp
// In your DbContext
public class ApplicationDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    
    // Repository usage
    public IProductRepository ProductRepository => 
        new ProductRepository(this, serviceProvider.GetService<ILogger<ProductRepository>>());
}
```

## Dependency Injection Setup

```csharp
// In Program.cs or Startup.cs
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

services.AddScoped<IProductRepository, ProductRepository>();
services.AddScoped<ProductService>();
```

## CQRS and ServiceBase with Logging

### Using ServiceBase for Structured Logging

ServiceBase provides built-in structured logging for all CQRS operations:

```csharp
using Idevs.Foundation.Services;
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
                return await SendCommandAsync<CreateProductCommand, ProductDto>(command);
            },
            "CreateProduct",
            new { name, price }
        );
    }

    public async Task<ProductDto?> GetProductWithLoggingAsync(int id)
    {
        var query = new GetProductByIdQuery(id);
        return await SendQueryAsync<GetProductByIdQuery, ProductDto?>(query);
    }
}
```

### Serilog Integration with Autofac

Easily configure Serilog with Foundation components:

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Idevs.Foundation.Autofac.Extensions;
using Idevs.Foundation.Serilog.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .UseSerilogLogging(config => config
        .WriteTo.Console()
        .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day))
    .ConfigureContainer<ContainerBuilder>((context, builder) =>
    {
        // Register Foundation with Serilog in one call
        builder.RegisterFoundationWithSerilog(
            context.Configuration,
            Assembly.GetExecutingAssembly());
            
        // Register your application components
        builder.RegisterType<ApplicationDbContext>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterType<ProductRepository>().As<IProductRepository>().InstancePerLifetimeScope();
    })
    .Build();
```

### Manual Serilog Configuration

For more control over logging configuration:

```csharp
builder.RegisterSerilogLogging(configuration, config => config
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.WithProperty("Application", "MyApp")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30));
```

### CQRS Generic Service with Logging

The CqrsGenericService automatically includes structured logging for all operations:

```csharp
public class ProductService : CqrsGenericService<Product, int>
{
    public ProductService(IMediator mediator, ILogger<CqrsGenericService<Product, int>> logger)
        : base(mediator, logger)
    {
    }

    // All operations automatically include structured logging:
    // - Command/Query type and data
    // - Execution time tracking
    // - Success/failure outcomes
    // - Exception details

    public async Task<List<Product>> GetActiveProductsAsync()
    {
        // Logging happens automatically via ServiceBase
        return await GetAsync(p => p.IsActive && !p.IsDeleted);
    }
}
```

### Configuration Options

Add Serilog configuration to your `appsettings.json`:

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Idevs.Foundation": "Debug"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

## Advanced Usage

### Custom Repository Implementation

```csharp
public class OrderRepository : RepositoryBase<Order, Guid>, IOrderRepository
{
    public OrderRepository(DbContext context, ILogger<OrderRepository> logger) 
        : base(context, logger) { }

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await QueryAsync(o => o.Status == status && !o.IsDeleted);
    }

    public async Task<Order?> GetOrderWithItemsAsync(Guid orderId)
    {
        return await Query()
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}
```

### Bulk Operations

```csharp
// Add multiple entities
var products = new List<Product>
{
    new() { Name = "Product 1", Price = 10.00m, IsActive = true },
    new() { Name = "Product 2", Price = 20.00m, IsActive = true }
};

var (entities, rowsAffected) = await repository.AddAsync(products);
await repository.SaveChangesAsync();
```

### Transaction Management

```csharp
using var transaction = await dbContext.Database.BeginTransactionAsync();
try
{
    await repository.AddAsync(product);
    await repository.SaveChangesAsync();
    
    await anotherRepository.UpdateAsync(relatedEntity);
    await anotherRepository.SaveChangesAsync();
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0 or later

## Contributing

We welcome contributions! Please see our contributing guidelines for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For questions, issues, or feature requests, please open an issue on GitHub.
