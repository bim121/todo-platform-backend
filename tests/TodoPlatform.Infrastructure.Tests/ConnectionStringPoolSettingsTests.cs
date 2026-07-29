using TodoPlatform.Infrastructure;

namespace TodoPlatform.Infrastructure.Tests;

public sealed class ConnectionStringPoolSettingsTests
{
    [Fact]
    public void EnsurePoolSettings_AppendsWhenMissing()
    {
        var result = DependencyInjection.EnsurePoolSettings(
            "Host=localhost;Database=tododb;Username=todo;Password=todo");

        Assert.Contains("Maximum Pool Size=100", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Timeout=15", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsurePoolSettings_DoesNotDuplicate()
    {
        var input = "Host=localhost;Maximum Pool Size=50;Username=todo";
        var result = DependencyInjection.EnsurePoolSettings(input);

        Assert.Equal(input, result);
    }
}
