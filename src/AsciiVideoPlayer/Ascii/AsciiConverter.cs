using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiConverter(IImageStyle style)
{
    public VideoFrame CreateFrame(int columns, int rows) =>
        new(columns * style.PixelsPerColumn, rows * style.PixelsPerRow);

    public void Convert(VideoFrame frame, AsciiImage image) => style.Convert(frame, image);
}
