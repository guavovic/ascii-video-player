using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiStyle(CharacterPalette palette) : IImageStyle
{
    public int PixelsPerColumn => 1;
    public int PixelsPerRow => 1;

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
            image.Colors[i] = Rgb.AtFullBrightness(red, green, blue);
        }
    }
}
