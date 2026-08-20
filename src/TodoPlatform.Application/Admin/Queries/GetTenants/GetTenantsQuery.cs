using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Admin.Queries.GetTenants;

/// <summary>Admin tenant list with schema version / track (B-12.3 / B-12.4).</summary>
public sealed record GetTenantsQuery(
    int Skip = 0,
    int Take = 20,
    string? Track = null,
    string? Status = null) : IRequest<PagedResult<TenantAdminDto>>;

public sealed class GetTenantsQueryHandler(ITenantAdminReadStore store)
    : IRequestHandler<GetTenantsQuery, PagedResult<TenantAdminDto>>
{
    public Task<PagedResult<TenantAdminDto>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken) =>
        store.ListAsync(
            new TenantAdminListFilter(request.Skip, request.Take, request.Track, request.Status),
            cancellationToken);
}
