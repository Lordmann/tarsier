namespace Tarsier.Core.Models;

/// <summary>Profiles are keyed by bare executable file name so they survive the app being moved or reinstalled.</summary>
public static class ExecutableKey
{
    public static string Normalize(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new ArgumentException("Executable name is required.", nameof(executable));
        }

        var fileName = Path.GetFileName(executable.Trim());
        return fileName.ToLowerInvariant();
    }

    public static bool Matches(string left, string right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
}
