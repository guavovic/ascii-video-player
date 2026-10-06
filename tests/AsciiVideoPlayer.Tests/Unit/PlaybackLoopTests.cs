using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class PlaybackLoopTests
{
    [Fact]
    public void Sem_loop_toca_uma_vez()
    {
        PlaybackLoop.Run((_, _) => PlaybackEnd.Finished, TimeSpan.Zero, loop: false).ShouldBe(1);
    }

    [Fact]
    public void Com_loop_recomeca_enquanto_o_video_termina()
    {
        var ends = new Queue<PlaybackEnd>([PlaybackEnd.Finished, PlaybackEnd.Finished, PlaybackEnd.Stopped]);

        PlaybackLoop.Run((_, _) => ends.Dequeue(), TimeSpan.Zero, loop: true).ShouldBe(3);
    }

    [Fact]
    public void Com_loop_para_no_esc_ou_ctrl_c()
    {
        PlaybackLoop.Run((_, _) => PlaybackEnd.Stopped, TimeSpan.Zero, loop: true).ShouldBe(1);
    }

    [Fact]
    public void Pular_recomeca_na_posicao_pedida_e_mantem_a_pausa()
    {
        var ends = new Queue<PlaybackEnd>([PlaybackEnd.SeekTo(TimeSpan.FromSeconds(15), paused: true), PlaybackEnd.Stopped]);
        var starts = new List<(TimeSpan Start, bool Paused)>();

        PlaybackLoop.Run((start, paused) =>
        {
            starts.Add((start, paused));
            return ends.Dequeue();
        }, TimeSpan.FromSeconds(10), loop: false);

        starts.ShouldBe([(TimeSpan.FromSeconds(10), false), (TimeSpan.FromSeconds(15), true)]);
    }

    [Fact]
    public void Com_loop_volta_ao_comeco_mesmo_tendo_comecado_adiante()
    {
        var ends = new Queue<PlaybackEnd>([PlaybackEnd.Finished, PlaybackEnd.Stopped]);
        var starts = new List<TimeSpan>();

        PlaybackLoop.Run((start, _) =>
        {
            starts.Add(start);
            return ends.Dequeue();
        }, TimeSpan.FromSeconds(30), loop: true);

        starts.ShouldBe([TimeSpan.FromSeconds(30), TimeSpan.Zero]);
    }
}
