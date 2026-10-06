namespace AsciiVideoPlayer.Terminal;

public interface ITerminal
{
    int Columns { get; }
    int Rows { get; }

    void Write(ReadOnlySpan<byte> bytes);

    ConsoleKey? ReadKey();
}
