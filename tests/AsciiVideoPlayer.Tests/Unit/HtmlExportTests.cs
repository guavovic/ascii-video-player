using System.IO.Compression;
using System.Text.RegularExpressions;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Export;
using AsciiVideoPlayer.Subtitles;
using AsciiVideoPlayer.Tests.Fakes;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class HtmlExportTests
{
    [Fact]
    public void Codifica_cada_celula_com_o_caractere_a_cor_e_o_fundo()
    {
        var image = new AsciiImage(2, 1);
        "a█".CopyTo(image.Characters);
        image.Colors[0] = 0x102030;
        image.Colors[1] = 0xFF0000;
        image.Backgrounds[1] = 0x0000FF;
        var cells = new byte[2 * HtmlExporter.BytesPerCell];

        HtmlExporter.EncodeCells(image, color: true, cells);

        cells.ShouldBe(new byte[] { 0x61, 0, 0x10, 0x20, 0x30, 0, 0, 0, 0, 0x88, 0x25, 0xFF, 0, 0, 1, 0, 0, 0xFF });
    }

    [Fact]
    public void Sem_cor_usa_cinza_e_ignora_o_fundo()
    {
        var image = new AsciiImage(1, 1);
        image.Characters[0] = 'x';
        image.Colors[0] = 0xFF0000;
        image.Backgrounds[0] = 0x0000FF;
        var cells = new byte[HtmlExporter.BytesPerCell];

        HtmlExporter.EncodeCells(image, color: false, cells);

        cells.ShouldBe(new byte[] { 0x78, 0, 0xCC, 0xCC, 0xCC, 0, 0, 0, 0 });
    }

    [Fact]
    public void Exporta_os_quadros_no_fps_pedido_numa_pagina_que_toca_sozinha()
    {
        var video = new FakeVideoSource(frameCount: 10, fps: 10);
        var exporter = new HtmlExporter(new AsciiConverter(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters))), color: true);
        var output = new StringWriter();

        int frames = exporter.Export(video, fps: 5, columns: 16, title: "Teste <1>", output);

        frames.ShouldBe(5);
        video.ReadFrames.ShouldBe([0, 2, 4, 6, 8]);
        string page = output.ToString();
        page.ShouldStartWith("<!doctype html>");
        page.ShouldContain("<title>Teste &lt;1&gt;</title>");
        page.ShouldContain("const fps = 5, colunas = 16, linhas = 4, total = 5,");

        string data = Regex.Match(page, "const dados = \"([^\"]*)\"").Groups[1].Value;
        using var gzip = new GZipStream(new MemoryStream(Convert.FromBase64String(data)), CompressionMode.Decompress);
        using var raw = new MemoryStream();
        gzip.CopyTo(raw);
        raw.Length.ShouldBe(5 * 16 * 4 * HtmlExporter.BytesPerCell);
        page.ShouldContain("const legendas = [];");
    }

    [Fact]
    public void Leva_as_legendas_a_partir_do_ponto_de_inicio()
    {
        var subtitles = new SubtitleTrack([
            new SubtitleCue(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Antes"),
            new SubtitleCue(TimeSpan.FromSeconds(11), TimeSpan.FromSeconds(12.5), "Diz \"oi\"\n</script>"),
        ]);
        var exporter = new HtmlExporter(new AsciiConverter(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters))), color: true);
        var output = new StringWriter();

        exporter.Export(new FakeVideoSource(frameCount: 2, fps: 10), fps: 10, columns: 16, title: "t", output,
            subtitles: subtitles, start: TimeSpan.FromSeconds(10));

        // O texto vai escapado: aspas, quebra de linha e "</script>" não podem fechar o script da página.
        string escape = "\\u00";
        output.ToString().ShouldContain(
            $"const legendas = [[1,2.5,\"Diz {escape}22oi{escape}22\\n{escape}3C/script{escape}3E\"]];");
    }
}
