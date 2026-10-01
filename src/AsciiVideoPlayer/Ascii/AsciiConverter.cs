using System.Text;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiConverter(CharacterPalette palette, int columnStep, int rowStep)
{
    private readonly StringBuilder _builder = new();

    public string Convert(VideoFrame frame)
    {
        _builder.Clear();

        for (int y = 0; y < frame.Height; y += rowStep)
        {
            for (int x = 0; x < frame.Width; x += columnStep)
                _builder.Append(palette.ForBrightness(frame.GetBrightness(x, y)));

            _builder.Append(Environment.NewLine);
        }

        return _builder.ToString();
    }
}
