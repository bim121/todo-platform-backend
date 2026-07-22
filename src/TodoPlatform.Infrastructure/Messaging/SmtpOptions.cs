namespace TodoPlatform.Infrastructure.Messaging;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// When true, also deliver via SMTP (Mailhog in local compose). Logging always happens.
    /// </summary>
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string From { get; set; } = "todo-platform@localhost";
}
