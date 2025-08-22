# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Repository Overview

**IdevsWork.Foundation** is a comprehensive .NET 8.0 foundation framework that provides essential building blocks for modern applications with CQRS patterns, Entity Framework integration, centralized logging, and dependency injection abstractions.

### Core Architecture

The framework follows a layered, modular architecture with the following key components:

#### **Foundation Layers:**
- **Abstractions Layer** (`IdevsWork.Foundation.Abstractions`) - Core interfaces and contracts
- **Entity Framework Layer** (`IdevsWork.Foundation.EntityFramework`) - EF Core implementations with audit trails and soft deletion
- **CQRS Layer** (`IdevsWork.Foundation.Cqrs`) - Command Query Responsibility Segregation patterns
- **Mediator Layer** (`IdevsWork.Foundation.Mediator`) - Mediator pattern implementation
- **Services Layer** (`IdevsWork.Foundation.Services`) - Generic service implementations with CQRS support
- **Infrastructure Layers** - Autofac DI integration, Serilog logging integration

#### **Key Patterns:**
- **Entity CQRS Pattern**: Unified `EntityCommand<TDto, TId>` and `EntityQuery<TDto, TId>` for all entity operations
- **Repository Pattern**: Generic repositories with audit trails, soft deletion, and expression-based querying
- **Service Base Pattern**: `ServiceBase` class with built-in mediator integration and structured logging
- **Mapper Pattern**: `IMapper<TEntity, TDto>` for entity-DTO conversions

## Essential Commands

### Build Commands

```bash
# Build entire solution
dotnet build IdevsWork.Foundation.sln

# Build with specific configuration
dotnet build --configuration Release
dotnet build --configuration Debug

# Build consolidated package (recommended for consumers)
./build-consolidated-package.sh [Release|Debug]

# Build individual packages (for granular dependency management)
./build-individual-packages.sh [Release|Debug]

# Restore NuGet packages
dotnet restore
```

### Testing Commands

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/IdevsWork.Foundation.Tests/IdevsWork.Foundation.Tests.csproj
```

### Package Management

```bash
# Pack consolidated package
dotnet pack src/IdevsWork.Foundation/IdevsWork.Foundation.csproj --configuration Release

# Pack individual packages
dotnet build --configuration Release -p:GenerateIndividualPackages=true

# Clean build artifacts
rm -rf artifacts/*.nupkg

# List generated packages
ls -la artifacts/*.nupkg
```

## Development Workflow

### Working with Entity CQRS

When implementing new entities, follow this pattern:

1. **Create Entity** (inheriting from `SoftDeletableEntity<TId>` or appropriate base)
2. **Create DTO** (implementing `IHasId<TId>`)
3. **Create Mapper** (inheriting from `BaseMapper<TEntity, TDto>`)
4. **Use Generic Handlers** - `EntityCommandHandler<TEntity, TDto, TId>` and `EntityQueryHandler<TEntity, TDto, TId>`

### Service Development

Services should inherit from `ServiceBase` to get:
- Built-in mediator integration via `SendCommandAsync<TCommand, TResponse>()` and `SendQueryAsync<TQuery, TResponse>()`
- Structured logging with `ExecuteWithLoggingAsync()`
- Logger access through `Logger` property or `ILogManager` integration

### Logging Integration

The framework provides centralized logging through:
- `ILogManager` for dependency injection scenarios
- Static `Log` class for utility and static method access
- `ServiceBase` integration for automatic operation logging

Use `services.AddFoundationLoggingWithStaticAccess()` for full logging capabilities.

### Testing Strategy

- Unit tests use xUnit framework
- Mock `ILogManager` and repository interfaces for service testing
- Test projects target .NET 9.0 for latest testing features
- Use in-memory databases for integration testing Entity Framework repositories

## Project Structure Understanding

### Packaging Strategy

The framework supports **dual packaging**:
- **Consolidated Package** (`IdevsWork.Foundation`) - Single package containing all components
- **Individual Packages** - Granular packages for specific functionality

Package generation is controlled by MSBuild properties in `Directory.Build.props`:
- `GenerateIndividualPackages=true` builds all individual packages
- Default behavior builds only the consolidated package

### Configuration Files

- **Directory.Build.props** - Global MSBuild properties for all projects
- **Directory.Packages.props** - Centralized NuGet package version management
- **Solution Structure** - Clean separation of source (`src/`) and test (`tests/`) projects

### Key Dependencies

- **.NET 8.0** - Primary target framework
- **Entity Framework Core 8.0** - Data access layer
- **Microsoft Extensions** - Logging, DI, Configuration (v9.0.0)
- **Autofac** - Advanced dependency injection scenarios
- **Serilog** - Structured logging implementation
- **xUnit** - Testing framework

## Entity Development Examples

### Basic Entity Setup
```csharp
// Entity
public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    // ... other properties
}

// DTO
public class ProductDto : IHasId<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    // ... matching properties
}

// Service using CQRS
public class ProductService : ServiceBase
{
    public async Task<EntityCommandResponse<ProductDto>> CreateAsync(ProductDto dto)
    {
        return await CreateEntityAsync<ProductDto, int>(dto);
    }
}
```

### Repository Implementation
```csharp
public class ProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext context, ILogger<ProductRepository> logger) 
        : base(context, logger) { }
        
    public async Task<List<Product>> GetActiveAsync()
    {
        return await QueryAsync(p => p.IsActive && !p.IsDeleted);
    }
}
```

## Debugging and Development Tips

### Common Issues

1. **Package Reference Conflicts**: Use centralized package management via `Directory.Packages.props`
2. **Logging Not Working**: Ensure proper DI setup with `AddFoundationLoggingWithStaticAccess()`
3. **Entity Mapping Issues**: Verify DTO implements `IHasId<TId>` and mapper logic is correct
4. **CQRS Handler Registration**: Use Autofac extensions for automatic handler registration

### Performance Considerations

- Entity queries support caching via `ICacheableQuery` interface
- Repository pattern includes both tracking and non-tracking query methods
- Bulk operations are supported for better performance with multiple entities
- Structured logging includes execution time tracking

### Extension Points

The framework is designed for extensibility:
- Custom entity base classes beyond `SoftDeletableEntity<TId>`
- Custom CQRS command/query implementations
- Custom repository implementations with specialized query methods
- Custom mapper implementations for complex entity-DTO relationships

<citations>
<document>
<document_type>WARP_DOCUMENTATION</document_type>
<document_id>SUMMARY</document_id>
</document>
</citations>
