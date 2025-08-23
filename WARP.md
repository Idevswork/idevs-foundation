# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Repository Overview

**Idevs.Foundation** is a comprehensive .NET 8.0 foundation framework that provides essential building blocks for modern applications with CQRS patterns, Entity Framework integration, centralized logging, and dependency injection abstractions.

### Core Architecture

The framework follows a layered, modular architecture with the following key components:

#### **Foundation Layers:**
- **Abstractions Layer** (`Idevs.Foundation.Abstractions`) - Core interfaces and contracts
- **Entity Framework Layer** (`Idevs.Foundation.EntityFramework`) - EF Core implementations with audit trails and soft deletion
- **CQRS Layer** (`Idevs.Foundation.Cqrs`) - Command Query Responsibility Segregation patterns
- **Mediator Layer** (`Idevs.Foundation.Mediator`) - Mediator pattern implementation
- **Services Layer** (`Idevs.Foundation.Services`) - Generic service implementations with CQRS support
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
dotnet build Idevs.Foundation.sln

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

### CI/CD Commands

```bash
# Check workflow status
gh workflow list
gh workflow view ci-cd.yml

# Trigger manual workflow run (if configured)
gh workflow run ci-cd.yml

# View recent workflow runs
gh run list --workflow=ci-cd.yml

# Check latest run status
gh run view --log
```

### Testing Commands

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Idevs.Foundation.Tests/Idevs.Foundation.Tests.csproj

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests for a specific class
dotnet test --filter "FullyQualifiedName~LogManagerTests"

# Run tests with live coverage (watch mode)
dotnet watch test --project tests/Idevs.Foundation.Tests/
```

### Package Management

```bash
# Pack consolidated package
dotnet pack src/Idevs.Foundation/Idevs.Foundation.csproj --configuration Release

# Pack individual packages
dotnet build --configuration Release -p:GenerateIndividualPackages=true

# Clean build artifacts
rm -rf artifacts/*.nupkg

# List generated packages
ls -la artifacts/*.nupkg

# Inspect package contents
unzip -l artifacts/Idevs.Foundation.*.nupkg | grep "\.dll"
```

### GitFlow Commands

The repository includes GitFlow helper scripts for structured branch management:

```bash
# Feature branches
./scripts/gitflow.sh feature start user-authentication
./scripts/gitflow.sh feature finish user-authentication

# Release branches
./scripts/gitflow.sh release start 1.2.0
./scripts/gitflow.sh release finish 1.2.0

# Hotfix branches  
./scripts/gitflow.sh hotfix start 1.2.1
./scripts/gitflow.sh hotfix finish 1.2.1

# Check status and branches
./scripts/gitflow.sh status

# Setup Git aliases (one-time setup)
./scripts/setup-git-aliases.sh

# Use Git aliases after setup
git feature-start user-authentication
git feature-finish user-authentication
git release-start 1.2.0
git sync  # Sync main and develop branches
```

### Development Debugging Commands

```bash
# Watch for file changes and rebuild
dotnet watch build

# Check for outdated packages
dotnet list package --outdated

# Analyze package dependencies
dotnet list package --include-transitive

# Format code
dotnet format

# Analyze code quality
dotnet build --verbosity diagnostic 2>&1 | grep -i warning

# Check project references
dotnet list reference

# Clean solution thoroughly
dotnet clean && rm -rf */bin */obj artifacts/*
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
- Test projects target .NET 8.0 to match the solution framework
- Use in-memory databases for integration testing Entity Framework repositories

## Project Structure Understanding

### Packaging Strategy

The framework supports **dual packaging**:
- **Consolidated Package** (`Idevs.Foundation`) - Single package containing all components
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

## CI/CD Pipeline

The repository includes a comprehensive GitHub Actions CI/CD pipeline:

### Workflow Overview

1. **Build and Test** - Runs on all pushes and pull requests
   - Builds solution with .NET 8.0
   - Runs unit tests with code coverage
   - Uploads test results and coverage reports

2. **Package Creation** - Automated semantic versioning and package generation
   - Uses GitVersion for automatic version calculation
   - Builds consolidated package for all branches
   - Builds individual packages for main branch and releases

3. **Publishing Strategy**:
   - **Preview** (develop branch) → GitHub Packages with alpha versions
   - **Release** (main branch) → NuGet.org and GitHub Packages
   - **GitHub Releases** → Automatic package attachment

### Branch Strategy (GitFlow)

- `main` - Production releases (publishes to NuGet.org)
- `develop` - Development integration (publishes preview packages)
- `feature/*` - Feature development branches
- `release/*` - Release preparation branches
- `hotfix/*` - Emergency fixes

### Semantic Versioning

Versions are automatically calculated based on branch and commit messages:

```bash
# Commit message examples for version control
git commit -m "feat: new feature +semver: minor"
git commit -m "fix: bug fix +semver: patch"
git commit -m "feat!: breaking change +semver: major"
git commit -m "docs: update docs +semver: none"
```

### Security and Quality

- **CodeQL Analysis** - Weekly security scans
- **Dependabot** - Automated dependency updates
- **Code Coverage** - Integrated with Codecov
- **Environment Protection** - Production deployments require approval

### Setup Requirements

To enable full CI/CD functionality, configure these repository secrets:
- `NUGET_API_KEY` - For publishing to NuGet.org
- `CODECOV_TOKEN` - For code coverage reports (optional)

See `.github/README.md` for detailed setup instructions.

<citations>
<document>
<document_type>WARP_DOCUMENTATION</document_type>
<document_id>SUMMARY</document_id>
</document>
</citations>
