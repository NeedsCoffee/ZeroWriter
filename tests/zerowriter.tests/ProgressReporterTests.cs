namespace Zerowriter.Tests;

public sealed class ProgressReporterTests
{
    [Fact]
    public void CreateSnapshot_UsesLiveFreeSpaceForProgressAndRate()
    {
        var now = DateTimeOffset.UnixEpoch;
        var reporter = new ProgressReporter(
            initialFreeSpace: 1_000,
            timeProvider: () => now);

        reporter.CreateSnapshot(currentFreeSpace: 1_000);
        now = now.AddSeconds(2);
        var warmup = reporter.CreateSnapshot(currentFreeSpace: 600);
        now = now.AddSeconds(2);

        var snapshot = reporter.CreateSnapshot(currentFreeSpace: 200);

        Assert.Equal(1_000, snapshot.InitialFreeSpace);
        Assert.Equal(800, snapshot.BytesConsumed);
        Assert.Equal(200, snapshot.BytesRemaining);
        Assert.Equal(80.0, snapshot.PercentComplete, 3);
        Assert.Equal(200, snapshot.BytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(1), snapshot.EstimatedTimeRemaining);
        Assert.Null(warmup.BytesPerSecond);
        Assert.Null(warmup.EstimatedTimeRemaining);
    }

    [Fact]
    public void CreateSnapshot_OmitsEtaWhenRateIsUnavailable()
    {
        var now = DateTimeOffset.UnixEpoch;
        var reporter = new ProgressReporter(
            initialFreeSpace: 1_000,
            timeProvider: () => now);

        var snapshot = reporter.CreateSnapshot(currentFreeSpace: 1_000);

        Assert.Null(snapshot.BytesPerSecond);
        Assert.Null(snapshot.EstimatedTimeRemaining);
    }

    [Fact]
    public void Render_IncludesBarPercentageRateAndEta()
    {
        var snapshot = new ProgressSnapshot(
            InitialFreeSpace: 1_000,
            BytesConsumed: 400,
            BytesRemaining: 600,
            PercentComplete: 40.0,
            BytesPerSecond: 200,
            EstimatedTimeRemaining: TimeSpan.FromSeconds(3));

        var line = ProgressReporter.Render(snapshot, barWidth: 10);

        Assert.Contains("[####------]", line);
        Assert.Contains("40.0%", line);
        Assert.Contains("400.00 B / 1000.00 B", line);
        Assert.Contains("left 600.00 B", line);
        Assert.Contains("200.00 B/s", line);
        Assert.Contains("ETA 00:00:03", line);
    }

    [Fact]
    public void Render_ShowsCalculatingWhenEtaUnavailable()
    {
        var snapshot = new ProgressSnapshot(
            InitialFreeSpace: 1_000,
            BytesConsumed: 0,
            BytesRemaining: 1_000,
            PercentComplete: 0.0,
            BytesPerSecond: null,
            EstimatedTimeRemaining: null);

        var line = ProgressReporter.Render(snapshot, barWidth: 10);

        Assert.Contains("ETA calculating", line);
    }

    [Fact]
    public void CreateSnapshot_ReusesLastKnownSpeedAndEtaWhenCurrentSampleCannotRefreshThem()
    {
        var now = DateTimeOffset.UnixEpoch;
        var reporter = new ProgressReporter(
            initialFreeSpace: 1_000,
            timeProvider: () => now);

        reporter.CreateSnapshot(currentFreeSpace: 1_000);
        now = now.AddSeconds(2);
        var firstMeasured = reporter.CreateSnapshot(currentFreeSpace: 600);

        now = now.AddSeconds(2);
        var stalled = reporter.CreateSnapshot(currentFreeSpace: 600);

        Assert.Equal(firstMeasured.BytesPerSecond, stalled.BytesPerSecond);
        Assert.Equal(firstMeasured.EstimatedTimeRemaining, stalled.EstimatedTimeRemaining);
    }

    [Fact]
    public void CreateSnapshot_HidesSpeedAndEtaUntilSecondMeasuredUpdate()
    {
        var now = DateTimeOffset.UnixEpoch;
        var reporter = new ProgressReporter(
            initialFreeSpace: 1_000,
            timeProvider: () => now);

        var first = reporter.CreateSnapshot(currentFreeSpace: 1_000);
        now = now.AddSeconds(2);
        var second = reporter.CreateSnapshot(currentFreeSpace: 600);
        now = now.AddSeconds(2);
        var third = reporter.CreateSnapshot(currentFreeSpace: 200);

        Assert.Null(first.BytesPerSecond);
        Assert.Null(first.EstimatedTimeRemaining);
        Assert.Null(second.BytesPerSecond);
        Assert.Null(second.EstimatedTimeRemaining);
        Assert.NotNull(third.BytesPerSecond);
        Assert.NotNull(third.EstimatedTimeRemaining);
    }
}
