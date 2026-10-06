using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiConverter(IImageStyle style, int colorSteps = 1)
{
    public VideoFrame CreateFrame(int columns, int rows) =>
        new(columns * style.PixelsPerColumn, rows * style.PixelsPerRow);

    public void Convert(VideoFrame frame, AsciiImage image)
    {
        style.Convert(frame, image);

        if (colorSteps <= 1)
            return;

        Quantize(image.Colors);
        Quantize(image.Backgrounds);
    }

    // Arredonda cada canal para um múltiplo do passo: com menos cores distintas, vizinhas iguais repetem o código.
    public int Quantize(int rgb) =>
        rgb == AsciiImage.NoColor ? rgb : Channel(rgb >> 16) << 16 | Channel(rgb >> 8 & 0xFF) << 8 | Channel(rgb & 0xFF);

    private void Quantize(int[] colors)
    {
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Quantize(colors[i]);
    }

    private int Channel(int value) => Math.Min(255, (value + colorSteps / 2) / colorSteps * colorSteps);
}
