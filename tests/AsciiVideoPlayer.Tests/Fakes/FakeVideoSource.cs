using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Tests.Fakes;

public sealed class FakeVideoSource(int frameCount, double fps) : IVideoSource
{
    private int _position;

    public int Width => 16;
    public int Height => 9;
    public double Fps => fps;

    public List<int> ReadFrames { get; } = [];

    public bool TryReadFrame(VideoFrame frame)
    {
        if (_position >= frameCount)
            return false;

        ReadFrames.Add(_position++);
        return true;
    }

    public bool SkipFrame()
    {
        if (_position >= frameCount)
            return false;

        _position++;
        return true;
    }

    public void Dispose()
    {
    }
}
