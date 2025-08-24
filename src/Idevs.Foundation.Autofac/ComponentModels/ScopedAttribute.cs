using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Foundation.Autofac.ComponentModels;

/// <summary>
/// Marks a class for automatic registration as scoped (instance per lifetime scope) in the Autofac container.
/// </summary>
public sealed class ScopedAttribute() : ComponentModelAttribute(lifetime: ServiceLifetime.Scoped)
{
}
