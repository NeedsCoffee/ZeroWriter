namespace Zerowriter;

public sealed class VolumeWritePolicy
{
    // FAT16's maximum file size is 2 GiB. Staying slightly below the boundary
    // matches the FAT32 policy and avoids edge-case failures near the limit.
    public const long Fat16SafeMaxFileSizeBytes = (2L * 1024L * 1024L * 1024L) - (1L * 1024L * 1024L);

    // FAT32's maximum file size is just under 4 GiB. Staying slightly below the
    // boundary avoids edge-case failures from filesystem and API rounding.
    public const long Fat32SafeMaxFileSizeBytes = (4L * 1024L * 1024L * 1024L) - (1L * 1024L * 1024L);

    private static readonly IReadOnlyDictionary<string, long> FilesystemSafeMaxFileSizeBytes =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["FAT"] = Fat16SafeMaxFileSizeBytes,
            ["FAT16"] = Fat16SafeMaxFileSizeBytes,
            ["FAT32"] = Fat32SafeMaxFileSizeBytes
        };

    private VolumeWritePolicy(string driveFormat, long? maxFileSizeBytes, bool usedFilesystemAutoCap, bool wasClampedToFilesystemLimit)
    {
        DriveFormat = driveFormat;
        MaxFileSizeBytes = maxFileSizeBytes;
        UsedFilesystemAutoCap = usedFilesystemAutoCap;
        WasClampedToFilesystemLimit = wasClampedToFilesystemLimit;
    }

    public string DriveFormat { get; }
    public long? MaxFileSizeBytes { get; }
    public bool UsedFilesystemAutoCap { get; }
    public bool UsedFat32AutoCap => UsedFilesystemAutoCap && string.Equals(DriveFormat, "FAT32", StringComparison.OrdinalIgnoreCase);
    public bool WasClampedToFilesystemLimit { get; }

    public static VolumeWritePolicy Create(string driveFormat, long? requestedMaxFileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveFormat);

        if (!FilesystemSafeMaxFileSizeBytes.TryGetValue(driveFormat, out var filesystemLimit))
        {
            return new VolumeWritePolicy(driveFormat, requestedMaxFileSizeBytes, usedFilesystemAutoCap: false, wasClampedToFilesystemLimit: false);
        }

        if (requestedMaxFileSizeBytes is null)
        {
            return new VolumeWritePolicy(driveFormat, filesystemLimit, usedFilesystemAutoCap: true, wasClampedToFilesystemLimit: false);
        }

        if (requestedMaxFileSizeBytes.Value > filesystemLimit)
        {
            return new VolumeWritePolicy(driveFormat, filesystemLimit, usedFilesystemAutoCap: false, wasClampedToFilesystemLimit: true);
        }

        return new VolumeWritePolicy(driveFormat, requestedMaxFileSizeBytes.Value, usedFilesystemAutoCap: false, wasClampedToFilesystemLimit: false);
    }
}
