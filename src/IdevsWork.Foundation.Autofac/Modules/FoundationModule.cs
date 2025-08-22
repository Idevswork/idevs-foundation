using System.Reflection;
using Autofac;
using IdevsWork.Foundation.Cqrs.Behaviors;
using IdevsWork.Foundation.Cqrs.Commands;
using IdevsWork.Foundation.Cqrs.Queries;
using IdevsWork.Foundation.Mediator.Behaviors;
using IdevsWork.Foundation.Mediator.Core;

namespace IdevsWork.Foundation.Autofac.Modules;

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
        : this(new[] { assembly }, registerLoggingBehavior)
    {
    }

    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        // Register mediator
        builder.RegisterType<IdevsWork.Foundation.Mediator.Core.Mediator>()
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
    }
}
