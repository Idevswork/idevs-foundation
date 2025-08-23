namespace Idevs.Foundation.Abstractions.Common;

/// <summary>
/// Defines a contract for auditable entities that combine identification, 
/// creation tracking, update tracking, and soft deletion capabilities.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IAuditableEntity<out TId> : IHasId<TId>, IHasCreatedLog, IHasUpdatedLog, IHasDeletedLog
{
}
