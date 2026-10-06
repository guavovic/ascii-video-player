using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class StyleTests
{
    [Fact]
    public void Conversor_cria_o_quadro_com_os_pixels_de_cada_celula_do_estilo()
    {
        var frame = new AsciiConverter(new BrailleStyle(dither: false)).CreateFrame(columns: 3, rows: 2);

        frame.Width.ShouldBe(6);
        frame.Height.ShouldBe(8);
    }

    [Fact]
    public void Meio_bloco_pinta_o_pixel_de_cima_na_letra_e_o_de_baixo_no_fundo()
    {
        var frame = Frame(1, 2, 0xFF0000, 0x0000FF);
        var image = new AsciiImage(1, 1);

        new HalfBlockStyle(color: true).Convert(frame, image);

        image.Characters.ShouldBe([HalfBlockStyle.Upper]);
        image.Colors.ShouldBe([0xFF0000]);
        image.Backgrounds.ShouldBe([0x0000FF]);
    }

    [Theory]
    [InlineData(0xFFFFFF, 0xFFFFFF, HalfBlockStyle.Full)]
    [InlineData(0xFFFFFF, 0x000000, HalfBlockStyle.Upper)]
    [InlineData(0x000000, 0xFFFFFF, HalfBlockStyle.Lower)]
    [InlineData(0x000000, 0x000000, ' ')]
    public void Meio_bloco_sem_cor_escolhe_o_bloco_pelos_pixels_acesos(int top, int bottom, char expected)
    {
        var image = new AsciiImage(1, 1);

        new HalfBlockStyle(color: false).Convert(Frame(1, 2, top, bottom), image);

        image.Characters.ShouldBe([expected]);
        image.Backgrounds.ShouldBe([AsciiImage.NoColor]);
    }

    [Fact]
    public void Braille_acende_os_pontos_acima_do_brilho_medio()
    {
        // Coluna da esquerda acesa: pontos 1, 2, 3 e 7 (0x01 | 0x02 | 0x04 | 0x40).
        var frame = Frame(2, 4,
            0xFF0000, 0x000000,
            0xFF0000, 0x000000,
            0xFF0000, 0x000000,
            0xFF0000, 0x000000);
        var image = new AsciiImage(1, 1);

        new BrailleStyle(dither: false).Convert(frame, image);

        image.Characters.ShouldBe([(char)(0x2800 + 0x47)]);
        image.Colors.ShouldBe([0xFF0000]);
    }

    [Fact]
    public void Braille_sem_ponto_aceso_vira_espaco()
    {
        var image = new AsciiImage(1, 1);

        new BrailleStyle(dither: false).Convert(Frame(2, 4, new int[8]), image);

        image.Characters.ShouldBe([' ']);
    }

    [Fact]
    public void Pontilhado_acende_a_proporcao_de_pontos_do_cinza()
    {
        int[] gray = Enumerable.Repeat(0x808080, 8 * 8).ToArray();
        var image = new AsciiImage(4, 2);

        new BrailleStyle(dither: true).Convert(Frame(8, 8, gray), image);

        int dots = image.Characters.Sum(character => character == ' ' ? 0 : int.PopCount(character - 0x2800));
        dots.ShouldBeInRange(28, 36);
    }

    [Theory]
    [InlineData(100, 0, '|')]
    [InlineData(0, 100, '-')]
    [InlineData(100, 200, '/')]
    [InlineData(-100, 200, '\\')]
    public void Contorno_segue_a_direcao_da_borda(int gx, int gy, char expected)
    {
        EdgeStyle.ForGradient(gx, gy).ShouldBe(expected);
    }

    [Fact]
    public void Contorno_desenha_o_traco_na_borda_e_mantem_o_ascii_no_resto()
    {
        int[] pixels = new int[5 * 3];

        for (int y = 0; y < 3; y++)
        {
            pixels[y * 5 + 3] = 0xFFFFFF;
            pixels[y * 5 + 4] = 0xFFFFFF;
        }

        var image = new AsciiImage(5, 3);

        new EdgeStyle(new CharacterPalette(" #")).Convert(Frame(5, 3, pixels), image);

        new string(image.Characters, 5, 5).ShouldBe("  ||#");
    }

    private static VideoFrame Frame(int width, int height, params int[] rgb)
    {
        var frame = new VideoFrame(width, height);

        for (int i = 0; i < rgb.Length; i++)
        {
            frame.Pixels[i * 3] = (byte)rgb[i];
            frame.Pixels[i * 3 + 1] = (byte)(rgb[i] >> 8);
            frame.Pixels[i * 3 + 2] = (byte)(rgb[i] >> 16);
        }

        return frame;
    }
}
