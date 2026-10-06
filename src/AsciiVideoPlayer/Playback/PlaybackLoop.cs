namespace AsciiVideoPlayer.Playback;

public static class PlaybackLoop
{
    public static int Run(Func<PlaybackEnd> playOnce, bool loop)
    {
        int rounds = 0;
        PlaybackEnd end;

        do
        {
            end = playOnce();
            rounds++;
        }
        while (loop && end == PlaybackEnd.Finished);

        return rounds;
    }
}
