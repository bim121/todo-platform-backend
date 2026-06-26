namespace TodoPlatform.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
