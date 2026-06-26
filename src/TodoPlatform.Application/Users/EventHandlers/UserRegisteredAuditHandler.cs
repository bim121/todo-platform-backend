using MediatR;
using Microsoft.Extensions.Logging;
using TodoPlatform.Domain.Events;

namespace TodoPlatform.Application.Users.EventHandlers;

public sealed class UserRegisteredAuditHandler(ILogger<UserRegisteredAuditHandler> logger)
    : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "User registered from Keycloak: {UserId}, email {Email}, sub {KeycloakSub}, at {OccurredOn}",
            notification.UserId,
            notification.Email,
            notification.KeycloakSub,
            notification.OccurredOn);

        return Task.CompletedTask;
    }
}
