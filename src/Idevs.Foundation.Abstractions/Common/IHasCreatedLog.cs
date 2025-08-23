namespace Idevs.Foundation.Abstractions.Common;

/// <summary>
/// Defines a contract for entities that track creation timestamps.
/// </summary>
public interface IHasCreatedLog
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// </summary>
    DateTimeOffset? CreatedAt { get; set; }
}
