namespace TodoPlatform.Application.Dtos;

public sealed record AuthResponse(string Token, UserDto User);
