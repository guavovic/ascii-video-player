namespace AsciiVideoPlayer.Playback;

public enum PlaybackEndReason
{
    Finished,
    Stopped,
    Seek,
}

// Ao pular durante a pausa, a próxima rodada já começa pausada.
public readonly record struct PlaybackEnd(PlaybackEndReason Reason, TimeSpan SeekPosition = default, bool Paused = false)
{
    public static PlaybackEnd Finished => new(PlaybackEndReason.Finished);

    public static PlaybackEnd Stopped => new(PlaybackEndReason.Stopped);

    public static PlaybackEnd SeekTo(TimeSpan position, bool paused = false) => new(PlaybackEndReason.Seek, position, paused);
}
