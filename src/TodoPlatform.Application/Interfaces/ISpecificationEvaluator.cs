using TodoPlatform.Domain.Specifications;

namespace TodoPlatform.Application.Interfaces;

public interface ISpecificationEvaluator
{
    IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
        where T : class;
}
