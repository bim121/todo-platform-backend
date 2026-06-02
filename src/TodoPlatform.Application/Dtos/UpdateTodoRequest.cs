namespace TodoPlatform.Application.Dtos;

public sealed record UpdateTodoRequest(
    string? Title = null,
    bool? Completed = null,
    string? Status = null);
