using System.Data.Common;

namespace Idevs.Foundation.Abstractions.Repositories;

/// <summary>
/// Defines a contract for Unit of Work pattern implementation.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uses an existing database transaction.
    /// </summary>
    /// <param name="transaction">The database transaction to use.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task UseExistingTransactionAsync(DbTransaction transaction, CancellationToken cancellationToken = default);
}
