using System.Reflection;
using Autofac;
using Autofac.Builder;
using Idevs.Foundation.Autofac.ComponentModels;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Queries;
using Idevs.Foundation.Mediator.Behaviors;
using Idevs.Foundation.Mediator.Core;

namespace Idevs.Foundation.Autofac.Modules;

/// <summary>
/// Autofac module for registering Foundation components.
/// </summary>
public class FoundationModule : global::Autofac.Module
{
    private readonly Assembly[] _assemblies;
    private readonly bool _registerLoggingBehavior;

    /// <summary>
    /// Initializes a new instance of the <see cref="FoundationModule"/> class.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for handlers and behaviors.</param>
    /// <param name="registerLoggingBehavior">Whether to register the logging behavior.</param>
    public FoundationModule(Assembly[] assemblies, bool registerLoggingBehavior = true)
    {
        _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
        _registerLoggingBehavior = registerLoggingBehavior;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FoundationModule"/> class with a single assembly.
    /// </summary>
    /// <param name="assembly">Assembly to scan for handlers and behaviors.</param>
    /// <param name="registerLoggingBehavior">Whether to register the logging behavior.</param>
    public FoundationModule(Assembly assembly, bool registerLoggingBehavior = true)
        : this([assembly], registerLoggingBehavior)
    {
    }

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        // Register mediator
        builder.RegisterType<Idevs.Foundation.Mediator.Core.Mediator>()
            .As<IMediator>()
            .InstancePerLifetimeScope();

        // Register command handlers
        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(ICommandHandler<>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(ICommandHandler<,>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Register query handlers
        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(IQueryHandler<,>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Register pipeline behaviors
        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(IPipelineBehavior<,>))
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        // Register built-in logging behavior if requested
        if (_registerLoggingBehavior)
        {
            builder.RegisterGeneric(typeof(LoggingBehavior<,>))
                .As(typeof(IPipelineBehavior<,>))
                .InstancePerLifetimeScope();
        }

        // Register component models
        RegisterComponentModels(builder);
    }

    /// <summary>
    /// Registers services marked with component model attributes.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    private void RegisterComponentModels(ContainerBuilder builder)
    {
        foreach (var assembly in _assemblies)
        {
            var types = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .ToArray();

            RegisterTypesWithAttribute<SingletonAttribute>(builder, types, lifetime => lifetime.SingleInstance());
            RegisterTypesWithAttribute<ScopedAttribute>(builder, types,
                lifetime => lifetime.InstancePerLifetimeScope());
            RegisterTypesWithAttribute<TransientAttribute>(builder, types,
                lifetime => lifetime.InstancePerDependency());
        }
    }

    /// <summary>
    /// Registers types marked with a specific component model attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type.</typeparam>
    /// <param name="builder">The container builder.</param>
    /// <param name="types">The types to scan.</param>
    /// <param name="lifetimeAction">The lifetime configuration action.</param>
    private static void RegisterTypesWithAttribute<TAttribute>(
        ContainerBuilder builder,
        Type[] types,
        Func<IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>,
            IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle>> lifetimeAction)
        where TAttribute : ComponentModelAttribute
    {
        var typesWithAttribute = types
            .Where(type => type.GetCustomAttribute<TAttribute>() != null)
            .ToArray();

        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttribute<TAttribute>()!;
            var registration = builder.RegisterType(type);

            if (attribute.AsSelf)
            {
                registration = registration.AsSelf();
            }
            else
            {
                var interfaces = type.GetInterfaces();
                registration = interfaces.Any()
                    ? registration.AsImplementedInterfaces()
                    : registration.AsSelf();
            }

            // Apply lifetime
            lifetimeAction(registration);
        }
    }
}
