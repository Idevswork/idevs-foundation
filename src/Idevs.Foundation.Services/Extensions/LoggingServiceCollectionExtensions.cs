using Idevs.Foundation.Abstractions.Logging;
using Idevs.Foundation.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Idevs.Foundation.Services.Extensions;

/// <summary>
/// Extension methods for configuring logging services in the dependency injection container.
/// </summary>
public static class LoggingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Foundation logging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFoundationLogging(this IServiceCollection services)
    {
        services.TryAddSingleton<ILogManager, LogManager>();
        
        return services;
    }
    
    /// <summary>
    /// Adds the Foundation logging services and initializes the static Log class.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFoundationLoggingWithStaticAccess(this IServiceCollection services)
    {
        services.AddFoundationLogging();
        
        // Register a hosted service to initialize the static Log class
        services.AddHostedService<LogInitializationService>();
        
        return services;
    }
}
