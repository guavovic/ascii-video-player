namespace AsciiVideoPlayer.Video;

public interface IVideoSource : IDisposable
{
    int Width { get; }
    int Height { get; }
    double Fps { get; }

    bool TryReadFrame(VideoFrame frame);
    bool SkipFrame();
}
