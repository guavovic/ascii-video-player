using System.Buffers;
using System.Text;
using AsciiVideoPlayer.Ascii;

namespace AsciiVideoPlayer.Terminal;

public sealed class TerminalRenderer(ITerminal terminal, bool color, int colorTolerance = 0)
{
    // Medido no ADR 0013: corta cerca de 40% dos bytes por quadro sem diferença visível.
    public const int DefaultColorTolerance = 16;

    private readonly ArrayBufferWriter<byte> _buffer = new();
    private (int Columns, int Rows) _terminalSize;
    private bool _clearPending;
    private int _terminalBackground = AsciiImage.NoColor;

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
            _terminalBackground = AsciiImage.NoColor;
        }

        int currentColor = -1;
        int currentBackground = _terminalBackground;

        for (int row = 0; row < image.Height; row++)
        {
            MoveCursor(layout.Top + row, layout.Left);

            for (int column = 0; column < image.Width; column++)
            {
                int index = row * image.Width + column;

                // A cor da letra não aparece no espaço, então não precisa ser enviada.
                if (color && image.Characters[index] != ' ' && !IsClose(image.Colors[index], currentColor))
                {
                    currentColor = image.Colors[index];
                    SetColor("\e[38;2;"u8, currentColor);
                }

                if (color && !IsClose(image.Backgrounds[index], currentBackground))
                {
                    currentBackground = image.Backgrounds[index];

                    if (currentBackground == AsciiImage.NoColor)
                        Append("\e[49m"u8);
                    else
                        SetColor("\e[48;2;"u8, currentBackground);
                }

                AppendCharacter(image.Characters[index]);
            }
        }

        _terminalBackground = currentBackground;
        terminal.Write(_buffer.WrittenSpan);
    }

    public bool EscapePressed() => terminal.EscapePressed();

    // Soma das diferenças de R, G e B. "Sem cor" só é próximo de "sem cor".
    private bool IsClose(int color, int current)
    {
        if (color == current)
            return true;

        if (color < 0 || current < 0)
            return false;

        int distance = Math.Abs((color >> 16) - (current >> 16))
            + Math.Abs((color >> 8 & 0xFF) - (current >> 8 & 0xFF))
            + Math.Abs((color & 0xFF) - (current & 0xFF));

        return distance <= colorTolerance;
    }

    private void MoveCursor(int row, int column)
    {
        Append("\e["u8);
        AppendNumber(row + 1);
        Append(";"u8);
        AppendNumber(column + 1);
        Append("H"u8);
    }

    private void SetColor(ReadOnlySpan<byte> prefix, int rgb)
    {
        Append(prefix);
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
