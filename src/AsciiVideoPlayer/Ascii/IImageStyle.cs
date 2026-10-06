using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public interface IImageStyle
{
    int PixelsPerColumn { get; }
    int PixelsPerRow { get; }

    void Convert(VideoFrame frame, AsciiImage image);
}
