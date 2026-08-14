using MediatR;
using TodoPlatform.Application.Dtos;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Application.Admin.Queries.GetSystemStats;

public sealed record GetSystemStatsQuery : IRequest<SystemStatsDto>;

public sealed class GetSystemStatsQueryHandler(ISystemStatsReadStore store)
    : IRequestHandler<GetSystemStatsQuery, SystemStatsDto>
{
    public Task<SystemStatsDto> Handle(
        GetSystemStatsQuery request,
        CancellationToken cancellationToken) =>
        store.GetAsync(cancellationToken);
}
