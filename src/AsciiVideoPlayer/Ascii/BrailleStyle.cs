using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

// Cada célula é um caractere braille com 2×4 pontos. Sem pontilhado, o ponto acende acima do brilho médio do quadro;
// com pontilhado (Floyd–Steinberg), o erro de cada ponto passa para os vizinhos e a densidade de pontos vira tom de cinza.
public sealed class BrailleStyle(bool dither) : IImageStyle
{
    private const char Blank = '⠀';

    // Bit de cada ponto, na ordem [linha, coluna] da célula.
    private static readonly int[,] DotBits = { { 0x01, 0x08 }, { 0x02, 0x10 }, { 0x04, 0x20 }, { 0x40, 0x80 } };

    private int[] _luminance = [];
    private bool[] _lit = [];

    public int PixelsPerColumn => 2;
    public int PixelsPerRow => 4;

    public void Convert(VideoFrame frame, AsciiImage image)
    {
        int count = frame.Width * frame.Height;

        if (_luminance.Length != count)
        {
            _luminance = new int[count];
            _lit = new bool[count];
        }

        for (int i = 0; i < count; i++)
            _luminance[i] = Rgb.Luminance(frame.Pixels, i);

        if (dither)
            Dither(frame.Width, frame.Height);
        else
            Threshold();

        for (int row = 0; row < image.Height; row++)
        {
            for (int column = 0; column < image.Width; column++)
                Fill(frame, image, row, column);
        }
    }

    private void Threshold()
    {
        long sum = 0;

        foreach (int value in _luminance)
            sum += value;

        int average = (int)(sum / _luminance.Length);

        for (int i = 0; i < _luminance.Length; i++)
            _lit[i] = _luminance[i] > average;
    }

    private void Dither(int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                int value = _luminance[i];
                _lit[i] = value >= 128;
                int error = value - (_lit[i] ? 255 : 0);

                if (x + 1 < width)
                    _luminance[i + 1] += error * 7 / 16;

                if (y + 1 < height)
                {
                    if (x > 0)
                        _luminance[i + width - 1] += error * 3 / 16;

                    _luminance[i + width] += error * 5 / 16;

                    if (x + 1 < width)
                        _luminance[i + width + 1] += error / 16;
                }
            }
        }
    }

    private void Fill(VideoFrame frame, AsciiImage image, int row, int column)
    {
        int bits = 0, red = 0, green = 0, blue = 0;

        for (int dotRow = 0; dotRow < 4; dotRow++)
        {
            for (int dotColumn = 0; dotColumn < 2; dotColumn++)
            {
                int pixel = (row * 4 + dotRow) * frame.Width + column * 2 + dotColumn;

                if (!_lit[pixel])
                    continue;

                bits |= DotBits[dotRow, dotColumn];
                int offset = pixel * VideoFrame.BytesPerPixel;
                blue += frame.Pixels[offset];
                green += frame.Pixels[offset + 1];
                red += frame.Pixels[offset + 2];
            }
        }

        int cell = row * image.Width + column;
        image.Characters[cell] = bits == 0 ? ' ' : (char)(Blank + bits);
        image.Colors[cell] = Rgb.AtFullBrightness(red, green, blue);
    }
}
