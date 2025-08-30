# Changelog

All notable changes to the Idevs.Foundation project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.11] - 2025-08-30

### Fixed
- Fixed dependency

## [0.2.10] - 2025-08-30

### Fixed
- Fixed version number

## [0.2.9] - 2025-08-30

### Added
- Added `IBaseMapper` interface for mapping entities and DTOs
- Added `IdevController` base controller for API controllers

## [0.2.8] - 2025-08-30

### Fixed
- Fixed infinite recursion in `Entity<TId>.Equals(Entity<TId>? other)` method
- Corrected equality comparison to use `EqualityComparer<TId>` instead of `EqualityComparer<Entity<TId>>`
- Improved entity equality logic to properly compare ID properties only

### Changed
- Enhanced entity equality implementation for better performance and correctness

## [0.2.6] - 2025-08-29

### Fixed
- Fixed release version

## [0.2.5] - 2025-01-XX

### Fixed
- Fixed infinite recursion in `Entity<TId>.Equals(Entity<TId>? other)` method
- Corrected equality comparison to use `EqualityComparer<TId>` instead of `EqualityComparer<Entity<TId>>`
- Improved entity equality logic to properly compare ID properties only

### Changed
- Enhanced entity equality implementation for better performance and correctness

## [0.2.4] - Previous Release

### Added
- **LogManager functionality** for easy logger access throughout applications
  - `ILogManager` interface for centralized logger creation
  - `LogManager` implementation with ILoggerFactory integration
  - Static `Log` class for global logger access without dependency injection
  - `LogInitializationService` for automatic static logger initialization in DI scenarios
  - Extension methods `AddFoundationLogging()` and `AddFoundationLoggingWithStaticAccess()`
  - Support for typed loggers, category-based loggers, and automatic calling-type detection
  - Comprehensive unit tests and documentation (docs/LOGGING.md)
- Enhanced `ServiceBase` with alternate constructor accepting `ILogManager`
- Documentation for packaging strategy in PACKAGING.md
- Build scripts for consolidated and individual package generation

### Changed
- Updated Microsoft.Extensions.Logging packages to version 9.0.0 for compatibility
- Updated Microsoft.Extensions.DependencyInjection.Abstractions to version 9.0.0
- Enhanced Services project with additional logging infrastructure

### Deprecated

### Removed

### Fixed

### Security
- Updated GitVersion.MsBuild from 6.0.2 to 6.0.3 to address security vulnerabilities

### Added
- **Core Foundation Framework Components:**
  - `Idevs.Foundation.Abstractions` - Core abstractions and interfaces for entities, repositories, and services
  - `Idevs.Foundation.Services` - Base service implementations with dependency injection support
  - `Idevs.Foundation.Mediator` - Mediator pattern implementation for request/response handling
  - `Idevs.Foundation.Cqrs` - Command Query Responsibility Segregation abstractions and implementations
  - `Idevs.Foundation.EntityFramework` - Entity Framework Core integration and repository implementations
  - `Idevs.Foundation.Serilog` - Structured logging integration with Serilog
  - `Idevs.Foundation.Autofac` - Dependency injection container integration with Autofac

- **Entity CQRS Pattern Support:**
  - Entity command and query abstractions (`EntityCommand`, `EntityQuery`)
  - Command and query handlers with built-in validation, logging, and mapping
  - Support for Create, Update, Delete, Get, and List operations
  - Request/response models with proper data transfer object patterns
  - Caching support for query operations

- **Repository Pattern Implementation:**
  - Generic repository interfaces and Entity Framework implementations
  - Unit of Work pattern support
  - Async/await support throughout
  - Specification pattern for complex queries

- **Service Layer Architecture:**
  - Base service class with common functionality
  - Mediator integration for CQRS operations
  - Structured logging support
  - Dependency injection abstractions

- **Infrastructure Integrations:**
  - Entity Framework Core repository implementations
  - Serilog structured logging configuration
  - Autofac container module registrations
  - Microsoft Extensions compatibility

- **Packaging Strategy:**
  - Consolidated NuGet package containing all Foundation components
  - Individual component packages for granular dependency management
  - Centralized package version management
  - Build scripts for different packaging scenarios

- **Development Tools:**
  - Solution-wide build configuration with Directory.Build.props
  - Centralized NuGet package management with Directory.Packages.props
  - Unit test project structure
  - Documentation and examples

### Changed
- N/A (initial release)

### Security
- Updated Microsoft.Extensions.Caching.Memory to version 8.0.1 to address security vulnerability
- Implemented centralized package management to ensure consistent security updates

## [0.0.0] - Development

### Added
- Initial project structure and build configuration
- Basic abstractions and service patterns
- Integration with real-estate platform CQRS patterns

---

## Version History Reference

- **[1.0.0]** - Initial stable release with complete Foundation framework
- **[Unreleased]** - Current development version

[unreleased]: https://github.com/Idevswork/idevs-foundation/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Idevswork/idevs-foundation/releases/tag/v1.0.0
