using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

// Cada célula é um caractere braille com 2×4 pontos. Sem pontilhado, o ponto acende acima do brilho médio do quadro.
// Com pontilhado, cada ponto tem o seu limite numa matriz de Bayer 4×4, e a densidade de pontos vira tom de cinza.
// O pontilhado ordenado não muda de um quadro para o outro onde a imagem está parada, então o vídeo não "ferve".
public sealed class BrailleStyle(bool dither) : IImageStyle
{
    private const char Blank = '⠀';

    // Bit de cada ponto, linha por linha: (0,0) (1,0) / (0,1) (1,1) / (0,2) (1,2) / (0,3) (1,3).
    private static readonly int[] DotBits = [0x01, 0x08, 0x02, 0x10, 0x04, 0x20, 0x40, 0x80];

    // Limites da matriz de Bayer 4×4, em brilho de 0 a 255.
    private static readonly int[] Bayer =
    [
        8, 136, 40, 168,
        200, 72, 232, 104,
        56, 184, 24, 152,
        248, 120, 216, 88,
    ];

    private int[] _luminance = [];

    public int PixelsPerColumn => 2;
    public int PixelsPerRow => 4;

    public void Convert(VideoFrame frame, AsciiImage image)
    {
        int count = frame.Width * frame.Height;
        byte[] pixels = frame.Pixels;

        if (_luminance.Length != count)
            _luminance = new int[count];

        long sum = 0;

        for (int i = 0, offset = 0; i < count; i++, offset += VideoFrame.BytesPerPixel)
        {
            int value = (pixels[offset] * 29 + pixels[offset + 1] * 150 + pixels[offset + 2] * 77) >> 8;
            _luminance[i] = value;
            sum += value;
        }

        int average = count == 0 ? 0 : (int)(sum / count);

        for (int row = 0, cell = 0; row < image.Height; row++)
        {
            for (int column = 0; column < image.Width; column++, cell++)
                Fill(frame, image, row, column, cell, average);
        }
    }

    private void Fill(VideoFrame frame, AsciiImage image, int row, int column, int cell, int average)
    {
        byte[] pixels = frame.Pixels;
        int width = frame.Width;
        int bits = 0, red = 0, green = 0, blue = 0;

        for (int dotRow = 0; dotRow < 4; dotRow++)
        {
            int y = row * 4 + dotRow;
            int pixel = y * width + column * 2;

            for (int dotColumn = 0; dotColumn < 2; dotColumn++, pixel++)
            {
                int threshold = dither ? Bayer[(y & 3) * 4 + (column * 2 + dotColumn & 3)] : average;

                if (_luminance[pixel] <= threshold)
                    continue;

                bits |= DotBits[dotRow * 2 + dotColumn];
                int offset = pixel * VideoFrame.BytesPerPixel;
                blue += pixels[offset];
                green += pixels[offset + 1];
                red += pixels[offset + 2];
            }
        }

        image.Characters[cell] = bits == 0 ? ' ' : (char)(Blank + bits);
        image.Colors[cell] = Rgb.AtFullBrightness(red, green, blue);
    }
}
