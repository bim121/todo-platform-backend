namespace TodoPlatform.Api.Configuration;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// When false, MassTransit is not registered (tests / local without broker).
    /// </summary>
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = "localhost";

    public ushort Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "todo";

    public string Password { get; set; } = "todo";
}
