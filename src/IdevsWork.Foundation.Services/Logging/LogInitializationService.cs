using IdevsWork.Foundation.Abstractions.Logging;
using Microsoft.Extensions.Hosting;

namespace IdevsWork.Foundation.Services.Logging;

/// <summary>
/// Background service responsible for initializing the static Log class.
/// </summary>
internal class LogInitializationService : IHostedService
{
    private readonly ILogManager _logManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogInitializationService"/> class.
    /// </summary>
    /// <param name="logManager">The log manager instance to use for initialization.</param>
    public LogInitializationService(ILogManager logManager)
    {
        _logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
    }

    /// <summary>
    /// Starts the service and initializes the static Log class.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Log.IsInitialized)
        {
            Log.Initialize(_logManager);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the service and optionally resets the static Log class.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Optionally reset the static Log class when the service stops
        // This is useful for testing scenarios or application shutdown
        return Task.CompletedTask;
    }
}
