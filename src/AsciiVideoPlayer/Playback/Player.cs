using System.Diagnostics;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Playback;

public sealed class Player(
    IVideoSource video,
    AsciiConverter converter,
    TerminalRenderer renderer,
    IAudioPlayer? audio,
    int frameStep)
{
    public void Play()
    {
        int frameDelay = (int)(1000 / ((int)video.Fps / frameStep));
        var frame = new VideoFrame(video.Width, video.Height);
        var stopwatch = new Stopwatch();
        bool firstFrame = true;

        while (video.TryReadFrame(frame))
        {
            SkipFrames();

            audio?.BufferAhead(firstFrame ? 2 : 1);
            firstFrame = false;

            SkipFrames();
            renderer.Draw(converter.Convert(frame));
            WaitForNextFrame(frameDelay, stopwatch);

            if (TerminalRenderer.EscapePressed())
                break;
        }
    }

    private void SkipFrames()
    {
        for (int i = 0; i < frameStep - 1; i++)
            video.SkipFrame();
    }

    private static void WaitForNextFrame(int frameDelay, Stopwatch stopwatch)
    {
        int remaining = frameDelay - (int)stopwatch.ElapsedMilliseconds;

        if (remaining > 0)
            Thread.Sleep(remaining);

        stopwatch.Restart();
    }
}
