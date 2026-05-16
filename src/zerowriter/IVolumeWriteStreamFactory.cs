namespace Zerowriter;

public interface IVolumeWriteStreamFactory
{
    ValueTask<Stream> OpenAsync(string path, CancellationToken cancellationToken);
}
