using IdevsWork.Foundation.Abstractions.Common;

namespace IdevsWork.Foundation.EntityFramework.Entities;

/// <summary>
/// Base class for entities that support soft deletion and full audit trail.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class SoftDeletableEntity<TId> : AuditableEntity<TId>, IHasDeletedLog
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was deleted.
    /// </summary>
    public virtual DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    public virtual bool IsDeleted { get; set; }
}
