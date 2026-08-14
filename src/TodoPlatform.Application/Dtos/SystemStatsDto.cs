namespace TodoPlatform.Application.Dtos;

/// <summary>Tenant-agnostic platform aggregates for admin dashboards (B-10.7).</summary>
public sealed record SystemStatsDto(
    int TotalUsers,
    int TotalTodos,
    decimal AvgTodosPerUser);
