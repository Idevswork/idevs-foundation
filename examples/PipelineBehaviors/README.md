# Pipeline Behaviors Examples

This directory contains examples demonstrating how to use the various pipeline behaviors in Idevs.Foundation.

## Examples Included

### 1. UserService Example (`UserService.cs`)

Demonstrates:
- Caching behavior for read operations
- Validation behavior for input validation
- Retry behavior for external service calls
- Transaction behavior for data modifications

### 2. OrderService Example (`OrderService.cs`)

Demonstrates:
- Complex transaction scenarios
- Combining multiple behaviors
- Error handling patterns

### 3. NotificationService Example (`NotificationService.cs`)

Demonstrates:
- Retry patterns for external services
- Different retry policies
- Custom retry conditions

## Running the Examples

The examples are designed to be integrated into your existing application. Simply:

1. Copy the relevant patterns to your codebase
2. Ensure you have the required NuGet packages installed
3. Register the behaviors in your DI container
4. Implement the behavior interfaces on your requests

## Key Patterns Demonstrated

- **Early Validation**: Validate input before processing
- **Smart Caching**: Cache read operations with appropriate TTL
- **Resilient Retries**: Handle transient failures gracefully  
- **Transaction Safety**: Ensure data consistency with automatic rollbacks
- **Observability**: Comprehensive logging for all operations

These examples follow the recommended practices outlined in the main documentation and provide a starting point for implementing similar patterns in your applications.