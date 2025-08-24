# Component Model Attribute Registration

This document explains how to use component model attributes for automatic service registration with Autofac in the Idevs.Foundation framework.

## Overview

The component model attributes provide a **declarative approach** to dependency injection registration, eliminating the need for manual service registration in container configuration. Simply decorate your classes with the appropriate lifetime attributes, and the `FoundationModule` will automatically discover and register them during container setup.

## Key Benefits

- ✅ **Declarative Registration**: Mark classes with attributes instead of manual container configuration
- ✅ **Automatic Discovery**: Services are automatically discovered during assembly scanning
- ✅ **Interface Registration**: Automatically registers as implemented interfaces (with fallback to self-registration)
- ✅ **Lifetime Management**: Clear, type-safe lifetime specification
- ✅ **Reduced Boilerplate**: No need to write container registration code for every service
- ✅ **Consistent Conventions**: Uniform approach across the entire application

## Available Attributes

### `[Singleton]`
Registers the service as a **singleton** - single instance for the entire application lifetime.
- **Autofac Equivalent**: `.SingleInstance()`
- **Use Case**: Expensive-to-create objects, stateless services, configuration objects

### `[Scoped]`
Registers the service as **scoped** - one instance per lifetime scope (typically per HTTP request in web applications).
- **Autofac Equivalent**: `.InstancePerLifetimeScope()`
- **Use Case**: Database contexts, repository patterns, request-specific services

### `[Transient]`
Registers the service as **transient** - new instance every time it's requested.
- **Autofac Equivalent**: `.InstancePerDependency()`
- **Use Case**: Lightweight services, stateless operations, factory patterns

## Attribute Properties

All component model attributes inherit from `ComponentModelAttribute` and support:

### `AsSelf` Property
- **Type**: `bool`
- **Default**: `false`
- **Description**: Controls registration behavior
  - `false` (default): Registers as implemented interfaces (with fallback to self if no interfaces)
  - `true`: Forces registration as the concrete implementation type

### `Lifetime` Property
- **Type**: `ServiceLifetime`
- **Description**: Automatically set by the specific attribute (read-only in practice)

## Registration Logic

### Interface vs Self Registration

```csharp
// Default behavior (AsSelf = false)
[Scoped]
public class UserService : IUserService  // → Registered as IUserService

[Scoped] 
public class EmailHelper                  // → Registered as EmailHelper (no interfaces)

// Explicit self registration (AsSelf = true)
[Scoped(AsSelf = true)]
public class UserService : IUserService  // → Registered as UserService (concrete type)
```

## Usage Examples

### Basic Service Registration

```csharp
using Idevs.Foundation.Autofac.ComponentModels;

// Singleton service - expensive to create, shared state
[Singleton]
public class ConfigurationService : IConfigurationService
{
    public string ConnectionString { get; }
    
    public ConfigurationService(IConfiguration config)
    {
        ConnectionString = config.GetConnectionString("DefaultConnection");
    }
}

// Scoped service - per-request lifetime in web apps
[Scoped]
public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public OrderService(IRepository<Order> orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }
}

// Transient service - lightweight, stateless
[Transient]
public class EmailValidator : IEmailValidator
{
    public bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) && email.Contains("@");
    }
}
```

### Advanced Registration Scenarios

```csharp
// Multiple interface implementation
[Scoped]
public class NotificationService : IEmailNotificationService, ISmsNotificationService
{
    // Registered as both IEmailNotificationService AND ISmsNotificationService
}

// Self registration for concrete type injection
[Singleton(AsSelf = true)]
public class CacheManager : ICacheManager
{
    // Registered as CacheManager (concrete type)
    // Useful when you need to inject the concrete implementation
}

// No interfaces - automatic self registration
[Scoped]
public class InternalHelper  // No interfaces
{
    // Automatically registered as InternalHelper
}
```

### Repository Pattern Example

```csharp
// Base repository interface
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Generic repository implementation
[Scoped]
public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    
    public Repository(DbContext context)
    {
        _context = context;
    }
    
    // Implementation...
}

// Specific repository with additional methods
[Scoped]
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(DbContext context) : base(context) { }
    
    public async Task<User> GetByEmailAsync(string email)
    {
        // Custom implementation
    }
}
```

### CQRS Handler Registration

```csharp
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Queries;

// Command handlers are automatically registered as scoped
[Scoped]
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, User>
{
    private readonly IUserRepository _userRepository;
    
    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<User> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Implementation
    }
}

// Query handlers
[Scoped]
public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, IEnumerable<User>>
{
    private readonly IUserRepository _userRepository;
    
    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<IEnumerable<User>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

## Container Setup

### Basic Setup

```csharp
using Autofac;
using Idevs.Foundation.Autofac.Extensions;
using System.Reflection;

// Program.cs or Startup.cs
var builder = new ContainerBuilder();

// Register Foundation with current assembly
builder.RegisterFoundation();

// Or specify specific assemblies
builder.RegisterFoundation(
    Assembly.GetExecutingAssembly(),
    Assembly.GetAssembly(typeof(SomeServiceInAnotherAssembly))
);

var container = builder.Build();
```

### ASP.NET Core Integration

```csharp
// Program.cs (ASP.NET Core 6+)
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Idevs.Foundation.Autofac.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Replace default DI container with Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register Foundation components (includes component model scanning)
    containerBuilder.RegisterFoundation(
        Assembly.GetExecutingAssembly(),
        // Add other assemblies as needed
        typeof(SomeService).Assembly
    );
    
    // Optional: Register with Serilog logging
    containerBuilder.RegisterFoundationWithSerilog(
        builder.Configuration,
        Assembly.GetExecutingAssembly()
    );
});

var app = builder.Build();
```

### Multi-Assembly Registration

```csharp
var assemblies = new[]
{
    Assembly.GetExecutingAssembly(),              // Current assembly
    typeof(UserService).Assembly,                 // Services assembly
    typeof(UserRepository).Assembly,              // Data assembly
    typeof(CreateUserCommandHandler).Assembly     // Handlers assembly
};

builder.RegisterFoundation(assemblies);
```

## Best Practices

### 1. Choose Appropriate Lifetimes

```csharp
// ✅ Good: Configuration as singleton
[Singleton]
public class AppConfiguration : IAppConfiguration { }

// ✅ Good: Database context as scoped
[Scoped]
public class ApplicationDbContext : DbContext { }

// ✅ Good: Lightweight validator as transient
[Transient]
public class EmailValidator : IEmailValidator { }

// ❌ Avoid: Database context as singleton (causes issues with EF Core)
// [Singleton]
// public class ApplicationDbContext : DbContext { }
```

### 2. Interface Design

```csharp
// ✅ Good: Clear interface contract
public interface IUserService
{
    Task<User> GetUserAsync(int id);
    Task<User> CreateUserAsync(CreateUserRequest request);
}

[Scoped]
public class UserService : IUserService
{
    // Implementation
}

// ✅ Good: Multiple interfaces for different contracts
[Scoped]
public class NotificationService : IEmailService, INotificationService
{
    // Registered as both interfaces
}
```

### 3. Avoid Circular Dependencies

```csharp
// ❌ Bad: Circular dependency
[Scoped] public class ServiceA : IServiceA
{
    public ServiceA(IServiceB serviceB) { }
}

[Scoped] public class ServiceB : IServiceB  
{
    public ServiceB(IServiceA serviceA) { } // Circular!
}

// ✅ Good: Use mediator or event-driven approach
[Scoped] public class ServiceA : IServiceA
{
    public ServiceA(IMediator mediator) { }
}

[Scoped] public class ServiceB : IServiceB
{
    public ServiceB(IMediator mediator) { }
}
```

### 4. Testing Considerations

```csharp
// ✅ Good: Design for testability
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

[Scoped]
public class EmailService : IEmailService
{
    // Can be easily mocked in tests
}

// In tests:
var mockEmailService = new Mock<IEmailService>();
// Setup and verify mock behavior
```

## Troubleshooting

### Common Issues

#### 1. Service Not Found
```
Autofac.Core.Registration.ComponentNotRegisteredException: 
The requested service 'MyService' has not been registered.
```

**Solutions:**
- Ensure the service class has a component model attribute
- Verify the assembly containing the service is included in `RegisterFoundation()`
- Check that the service class is `public` and not `abstract`

#### 2. Missing Interface Registration
```
The requested service 'IMyService' has not been registered.
```

**Solutions:**
- Ensure your service class implements the interface
- Verify the interface is `public`
- Consider using `AsSelf = true` if you need concrete type registration

#### 3. Wrong Lifetime Behavior

**Problem**: Service instances aren't behaving as expected

**Solutions:**
- Review lifetime attribute choice ([Singleton] vs [Scoped] vs [Transient])
- Understand the scope context (web request, background service, etc.)
- Avoid capturing scoped services in singletons

### Debugging Registration

```csharp
// Enable detailed Autofac logging to see registrations
var builder = new ContainerBuilder();
builder.RegisterFoundation(Assembly.GetExecutingAssembly());

var container = builder.Build();

// In development, you can inspect registrations
foreach (var registration in container.ComponentRegistry.Registrations)
{
    Console.WriteLine($"Registered: {registration.Activator.LimitType.Name}");
    foreach (var service in registration.Services)
    {
        Console.WriteLine($"  As: {service.Description}");
    }
}
```

## Migration from Manual Registration

### Before (Manual Registration)
```csharp
// Old approach - manual registration
builder.RegisterType<UserService>().As<IUserService>().InstancePerLifetimeScope();
builder.RegisterType<EmailService>().As<IEmailService>().InstancePerLifetimeScope();
builder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
// ... dozens more registrations
```

### After (Attribute-Based Registration)
```csharp
// New approach - attribute-based
[Scoped] public class UserService : IUserService { }
[Scoped] public class EmailService : IEmailService { }
[Scoped] public class OrderService : IOrderService { }

// Single registration call
builder.RegisterFoundation(Assembly.GetExecutingAssembly());
```

## Performance Considerations

- **Assembly Scanning**: Occurs once during container build, minimal runtime impact
- **Reflection Overhead**: Attribute discovery uses efficient caching
- **Registration Performance**: Equivalent to manual registration after container build
- **Memory Usage**: Negligible overhead compared to manual registration

## Summary

Component Model Attributes provide a clean, maintainable approach to dependency injection in Autofac-based applications. By decorating your service classes with `[Singleton]`, `[Scoped]`, or `[Transient]` attributes, you can:

- Eliminate boilerplate container configuration code
- Ensure consistent lifetime management
- Improve code readability and maintainability  
- Reduce registration errors and omissions
- Support automatic interface registration with sensible fallbacks

The system automatically handles interface detection, lifetime management, and registration during the `RegisterFoundation()` call, making dependency injection configuration both simpler and more reliable.
