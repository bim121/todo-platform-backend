using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoPlatform.Application.Exceptions;

namespace TodoPlatform.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = MapException(exception, environment);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

    private static (int StatusCode, string Title, string Detail, IDictionary<string, string[]>? Errors)
        MapException(Exception exception, IHostEnvironment environment)
    {
        return exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFound.Message,
                null),
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                validation.Message,
                validation.Errors),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An error occurred",
                environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                null)
        };
    }
}
