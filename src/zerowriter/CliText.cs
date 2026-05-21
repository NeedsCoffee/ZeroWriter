using System.Reflection;

namespace Zerowriter;

public static class CliText
{
    public const string UsageLine = "Usage: zerowriter <drive-letter> [-m|--max-file-size <size>]";

    public static string GetVersion()
    {
        var assembly = typeof(CliText).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "unknown";
    }

    public static string RenderHeader(string version) => $"ZeroWriter {version}";

    public static string RenderUsage(string version) =>
        string.Join(
            Environment.NewLine,
            RenderHeader(version),
            UsageLine,
            "Example: zerowriter C: --max-file-size 4095m") + Environment.NewLine;
}
