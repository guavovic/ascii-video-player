namespace AsciiVideoPlayer.Video;

public sealed class VideoFrame(int width, int height)
{
    public int Width { get; } = width;
    public int Height { get; } = height;

    public byte[] Pixels { get; } = new byte[width * height * BytesPerPixel];

    public const int BytesPerPixel = 3;

    public int GetBrightness(int x, int y)
    {
        int index = (y * Width + x) * BytesPerPixel;
        return Pixels[index] + Pixels[index + 1] + Pixels[index + 2];
    }
}
