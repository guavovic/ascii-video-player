using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

// Cada célula mostra dois pixels: o de cima na cor da letra ▀ e o de baixo na cor de fundo.
public sealed class HalfBlockStyle(bool color) : IImageStyle
{
    public const char Upper = '▀';
    public const char Lower = '▄';
    public const char Full = '█';

    private const int LitLuminance = 128;

    public int PixelsPerColumn => 1;
    public int PixelsPerRow => 2;

    public void Convert(VideoFrame frame, AsciiImage image)
    {
        byte[] pixels = frame.Pixels;

        for (int row = 0; row < image.Height; row++)
        {
            for (int column = 0; column < image.Width; column++)
            {
                int cell = row * image.Width + column;
                int top = 2 * row * frame.Width + column;
                int bottom = top + frame.Width;

                if (color)
                {
                    image.Characters[cell] = Upper;
                    image.Colors[cell] = Rgb.At(pixels, top);
                    image.Backgrounds[cell] = Rgb.At(pixels, bottom);
                    continue;
                }

                bool topLit = Rgb.Luminance(pixels, top) >= LitLuminance;
                bool bottomLit = Rgb.Luminance(pixels, bottom) >= LitLuminance;

                image.Characters[cell] = (topLit, bottomLit) switch
                {
                    (true, true) => Full,
                    (true, false) => Upper,
                    (false, true) => Lower,
                    _ => ' ',
                };
            }
        }
    }
}
