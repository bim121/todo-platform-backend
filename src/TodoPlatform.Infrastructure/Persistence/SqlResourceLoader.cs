using System.Reflection;

namespace TodoPlatform.Infrastructure.Persistence;

internal static class SqlResourceLoader
{
    public static string Load(string fileName)
    {
        var assembly = typeof(SqlResourceLoader).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(n => n.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase)
                || n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded SQL resource '{fileName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
