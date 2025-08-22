namespace IdevsWork.Foundation.Abstractions.Common;

/// <summary>
/// Defines a contract for entities that track update timestamps.
/// </summary>
public interface IHasUpdatedLog
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was last updated.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; set; }
}
