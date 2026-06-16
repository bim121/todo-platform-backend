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

        if (specification.OrderBy is not null)
            query = query.OrderBy(specification.OrderBy);

        if (specification.OrderByDescending is not null)
            query = query.OrderByDescending(specification.OrderByDescending);

        if (specification.Skip is > 0)
            query = query.Skip(specification.Skip.Value);

        if (specification.Take is > 0)
            query = query.Take(specification.Take.Value);

        return query;
    }
}
