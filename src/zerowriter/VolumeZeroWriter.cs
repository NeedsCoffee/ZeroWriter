using System.Diagnostics;

namespace Zerowriter;

public sealed class VolumeZeroWriter
{
    private static readonly int[] ChunkSizes =
    [
        8 * 1024 * 1024,
        1 * 1024 * 1024,
        64 * 1024,
        4 * 1024,
        512,
        1
    ];

    public VolumeWipeOperation CreateOperation(string volumeRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(volumeRoot);

        var drive = new DriveInfo(volumeRoot);
        if (!drive.IsReady)
        {
            throw new IOException($"The drive {volumeRoot} is not ready.");
        }

        return VolumeWipeOperation.Create(volumeRoot);
    }

    public VolumeWritePolicy CreatePolicy(string volumeRoot, long? requestedMaxFileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(volumeRoot);
        var drive = new DriveInfo(volumeRoot);
        return VolumeWritePolicy.Create(drive.DriveFormat, requestedMaxFileSizeBytes);
    }

    public async Task<long> WipeFreeSpaceAsync(VolumeWipeOperation operation, VolumeWritePolicy policy, CancellationToken cancellationToken) =>
        await WipeFreeSpaceAsync(operation, policy, new FileVolumeWriteStreamFactory(), cancellationToken);

    internal async Task<long> WipeFreeSpaceAsync(
        VolumeWipeOperation operation,
        VolumeWritePolicy policy,
        IVolumeWriteStreamFactory streamFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(streamFactory);

        var displayedProgress = false;

        try
        {
            var initialFreeSpace = GetAvailableFreeSpace(operation.VolumeRoot);
            var progressReporter = new ProgressReporter(initialFreeSpace);
            var stopwatch = Stopwatch.StartNew();
            long lastReportedBytes = 0;
            var lastLineLength = 0;

            long totalBytesWritten = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Each pass uses a fresh temp file so filesystems with per-file size
                // limits, such as FAT16 and FAT32, can still have all free space filled.
                var wipeFilePath = operation.CreateNextWipeFilePath();
                Stream stream;
                try
                {
                    stream = await streamFactory.OpenAsync(wipeFilePath, cancellationToken);
                }
                catch (IOException ex) when (ZeroFillEngine.IsDiskFull(ex))
                {
                    break;
                }

                await using (stream)
                {
                    var bytesWrittenThisFile = await ZeroFillEngine.FillAsync(
                        stream,
                        ChunkSizes,
                        cancellationToken,
                        _ =>
                        {
                            var currentFreeSpace = GetAvailableFreeSpace(operation.VolumeRoot);
                            var snapshot = progressReporter.CreateSnapshot(currentFreeSpace);
                            var shouldReport =
                                snapshot.BytesConsumed == 0 ||
                                snapshot.BytesConsumed - lastReportedBytes >= 4L * 1024 * 1024 ||
                                stopwatch.Elapsed >= TimeSpan.FromMilliseconds(100) ||
                                snapshot.BytesRemaining == 0;

                            if (!shouldReport)
                            {
                                return;
                            }

                            lastReportedBytes = snapshot.BytesConsumed;
                            stopwatch.Restart();

                            var line = ProgressReporter.Render(snapshot);
                            if (Console.IsOutputRedirected)
                            {
                                Console.WriteLine(line);
                            }
                            else
                            {
                                Console.Write('\r');
                                Console.Write(line.PadRight(lastLineLength));
                                lastLineLength = Math.Max(lastLineLength, line.Length);
                                displayedProgress = true;
                            }
                        },
                        policy.MaxFileSizeBytes);

                    totalBytesWritten += bytesWrittenThisFile;

                    if (bytesWrittenThisFile == 0)
                    {
                        break;
                    }

                    if (policy.MaxFileSizeBytes is null || bytesWrittenThisFile < policy.MaxFileSizeBytes.Value)
                    {
                        break;
                    }
                }
            }

            return totalBytesWritten;
        }
        finally
        {
            if (displayedProgress && !Console.IsOutputRedirected)
            {
                Console.WriteLine();
            }

            operation.Cleanup();
        }
    }

    public static string FormatBytes(long value)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        double size = value;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private static long GetAvailableFreeSpace(string volumeRoot) => new DriveInfo(volumeRoot).AvailableFreeSpace;
}
