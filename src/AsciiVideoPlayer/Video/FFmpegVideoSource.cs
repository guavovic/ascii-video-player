using System.Diagnostics;
using AsciiVideoPlayer.Media;

namespace AsciiVideoPlayer.Video;

public sealed class FFmpegVideoSource : IVideoSource
{
    private const int MaxDecodeWidth = 640;

    private readonly Process _process;
    private readonly Stream _output;
    private readonly byte[] _decoded;
    private readonly int _decodedWidth;
    private readonly int _decodedHeight;

    public FFmpegVideoSource(MediaInput input, MediaInfo info, TimeSpan start = default)
    {
        Width = info.Width;
        Height = info.Height;
        Fps = info.Fps;

        _decodedWidth = Even(Math.Min(info.Width, MaxDecodeWidth));
        _decodedHeight = Even((int)Math.Round((double)_decodedWidth * info.Height / info.Width));
        _decoded = new byte[_decodedWidth * _decodedHeight * VideoFrame.BytesPerPixel];

        _process = FFmpeg.Start("ffmpeg", [
            "-nostdin", "-v", "error",
            .. input.Arguments(start),
            "-map", "0:v:0",
            "-vf", $"{(input.Live ? "hflip," : "")}scale={_decodedWidth}:{_decodedHeight}:flags=area",
            "-r", FFmpeg.Format(info.Fps),
            "-f", "rawvideo", "-pix_fmt", "bgr24",
            "pipe:1"]);

        _output = _process.StandardOutput.BaseStream;
    }

    public int Width { get; }
    public int Height { get; }
    public double Fps { get; }

    public bool TryReadFrame(VideoFrame frame)
    {
        if (!TryReadDecoded())
            return false;

        AreaResampler.Resize(_decoded, _decodedWidth, _decodedHeight, frame);
        return true;
    }

    public bool SkipFrame() => TryReadDecoded();

    public void Dispose()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }

        _process.Dispose();
    }

    private bool TryReadDecoded() =>
        _output.ReadAtLeast(_decoded, _decoded.Length, throwOnEndOfStream: false) == _decoded.Length;

    private static int Even(int value) => Math.Max(2, value & ~1);
}
