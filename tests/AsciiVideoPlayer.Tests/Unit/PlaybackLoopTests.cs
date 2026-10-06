using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class PlaybackLoopTests
{
    [Fact]
    public void Sem_loop_toca_uma_vez()
    {
        PlaybackLoop.Run(() => PlaybackEnd.Finished, loop: false).ShouldBe(1);
    }

    [Fact]
    public void Com_loop_recomeca_enquanto_o_video_termina()
    {
        var ends = new Queue<PlaybackEnd>([PlaybackEnd.Finished, PlaybackEnd.Finished, PlaybackEnd.Stopped]);

        PlaybackLoop.Run(ends.Dequeue, loop: true).ShouldBe(3);
    }

    [Fact]
    public void Com_loop_para_no_esc_ou_ctrl_c()
    {
        PlaybackLoop.Run(() => PlaybackEnd.Stopped, loop: true).ShouldBe(1);
    }
}
