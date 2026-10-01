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
            image.Colors[i] = red << 16 | green << 8 | blue;
        }
    }
}
