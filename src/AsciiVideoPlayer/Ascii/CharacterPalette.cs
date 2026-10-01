namespace AsciiVideoPlayer.Ascii;

public sealed class CharacterPalette(string characters)
{
    public const string DefaultCharacters = " .,:;i1tfLCOG08@#";

    private const int MaxBrightness = 256 * 3;

    public char ForBrightness(int brightness) =>
        characters[brightness * characters.Length / MaxBrightness];
}
