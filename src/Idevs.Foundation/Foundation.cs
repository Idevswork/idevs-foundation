using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Foundation;

/// <summary>
/// Main entry point for Idevs Foundation framework.
/// Provides easy access to all Foundation functionality.
/// </summary>
public static class Foundation
{
    /// <summary>
    /// Gets the current version of the Foundation framework.
    /// </summary>
    public static string Version => "1.0.0";
    
    /// <summary>
    /// Gets information about the Foundation framework.
    /// </summary>
    public static string Description => "Complete foundation framework for building modern .NET applications with CQRS, Entity Framework, Mediator patterns, and more";
    
    /// <summary>
    /// Gets the list of included components.
    /// </summary>
    public static string[] Components => new[]
    {
        "Idevs.Foundation.Abstractions",
        "Idevs.Foundation.Services", 
        "Idevs.Foundation.Mediator",
        "Idevs.Foundation.Cqrs",
        "Idevs.Foundation.EntityFramework",
        "Idevs.Foundation.Serilog",
        "Idevs.Foundation.Autofac"
    };
}
