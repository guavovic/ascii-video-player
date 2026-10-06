namespace AsciiVideoPlayer.Playback;

public interface IPlaybackClock
{
    TimeSpan Elapsed { get; }

    void Start();

    void Pause();

    void Resume();

    void WaitUntil(TimeSpan position)
    {
        var remaining = position - Elapsed;

        if (remaining > TimeSpan.Zero)
            Thread.Sleep(remaining);
    }
}
