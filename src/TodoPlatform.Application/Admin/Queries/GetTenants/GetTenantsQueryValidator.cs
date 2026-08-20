using FluentValidation;
using TodoPlatform.Application.Todos.Queries.GetTodos;

namespace TodoPlatform.Application.Admin.Queries.GetTenants;

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    private static readonly HashSet<string> AllowedTracks =
        new(StringComparer.OrdinalIgnoreCase) { "stable", "beta", "blue", "green" };

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "active", "inactive", "migrating", "error" };

    public GetTenantsQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Query parameter 'skip' must be greater than or equal to 0.");

        RuleFor(x => x.Take)
            .InclusiveBetween(1, GetTodosQueryValidator.MaxPageSize)
            .WithMessage($"Query parameter 'take' must be between 1 and {GetTodosQueryValidator.MaxPageSize}.");

        RuleFor(x => x.Track)
            .Must(t => t is null || AllowedTracks.Contains(t))
            .WithMessage("Query parameter 'track' must be one of: stable, beta, blue, green.");

        RuleFor(x => x.Status)
            .Must(s => s is null || AllowedStatuses.Contains(s))
            .WithMessage("Query parameter 'status' must be one of: active, inactive, migrating, error.");
    }
}
