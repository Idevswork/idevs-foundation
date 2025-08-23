using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Abstractions.Logging;

/// <summary>
/// Provides a centralized way to create and manage loggers.
/// </summary>
public interface ILogManager
{
    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>A logger instance for the specified type.</returns>
    ILogger<T> GetLogger<T>();
    
    /// <summary>
    /// Gets a logger with the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A logger instance with the specified category name.</returns>
    ILogger GetLogger(string categoryName);
    
    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    /// <param name="type">The type to create a logger for.</param>
    /// <returns>A logger instance for the specified type.</returns>
    ILogger GetLogger(Type type);
}
