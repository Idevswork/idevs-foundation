using System.Data;

namespace Idevs.Foundation.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that should be executed within a transaction.
/// </summary>
public interface ITransactional
{
    /// <summary>
    /// Gets the transaction isolation level. If null, the default isolation level is used.
    /// </summary>
    IsolationLevel? IsolationLevel { get; }

    /// <summary>
    /// Gets the transaction timeout. If null, the default timeout is used.
    /// </summary>
    TimeSpan? Timeout { get; }
}