using AsciiVideoPlayer.Terminal;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class FrameLayoutTests
{
    [Fact]
    public void Limita_pela_altura_e_centraliza_na_horizontal_quando_o_terminal_e_baixo()
    {
        var layout = FrameLayout.Fit(1920, 1080, terminalColumns: 120, terminalRows: 30, maxWidth: null);

        layout.ShouldBe(new FrameLayout(Columns: 107, Rows: 30, Left: 6, Top: 0));
    }

    [Fact]
    public void Limita_pela_largura_e_centraliza_na_vertical_quando_o_terminal_e_alto()
    {
        var layout = FrameLayout.Fit(1920, 1080, terminalColumns: 200, terminalRows: 100, maxWidth: null);

        layout.ShouldBe(new FrameLayout(Columns: 200, Rows: 56, Left: 0, Top: 22));
    }

    [Fact]
    public void Respeita_a_largura_maxima()
    {
        var layout = FrameLayout.Fit(1920, 1080, terminalColumns: 200, terminalRows: 100, maxWidth: 100);

        layout.ShouldBe(new FrameLayout(Columns: 100, Rows: 28, Left: 50, Top: 36));
    }

    [Fact]
    public void Largura_maxima_maior_que_o_terminal_nao_passa_do_terminal()
    {
        var layout = FrameLayout.Fit(1920, 1080, terminalColumns: 120, terminalRows: 30, maxWidth: 500);

        layout.Columns.ShouldBeLessThanOrEqualTo(120);
    }

    [Fact]
    public void Video_em_pe_fica_estreito()
    {
        var layout = FrameLayout.Fit(1080, 1920, terminalColumns: 120, terminalRows: 30, maxWidth: null);

        layout.ShouldBe(new FrameLayout(Columns: 34, Rows: 30, Left: 43, Top: 0));
    }

    [Fact]
    public void Terminal_minusculo_ainda_tem_pelo_menos_um_caractere()
    {
        var layout = FrameLayout.Fit(1920, 1080, terminalColumns: 1, terminalRows: 1, maxWidth: null);

        layout.Columns.ShouldBeGreaterThanOrEqualTo(1);
        layout.Rows.ShouldBeGreaterThanOrEqualTo(1);
    }
}
