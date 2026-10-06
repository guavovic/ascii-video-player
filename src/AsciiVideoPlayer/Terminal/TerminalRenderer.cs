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
    private int _overlayTop = -1;

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

    public int Columns => terminal.Columns;

    public int LastFrameBytes { get; private set; }

    public void Draw(AsciiImage image, FrameLayout layout, Overlay overlay = default)
    {
        _buffer.ResetWrittenCount();

        if (_clearPending)
        {
            Append("\e[0m\e[2J"u8);
            _clearPending = false;
            _terminalBackground = AsciiImage.NoColor;
            _overlayTop = -1;
        }

        ClearOverlay();

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
        DrawOverlay(overlay);
        LastFrameBytes = _buffer.WrittenCount;
        terminal.Write(_buffer.WrittenSpan);
    }

    public void SetTitle(string title)
    {
        _buffer.ResetWrittenCount();
        Append("\e]0;"u8);
        AppendText(title.Replace('\e', ' ').Replace('\a', ' '));
        Append("\a"u8);
        terminal.Write(_buffer.WrittenSpan);
    }

    public ConsoleKey? ReadKey() => terminal.ReadKey();

    // A imagem nem sempre cobre as linhas de baixo, então o texto do quadro anterior é apagado antes.
    private void ClearOverlay()
    {
        if (_overlayTop < 0)
            return;

        Append("\e[0m"u8);
        _terminalBackground = AsciiImage.NoColor;

        for (int row = _overlayTop; row < terminal.Rows; row++)
        {
            MoveCursor(row, 0);
            Append("\e[2K"u8);
        }

        _overlayTop = -1;
    }

    private void DrawOverlay(Overlay overlay)
    {
        if (overlay.IsEmpty)
            return;

        int columns = terminal.Columns;
        int bottom = terminal.Rows - 1;
        _overlayTop = bottom;
        Append("\e[0m"u8);

        if (overlay.Status is { } status)
        {
            MoveCursor(bottom, 0);
            Append(color ? "\e[48;2;24;24;24m\e[38;2;230;230;230m"u8 : "\e[7m"u8);
            AppendText(status.Length > columns ? status[..columns] : status.PadRight(columns));
            Append("\e[0m"u8);
        }

        if (overlay.Subtitle is { } subtitle)
        {
            string[] lines = subtitle.Split('\n');
            int first = bottom - lines.Length;

            for (int i = 0; i < lines.Length; i++)
            {
                int row = first + i;
                string line = $" {lines[i].Trim()} ";

                if (row < 0 || line.Length == 2)
                    continue;

                if (line.Length > columns)
                    line = line[..columns];

                MoveCursor(row, (columns - line.Length) / 2);
                Append(color ? "\e[1;48;2;0;0;0m\e[38;2;255;255;255m"u8 : "\e[1m"u8);
                AppendText(line);
                Append("\e[0m"u8);
            }

            _overlayTop = Math.Max(0, first);
        }

        _terminalBackground = AsciiImage.NoColor;
    }

    private void AppendText(string text) =>
        _buffer.Advance(Encoding.UTF8.GetBytes(text, _buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length))));

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
