using Zerowriter;

using var shutdown = new ShutdownCoordinator();
var hideCursor = !Console.IsOutputRedirected;
var previousCursorVisible = true;

if (hideCursor)
{
    try
    {
        previousCursorVisible = Console.CursorVisible;
        Console.CursorVisible = false;
    }
    catch
    {
        hideCursor = false;
    }
}

try
{
    AppOptions options;
    try
    {
        options = AppOptions.Parse(args);
    }
    catch (ArgumentException ex)
    {
        Console.Error.WriteLine(ex.Message);
        PrintUsage();
        return 1;
    }

    try
    {
        var writer = new VolumeZeroWriter();
        var operation = writer.CreateOperation(options.VolumeRoot);
        var policy = writer.CreatePolicy(options.VolumeRoot, options.RequestedMaxFileSizeBytes);
        shutdown.Register(operation.Cleanup);
        Console.WriteLine($"Zero-filling free space on {options.VolumeRoot}");
        Console.WriteLine($"Filesystem: {policy.DriveFormat}; max temp file size: {DescribeMaxFileSize(policy)}");
        if (policy.UsedFat32AutoCap)
        {
            Console.WriteLine("Applied FAT32-safe file splitting automatically.");
        }
        else if (policy.WasClampedToFilesystemLimit)
        {
            Console.WriteLine("Requested max file size exceeded the FAT32 limit and was clamped.");
        }

        var bytesWritten = await writer.WipeFreeSpaceAsync(operation, policy, shutdown.Token);
        Console.WriteLine($"Completed. Wrote {VolumeZeroWriter.FormatBytes(bytesWritten)} of zero data.");
        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Cancellation requested. Cleanup completed or was attempted.");
        return 3;
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 2;
    }
}
finally
{
    if (hideCursor)
    {
        try
        {
            Console.CursorVisible = previousCursorVisible;
        }
        catch
        {
        }
    }
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: zerowriter <drive-letter> [--max-file-size <size>]");
    Console.Error.WriteLine("Example: zerowriter C: --max-file-size 4095MiB");
}

static string DescribeMaxFileSize(VolumeWritePolicy policy) =>
    policy.MaxFileSizeBytes is null
        ? "unlimited"
        : VolumeZeroWriter.FormatBytes(policy.MaxFileSizeBytes.Value);
