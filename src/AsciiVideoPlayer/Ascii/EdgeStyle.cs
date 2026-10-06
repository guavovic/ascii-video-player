using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

// ASCII com os contornos desenhados por cima: onde o brilho muda forte (filtro de Sobel), a célula vira
// um traço na direção da borda.
public sealed class EdgeStyle(CharacterPalette palette) : IImageStyle
{
    private const int MinEdgeStrength = 200;

    private readonly AsciiStyle _ascii = new(palette);
    private int[] _luminance = [];

    public int PixelsPerColumn => 1;
    public int PixelsPerRow => 1;

    public void Convert(VideoFrame frame, AsciiImage image)
    {
        _ascii.Convert(frame, image);

        int width = frame.Width, height = frame.Height;

        if (_luminance.Length != width * height)
            _luminance = new int[width * height];

        for (int i = 0; i < _luminance.Length; i++)
            _luminance[i] = Rgb.Luminance(frame.Pixels, i);

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int i = y * width + x;
                int topLeft = _luminance[i - width - 1], top = _luminance[i - width], topRight = _luminance[i - width + 1];
                int left = _luminance[i - 1], right = _luminance[i + 1];
                int bottomLeft = _luminance[i + width - 1], bottom = _luminance[i + width], bottomRight = _luminance[i + width + 1];

                int gx = topRight + 2 * right + bottomRight - topLeft - 2 * left - bottomLeft;
                int gy = bottomLeft + 2 * bottom + bottomRight - topLeft - 2 * top - topRight;

                if (Math.Abs(gx) + Math.Abs(gy) >= MinEdgeStrength)
                    image.Characters[i] = ForGradient(gx, gy);
            }
        }
    }

    // A borda corre perpendicular ao gradiente. A célula tem o dobro de altura, então o gradiente vertical por
    // pixel vale metade.
    public static char ForGradient(int gx, int gy)
    {
        double angle = Math.Atan2(gy / 2.0, gx) * 180 / Math.PI;

        if (angle < 0)
            angle += 180;

        return angle switch
        {
            < 22.5 or >= 157.5 => '|',
            < 67.5 => '/',
            < 112.5 => '-',
            _ => '\\',
        };
    }
}
