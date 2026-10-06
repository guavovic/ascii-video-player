using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public static class Rgb
{
    public static int At(byte[] pixels, int pixel)
    {
        int offset = pixel * VideoFrame.BytesPerPixel;
        return pixels[offset + 2] << 16 | pixels[offset + 1] << 8 | pixels[offset];
    }

    public static int Luminance(byte[] pixels, int pixel)
    {
        int offset = pixel * VideoFrame.BytesPerPixel;
        return (pixels[offset] * 29 + pixels[offset + 1] * 150 + pixels[offset + 2] * 77) >> 8;
    }

    // O brilho já está na densidade do caractere; a cor só leva o tom, senão a parte escura vira letra escura em fundo escuro.
    public static int AtFullBrightness(int red, int green, int blue)
    {
        int max = Math.Max(red, Math.Max(green, blue));

        if (max == 0)
            return 0;

        return red * 255 / max << 16 | green * 255 / max << 8 | blue * 255 / max;
    }
}
