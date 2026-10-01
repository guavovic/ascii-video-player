namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiImage(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;

    public char[] Characters { get; } = new char[width * height];

    public int[] Colors { get; } = new int[width * height];
}
