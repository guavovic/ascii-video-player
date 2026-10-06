namespace AsciiVideoPlayer.Playback;

public static class PlaybackLoop
{
    // Cada rodada abre o vídeo e o áudio a partir de uma posição: pular é só recomeçar mais adiante.
    public static int Run(Func<TimeSpan, bool, PlaybackEnd> playFrom, TimeSpan start, bool loop)
    {
        int rounds = 0;
        bool paused = false;

        while (true)
        {
            var end = playFrom(start, paused);
            rounds++;

            switch (end.Reason)
            {
                case PlaybackEndReason.Seek:
                    start = end.SeekPosition;
                    paused = end.Paused;
                    break;
                case PlaybackEndReason.Finished when loop:
                    start = TimeSpan.Zero;
                    paused = false;
                    break;
                default:
                    return rounds;
            }
        }
    }
}
