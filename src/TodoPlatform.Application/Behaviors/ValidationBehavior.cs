using FluentValidation;
using MediatR;
using TodoPlatform.Application.Exceptions;
using AppValidationException = TodoPlatform.Application.Exceptions.ValidationException;

namespace TodoPlatform.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .GroupBy(f => string.IsNullOrWhiteSpace(f.PropertyName) ? string.Empty : f.PropertyName)
            .ToDictionary(
                g => ToCamelCase(g.Key),
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        throw new AppValidationException(errors);
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
