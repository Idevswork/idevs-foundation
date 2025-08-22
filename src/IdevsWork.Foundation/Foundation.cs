using Microsoft.Extensions.DependencyInjection;

namespace IdevsWork.Foundation;

/// <summary>
/// Main entry point for IdevsWork Foundation framework.
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
        "IdevsWork.Foundation.Abstractions",
        "IdevsWork.Foundation.Services", 
        "IdevsWork.Foundation.Mediator",
        "IdevsWork.Foundation.Cqrs",
        "IdevsWork.Foundation.EntityFramework",
        "IdevsWork.Foundation.Serilog",
        "IdevsWork.Foundation.Autofac"
    };
}
