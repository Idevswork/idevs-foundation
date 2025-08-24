using Microsoft.Extensions.DependencyInjection;

namespace Idevs.Foundation.Autofac.ComponentModels;

/// <summary>
/// Base attribute for component model registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public abstract class ComponentModelAttribute(
    ServiceLifetime lifetime = ServiceLifetime.Transient,
    bool asSelf = false
) : Attribute
{
    /// <summary>
    /// Gets a value indicating whether to register as self (implementation type).
    /// </summary>
    public bool AsSelf { get; set; } = asSelf;

    /// <summary>
    /// Gets or sets the lifetime of the service.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = lifetime;
}
