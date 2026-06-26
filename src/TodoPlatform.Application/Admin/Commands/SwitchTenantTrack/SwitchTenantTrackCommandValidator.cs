using FluentValidation;
using TodoPlatform.Application.Admin.Commands.SwitchTenantTrack;

namespace TodoPlatform.Application.Admin.Commands.SwitchTenantTrack;

public sealed class SwitchTenantTrackCommandValidator : AbstractValidator<SwitchTenantTrackCommand>
{
    private static readonly HashSet<string> AllowedTracks =
        new(StringComparer.OrdinalIgnoreCase) { "blue", "green" };

    public SwitchTenantTrackCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant id is required.");

        RuleFor(x => x.Track)
            .NotEmpty()
            .Must(track => AllowedTracks.Contains(track))
            .WithMessage("Track must be 'blue' or 'green'.");
    }
}
