namespace AsciiVideoPlayer.Ascii;

public sealed class CharacterPalette(string characters)
{
    private const int MaxBrightness = 256 * 3;

    public static CharacterPalette Default { get; } = new(" .,:;i1tfLCOG08@#");

    public char ForBrightness(int brightness) =>
        characters[brightness * characters.Length / MaxBrightness];
}
