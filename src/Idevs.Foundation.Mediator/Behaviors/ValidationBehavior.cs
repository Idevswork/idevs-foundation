using Idevs.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that provides validation functionality for requests that implement IValidatable.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IValidatable
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ValidationBehavior(ILogger<ValidationBehavior<TRequest, TResponse>> logger)
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
        
        _logger.LogDebug("Validating {RequestName}", requestName);

        var validationResult = request.Validate();

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for {RequestName} with {ErrorCount} error(s): {Errors}",
                requestName, 
                validationResult.Errors.Count,
                string.Join(", ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            throw new ValidationException(validationResult.Errors);
        }

        _logger.LogDebug("Validation passed for {RequestName}", requestName);

        return await next();
    }
}