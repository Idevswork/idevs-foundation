using System.Diagnostics;
using IdevsWork.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Logging;

namespace IdevsWork.Foundation.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that logs request and response information.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString()[..8];
        
        _logger.LogInformation("Handling {RequestName} [{RequestId}]", requestName, requestId);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            _logger.LogInformation("Completed {RequestName} [{RequestId}] in {ElapsedMs}ms", 
                requestName, requestId, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed {RequestName} [{RequestId}] after {ElapsedMs}ms: {ErrorMessage}", 
                requestName, requestId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
