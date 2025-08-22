namespace IdevsWork.Foundation.Abstractions.Common;

/// <summary>
/// Defines a contract for entities that have an identifier.
/// </summary>
/// <typeparam name="TId">The type of the identifier.</typeparam>
public interface IHasId<out TId>
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    TId Id { get; }
}
