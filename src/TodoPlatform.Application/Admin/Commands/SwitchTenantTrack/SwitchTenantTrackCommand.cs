using MediatR;
using TodoPlatform.Application.Common;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Admin.Commands.SwitchTenantTrack;

/// <summary>
/// Stub until B-28 (blue/green track switch). Frontend: AdminFacade.switchTrack().
/// </summary>
public sealed record SwitchTenantTrackCommand(string TenantId, string Track) : IRequest<TenantAdminDto>, ICommand;

public sealed class SwitchTenantTrackHandler : IRequestHandler<SwitchTenantTrackCommand, TenantAdminDto>
{
    public Task<TenantAdminDto> Handle(SwitchTenantTrackCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(new TenantAdminDto(
            request.TenantId,
            Name: "Stub Tenant",
            SchemaVersion: "1.0.0",
            DeploymentTrack: request.Track,
            AppVersion: "1.0.0",
            Status: "active"));
}
