namespace AsciiVideoPlayer.Playback;

public interface IPlaybackClock
{
    TimeSpan Elapsed { get; }

    void Start();
}
