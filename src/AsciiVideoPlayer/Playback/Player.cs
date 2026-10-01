using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Playback;

public sealed class Player(
    IVideoSource video,
    AsciiConverter converter,
    TerminalRenderer renderer,
    IPlaybackClock clock,
    double fps,
    int? maxWidth)
{
    public void Play(CancellationToken cancellationToken)
    {
        var displayInterval = TimeSpan.FromSeconds(1 / Math.Min(fps, video.Fps));
        var nextDisplay = TimeSpan.Zero;
        long nextFrameIndex = 0;
        var frame = new VideoFrame(0, 0);
        var image = new AsciiImage(0, 0);

        clock.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var now = clock.Elapsed;

            if (now < nextDisplay)
            {
                clock.WaitUntil(nextDisplay);
                continue;
            }

            long currentFrameIndex = (long)(now.TotalSeconds * video.Fps);

            for (; nextFrameIndex < currentFrameIndex; nextFrameIndex++)
            {
                if (!video.SkipFrame())
                    return;
            }

            var layout = renderer.Fit(video.Width, video.Height, maxWidth);

            if (frame.Width != layout.Columns || frame.Height != layout.Rows)
            {
                frame = new VideoFrame(layout.Columns, layout.Rows);
                image = new AsciiImage(layout.Columns, layout.Rows);
            }

            if (!video.TryReadFrame(frame))
                return;

            nextFrameIndex++;
            converter.Convert(frame, image);
            renderer.Draw(image, layout);

            nextDisplay += displayInterval;

            if (nextDisplay < now)
                nextDisplay = now;

            if (renderer.EscapePressed())
                return;
        }
    }
}
