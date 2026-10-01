namespace AsciiVideoPlayer.Video;

public sealed class VideoFrame(int width, int height)
{
    public const int BytesPerPixel = 3;

    public int Width { get; } = width;
    public int Height { get; } = height;

    public byte[] Pixels { get; } = new byte[width * height * BytesPerPixel];
}
