namespace Idevs.Foundation.Cqrs.Commands;

/// <summary>
/// Interface for commands that return a result in CQRS pattern.
/// Commands are used to modify data and can return values.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command.</typeparam>
public interface ICommand<out TResult> : ICommand
{
}
