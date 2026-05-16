namespace Zerowriter;

public sealed class FileVolumeWriteStreamFactory : IVolumeWriteStreamFactory
{
    public ValueTask<Stream> OpenAsync(string path, CancellationToken cancellationToken)
    {
        Stream stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough
            });

        return ValueTask.FromResult(stream);
    }
}
