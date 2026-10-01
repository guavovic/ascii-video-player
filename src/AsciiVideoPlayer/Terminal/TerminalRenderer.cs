using System.Buffers;
using System.Text;
using AsciiVideoPlayer.Ascii;

namespace AsciiVideoPlayer.Terminal;

public sealed class TerminalRenderer(ITerminal terminal, bool color)
{
    private readonly ArrayBufferWriter<byte> _buffer = new();
    private (int Columns, int Rows) _terminalSize;
    private bool _clearPending;

    public FrameLayout Fit(int videoWidth, int videoHeight, int? maxWidth)
    {
        (int Columns, int Rows) size = (terminal.Columns, terminal.Rows);

        if (size != _terminalSize)
        {
            _terminalSize = size;
            _clearPending = true;
        }

        return FrameLayout.Fit(videoWidth, videoHeight, size.Columns, size.Rows, maxWidth);
    }

    public void Draw(AsciiImage image, FrameLayout layout)
    {
        _buffer.ResetWrittenCount();

        if (_clearPending)
        {
            Append("\e[0m\e[2J"u8);
            _clearPending = false;
        }

        int currentColor = -1;

        for (int row = 0; row < image.Height; row++)
        {
            MoveCursor(layout.Top + row, layout.Left);

            for (int column = 0; column < image.Width; column++)
            {
                int index = row * image.Width + column;

                if (color && image.Colors[index] != currentColor)
                {
                    currentColor = image.Colors[index];
                    SetColor(currentColor);
                }

                AppendCharacter(image.Characters[index]);
            }
        }

        terminal.Write(_buffer.WrittenSpan);
    }

    public bool EscapePressed() => terminal.EscapePressed();

    private void MoveCursor(int row, int column)
    {
        Append("\e["u8);
        AppendNumber(row + 1);
        Append(";"u8);
        AppendNumber(column + 1);
        Append("H"u8);
    }

    private void SetColor(int rgb)
    {
        Append("\e[38;2;"u8);
        AppendNumber(rgb >> 16 & 0xFF);
        Append(";"u8);
        AppendNumber(rgb >> 8 & 0xFF);
        Append(";"u8);
        AppendNumber(rgb & 0xFF);
        Append("m"u8);
    }

    private void AppendCharacter(char character)
    {
        if (character < 0x80)
        {
            _buffer.GetSpan(1)[0] = (byte)character;
            _buffer.Advance(1);
            return;
        }

        _buffer.Advance(Encoding.UTF8.GetBytes([character], _buffer.GetSpan(4)));
    }

    private void AppendNumber(int value)
    {
        value.TryFormat(_buffer.GetSpan(11), out int written);
        _buffer.Advance(written);
    }

    private void Append(ReadOnlySpan<byte> bytes) => _buffer.Write(bytes);
}
