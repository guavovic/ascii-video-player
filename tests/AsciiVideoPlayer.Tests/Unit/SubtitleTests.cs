using System.Text;
using AsciiVideoPlayer.Subtitles;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class SubtitleTests
{
    private const string Srt = """
        1
        00:00:01,000 --> 00:00:03,500
        <i>Olá,</i> mundo!

        2
        00:00:04,000 --> 00:00:06,000
        {\an8}Primeira linha
        <font color="#ffff00">Segunda linha</font>

        """;

    [Fact]
    public void Le_o_tempo_e_o_texto_sem_as_tags()
    {
        var track = SubtitleTrack.Parse(Srt);

        track.Cues.ShouldBe([
            new SubtitleCue(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3.5), "Olá, mundo!"),
            new SubtitleCue(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), "Primeira linha\nSegunda linha"),
        ]);
    }

    [Theory]
    [InlineData(0.5, null)]
    [InlineData(1, "Olá, mundo!")]
    [InlineData(3.4, "Olá, mundo!")]
    [InlineData(3.5, null)]
    [InlineData(5, "Primeira linha\nSegunda linha")]
    [InlineData(7, null)]
    public void Mostra_a_legenda_so_no_intervalo_dela(double seconds, string? expected)
    {
        SubtitleTrack.Parse(Srt).At(TimeSpan.FromSeconds(seconds)).ShouldBe(expected);
    }

    [Fact]
    public void Legenda_comprida_continua_na_tela_quando_outra_comeca_por_cima()
    {
        var track = new SubtitleTrack([
            new SubtitleCue(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(10), "longa"),
            new SubtitleCue(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "curta"),
        ]);

        track.At(TimeSpan.FromSeconds(2.5)).ShouldBe("curta");
        track.At(TimeSpan.FromSeconds(5)).ShouldBe("longa");
    }

    [Fact]
    public void Aceita_quebra_de_linha_do_windows_e_ponto_nos_milissegundos()
    {
        var track = SubtitleTrack.Parse("1\r\n00:01.250 --> 00:02.000\r\nOi\r\n");

        track.Cues.ShouldBe([new SubtitleCue(TimeSpan.FromSeconds(1.25), TimeSpan.FromSeconds(2), "Oi")]);
    }

    [Fact]
    public void Arquivo_em_windows_1252_ainda_le_os_acentos()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllBytes(path, Encoding.Latin1.GetBytes("1\n00:00:01,000 --> 00:00:02,000\nAção\n"));

            SubtitleTrack.Load(path).At(TimeSpan.FromSeconds(1.5)).ShouldBe("Ação");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
