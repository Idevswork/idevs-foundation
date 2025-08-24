namespace Idevs.Foundation.Cqrs.Behaviors;

/// <summary>
/// Represents a validation error.
/// </summary>
public record ValidationError(string PropertyName, string ErrorMessage);

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public record ValidationResult(bool IsValid, IReadOnlyCollection<ValidationError> Errors)
{
    /// <summary>
    /// Gets a successful validation result.
    /// </summary>
    public static ValidationResult Success { get; } = new(true, Array.Empty<ValidationError>());

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) 
        => new(false, errors);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) 
        => new(false, errors.ToArray());
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyCollection<ValidationError> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(IReadOnlyCollection<ValidationError> errors)
        : base($"Validation failed with {errors.Count} error(s)")
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(params ValidationError[] errors)
        : this((IReadOnlyCollection<ValidationError>)errors)
    {
    }
}

/// <summary>
/// Marker interface for requests that support validation.
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <returns>The validation result.</returns>
    ValidationResult Validate();
}