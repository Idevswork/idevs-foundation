using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Foundation.Autofac.ComponentModels;

/// <summary>
/// Marks a class for automatic registration as transient (instance per dependency) in the Autofac container.
/// </summary>
public sealed class TransientAttribute() : ComponentModelAttribute(lifetime: ServiceLifetime.Transient)
{
}
