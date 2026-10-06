using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Fakes;

public sealed class FakeClock : IPlaybackClock
{
    public TimeSpan Elapsed { get; private set; }

    public bool Started { get; private set; }

    public bool Paused { get; private set; }

    public void Start() => Started = true;

    public void Pause() => Paused = true;

    public void Resume() => Paused = false;

    public void WaitUntil(TimeSpan position)
    {
        if (position > Elapsed)
            Elapsed = position;
    }

    public void Advance(TimeSpan time) => Elapsed += time;
}
