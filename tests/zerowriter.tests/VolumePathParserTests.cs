namespace Zerowriter.Tests;

public sealed class VolumePathParserTests
{
    [Theory]
    [InlineData("C", "C:\\")]
    [InlineData("c:", "C:\\")]
    [InlineData("Z", "Z:\\")]
    public void NormalizeAcceptsDriveLetter(string input, string expectedRoot)
    {
        var root = VolumePathParser.NormalizeVolumeRoot(input);

        Assert.Equal(expectedRoot, root);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("AB")]
    [InlineData("1")]
    [InlineData("C:/")]
    public void NormalizeRejectsInvalidValues(string input)
    {
        Assert.Throws<ArgumentException>(() => VolumePathParser.NormalizeVolumeRoot(input));
    }
}
