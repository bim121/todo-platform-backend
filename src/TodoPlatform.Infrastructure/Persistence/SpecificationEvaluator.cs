using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Interfaces;
using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Infrastructure.Persistence;

public sealed class SpecificationEvaluator : ISpecificationEvaluator
{
    public IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
        where T : class
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
            query = query.Where(specification.Criteria);

        query = specification.Includes.Aggregate(
            query,
            (current, include) => current.Include(include));

        if (specification.OrderById)
            query = query.OrderBy(e => EF.Property<Guid>(e, "Id"));
        else if (specification.OrderBy is not null)
            query = ApplyOrderBy(query, specification.OrderBy, descending: false);

        if (specification.OrderByDescending is not null)
            query = ApplyOrderBy(query, specification.OrderByDescending, descending: true);

        if (specification.Skip is > 0)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take is > 0)
            query = query.Take(specification.Take.Value);

        return query;
    }

    private static IQueryable<T> ApplyOrderBy<T>(
        IQueryable<T> query,
        Expression<Func<T, object>> orderExpression,
        bool descending)
    {
        var body = orderExpression.Body is UnaryExpression
        {
            NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked
        } unary
            ? unary.Operand
            : orderExpression.Body;

        if (body.Type == typeof(Guid))
        {
            var typed = Expression.Lambda<Func<T, Guid>>(body, orderExpression.Parameters);
            return descending ? query.OrderByDescending(typed) : query.OrderBy(typed);
        }

        var keyLambda = Expression.Lambda(body, orderExpression.Parameters);
        var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == methodName
                && m.IsGenericMethodDefinition
                && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), body.Type);

        return (IQueryable<T>)method.Invoke(null, [query, keyLambda])!;
    }
}
