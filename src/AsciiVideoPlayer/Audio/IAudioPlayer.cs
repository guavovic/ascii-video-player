namespace AsciiVideoPlayer.Audio;

public interface IAudioPlayer : IDisposable
{
    void BufferAhead(int seconds);
}
