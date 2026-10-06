using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Subtitles;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Web;

// A página desenha o quadro do <video> num canvas do tamanho certo, manda os pixels (RGBA) para cá
// e recebe as células codificadas para desenhar. O vídeo nunca sai do navegador.
[SupportedOSPlatform("browser")]
public static partial class WebPlayer
{
    private static AsciiConverter _converter = new(new AsciiStyle(new CharacterPalette(CharacterPalette.DefaultCharacters)));
    private static bool _color = true;
    private static VideoFrame _frame = new(0, 0);
    private static AsciiImage _image = new(0, 0);
    private static byte[] _cells = [];
    private static SubtitleTrack? _subtitles;

    public static void Main()
    {
    }

    // Devolve [colunas, linhas, largura em pixels, altura em pixels] do quadro que a página deve mandar.
    [JSExport]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    public static int[] Configure(string style, bool color, int colorSteps, int videoWidth, int videoHeight, int columns)
    {
        var imageStyle = Enum.TryParse<ImageStyle>(style, ignoreCase: true, out var parsed) ? parsed : ImageStyle.Ascii;
        var palette = new CharacterPalette(CharacterPalette.DefaultCharacters);
        var layout = FrameLayout.Fit(videoWidth, videoHeight, columns, int.MaxValue, maxWidth: null);

        _color = color;
        _converter = new AsciiConverter(ImageStyles.Create(imageStyle, palette, color), colorSteps);
        _frame = _converter.CreateFrame(layout.Columns, layout.Rows);
        _image = new AsciiImage(layout.Columns, layout.Rows);
        _cells = new byte[layout.Columns * layout.Rows * CellEncoding.BytesPerCell];

        return [layout.Columns, layout.Rows, _frame.Width, _frame.Height];
    }

    // Recebe os pixels em RGBA, do canvas, e devolve as células.
    [JSExport]
    public static byte[] Convert(byte[] rgba)
    {
        byte[] pixels = _frame.Pixels;

        for (int source = 0, target = 0; target < pixels.Length; source += 4, target += VideoFrame.BytesPerPixel)
        {
            pixels[target] = rgba[source + 2];
            pixels[target + 1] = rgba[source + 1];
            pixels[target + 2] = rgba[source];
        }

        _converter.Convert(_frame, _image);
        CellEncoding.Encode(_image, _color, _cells);
        return _cells;
    }

    [JSExport]
    public static int LoadSubtitles(string text)
    {
        _subtitles = SubtitleTrack.Parse(text);
        return _subtitles.Cues.Count;
    }

    [JSExport]
    public static string? SubtitleAt(double seconds) => _subtitles?.At(TimeSpan.FromSeconds(seconds));
}
