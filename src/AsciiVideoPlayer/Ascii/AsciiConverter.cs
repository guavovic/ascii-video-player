using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiConverter(CharacterPalette palette)
{
    public void Convert(VideoFrame frame, AsciiImage image)
    {
        byte[] pixels = frame.Pixels;

        for (int i = 0; i < image.Characters.Length; i++)
        {
            int offset = i * VideoFrame.BytesPerPixel;
            byte blue = pixels[offset];
            byte green = pixels[offset + 1];
            byte red = pixels[offset + 2];

            image.Characters[i] = palette.ForBrightness(blue + green + red);
            image.Colors[i] = AtFullBrightness(red, green, blue);
        }
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
