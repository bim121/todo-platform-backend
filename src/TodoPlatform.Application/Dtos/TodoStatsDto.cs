namespace TodoPlatform.Application.Dtos;

public sealed record TodoStatsDto(
    Guid UserId,
    int Total,
    int Active,
    int Completed);
