using MediatR;
using TodoPlatform.Application.Dtos;

namespace TodoPlatform.Application.Admin.Queries.GetTenants;

/// <summary>
/// Stub until B-12 (Dapper admin list). Frontend: AdminFacade / selectTenants.
/// </summary>
public sealed record GetTenantsQuery : IRequest<IReadOnlyList<TenantAdminDto>>;

public sealed class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantAdminDto>>
{
    public Task<IReadOnlyList<TenantAdminDto>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TenantAdminDto>>([]);
}
