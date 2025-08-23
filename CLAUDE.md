# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Idevs.Foundation is a comprehensive .NET foundation framework that provides building blocks for modern applications. The repository follows a dual packaging strategy:

1. **Consolidated Package (Recommended)**: `Idevs.Foundation` - Single NuGet package containing all components
2. **Individual Packages**: Separate packages for each component when granular dependencies are needed

## Architecture

The codebase is organized into modular components under `src/`:

- **Idevs.Foundation.Abstractions**: Core interfaces and contracts (IAuditableEntity, IRepositoryBase, IUnitOfWork, IServiceBase)
- **Idevs.Foundation.EntityFramework**: EF Core implementations (RepositoryBase, Entity base classes)
- **Idevs.Foundation.Cqrs**: CQRS abstractions (ICommand, IQuery, ICommandHandler, IQueryHandler)
- **Idevs.Foundation.Mediator**: Mediator pattern implementation with pipeline behaviors
- **Idevs.Foundation.Services**: Service layer implementations (ServiceBase, CqrsGenericService, LogManager)
- **Idevs.Foundation.Autofac**: Autofac DI container integration
- **Idevs.Foundation.Serilog**: Serilog logging integration with Foundation-specific extensions
- **Idevs.Foundation**: Main consolidated package that references all components

## Key Patterns

### Entity Hierarchy
- `Entity<TId>`: Base entity with identifier
- `AuditableEntity<TId>`: Adds creation/update timestamps
- `SoftDeletableEntity<TId>`: Adds soft delete functionality

### Service Layer
- `ServiceBase`: Base class with logging and mediator integration
- `CqrsGenericService<TEntity, TId>`: Generic CQRS operations for entities

### Logging Strategy
- Centralized `ILogManager` interface with static `Log` class access
- Structured logging for all CQRS operations via `ServiceBase`
- Serilog integration with automatic configuration

## Common Development Commands

### Building
```bash
# Build consolidated package (recommended)
./build-consolidated-package.sh [Release|Debug]

# Build individual packages
./build-individual-packages.sh [Release|Debug]

# Standard .NET build
dotnet build
dotnet build --configuration Release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Idevs.Foundation.Tests/
```

### Package Management
```bash
# Clean artifacts
rm -rf artifacts/*.nupkg

# List generated packages
ls -la artifacts/

# Inspect package contents
unzip -l artifacts/Idevs.Foundation.*.nupkg
```

## Development Guidelines

### Project Structure
- All source code lives under `src/`
- Tests are in `tests/` with corresponding project structure
- Build scripts are at root level
- Documentation in `docs/` folder

### Versioning
- Centralized version management via `Directory.Build.props`
- Version format: `1.0.0` for releases, `1.0.0-alpha` for debug builds
- All packages share the same version number

### Package Generation
- Default: Only consolidated package is generated
- Use `-p:GenerateIndividualPackages=true` to build individual packages
- Package output goes to `artifacts/` folder

### Testing Framework
- Uses xUnit for unit testing
- Test projects reference specific Foundation components
- Coverage tools included (coverlet.collector)

### Code Conventions
- .NET 8.0 target framework
- Nullable reference types enabled
- Implicit usings enabled
- Latest C# language version
- Warnings treated as errors