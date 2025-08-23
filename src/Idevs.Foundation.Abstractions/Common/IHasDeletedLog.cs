namespace Idevs.Foundation.Abstractions.Common;

/// <summary>
/// Defines a contract for entities that support soft deletion.
/// </summary>
public interface IHasDeletedLog
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
