using System.Linq.Expressions;

namespace TodoPlatform.Domain.Specifications;

public abstract class Specification<T> where T : class
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    public List<Expression<Func<T, object>>> Includes { get; } = [];

    public Expression<Func<T, object>>? OrderBy { get; protected set; }

    public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

    public int? Skip { get; protected set; }

    public int? Take { get; protected set; }

    public bool AsNoTracking { get; protected set; } = true;

    /// <summary>When true, infrastructure applies <c>ORDER BY Id</c> (stable paging, B-09.4).</summary>
    public bool OrderById { get; protected set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) =>
        Includes.Add(includeExpression);

    protected void ApplyOrderBy<TKey>(Expression<Func<T, TKey>> orderByExpression) =>
        OrderBy = ToObjectExpression(orderByExpression);

    protected void ApplyOrderByDescending<TKey>(Expression<Func<T, TKey>> orderByExpression) =>
        OrderByDescending = ToObjectExpression(orderByExpression);

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    public Specification<T> And(Specification<T> specification) =>
        new AndSpecification<T>(this, specification);

    public static Specification<T> operator &(Specification<T> left, Specification<T> right) =>
        left.And(right);

    private static Expression<Func<T, object>> ToObjectExpression<TKey>(
        Expression<Func<T, TKey>> expression) =>
        Expression.Lambda<Func<T, object>>(
            Expression.Convert(expression.Body, typeof(object)),
            expression.Parameters);
}

internal sealed class AndSpecification<T> : Specification<T> where T : class
{
    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        var parameter = Expression.Parameter(typeof(T), "entity");
        Criteria = Expression.Lambda<Func<T, bool>>(
            CombineCriteria(left.Criteria, right.Criteria, parameter),
            parameter);

        Includes.AddRange(left.Includes);
        Includes.AddRange(right.Includes);
        OrderBy = left.OrderBy ?? right.OrderBy;
        OrderByDescending = left.OrderByDescending ?? right.OrderByDescending;
        Skip = left.Skip ?? right.Skip;
        Take = left.Take ?? right.Take;
        AsNoTracking = left.AsNoTracking && right.AsNoTracking;
        OrderById = left.OrderById || right.OrderById;
    }

    private static Expression CombineCriteria(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right,
        ParameterExpression parameter)
    {
        if (left is null)
            return right is null
                ? Expression.Constant(true)
                : ReplaceParameter(right.Body, right.Parameters[0], parameter);

        if (right is null)
            return ReplaceParameter(left.Body, left.Parameters[0], parameter);

        return Expression.AndAlso(
            ReplaceParameter(left.Body, left.Parameters[0], parameter),
            ReplaceParameter(right.Body, right.Parameters[0], parameter));
    }

    private static Expression ReplaceParameter(
        Expression expression,
        ParameterExpression source,
        ParameterExpression target) =>
        new ParameterReplacer(source, target).Visit(expression)!;
}

file sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        node == source ? target : base.VisitParameter(node);
}
