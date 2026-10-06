namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiImage
{
    public const int NoColor = -1;

    public AsciiImage(int width, int height)
    {
        Width = width;
        Height = height;
        Characters = new char[width * height];
        Colors = new int[width * height];
        Backgrounds = new int[width * height];
        Array.Fill(Backgrounds, NoColor);
    }

    public int Width { get; }
    public int Height { get; }

    public char[] Characters { get; }

    public int[] Colors { get; }

    public int[] Backgrounds { get; }
}
