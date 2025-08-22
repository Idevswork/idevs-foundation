using IdevsWork.Foundation.Abstractions.Common;

namespace IdevsWork.Foundation.EntityFramework.Entities;

/// <summary>
/// Base class for entities that support audit trail (creation and update tracking).
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IHasCreatedLog, IHasUpdatedLog
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// </summary>
    public virtual DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated.
    /// </summary>
    public virtual DateTimeOffset? UpdatedAt { get; set; }
}
