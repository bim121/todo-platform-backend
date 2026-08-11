namespace TodoPlatform.Application.Dtos;

/// <summary>Read-model row for filtered todo lists (Dapper / EF fallback).</summary>
public sealed record TodoListItemDto(
    Guid Id,
    string Title,
    bool Completed,
    Guid UserId,
    string Status,
    string Priority);
