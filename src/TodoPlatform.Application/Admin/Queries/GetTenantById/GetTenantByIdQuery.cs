using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Exceptions;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Admin.Queries.GetTenantById;

/// <summary>Admin tenant detail including logical schema version (B-12.3).</summary>
public sealed record GetTenantByIdQuery(Guid Id) : IRequest<TenantAdminDto>;

public sealed class GetTenantByIdQueryHandler(ITenantAdminReadStore store)
    : IRequestHandler<GetTenantByIdQuery, TenantAdminDto>
{
    public async Task<TenantAdminDto> Handle(
        GetTenantByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await store.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
            throw new NotFoundException($"Tenant '{request.Id}' was not found.");

        return tenant;
    }
}
