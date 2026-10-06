using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class ConversionTests
{
    [Theory]
    [InlineData(0, ' ')]
    [InlineData(255, ' ')]
    [InlineData(256, '.')]
    [InlineData(511, '.')]
    [InlineData(512, '#')]
    [InlineData(765, '#')]
    public void Paleta_escolhe_o_caractere_pelo_brilho(int brightness, char expected)
    {
        var palette = new CharacterPalette(" .#");

        palette.ForBrightness(brightness).ShouldBe(expected);
    }

    [Fact]
    public void Conversor_gera_o_caractere_pelo_brilho_e_a_cor_em_rgb()
    {
        var frame = new VideoFrame(2, 1);
        byte[] bluishGreen = [200, 255, 0];
        bluishGreen.CopyTo(frame.Pixels, VideoFrame.BytesPerPixel);
        var image = new AsciiImage(2, 1);

        new AsciiConverter(new CharacterPalette(" .#")).Convert(frame, image);

        image.Characters.ShouldBe([' ', '.']);
        image.Colors.ShouldBe([0x000000, 0x00FFC8]);
    }

    [Theory]
    [InlineData(40, 20, 10, 0xFF7F3F)]
    [InlineData(10, 10, 10, 0xFFFFFF)]
    [InlineData(0, 0, 0, 0x000000)]
    public void Cor_vai_ao_brilho_maximo_mantendo_o_tom(int red, int green, int blue, int expected)
    {
        AsciiConverter.AtFullBrightness(red, green, blue).ShouldBe(expected);
    }

    [Fact]
    public void Reducao_tira_a_media_dos_pixels_cobertos()
    {
        byte[] source = [0, 0, 0, 100, 50, 20, 200, 100, 40, 100, 50, 20];
        var target = new VideoFrame(1, 1);

        AreaResampler.Resize(source, sourceWidth: 2, sourceHeight: 2, target);

        target.Pixels.ShouldBe(new byte[] { 100, 50, 20 });
    }

    [Fact]
    public void Ampliacao_repete_o_pixel()
    {
        byte[] source = [10, 20, 30];
        var target = new VideoFrame(2, 2);

        AreaResampler.Resize(source, sourceWidth: 1, sourceHeight: 1, target);

        target.Pixels.ShouldBe(new byte[] { 10, 20, 30, 10, 20, 30, 10, 20, 30, 10, 20, 30 });
    }

    [Fact]
    public void Mesmo_tamanho_copia_os_pixels()
    {
        byte[] source = [1, 2, 3, 4, 5, 6];
        var target = new VideoFrame(2, 1);

        AreaResampler.Resize(source, sourceWidth: 2, sourceHeight: 1, target);

        target.Pixels.ShouldBe(source);
    }
}
