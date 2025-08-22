using System.Linq.Expressions;
using IdevsWork.Foundation.Abstractions.Common;

namespace IdevsWork.Foundation.EntityFramework.Extensions;

/// <summary>
/// Extension methods for working with expressions.
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// Creates a predicate expression for finding an entity by its identifier.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="id">The identifier value.</param>
    /// <returns>An expression predicate for the identifier match.</returns>
    public static Expression<Func<T, bool>> CreateIdPredicate<T, TId>(TId id)
        where T : class, IHasId<TId>
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var idProperty = Expression.Property(parameter, nameof(IHasId<TId>.Id));
        var idConstant = Expression.Constant(id, typeof(TId));
        var equals = Expression.Equal(idProperty, idConstant);
        return Expression.Lambda<Func<T, bool>>(equals, parameter);
    }

    /// <summary>
    /// Creates a predicate expression for finding entities by multiple identifiers.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="ids">The identifier values.</param>
    /// <returns>An expression predicate for the identifier match.</returns>
    public static Expression<Func<T, bool>> CreateIdPredicate<T, TId>(IEnumerable<TId> ids)
        where T : class, IHasId<TId>
    {
        var idsArray = ids.ToArray();
        var parameter = Expression.Parameter(typeof(T), "x");
        var idProperty = Expression.Property(parameter, nameof(IHasId<TId>.Id));
        var idsConstant = Expression.Constant(idsArray, typeof(TId[]));
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TId));
        var contains = Expression.Call(containsMethod, idsConstant, idProperty);
        return Expression.Lambda<Func<T, bool>>(contains, parameter);
    }

    /// <summary>
    /// Combines two expressions with an AND operation.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="first">The first expression.</param>
    /// <param name="second">The second expression.</param>
    /// <returns>A combined expression using AND.</returns>
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var andAlso = Expression.AndAlso(firstBody, secondBody);
        return Expression.Lambda<Func<T, bool>>(andAlso, parameter);
    }

    /// <summary>
    /// Combines two expressions with an OR operation.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    /// <param name="first">The first expression.</param>
    /// <param name="second">The second expression.</param>
    /// <returns>A combined expression using OR.</returns>
    public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var orElse = Expression.OrElse(firstBody, secondBody);
        return Expression.Lambda<Func<T, bool>>(orElse, parameter);
    }

    /// <summary>
    /// Replaces a parameter in an expression with a new parameter.
    /// </summary>
    /// <param name="expression">The expression to modify.</param>
    /// <param name="oldParameter">The parameter to replace.</param>
    /// <param name="newParameter">The new parameter.</param>
    /// <returns>The modified expression.</returns>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// Expression visitor for replacing parameters.
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
