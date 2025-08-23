# Idevs Foundation Packaging Strategy

## Overview

The Idevs.Foundation repository now supports both individual component packages and a single consolidated package, providing flexibility for different use cases.

## Consolidated Package (Recommended)

### Package: `Idevs.Foundation`

A single NuGet package containing all Foundation components as assemblies in the `lib/net8.0/` folder:

- **Idevs.Foundation.dll** - Main entry point and utilities
- **Idevs.Foundation.Abstractions.dll** - Core abstractions and interfaces  
- **Idevs.Foundation.Services.dll** - Base service implementations
- **Idevs.Foundation.Mediator.dll** - Mediator pattern implementation
- **Idevs.Foundation.Cqrs.dll** - CQRS abstractions and implementations
- **Idevs.Foundation.EntityFramework.dll** - Entity Framework integration
- **Idevs.Foundation.Serilog.dll** - Serilog logging integration
- **Idevs.Foundation.Autofac.dll** - Autofac DI container integration

### Benefits

✅ **Single dependency** - Only one package reference needed
✅ **No version conflicts** - All components guaranteed compatible
✅ **Simpler deployment** - Single NuGet package to manage
✅ **Complete functionality** - All Foundation features included
✅ **External dependencies only** - No internal package dependencies

### Usage

```bash
dotnet add package Idevs.Foundation
```

This automatically includes all Foundation assemblies and their external dependencies.

### Build Command

```bash
./build-consolidated-package.sh [Release|Debug]
```

## Individual Packages (Alternative)

For scenarios where you only need specific Foundation components, you can still build individual packages.

### Available Packages

- `Idevs.Foundation.Abstractions`
- `Idevs.Foundation.Services` 
- `Idevs.Foundation.Mediator`
- `Idevs.Foundation.Cqrs`
- `Idevs.Foundation.EntityFramework`
- `Idevs.Foundation.Serilog`
- `Idevs.Foundation.Autofac`

### Build Command

```bash
./build-individual-packages.sh [Release|Debug]
```

## Configuration Details

### Package Generation Control

The build system uses conditional package generation:

```xml
<GeneratePackageOnBuild Condition="'$(PackageId)' == 'Idevs.Foundation' OR '$(GenerateIndividualPackages)' == 'true'">true</GeneratePackageOnBuild>
<GeneratePackageOnBuild Condition="'$(PackageId)' != 'Idevs.Foundation' AND '$(GenerateIndividualPackages)' != 'true'">false</GeneratePackageOnBuild>
```

### Default Behavior

- **Default build**: Only generates the consolidated package
- **With `-p:GenerateIndividualPackages=true`**: Generates all individual packages

### Version Management

All packages use centralized version management via:
- `Directory.Build.props` - Common properties and versioning
- `Directory.Packages.props` - Centralized NuGet package versions

## Recommendation

**Use the consolidated package (`Idevs.Foundation`)** unless you have specific requirements for individual components. This provides the best developer experience with simplified dependency management and guaranteed compatibility.
