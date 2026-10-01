using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Fakes;

public sealed class FakeClock : IPlaybackClock
{
    public TimeSpan Elapsed { get; private set; }

    public bool Started { get; private set; }

    public void Start() => Started = true;

    public void WaitUntil(TimeSpan position)
    {
        if (position > Elapsed)
            Elapsed = position;
    }

    public void Advance(TimeSpan time) => Elapsed += time;
}
