namespace IdevsWork.Foundation.Cqrs.Results;

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets any additional error details.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success()
    {
        return new Result { IsSuccess = true };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string errorMessage)
    {
        return new Result { IsSuccess = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// Creates a failed result with an error message and validation errors.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string errorMessage, Dictionary<string, string[]> errors)
    {
        return new Result { IsSuccess = false, ErrorMessage = errorMessage, Errors = errors };
    }
}
