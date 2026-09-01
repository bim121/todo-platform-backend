namespace TodoPlatform.Application.Realtime;

/// <summary>Minimal todo payload pushed over SignalR (B-13.2 / B-13.5).</summary>
public sealed record TodoRealtimeMessage(
    Guid Id,
    string Title,
    bool Completed,
    long Version = 0);
