using System.Runtime.InteropServices;
using OpenCvSharp;

namespace AsciiVideoPlayer.Video;

public sealed class OpenCvVideoSource : IVideoSource
{
    private const double FallbackFps = 30;

    private readonly VideoCapture _capture;
    private readonly Mat _mat = new();
    private readonly Mat _resized = new();

    private OpenCvVideoSource(VideoCapture capture)
    {
        _capture = capture;
    }

    public int Width => _capture.FrameWidth;
    public int Height => _capture.FrameHeight;
    public double Fps => _capture.Fps > 0 ? _capture.Fps : FallbackFps;

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

        if (_mat.Type() != MatType.CV_8UC3)
            throw new InvalidOperationException($"Formato de quadro inesperado: {_mat.Type()}.");

        Cv2.Resize(_mat, _resized, new Size(frame.Width, frame.Height), interpolation: InterpolationFlags.Area);
        Marshal.Copy(_resized.Data, frame.Pixels, 0, frame.Pixels.Length);
        return true;
    }

    public bool SkipFrame() => _capture.Grab();

    public void Dispose()
    {
        _resized.Dispose();
        _mat.Dispose();
        _capture.Dispose();
    }
}
