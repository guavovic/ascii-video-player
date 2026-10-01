namespace AsciiVideoPlayer.Terminal;

public sealed class ConsoleTerminal : ITerminal
{
    private readonly Stream _output = Console.OpenStandardOutput();

    public int Columns => Console.WindowWidth;
    public int Rows => Console.WindowHeight;

    public void Write(ReadOnlySpan<byte> bytes) => _output.Write(bytes);

    public bool EscapePressed() =>
        Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape;
}
