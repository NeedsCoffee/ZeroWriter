namespace Zerowriter;

public static class ZeroFillEngine
{
    private const int ErrorDiskFull = 112;

    public static async Task<long> FillAsync(
        Stream stream,
        IReadOnlyList<int> chunkSizes,
        CancellationToken cancellationToken,
        Action<long>? progress = null,
        long? maxBytesToWrite = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(chunkSizes);

        if (!stream.CanWrite)
        {
            throw new ArgumentException("The target stream must be writable.", nameof(stream));
        }

        var normalizedChunkSizes = chunkSizes
            .Where(size => size > 0)
            .Distinct()
            .OrderByDescending(size => size)
            .ToArray();

        if (normalizedChunkSizes.Length == 0)
        {
            throw new ArgumentException("At least one positive chunk size is required.", nameof(chunkSizes));
        }

        var buffers = normalizedChunkSizes.ToDictionary(size => size, size => new byte[size]);
        long bytesWritten = 0;

        // Try large writes first for throughput, then smaller writes to consume the
        // remaining free space as closely as the filesystem will allow.
        foreach (var chunkSize in normalizedChunkSizes)
        {
            while (maxBytesToWrite is null || bytesWritten < maxBytesToWrite.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesRemainingForThisFile = maxBytesToWrite is null
                    ? chunkSize
                    : maxBytesToWrite.Value - bytesWritten;
                var writeSize = (int)Math.Min(chunkSize, bytesRemainingForThisFile);
                if (writeSize <= 0)
                {
                    break;
                }

                var buffer = buffers[chunkSize].AsMemory(0, writeSize);

                try
                {
                    await stream.WriteAsync(buffer, cancellationToken);
                    bytesWritten += writeSize;
                    progress?.Invoke(bytesWritten);
                }
                catch (IOException ex) when (IsDiskFull(ex))
                {
                    // Running out of free space is the expected terminal condition
                    // for a wipe pass, not a failure that should escape to the CLI.
                    break;
                }
            }
        }

        await stream.FlushAsync(cancellationToken);
        return bytesWritten;
    }

    internal static bool IsDiskFull(IOException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return (exception.HResult & 0xFFFF) == ErrorDiskFull;
    }
}
