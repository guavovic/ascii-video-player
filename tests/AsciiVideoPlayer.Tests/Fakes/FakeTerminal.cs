using System.Text;
using AsciiVideoPlayer.Terminal;

namespace AsciiVideoPlayer.Tests.Fakes;

public sealed class FakeTerminal(int columns = 80, int rows = 24) : ITerminal
{
    private readonly MemoryStream _output = new();

    public int Columns { get; set; } = columns;
    public int Rows { get; set; } = rows;

    public int Writes { get; private set; }

    public int? EscapeAfterWrites { get; init; }

    public Action? OnWrite { get; init; }

    public string Output => Encoding.UTF8.GetString(_output.ToArray());

    public byte[] OutputBytes => _output.ToArray();

    public void Write(ReadOnlySpan<byte> bytes)
    {
        _output.Write(bytes);
        Writes++;
        OnWrite?.Invoke();
    }

    public bool EscapePressed() => Writes >= EscapeAfterWrites;

    public void ClearOutput() => _output.SetLength(0);
}
