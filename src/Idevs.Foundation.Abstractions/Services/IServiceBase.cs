using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Abstractions.Services;

/// <summary>
/// Base interface for services with common functionality
/// </summary>
public interface IServiceBase
{
    /// <summary>
    /// The logger instance for this service
    /// </summary>
    ILogger Logger { get; }
}
