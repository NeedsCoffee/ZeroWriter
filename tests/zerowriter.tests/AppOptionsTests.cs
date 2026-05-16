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

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Parse_RejectsInvalidMaxFileSize(string value)
    {
        Assert.Throws<ArgumentException>(() => AppOptions.Parse(["E:", "--max-file-size", value]));
    }
}
