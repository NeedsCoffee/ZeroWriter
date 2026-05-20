namespace Zerowriter;

public sealed class AppOptions
{
    private AppOptions(string volumeRoot, long? requestedMaxFileSizeBytes)
    {
        VolumeRoot = volumeRoot;
        RequestedMaxFileSizeBytes = requestedMaxFileSizeBytes;
    }

    public string VolumeRoot { get; }
    public long? RequestedMaxFileSizeBytes { get; }

    public static AppOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length is < 1 or > 3)
        {
            throw new ArgumentException("Usage: zerowriter <drive-letter> [--max-file-size <size>]");
        }

        var volumeRoot = VolumePathParser.NormalizeVolumeRoot(args[0]);
        long? requestedMaxFileSizeBytes = null;

        if (args.Length > 1)
        {
            if (args.Length != 3 || !string.Equals(args[1], "--max-file-size", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Usage: zerowriter <drive-letter> [--max-file-size <size>]");
            }

            requestedMaxFileSizeBytes = ParseSize(args[2]);
        }

        return new AppOptions(volumeRoot, requestedMaxFileSizeBytes);
    }

    private static long ParseSize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The max file size must not be empty.", nameof(value));
        }

        var trimmed = value.Trim();
        var splitIndex = 0;
        while (splitIndex < trimmed.Length && char.IsDigit(trimmed[splitIndex]))
        {
            splitIndex++;
        }

        if (splitIndex == 0 || !long.TryParse(trimmed[..splitIndex], out var numericValue) || numericValue <= 0)
        {
            throw new ArgumentException("The max file size must be a positive number.", nameof(value));
        }

        var suffix = trimmed[splitIndex..].Trim().ToUpperInvariant();
        var multiplier = suffix switch
        {
            "" or "B" => 1L,
            "K" or "KB" or "KIB" => 1024L,
            "M" or "MB" or "MIB" => 1024L * 1024L,
            "G" or "GB" or "GIB" => 1024L * 1024L * 1024L,
            "TIB" => 1024L * 1024L * 1024L * 1024L,
            _ => throw new ArgumentException("Unsupported size suffix. Use B, K, KB, KiB, M, MB, MiB, G, GB, GiB, or TiB.", nameof(value))
        };

        checked
        {
            return numericValue * multiplier;
        }
    }
}
