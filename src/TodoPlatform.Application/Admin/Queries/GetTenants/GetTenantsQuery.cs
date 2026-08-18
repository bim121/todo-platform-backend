using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Admin.Queries.GetTenants;

/// <summary>Admin tenant list with schema version / track (B-12.3).</summary>
public sealed record GetTenantsQuery : IRequest<IReadOnlyList<TenantAdminDto>>;

public sealed class GetTenantsQueryHandler(ITenantAdminReadStore store)
    : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantAdminDto>>
{
    public Task<IReadOnlyList<TenantAdminDto>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken) =>
        store.ListAsync(cancellationToken);
}
