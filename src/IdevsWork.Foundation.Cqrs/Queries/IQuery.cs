namespace IdevsWork.Foundation.Cqrs.Queries;

/// <summary>
/// Interface for queries in CQRS pattern.
/// Queries are used to retrieve data and should always return a result.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query.</typeparam>
public interface IQuery<out TResult>
{
}
