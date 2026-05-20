namespace Zerowriter;

public sealed class VolumeWritePolicy
{
    // FAT32's maximum file size is just under 4 GiB. Staying slightly below the
    // boundary avoids edge-case failures from filesystem and API rounding.
    public const long Fat32SafeMaxFileSizeBytes = (4L * 1024L * 1024L * 1024L) - (1L * 1024L * 1024L);

    private VolumeWritePolicy(string driveFormat, long? maxFileSizeBytes, bool usedFat32AutoCap, bool wasClampedToFilesystemLimit)
    {
        DriveFormat = driveFormat;
        MaxFileSizeBytes = maxFileSizeBytes;
        UsedFat32AutoCap = usedFat32AutoCap;
        WasClampedToFilesystemLimit = wasClampedToFilesystemLimit;
    }

    public string DriveFormat { get; }
    public long? MaxFileSizeBytes { get; }
    public bool UsedFat32AutoCap { get; }
    public bool WasClampedToFilesystemLimit { get; }

    public static VolumeWritePolicy Create(string driveFormat, long? requestedMaxFileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveFormat);

        var isFat32 = string.Equals(driveFormat, "FAT32", StringComparison.OrdinalIgnoreCase);
        if (!isFat32)
        {
            return new VolumeWritePolicy(driveFormat, requestedMaxFileSizeBytes, usedFat32AutoCap: false, wasClampedToFilesystemLimit: false);
        }

        if (requestedMaxFileSizeBytes is null)
        {
            return new VolumeWritePolicy(driveFormat, Fat32SafeMaxFileSizeBytes, usedFat32AutoCap: true, wasClampedToFilesystemLimit: false);
        }

        if (requestedMaxFileSizeBytes.Value > Fat32SafeMaxFileSizeBytes)
        {
            return new VolumeWritePolicy(driveFormat, Fat32SafeMaxFileSizeBytes, usedFat32AutoCap: false, wasClampedToFilesystemLimit: true);
        }

        return new VolumeWritePolicy(driveFormat, requestedMaxFileSizeBytes.Value, usedFat32AutoCap: false, wasClampedToFilesystemLimit: false);
    }
}
