namespace Zerowriter.Tests;

public sealed class VolumeWipeOperationTests : IDisposable
{
    private readonly string rootPath = Path.Combine(Path.GetTempPath(), $"zerowriter-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Cleanup_RemovesWorkspaceAndIsIdempotent()
    {
        Directory.CreateDirectory(rootPath);
        var operation = VolumeWipeOperation.Create(rootPath);
        var wipeFilePath = operation.CreateNextWipeFilePath();
        File.WriteAllText(wipeFilePath, "test");

        operation.Cleanup();
        operation.Cleanup();

        Assert.False(Directory.Exists(operation.WorkspacePath));
    }

    [Fact]
    public void Cleanup_CanBeRetriedAfterAnOpenFileBlockedTheFirstAttempt()
    {
        Directory.CreateDirectory(rootPath);
        var operation = VolumeWipeOperation.Create(rootPath);
        var wipeFilePath = operation.CreateNextWipeFilePath();
        File.WriteAllText(wipeFilePath, "test");

        using var stream = new FileStream(wipeFilePath, FileMode.Open, FileAccess.Read, FileShare.None);

        operation.Cleanup();
        Assert.True(Directory.Exists(operation.WorkspacePath));

        stream.Dispose();

        operation.Cleanup();
        Assert.False(Directory.Exists(operation.WorkspacePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }
}
