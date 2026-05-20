namespace Zerowriter.Tests;

public sealed class VolumeWritePolicyTests
{
    [Fact]
    public void Create_UsesFat32SafeDefaultWhenNoOverrideIsProvided()
    {
        var policy = VolumeWritePolicy.Create("FAT32", requestedMaxFileSizeBytes: null);

        Assert.Equal(VolumeWritePolicy.Fat32SafeMaxFileSizeBytes, policy.MaxFileSizeBytes);
        Assert.True(policy.UsedFilesystemAutoCap);
        Assert.True(policy.UsedFat32AutoCap);
    }

    [Fact]
    public void Create_UsesFat16SafeDefaultWhenNoOverrideIsProvided()
    {
        var policy = VolumeWritePolicy.Create("FAT16", requestedMaxFileSizeBytes: null);

        Assert.Equal(VolumeWritePolicy.Fat16SafeMaxFileSizeBytes, policy.MaxFileSizeBytes);
        Assert.True(policy.UsedFilesystemAutoCap);
    }

    [Fact]
    public void Create_TreatsFatDriveFormatAsFat16()
    {
        var policy = VolumeWritePolicy.Create("FAT", requestedMaxFileSizeBytes: null);

        Assert.Equal(VolumeWritePolicy.Fat16SafeMaxFileSizeBytes, policy.MaxFileSizeBytes);
        Assert.True(policy.UsedFilesystemAutoCap);
    }

    [Fact]
    public void Create_ClampsOverrideDownForFat32()
    {
        var policy = VolumeWritePolicy.Create("FAT32", requestedMaxFileSizeBytes: long.MaxValue);

        Assert.Equal(VolumeWritePolicy.Fat32SafeMaxFileSizeBytes, policy.MaxFileSizeBytes);
        Assert.True(policy.WasClampedToFilesystemLimit);
    }

    [Fact]
    public void Create_ClampsOverrideDownForFat16()
    {
        var policy = VolumeWritePolicy.Create("FAT16", requestedMaxFileSizeBytes: long.MaxValue);

        Assert.Equal(VolumeWritePolicy.Fat16SafeMaxFileSizeBytes, policy.MaxFileSizeBytes);
        Assert.True(policy.WasClampedToFilesystemLimit);
    }

    [Fact]
    public void Create_UsesOverrideForNtfs()
    {
        var policy = VolumeWritePolicy.Create("NTFS", requestedMaxFileSizeBytes: 1234);

        Assert.Equal(1234, policy.MaxFileSizeBytes);
        Assert.False(policy.UsedFilesystemAutoCap);
        Assert.False(policy.UsedFat32AutoCap);
        Assert.False(policy.WasClampedToFilesystemLimit);
    }
}
