namespace Zerowriter.Tests;

public sealed class ZeroFillEngineTests
{
    [Fact]
    public async Task FillAsync_WritesUntilDiskFullUsingSmallerChunks()
    {
        await using var stream = new DiskFullStream(capacity: 10);

        var bytesWritten = await ZeroFillEngine.FillAsync(stream, [8, 4, 1], CancellationToken.None);

        Assert.Equal(10, bytesWritten);
    }

    [Fact]
    public async Task FillAsync_RethrowsUnexpectedIoErrors()
    {
        await using var stream = new ThrowingStream(new IOException("boom"));

        var exception = await Assert.ThrowsAsync<IOException>(() =>
            ZeroFillEngine.FillAsync(stream, [8, 4, 1], CancellationToken.None));

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public async Task FillAsync_HonorsCancellation()
    {
        using var cts = new CancellationTokenSource();
        await using var stream = new CancelAfterFirstWriteStream(cts);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ZeroFillEngine.FillAsync(stream, [8, 4, 1], cts.Token));
    }

    [Fact]
    public async Task FillAsync_StopsAtRequestedByteLimit()
    {
        await using var stream = new MemoryStream();

        var bytesWritten = await ZeroFillEngine.FillAsync(
            stream,
            [8, 4, 1],
            CancellationToken.None,
            maxBytesToWrite: 10);

        Assert.Equal(10, bytesWritten);
        Assert.Equal(10, stream.Length);
    }

    private sealed class DiskFullStream : MemoryStream
    {
        private readonly long capacity;

        public DiskFullStream(long capacity)
        {
            this.capacity = capacity;
        }

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

    private sealed class ThrowingStream(IOException exception) : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw exception;

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException(exception);
    }

    private sealed class CancelAfterFirstWriteStream(CancellationTokenSource cts) : MemoryStream
    {
        private bool hasWritten;

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!hasWritten)
            {
                hasWritten = true;
                cts.Cancel();
            }

            return base.WriteAsync(buffer, cancellationToken);
        }
    }
}
