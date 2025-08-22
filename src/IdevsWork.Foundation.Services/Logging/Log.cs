using IdevsWork.Foundation.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace IdevsWork.Foundation.Services.Logging;

/// <summary>
/// Static log manager for easy access to loggers throughout the application.
/// Must be initialized before use by calling <see cref="Initialize"/>.
/// </summary>
public static class Log
{
    private static ILogManager? _logManager;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets a value indicating whether the log manager has been initialized.
    /// </summary>
    public static bool IsInitialized => _logManager is not null;

    /// <summary>
    /// Initializes the static log manager with the specified log manager instance.
    /// </summary>
    /// <param name="logManager">The log manager instance to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when logManager is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the log manager has already been initialized.</exception>
    public static void Initialize(ILogManager logManager)
    {
        ArgumentNullException.ThrowIfNull(logManager);
        
        lock (_lock)
        {
            if (_logManager is not null)
                throw new InvalidOperationException("Log manager has already been initialized.");
            
            _logManager = logManager;
        }
    }

    /// <summary>
    /// Resets the static log manager. This is primarily for testing purposes.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _logManager = null;
        }
    }

    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>A logger instance for the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the log manager has not been initialized.</exception>
    public static ILogger<T> GetLogger<T>()
    {
        EnsureInitialized();
        return _logManager!.GetLogger<T>();
    }

    /// <summary>
    /// Gets a logger with the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A logger instance with the specified category name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the log manager has not been initialized.</exception>
    public static ILogger GetLogger(string categoryName)
    {
        EnsureInitialized();
        return _logManager!.GetLogger(categoryName);
    }

    /// <summary>
    /// Gets a logger for the specified type.
    /// </summary>
    /// <param name="type">The type to create a logger for.</param>
    /// <returns>A logger instance for the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the log manager has not been initialized.</exception>
    public static ILogger GetLogger(Type type)
    {
        EnsureInitialized();
        return _logManager!.GetLogger(type);
    }

    /// <summary>
    /// Gets a logger for the calling type. Uses reflection to determine the calling type.
    /// </summary>
    /// <returns>A logger instance for the calling type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the log manager has not been initialized or the calling type cannot be determined.</exception>
    public static ILogger GetCurrentLogger()
    {
        EnsureInitialized();
        
        var frame = new System.Diagnostics.StackFrame(1, false);
        var method = frame.GetMethod();
        var type = method?.DeclaringType;
        
        if (type is null)
            throw new InvalidOperationException("Could not determine the calling type for logger creation.");
        
        return _logManager!.GetLogger(type);
    }

    private static void EnsureInitialized()
    {
        if (_logManager is null)
            throw new InvalidOperationException("Log manager has not been initialized. Call Log.Initialize() first.");
    }
}
