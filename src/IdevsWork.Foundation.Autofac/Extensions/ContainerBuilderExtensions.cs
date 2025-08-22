using System.Reflection;
using Autofac;
using IdevsWork.Foundation.Autofac.Modules;
using IdevsWork.Foundation.Serilog.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace IdevsWork.Foundation.Autofac.Extensions;

/// <summary>
/// Extension methods for <see cref="ContainerBuilder"/> to simplify Foundation registration.
/// </summary>
public static class ContainerBuilderExtensions
{
    /// <summary>
    /// Registers IdevsWork Foundation components with CQRS and mediator support.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <param name="registerLoggingBehavior">Whether to register the logging behavior.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterFoundation(
        this ContainerBuilder builder,
        Assembly[] assemblies,
        bool registerLoggingBehavior = true)
    {
        builder.RegisterModule(new FoundationModule(assemblies, registerLoggingBehavior));
        return builder;
    }

    /// <summary>
    /// Registers IdevsWork Foundation components with CQRS and mediator support.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="assembly">Assembly to scan for handlers and behaviors.</param>
    /// <param name="registerLoggingBehavior">Whether to register the logging behavior.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterFoundation(
        this ContainerBuilder builder,
        Assembly assembly,
        bool registerLoggingBehavior = true)
    {
        builder.RegisterModule(new FoundationModule(assembly, registerLoggingBehavior));
        return builder;
    }

    /// <summary>
    /// Registers IdevsWork Foundation components with CQRS and mediator support for the calling assembly.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="registerLoggingBehavior">Whether to register the logging behavior.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterFoundation(
        this ContainerBuilder builder,
        bool registerLoggingBehavior = true)
    {
        var assembly = Assembly.GetCallingAssembly();
        builder.RegisterModule(new FoundationModule(assembly, registerLoggingBehavior));
        return builder;
    }

    /// <summary>
    /// Registers Serilog logging with Foundation defaults.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="configuration">The configuration to read Serilog settings from.</param>
    /// <param name="configurationAction">Optional action to customize logger configuration.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterSerilogLogging(
        this ContainerBuilder builder,
        IConfiguration configuration,
        Action<LoggerConfiguration>? configurationAction = null)
    {
        var loggerConfiguration = SerilogExtensions.CreateFoundationLogger(configuration);
        configurationAction?.Invoke(loggerConfiguration);
        
        Log.Logger = loggerConfiguration.CreateLogger();
        
        builder.RegisterInstance(Log.Logger).As<ILogger>().SingleInstance();
        builder.RegisterGeneric(typeof(Microsoft.Extensions.Logging.Logger<>))
            .As(typeof(Microsoft.Extensions.Logging.ILogger<>))
            .InstancePerLifetimeScope();
            
        return builder;
    }

    /// <summary>
    /// Registers Foundation components with Serilog logging in one call.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="configuration">The configuration to read Serilog settings from.</param>
    /// <param name="assembly">Assembly to scan for handlers and behaviors.</param>
    /// <param name="serilogConfigurationAction">Optional action to customize logger configuration.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterFoundationWithSerilog(
        this ContainerBuilder builder,
        IConfiguration configuration,
        Assembly assembly,
        Action<LoggerConfiguration>? serilogConfigurationAction = null)
    {
        builder.RegisterSerilogLogging(configuration, serilogConfigurationAction);
        builder.RegisterFoundation(assembly);
        return builder;
    }

    /// <summary>
    /// Registers Foundation components with Serilog logging for multiple assemblies.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="configuration">The configuration to read Serilog settings from.</param>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <param name="serilogConfigurationAction">Optional action to customize logger configuration.</param>
    /// <returns>The container builder for chaining.</returns>
    public static ContainerBuilder RegisterFoundationWithSerilog(
        this ContainerBuilder builder,
        IConfiguration configuration,
        Assembly[] assemblies,
        Action<LoggerConfiguration>? serilogConfigurationAction = null)
    {
        builder.RegisterSerilogLogging(configuration, serilogConfigurationAction);
        builder.RegisterFoundation(assemblies);
        return builder;
    }
}
