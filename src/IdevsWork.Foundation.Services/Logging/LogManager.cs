using IdevsWork.Foundation.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace IdevsWork.Foundation.Services.Logging;

/// <summary>
/// Default implementation of ILogManager that provides centralized logger creation.
/// </summary>
public class LogManager : ILogManager
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogManager"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use for creating loggers.</param>
    public LogManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public ILogger<T> GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    /// <inheritdoc />
    public ILogger GetLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }

    /// <inheritdoc />
    public ILogger GetLogger(Type type)
    {
        return _loggerFactory.CreateLogger(type);
    }
}
