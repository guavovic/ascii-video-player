using System.Diagnostics;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Playback;

public sealed class Player(
    IVideoSource video,
    AsciiConverter converter,
    TerminalRenderer renderer,
    IAudioPlayer? audio,
    double fps,
    int? maxWidth)
{
    private readonly int _frameStep = Math.Max(1, (int)Math.Round(video.Fps / fps));

    public void Play(CancellationToken cancellationToken)
    {
        int frameDelay = (int)(1000 * _frameStep / video.Fps);
        var stopwatch = new Stopwatch();
        bool firstFrame = true;
        var frame = new VideoFrame(0, 0);
        var image = new AsciiImage(0, 0);

        while (!cancellationToken.IsCancellationRequested)
        {
            var layout = renderer.Fit(video.Width, video.Height, maxWidth);

            if (frame.Width != layout.Columns || frame.Height != layout.Rows)
            {
                frame = new VideoFrame(layout.Columns, layout.Rows);
                image = new AsciiImage(layout.Columns, layout.Rows);
            }

            if (!video.TryReadFrame(frame))
                break;

            audio?.BufferAhead(firstFrame ? 2 : 1);
            firstFrame = false;

            SkipFrames();
            converter.Convert(frame, image);
            renderer.Draw(image, layout);
            WaitForNextFrame(frameDelay, stopwatch);

            if (TerminalRenderer.EscapePressed())
                break;
        }
    }

    private void SkipFrames()
    {
        for (int i = 0; i < _frameStep - 1; i++)
            video.SkipFrame();
    }

    private static void WaitForNextFrame(int frameDelay, Stopwatch stopwatch)
    {
        int remaining = frameDelay - (int)stopwatch.ElapsedMilliseconds;

        if (remaining > 0)
            Thread.Sleep(remaining);

        stopwatch.Restart();
    }
}
