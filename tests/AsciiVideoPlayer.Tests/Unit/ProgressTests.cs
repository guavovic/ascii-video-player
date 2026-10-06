using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class ProgressTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(75, "1:15")]
    [InlineData(3723, "1:02:03")]
    [InlineData(-3, "0:00")]
    public void Formata_o_tempo_em_minutos_ou_horas(int seconds, string expected)
    {
        TimeFormat.Format(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);
    }

    [Theory]
    [InlineData("90", 90)]
    [InlineData("12.5", 12.5)]
    [InlineData("1:30", 90)]
    [InlineData("1:02:03", 3723)]
    public void Le_o_tempo_em_segundos_ou_com_dois_pontos(string text, double expectedSeconds)
    {
        TimeFormat.TryParse(text, out var time).ShouldBeTrue();
        time.ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("1:2:3:4")]
    public void Recusa_tempo_invalido(string text)
    {
        TimeFormat.TryParse(text, out _).ShouldBeFalse();
    }

    [Fact]
    public void Barra_ocupa_a_largura_e_mostra_o_progresso()
    {
        string bar = ProgressBar.Format(TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), paused: false, width: 40);

        bar.Length.ShouldBe(40);
        bar.ShouldStartWith(" 0:30 / 1:00 ");
        bar.Count(character => character == '━').ShouldBe(bar.Count(character => character == '─'));
    }

    [Fact]
    public void Na_pausa_mostra_as_teclas_quando_cabem()
    {
        string bar = ProgressBar.Format(TimeSpan.Zero, TimeSpan.FromMinutes(1), paused: true, width: 120);

        bar.Length.ShouldBe(120);
        bar.ShouldEndWith($" pausado · {ProgressBar.Keys} ");
    }

    [Fact]
    public void Em_terminal_estreito_mostra_so_que_esta_pausado()
    {
        string bar = ProgressBar.Format(TimeSpan.Zero, TimeSpan.FromMinutes(1), paused: true, width: 40);

        bar.Length.ShouldBe(40);
        bar.ShouldEndWith(" pausado ");
    }

    [Fact]
    public void Sem_duracao_mostra_so_o_tempo_atual()
    {
        ProgressBar.Format(TimeSpan.FromSeconds(5), TimeSpan.Zero, paused: false, width: 10).ShouldBe(" 0:05     ");
    }
}
