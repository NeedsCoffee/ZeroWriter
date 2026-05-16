namespace Zerowriter;

public static class VolumePathParser
{
    public static string NormalizeVolumeRoot(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("A drive letter is required.", nameof(input));
        }

        var value = input.Trim();
        if (value.Length == 2 && value[1] == ':')
        {
            value = value[..1];
        }

        if (value.Length != 1 || !char.IsAsciiLetter(value[0]))
        {
            throw new ArgumentException("The drive must be a single letter such as C or C:.", nameof(input));
        }

        var letter = char.ToUpperInvariant(value[0]);
        return $"{letter}:\\";
    }
}
