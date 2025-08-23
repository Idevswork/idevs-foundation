namespace Idevs.Foundation.Cqrs.Results;

/// <summary>
/// Represents the result of an operation with a return value.
/// </summary>
/// <typeparam name="T">The type of the return value.</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// Gets the data returned by the operation if it was successful.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful result with data.
    /// </summary>
    /// <param name="data">The data to return.</param>
    /// <returns>A successful result with data.</returns>
    public static Result<T> Success(T data)
    {
        return new Result<T> { IsSuccess = true, Data = data };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static new Result<T> Failure(string errorMessage)
    {
        return new Result<T> { IsSuccess = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// Creates a failed result with an error message and validation errors.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed result.</returns>
    public static new Result<T> Failure(string errorMessage, Dictionary<string, string[]> errors)
    {
        return new Result<T> { IsSuccess = false, ErrorMessage = errorMessage, Errors = errors };
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    /// <param name="data">The data to wrap in a result.</param>
    /// <returns>A successful result with the data.</returns>
    public static implicit operator Result<T>(T data)
    {
        return Success(data);
    }
}
