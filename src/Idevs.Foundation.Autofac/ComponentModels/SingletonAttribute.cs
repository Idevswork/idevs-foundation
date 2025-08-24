using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Foundation.Autofac.ComponentModels;

/// <summary>
/// Marks a class for automatic registration as a singleton in the Autofac container.
/// </summary>
public sealed class SingletonAttribute() : ComponentModelAttribute(lifetime: ServiceLifetime.Singleton)
{
}
