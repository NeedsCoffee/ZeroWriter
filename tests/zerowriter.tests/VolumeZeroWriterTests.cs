namespace Zerowriter.Tests;

public sealed class VolumeZeroWriterTests
{
    [Fact]
    public async Task FileStreamFactory_LeavesCompletedFilesInPlaceUntilOperationCleanup()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"zerowriter-filefactory-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var operation = VolumeWipeOperation.Create(rootPath);
            var factory = new FileVolumeWriteStreamFactory();
            var firstPath = operation.CreateNextWipeFilePath();
            var secondPath = operation.CreateNextWipeFilePath();

            await using (var firstStream = await factory.OpenAsync(firstPath, CancellationToken.None))
            {
                await firstStream.WriteAsync(new byte[] { 1, 2, 3 }, CancellationToken.None);
            }

            Assert.True(File.Exists(firstPath));

            await using (var secondStream = await factory.OpenAsync(secondPath, CancellationToken.None))
            {
                await secondStream.WriteAsync(new byte[] { 4, 5, 6 }, CancellationToken.None);
            }

            Assert.True(File.Exists(firstPath));
            Assert.True(File.Exists(secondPath));

            operation.Cleanup();

            Assert.False(Directory.Exists(operation.WorkspacePath));
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WipeFreeSpaceAsync_SplitsAcrossMultipleFilesWhenPolicyCapsFileSize()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"zerowriter-multi-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var writer = new VolumeZeroWriter();
            var operation = VolumeWipeOperation.Create(rootPath);
            var policy = VolumeWritePolicy.Create("NTFS", requestedMaxFileSizeBytes: 5);
            var factory = new TestStreamFactory([5, 5, 2]);

            var bytesWritten = await writer.WipeFreeSpaceAsync(operation, policy, factory, CancellationToken.None);

            Assert.Equal(12, bytesWritten);
            Assert.Equal(["wipe-0001.bin", "wipe-0002.bin", "wipe-0003.bin"], factory.CreatedFiles);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WipeFreeSpaceAsync_TreatsDiskFullWhileOpeningNextFileAsSuccessfulCompletion()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"zerowriter-openfull-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var writer = new VolumeZeroWriter();
            var operation = VolumeWipeOperation.Create(rootPath);
            var policy = VolumeWritePolicy.Create("NTFS", requestedMaxFileSizeBytes: 5);
            var factory = new OpenThenDiskFullFactory();

            var bytesWritten = await writer.WipeFreeSpaceAsync(operation, policy, factory, CancellationToken.None);

            Assert.Equal(5, bytesWritten);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    private sealed class TestStreamFactory(IReadOnlyList<long> capacities) : IVolumeWriteStreamFactory
    {
        private int index;

        public List<string> CreatedFiles { get; } = [];

        public ValueTask<Stream> OpenAsync(string path, CancellationToken cancellationToken)
        {
            CreatedFiles.Add(Path.GetFileName(path));
            Stream stream = new CapacityStream(capacities[index++]);
            return ValueTask.FromResult(stream);
        }
    }

    private sealed class CapacityStream(long capacity) : MemoryStream
    {
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (Length + buffer.Length > capacity)
            {
                throw CreateDiskFullException();
            }

            return base.WriteAsync(buffer, cancellationToken);
        }

        private static IOException CreateDiskFullException()
        {
            var exception = new IOException("There is not enough space on the disk.");
            typeof(Exception)
                .GetProperty(nameof(Exception.HResult))!
                .SetValue(exception, unchecked((int)0x80070070));
            return exception;
        }
    }

    private sealed class OpenThenDiskFullFactory : IVolumeWriteStreamFactory
    {
        private int openCount;

        public ValueTask<Stream> OpenAsync(string path, CancellationToken cancellationToken)
        {
            openCount++;
            if (openCount == 1)
            {
                Stream stream = new CapacityStream(5);
                return ValueTask.FromResult(stream);
            }

            return ValueTask.FromException<Stream>(CreateDiskFullException());
        }

        private static IOException CreateDiskFullException()
        {
            var exception = new IOException("There is not enough space on the disk.");
            typeof(Exception)
                .GetProperty(nameof(Exception.HResult))!
                .SetValue(exception, unchecked((int)0x80070070));
            return exception;
        }
    }
}
