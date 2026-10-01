using System.Runtime.InteropServices;
using OpenCvSharp;

namespace AsciiVideoPlayer.Video;

public sealed class OpenCvVideoSource : IVideoSource
{
    private readonly VideoCapture _capture;
    private readonly Mat _mat = new();

    private OpenCvVideoSource(VideoCapture capture)
    {
        _capture = capture;
    }

    public int Width => _capture.FrameWidth;
    public int Height => _capture.FrameHeight;
    public double Fps => _capture.Fps;

    public static OpenCvVideoSource? TryOpen(string path)
    {
        var capture = new VideoCapture(path, VideoCaptureAPIs.ANY);

        if (capture.IsOpened())
            return new OpenCvVideoSource(capture);

        capture.Dispose();
        return null;
    }

    public bool TryReadFrame(VideoFrame frame)
    {
        if (!_capture.Read(_mat) || _mat.Empty())
            return false;

        if (_mat.Width != frame.Width || _mat.Height != frame.Height || _mat.Type() != MatType.CV_8UC3)
            throw new InvalidOperationException($"Quadro inesperado: {_mat.Width}x{_mat.Height} {_mat.Type()}.");

        Marshal.Copy(_mat.Data, frame.Pixels, 0, frame.Pixels.Length);
        return true;
    }

    public void SkipFrame() => _capture.Grab();

    public void Dispose()
    {
        _mat.Dispose();
        _capture.Dispose();
    }
}
