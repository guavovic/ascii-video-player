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
    int? maxWidth,
    TimeSpan start = default,
    TimeSpan duration = default,
    bool startPaused = false)
{
    public static readonly TimeSpan ShortSeek = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan LongSeek = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan StatusTime = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PausePoll = TimeSpan.FromMilliseconds(30);

    private VideoFrame _frame = converter.CreateFrame(0, 0);
    private AsciiImage _image = new(0, 0);
    private FrameLayout _layout;

    // Depois de pular, a barra aparece por alguns segundos para mostrar onde o vídeo está.
    private TimeSpan _statusUntil = start > TimeSpan.Zero ? StatusTime : TimeSpan.Zero;

    private TimeSpan Position => start + clock.Elapsed;

    public PlaybackEnd Play(CancellationToken cancellationToken)
    {
        var displayInterval = TimeSpan.FromSeconds(1 / Math.Min(fps, video.Fps));
        var nextDisplay = TimeSpan.Zero;
        long nextFrameIndex = 0;
        bool pausePending = startPaused;

        clock.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = renderer.ReadKey();

            if (key is ConsoleKey.Spacebar)
                pausePending = true;
            else if (HandleKey(key, paused: false) is { } end)
                return end;

            if (pausePending && _image.Width > 0)
            {
                pausePending = false;

                if (Pause(cancellationToken) is { } stop)
                    return stop;

                _statusUntil = clock.Elapsed + StatusTime;
                continue;
            }

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
                    return PlaybackEnd.Finished;
            }

            _layout = renderer.Fit(video.Width, video.Height, maxWidth);

            if (_image.Width != _layout.Columns || _image.Height != _layout.Rows)
            {
                _frame = converter.CreateFrame(_layout.Columns, _layout.Rows);
                _image = new AsciiImage(_layout.Columns, _layout.Rows);
            }

            if (!video.TryReadFrame(_frame))
                return PlaybackEnd.Finished;

            nextFrameIndex++;
            converter.Convert(_frame, _image);
            renderer.Draw(_image, _layout, now < _statusUntil ? Status(paused: false) : default);

            nextDisplay += displayInterval;

            if (nextDisplay < now)
                nextDisplay = now;
        }

        return PlaybackEnd.Stopped;
    }

    private PlaybackEnd? Pause(CancellationToken cancellationToken)
    {
        clock.Pause();
        renderer.Draw(_image, _layout, Status(paused: true));

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = renderer.ReadKey();

            if (key is ConsoleKey.Spacebar)
            {
                clock.Resume();
                return null;
            }

            if (HandleKey(key, paused: true) is { } end)
                return end;

            Thread.Sleep(PausePoll);
        }

        return PlaybackEnd.Stopped;
    }

    private PlaybackEnd? HandleKey(ConsoleKey? key, bool paused) => key switch
    {
        ConsoleKey.Escape or ConsoleKey.Q => PlaybackEnd.Stopped,
        ConsoleKey.RightArrow => SeekBy(ShortSeek, paused),
        ConsoleKey.LeftArrow => SeekBy(-ShortSeek, paused),
        ConsoleKey.UpArrow => SeekBy(LongSeek, paused),
        ConsoleKey.DownArrow => SeekBy(-LongSeek, paused),
        _ => null,
    };

    private PlaybackEnd SeekBy(TimeSpan offset, bool paused)
    {
        var target = Position + offset;

        if (target < TimeSpan.Zero)
            target = TimeSpan.Zero;

        if (duration > TimeSpan.Zero && target >= duration)
            return PlaybackEnd.Finished;

        return PlaybackEnd.SeekTo(target, paused);
    }

    private Overlay Status(bool paused) =>
        new(Status: ProgressBar.Format(Position, duration, paused, renderer.Columns));
}
