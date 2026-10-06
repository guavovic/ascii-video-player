using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;
using BenchmarkDotNet.Attributes;

namespace AsciiVideoPlayer.Benchmarks;

[MemoryDiagnoser]
public class FrameBenchmarks
{
    private const int DecodedWidth = 640;
    private const int DecodedHeight = 360;

    private byte[] _decoded = [];
    private VideoFrame _frame = null!;
    private AsciiImage _image = null!;
    private FrameLayout _layout;
    private AsciiConverter _converter = null!;
    private TerminalRenderer _colorRenderer = null!;
    private TerminalRenderer _plainRenderer = null!;

    [Params("120x30", "240x60")]
    public string Terminal { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        string[] size = Terminal.Split('x');
        int columns = int.Parse(size[0]);
        int rows = int.Parse(size[1]);

        _decoded = new byte[DecodedWidth * DecodedHeight * VideoFrame.BytesPerPixel];
        new Random(42).NextBytes(_decoded);

        _frame = new VideoFrame(columns, rows);
        _image = new AsciiImage(columns, rows);
        _layout = new FrameLayout(columns, rows, 0, 0);
        _converter = new AsciiConverter(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters)));
        _colorRenderer = new TerminalRenderer(new NullTerminal(), color: true);
        _plainRenderer = new TerminalRenderer(new NullTerminal(), color: false);

        AreaResampler.Resize(_decoded, DecodedWidth, DecodedHeight, _frame);
        _converter.Convert(_frame, _image);
    }

    [Benchmark]
    public void Reduzir() => AreaResampler.Resize(_decoded, DecodedWidth, DecodedHeight, _frame);

    [Benchmark]
    public void Converter() => _converter.Convert(_frame, _image);

    [Benchmark]
    public void DesenharColorido() => _colorRenderer.Draw(_image, _layout);

    [Benchmark]
    public void DesenharSemCor() => _plainRenderer.Draw(_image, _layout);

    [Benchmark]
    public void QuadroInteiroColorido()
    {
        AreaResampler.Resize(_decoded, DecodedWidth, DecodedHeight, _frame);
        _converter.Convert(_frame, _image);
        _colorRenderer.Draw(_image, _layout);
    }

    private sealed class NullTerminal : ITerminal
    {
        public int Columns => 0;
        public int Rows => 0;

        public void Write(ReadOnlySpan<byte> bytes)
        {
        }

        public ConsoleKey? ReadKey() => null;
    }
}
