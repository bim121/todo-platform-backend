namespace TodoPlatform.Application.Dtos;

public sealed record CreateTodoRequest(string Title, Guid UserId);
