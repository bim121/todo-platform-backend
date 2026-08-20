using FluentValidation;

namespace TodoPlatform.Application.Admin.Commands.ApplyTenantMigration;

public sealed class ApplyTenantMigrationCommandValidator : AbstractValidator<ApplyTenantMigrationCommand>
{
    public ApplyTenantMigrationCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant id is required.");

        RuleFor(x => x.TargetVersion)
            .GreaterThan(0)
            .When(x => x.TargetVersion is not null)
            .WithMessage("Target version must be a positive migration number.");
    }
}
