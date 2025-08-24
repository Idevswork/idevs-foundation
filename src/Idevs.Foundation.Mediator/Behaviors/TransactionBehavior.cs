using System.Data;
using System.Transactions;
using Idevs.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that provides transaction functionality for requests that implement ITransactional.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, ITransactional
{
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public TransactionBehavior(ILogger<TransactionBehavior<TRequest, TResponse>> logger)
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

        // Check if we're already in a transaction scope
        if (Transaction.Current != null)
        {
            _logger.LogDebug("Transaction already exists for {RequestName}, using existing transaction", requestName);
            return await next();
        }

        var timeout = request.Timeout ?? DefaultTimeout;
        var isolationLevel = MapToTransactionScopeIsolationLevel(request.IsolationLevel);

        var transactionOptions = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            Timeout = timeout
        };

        _logger.LogDebug("Starting transaction for {RequestName} with isolation level {IsolationLevel} and timeout {Timeout}",
            requestName, isolationLevel, timeout);

        using var transactionScope = new TransactionScope(
            TransactionScopeOption.Required,
            transactionOptions,
            TransactionScopeAsyncFlowOption.Enabled);

        try
        {
            var response = await next();

            transactionScope.Complete();
            
            _logger.LogDebug("Transaction completed successfully for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transaction failed for {RequestName}, rolling back: {ErrorMessage}",
                requestName, ex.Message);
            throw;
        }
    }

    private static System.Transactions.IsolationLevel MapToTransactionScopeIsolationLevel(System.Data.IsolationLevel? isolationLevel)
    {
        return isolationLevel switch
        {
            System.Data.IsolationLevel.ReadUncommitted => System.Transactions.IsolationLevel.ReadUncommitted,
            System.Data.IsolationLevel.ReadCommitted => System.Transactions.IsolationLevel.ReadCommitted,
            System.Data.IsolationLevel.RepeatableRead => System.Transactions.IsolationLevel.RepeatableRead,
            System.Data.IsolationLevel.Serializable => System.Transactions.IsolationLevel.Serializable,
            System.Data.IsolationLevel.Snapshot => System.Transactions.IsolationLevel.Snapshot,
            System.Data.IsolationLevel.Chaos => System.Transactions.IsolationLevel.Chaos,
            System.Data.IsolationLevel.Unspecified => System.Transactions.IsolationLevel.Unspecified,
            null => System.Transactions.IsolationLevel.ReadCommitted, // Default
            _ => System.Transactions.IsolationLevel.ReadCommitted
        };
    }
}