using Idevs.Foundation.Abstractions.Common;

namespace Idevs.Foundation.EntityFramework.Entities;

/// <summary>
/// Base class for entities with an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class EntityBase<TId> : IHasId<TId>, IEquatable<EntityBase<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public virtual TId Id { get; init; } = default!;

    public bool Equals(EntityBase<TId>? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() &&
               obj is EntityBase<TId> other &&
               EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return !Equals(left, right);
    }
}
