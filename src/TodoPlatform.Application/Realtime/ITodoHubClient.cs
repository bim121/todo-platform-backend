namespace TodoPlatform.Application.Realtime;

/// <summary>Strongly typed SignalR client callbacks for todo live updates (B-13.2).</summary>
public interface ITodoHubClient
{
    Task TodoCreated(TodoRealtimeMessage message);

    Task TodoUpdated(TodoRealtimeMessage message);

    Task TodoDeleted(TodoRealtimeMessage message);
}
