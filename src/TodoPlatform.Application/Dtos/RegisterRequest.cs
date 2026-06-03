namespace TodoPlatform.Application.Dtos;

public sealed record RegisterRequest(string Email, string Password, string Name);
