namespace Zerowriter.Tests;

public sealed class AppOptionsTests
{
    [Fact]
    public void Parse_UsesDriveLetterOnlyByDefault()
    {
        var options = AppOptions.Parse(["E:"]);

        Assert.Equal("E:\\", options.VolumeRoot);
        Assert.Null(options.RequestedMaxFileSizeBytes);
    }

    [Fact]
    public void Parse_AcceptsMaxFileSizeWithUnits()
    {
        var options = AppOptions.Parse(["E:", "--max-file-size", "4095MiB"]);

        Assert.Equal(4_293_918_720L, options.RequestedMaxFileSizeBytes);
    }

    [Fact]
    public void Parse_AcceptsShortMaxFileSizeOption()
    {
        var options = AppOptions.Parse(["E:", "-m", "2g"]);

        Assert.Equal(2L * 1024L * 1024L * 1024L, options.RequestedMaxFileSizeBytes);
    }

    [Theory]
    [InlineData("2g", 2L * 1024L * 1024L * 1024L)]
    [InlineData("2gb", 2L * 1024L * 1024L * 1024L)]
    [InlineData("512m", 512L * 1024L * 1024L)]
    [InlineData("512mb", 512L * 1024L * 1024L)]
    [InlineData("64k", 64L * 1024L)]
    [InlineData("64kb", 64L * 1024L)]
    [InlineData("64kib", 64L * 1024L)]
    public void Parse_AcceptsSimpleMaxFileSizeSuffixes(string value, long expectedBytes)
    {
        var options = AppOptions.Parse(["E:", "--max-file-size", value]);

        Assert.Equal(expectedBytes, options.RequestedMaxFileSizeBytes);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Parse_RejectsInvalidMaxFileSize(string value)
    {
        Assert.Throws<ArgumentException>(() => AppOptions.Parse(["E:", "--max-file-size", value]));
    }
}
