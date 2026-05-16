using System.Threading;

namespace Zerowriter;

public sealed class VolumeWipeOperation
{
    private readonly Lock cleanupLock = new();
    private bool cleanedUp;
    private int nextFileNumber;

    private VolumeWipeOperation(string volumeRoot, string workspacePath)
    {
        VolumeRoot = volumeRoot;
        WorkspacePath = workspacePath;
    }

    public string VolumeRoot { get; }
    public string WorkspacePath { get; }

    public static VolumeWipeOperation Create(string volumeRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(volumeRoot);

        var workspacePath = Path.Combine(volumeRoot, $".zerowriter-{Guid.NewGuid():N}");
        return new VolumeWipeOperation(volumeRoot, workspacePath);
    }

    public string CreateNextWipeFilePath()
    {
        Directory.CreateDirectory(WorkspacePath);
        var fileNumber = Interlocked.Increment(ref nextFileNumber);
        return Path.Combine(WorkspacePath, $"wipe-{fileNumber:0000}.bin");
    }

    public void Cleanup()
    {
        lock (cleanupLock)
        {
            if (cleanedUp)
            {
                return;
            }

            try
            {
                if (Directory.Exists(WorkspacePath))
                {
                    Directory.Delete(WorkspacePath, recursive: true);
                }

                cleanedUp = !Directory.Exists(WorkspacePath);
            }
            catch
            {
                // Best-effort workspace cleanup on shutdown or failure.
                cleanedUp = false;
            }
        }
    }
}
