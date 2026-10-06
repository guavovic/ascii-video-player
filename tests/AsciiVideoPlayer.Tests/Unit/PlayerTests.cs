using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Playback;
using AsciiVideoPlayer.Subtitles;
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

    [Fact]
    public void Diz_que_terminou_quando_o_video_acaba()
    {
        Play(new FakeVideoSource(frameCount: 3, fps: 10), new FakeTerminal(), fps: 10).ShouldBe(PlaybackEnd.Finished);
    }

    [Fact]
    public void Diz_que_parou_quando_o_esc_e_apertado()
    {
        Play(new FakeVideoSource(frameCount: 10, fps: 10), new FakeTerminal { EscapeAfterWrites = 2 }, fps: 10)
            .ShouldBe(PlaybackEnd.Stopped);
    }

    [Fact]
    public void Diz_que_parou_quando_a_reproducao_e_cancelada()
    {
        using var cancellation = new CancellationTokenSource();

        Play(new FakeVideoSource(frameCount: 10, fps: 10), new FakeTerminal { OnWrite = cancellation.Cancel }, fps: 10, cancellation.Token)
            .ShouldBe(PlaybackEnd.Stopped);
    }

    [Theory]
    [InlineData(ConsoleKey.RightArrow, 16.9)]
    [InlineData(ConsoleKey.LeftArrow, 6.9)]
    [InlineData(ConsoleKey.UpArrow, 71.9)]
    [InlineData(ConsoleKey.DownArrow, 0)]
    public void Setas_pulam_a_partir_da_posicao_atual(ConsoleKey key, double expectedSeconds)
    {
        var terminal = new FakeTerminal { KeysAfterWrites = { [20] = key } };

        var end = Play(new FakeVideoSource(frameCount: 1000, fps: 10), terminal, fps: 10,
            start: TimeSpan.FromSeconds(10), duration: TimeSpan.FromMinutes(2));

        end.ShouldBe(PlaybackEnd.SeekTo(TimeSpan.FromSeconds(expectedSeconds)));
    }

    [Fact]
    public void Pular_alem_do_fim_termina_o_video()
    {
        var terminal = new FakeTerminal { KeysAfterWrites = { [1] = ConsoleKey.RightArrow } };

        Play(new FakeVideoSource(frameCount: 100, fps: 10), terminal, fps: 10,
                start: TimeSpan.FromSeconds(8), duration: TimeSpan.FromSeconds(10))
            .ShouldBe(PlaybackEnd.Finished);
    }

    [Fact]
    public void Espaco_pausa_o_relogio_mostra_a_barra_e_espaco_de_novo_continua()
    {
        var video = new FakeVideoSource(frameCount: 5, fps: 10);
        bool pausedWhileDrawing = false;
        var terminal = new FakeTerminal(columns: 60)
        {
            KeysAfterWrites = { [2] = ConsoleKey.Spacebar, [3] = ConsoleKey.Spacebar },
            OnWrite = () => pausedWhileDrawing |= _clock.Paused,
        };

        var end = Play(video, terminal, fps: 10, duration: TimeSpan.FromSeconds(1));

        end.ShouldBe(PlaybackEnd.Finished);
        pausedWhileDrawing.ShouldBeTrue();
        _clock.Paused.ShouldBeFalse();
        terminal.Output.ShouldContain("pausado");
        video.ReadFrames.ShouldBe([0, 1, 2, 3, 4]);
    }

    [Fact]
    public void Pular_durante_a_pausa_continua_pausado()
    {
        var terminal = new FakeTerminal { KeysAfterWrites = { [1] = ConsoleKey.Spacebar, [2] = ConsoleKey.RightArrow } };

        Play(new FakeVideoSource(frameCount: 100, fps: 10), terminal, fps: 10)
            .ShouldBe(PlaybackEnd.SeekTo(TimeSpan.FromSeconds(5), paused: true));
    }

    [Fact]
    public void Comecando_pausado_desenha_o_primeiro_quadro_e_espera()
    {
        var video = new FakeVideoSource(frameCount: 100, fps: 10);
        var terminal = new FakeTerminal { KeysAfterWrites = { [2] = ConsoleKey.Escape } };

        Play(video, terminal, fps: 10, startPaused: true).ShouldBe(PlaybackEnd.Stopped);

        video.ReadFrames.ShouldBe([0]);
        terminal.Output.ShouldContain("pausado");
    }

    [Fact]
    public void Desenha_a_legenda_do_momento_por_cima_da_imagem()
    {
        var subtitles = new SubtitleTrack([new SubtitleCue(TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.3), "Oi")]);
        var drawn = new List<bool>();
        var terminal = new FakeTerminal();
        terminal = new FakeTerminal { OnWrite = () => drawn.Add(terminal.Output.EndsWith(" Oi \e[0m")) };

        Play(new FakeVideoSource(frameCount: 4, fps: 10), terminal, fps: 10, subtitles: subtitles);

        drawn.ShouldBe([false, false, true, false]);
    }

    [Fact]
    public void Com_estatisticas_escreve_no_titulo_uma_vez_por_segundo()
    {
        var terminal = new FakeTerminal();
        var converter = new AsciiConverter(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters)));
        var renderer = new TerminalRenderer(terminal, color: false);
        var stats = new PlaybackStats("filme");

        new Player(new FakeVideoSource(frameCount: 25, fps: 10), converter, renderer, _clock, fps: 10, maxWidth: null, stats: stats)
            .Play(TestContext.Current.CancellationToken);

        terminal.Output.Split("\e]0;filme · 10,0 fps · 0 pulados").Length.ShouldBe(3);
    }

    private PlaybackEnd Play(
        FakeVideoSource video,
        FakeTerminal terminal,
        double fps,
        CancellationToken? cancellationToken = null,
        TimeSpan start = default,
        TimeSpan duration = default,
        bool startPaused = false,
        SubtitleTrack? subtitles = null)
    {
        var converter = new AsciiConverter(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters)));
        var renderer = new TerminalRenderer(terminal, color: false);

        return new Player(video, converter, renderer, _clock, fps, maxWidth: null, start, duration, startPaused, subtitles)
            .Play(cancellationToken ?? TestContext.Current.CancellationToken);
    }
}
