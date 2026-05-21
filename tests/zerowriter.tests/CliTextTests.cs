namespace Zerowriter.Tests;

public sealed class CliTextTests
{
    [Fact]
    public void RenderHeader_IncludesAppNameAndVersion()
    {
        Assert.Equal("ZeroWriter 1.0.1", CliText.RenderHeader("1.0.1"));
    }

    [Fact]
    public void RenderUsage_IncludesAppNameVersionAndSingleUsageLine()
    {
        var text = CliText.RenderUsage("1.0.1");

        Assert.StartsWith("ZeroWriter 1.0.1", text);
        Assert.Equal(1, CountOccurrences(text, "Usage: zerowriter <drive-letter> [-m|--max-file-size <size>]"));
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
