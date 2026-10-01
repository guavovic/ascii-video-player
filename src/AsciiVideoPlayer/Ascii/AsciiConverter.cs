using System.Text;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Ascii;

public sealed class AsciiConverter(CharacterPalette palette, int columns, int rows)
{
    private readonly StringBuilder _builder = new();

    public string Convert(VideoFrame frame)
    {
        _builder.Clear();

        for (int row = 0; row < rows; row++)
        {
            int y = row * frame.Height / rows;

            for (int column = 0; column < columns; column++)
            {
                int x = column * frame.Width / columns;
                _builder.Append(palette.ForBrightness(frame.GetBrightness(x, y)));
            }

            _builder.Append(Environment.NewLine);
        }

        return _builder.ToString();
    }
}
