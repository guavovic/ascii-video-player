using System.Diagnostics;

namespace AsciiVideoPlayer.Playback;

public sealed class StopwatchClock : IPlaybackClock
{
    private readonly Stopwatch _stopwatch = new();

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Start() => _stopwatch.Start();
}
