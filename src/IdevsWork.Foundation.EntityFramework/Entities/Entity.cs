using IdevsWork.Foundation.Abstractions.Common;

namespace IdevsWork.Foundation.EntityFramework.Entities;

/// <summary>
/// Base class for entities with an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class Entity<TId> : IHasId<TId>
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public virtual TId Id { get; set; } = default!;
}
