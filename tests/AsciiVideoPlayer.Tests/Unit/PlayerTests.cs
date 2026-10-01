using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Playback;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Tests.Fakes;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class PlayerTests
{
    private readonly FakeClock _clock = new();

    [Fact]
    public void Mostra_todos_os_quadros_quando_o_terminal_acompanha()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);

        Play(video, new FakeTerminal(), fps: 10);

        video.ReadFrames.ShouldBe([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }

    [Fact]
    public void Espera_o_relogio_antes_de_cada_quadro()
    {
        var video = new FakeVideoSource(frameCount: 5, fps: 10);
        var drawnAt = new List<TimeSpan>();
        var terminal = new FakeTerminal { OnWrite = () => drawnAt.Add(_clock.Elapsed) };

        Play(video, terminal, fps: 10);

        drawnAt.Select(time => time.TotalMilliseconds).ShouldBe([0, 100, 200, 300, 400]);
    }

    [Fact]
    public void Pula_os_quadros_atrasados_quando_o_terminal_e_lento()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);
        var terminal = new FakeTerminal { OnWrite = () => _clock.Advance(TimeSpan.FromMilliseconds(250)) };

        Play(video, terminal, fps: 10);

        video.ReadFrames.ShouldBe([0, 2, 5, 7]);
    }

    [Fact]
    public void Mostra_menos_quadros_quando_o_fps_pedido_e_menor_que_o_do_video()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);

        Play(video, new FakeTerminal(), fps: 5);

        video.ReadFrames.ShouldBe([0, 2, 4, 6, 8]);
    }

    [Fact]
    public void Para_quando_o_esc_e_apertado()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);

        Play(video, new FakeTerminal { EscapeAfterWrites = 3 }, fps: 10);

        video.ReadFrames.Count.ShouldBe(3);
    }

    [Fact]
    public void Para_quando_a_reproducao_e_cancelada()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);
        using var cancellation = new CancellationTokenSource();

        Play(video, new FakeTerminal { OnWrite = cancellation.Cancel }, fps: 10, cancellation.Token);

        video.ReadFrames.Count.ShouldBe(1);
    }

    [Fact]
    public void Inicia_o_relogio_ao_comecar()
    {
        Play(new FakeVideoSource(frameCount: 1, fps: 10), new FakeTerminal(), fps: 10);

        _clock.Started.ShouldBeTrue();
    }

    private void Play(FakeVideoSource video, FakeTerminal terminal, double fps, CancellationToken? cancellationToken = null)
    {
        var converter = new AsciiConverter(new CharacterPalette(CharacterPalette.DefaultCharacters));
        var renderer = new TerminalRenderer(terminal, color: false);

        new Player(video, converter, renderer, _clock, fps, maxWidth: null)
            .Play(cancellationToken ?? TestContext.Current.CancellationToken);
    }
}
