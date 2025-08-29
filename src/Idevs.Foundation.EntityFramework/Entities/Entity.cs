using Idevs.Foundation.Abstractions.Common;

namespace Idevs.Foundation.EntityFramework.Entities;

/// <summary>
/// Base class for entities with an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public abstract class Entity<TId> : IHasId<TId>, IEquatable<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public virtual TId Id { get; set; } = default!;

    public bool Equals(TId? other)
    {
        return EqualityComparer<TId>.Default.Equals(Id, other);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() &&
               obj is Entity<TId> other &&
               EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
