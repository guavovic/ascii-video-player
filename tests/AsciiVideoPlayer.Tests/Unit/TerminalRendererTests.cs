using System.Text.RegularExpressions;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Tests.Fakes;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class TerminalRendererTests
{
    private const string ClearScreen = "\e[0m\e[2J";

    [Fact]
    public void Limpa_a_tela_no_primeiro_quadro()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: false);

        Draw(renderer, Image("ab"));

        terminal.Output.ShouldStartWith(ClearScreen);
    }

    [Fact]
    public void So_limpa_de_novo_quando_o_terminal_muda_de_tamanho()
    {
        var terminal = new FakeTerminal(columns: 80, rows: 24);
        var renderer = new TerminalRenderer(terminal, color: false);
        Draw(renderer, Image("ab"));

        terminal.ClearOutput();
        Draw(renderer, Image("ab"));
        terminal.Output.ShouldNotContain(ClearScreen);

        terminal.Columns = 100;
        terminal.ClearOutput();
        Draw(renderer, Image("ab"));
        terminal.Output.ShouldStartWith(ClearScreen);
    }

    [Fact]
    public void Posiciona_cada_linha_com_o_deslocamento_do_layout()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: false);
        var image = new AsciiImage(2, 2);
        "abcd".CopyTo(image.Characters);

        renderer.Draw(image, new FrameLayout(Columns: 2, Rows: 2, Left: 3, Top: 1));

        terminal.Output.ShouldEndWith("\e[2;4Hab\e[3;4Hcd");
    }

    [Fact]
    public void So_envia_a_cor_quando_ela_muda()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: true);
        var image = Image("abc");
        image.Colors[0] = 0xFF0000;
        image.Colors[1] = 0xFF0000;
        image.Colors[2] = 0x00FF80;

        Draw(renderer, image);

        Regex.Matches(terminal.Output, @"\e\[38;2;").Count.ShouldBe(2);
        terminal.Output.ShouldContain("\e[38;2;255;0;0mab\e[38;2;0;255;128mc");
    }

    [Fact]
    public void Envia_o_fundo_so_quando_ele_muda_e_volta_ao_padrao_sem_fundo()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: true);
        var image = Image("abc");
        image.Backgrounds[0] = 0x102030;
        image.Backgrounds[1] = 0x102030;

        Draw(renderer, image);

        Regex.Matches(terminal.Output, @"\e\[48;2;").Count.ShouldBe(1);
        terminal.Output.ShouldContain("\e[48;2;16;32;48mab\e[49mc");
    }

    [Fact]
    public void Sem_cor_nao_envia_codigo_de_cor()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: false);
        var image = Image("ab");
        image.Colors[0] = 0xFF0000;

        Draw(renderer, image);

        terminal.Output.ShouldNotContain("\e[38;2;");
    }

    [Fact]
    public void Escreve_caracteres_fora_do_ascii_em_utf8()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: false);

        Draw(renderer, Image("█"));

        terminal.OutputBytes[^3..].ShouldBe(new byte[] { 0xE2, 0x96, 0x88 });
    }

    [Fact]
    public void Escreve_um_quadro_inteiro_de_uma_vez()
    {
        var terminal = new FakeTerminal();
        var renderer = new TerminalRenderer(terminal, color: true);
        var image = new AsciiImage(4, 3);
        "abcdefghijkl".CopyTo(image.Characters);

        renderer.Draw(image, new FrameLayout(4, 3, 0, 0));

        terminal.Writes.ShouldBe(1);
    }

    private static AsciiImage Image(string characters)
    {
        var image = new AsciiImage(characters.Length, 1);
        characters.CopyTo(image.Characters);
        return image;
    }

    private static void Draw(TerminalRenderer renderer, AsciiImage image)
    {
        renderer.Fit(image.Width, image.Height, maxWidth: null);
        renderer.Draw(image, new FrameLayout(image.Width, image.Height, 0, 0));
    }
}
