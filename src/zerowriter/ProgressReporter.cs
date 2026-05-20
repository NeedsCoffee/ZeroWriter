namespace Zerowriter;

public sealed class ProgressReporter
{
    private readonly long initialFreeSpace;
    private readonly Func<DateTimeOffset> timeProvider;
    private readonly Queue<Sample> samples = new();
    private int measuredUpdateCount;
    private double? lastKnownBytesPerSecond;
    private TimeSpan? lastKnownEta;

    public ProgressReporter(long initialFreeSpace, Func<DateTimeOffset>? timeProvider = null)
    {
        if (initialFreeSpace <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialFreeSpace), "Initial free space must be positive.");
        }

        this.initialFreeSpace = initialFreeSpace;
        this.timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
    }

    public ProgressSnapshot CreateSnapshot(long currentFreeSpace)
    {
        var now = timeProvider();
        var bytesRemaining = Math.Max(0, currentFreeSpace);
        var bytesConsumed = Math.Clamp(initialFreeSpace - bytesRemaining, 0, initialFreeSpace);
        var percentComplete = initialFreeSpace == 0
            ? 100.0
            : bytesConsumed * 100.0 / initialFreeSpace;

        var previousSample = samples.Count > 0 ? samples.Last() : (Sample?)null;
        samples.Enqueue(new Sample(now, bytesConsumed));
        TrimSamples(now);

        double? bytesPerSecond = null;
        TimeSpan? eta = null;

        // Estimate speed from a rolling window of samples rather than the latest
        // write callback, which keeps the display useful during bursty disk I/O.
        if (samples.Count >= 2 && (previousSample is null || bytesConsumed > previousSample.Value.BytesConsumed))
        {
            var first = samples.Peek();
            var elapsedSeconds = (now - first.Timestamp).TotalSeconds;
            var consumedDelta = bytesConsumed - first.BytesConsumed;

            if (elapsedSeconds > 0 && consumedDelta > 0)
            {
                measuredUpdateCount++;
                bytesPerSecond = consumedDelta / elapsedSeconds;
                eta = TimeSpan.FromSeconds(bytesRemaining / bytesPerSecond.Value);
                lastKnownBytesPerSecond = bytesPerSecond;
                lastKnownEta = eta;
            }
        }

        // Suppress early estimates until there is enough movement to avoid showing
        // misleading speeds and ETAs during startup.
        if (measuredUpdateCount >= 2)
        {
            bytesPerSecond ??= lastKnownBytesPerSecond;
            eta ??= lastKnownEta;
        }
        else
        {
            bytesPerSecond = null;
            eta = null;
        }

        return new ProgressSnapshot(
            initialFreeSpace,
            bytesConsumed,
            bytesRemaining,
            percentComplete,
            bytesPerSecond,
            eta);
    }

    public static string Render(ProgressSnapshot snapshot, int barWidth = 24)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(barWidth);

        var filledWidth = (int)Math.Round(snapshot.PercentComplete / 100.0 * barWidth, MidpointRounding.AwayFromZero);
        filledWidth = Math.Clamp(filledWidth, 0, barWidth);

        var bar = new string('#', filledWidth) + new string('-', barWidth - filledWidth);
        var speed = snapshot.BytesPerSecond is null
            ? "speed calculating"
            : $"{VolumeZeroWriter.FormatBytes((long)snapshot.BytesPerSecond.Value)}/s";
        var eta = snapshot.EstimatedTimeRemaining is null
            ? "ETA calculating"
            : $"ETA {snapshot.EstimatedTimeRemaining.Value:hh\\:mm\\:ss}";

        return
            $"[{bar}] {snapshot.PercentComplete,5:0.0}%  " +
            $"{VolumeZeroWriter.FormatBytes(snapshot.BytesConsumed)} / {VolumeZeroWriter.FormatBytes(snapshot.InitialFreeSpace)}  " +
            $"left {VolumeZeroWriter.FormatBytes(snapshot.BytesRemaining)}  " +
            $"{speed}  {eta}";
    }

    private void TrimSamples(DateTimeOffset now)
    {
        while (samples.Count > 1 && now - samples.Peek().Timestamp > TimeSpan.FromSeconds(15))
        {
            samples.Dequeue();
        }
    }

    private readonly record struct Sample(DateTimeOffset Timestamp, long BytesConsumed);
}

public readonly record struct ProgressSnapshot(
    long InitialFreeSpace,
    long BytesConsumed,
    long BytesRemaining,
    double PercentComplete,
    double? BytesPerSecond,
    TimeSpan? EstimatedTimeRemaining);
