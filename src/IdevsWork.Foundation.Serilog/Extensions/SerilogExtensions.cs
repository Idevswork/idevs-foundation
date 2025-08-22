using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace IdevsWork.Foundation.Serilog.Extensions;

/// <summary>
/// Extensions for configuring Serilog logging
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog with default settings
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <param name="configurationAction">Optional configuration action</param>
    /// <returns>The host builder</returns>
    public static IHostBuilder UseSerilogLogging(
        this IHostBuilder hostBuilder,
        Action<LoggerConfiguration>? configurationAction = null)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Apply custom configuration if provided
            configurationAction?.Invoke(configuration);
        });
    }

    /// <summary>
    /// Configures Serilog with structured logging for web applications
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <param name="configurationAction">Optional configuration action</param>
    /// <returns>The host builder</returns>
    public static IHostBuilder UseSerilogForWebApp(
        this IHostBuilder hostBuilder,
        Action<LoggerConfiguration>? configurationAction = null)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: $"logs/{context.HostingEnvironment.ApplicationName}/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

            // Set minimum level based on environment
            var minimumLevel = context.HostingEnvironment.IsProduction() 
                ? LogEventLevel.Warning 
                : LogEventLevel.Debug;
            configuration.MinimumLevel.Is(minimumLevel);

            // Apply custom configuration if provided
            configurationAction?.Invoke(configuration);
        });
    }

    /// <summary>
    /// Adds Serilog to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="configurationAction">Optional configuration action</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSerilogLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<LoggerConfiguration>? configurationAction = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();

        // Apply custom configuration if provided
        configurationAction?.Invoke(loggerConfiguration);

        Log.Logger = loggerConfiguration.CreateLogger();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        return services;
    }

    /// <summary>
    /// Creates a logger configuration with Foundation defaults
    /// </summary>
    /// <param name="configuration">Optional base configuration</param>
    /// <returns>Logger configuration</returns>
    public static LoggerConfiguration CreateFoundationLogger(IConfiguration? configuration = null)
    {
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/foundation-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        if (configuration != null)
        {
            loggerConfig.ReadFrom.Configuration(configuration);
        }

        return loggerConfig;
    }
}
