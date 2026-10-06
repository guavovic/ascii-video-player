namespace AsciiVideoPlayer.Media;

// O que o FFmpeg abre: um arquivo, um link ou uma câmera, com os argumentos de entrada de cada um.
// Ao vivo (câmera) não tem como começar adiante, e a imagem sai espelhada, como num espelho.
// O som pode vir de outro endereço, como nos sites que separam imagem e áudio.
public sealed record MediaInput(string Name, string Location, IReadOnlyList<string> Format, bool Live = false, string? AudioLocation = null)
{
    public static MediaInput FromFile(string path) =>
        new(Path.GetFileNameWithoutExtension(path), Path.GetFullPath(path), []);

    public static MediaInput FromUrl(string url, string? title = null, string? audioUrl = null) =>
        new(title ?? NameFromUrl(url), url, [], AudioLocation: audioUrl);

    public static MediaInput FromCamera(string camera)
    {
        if (OperatingSystem.IsWindows())
            return new(camera, $"video={camera}", ["-f", "dshow"], Live: true);

        if (OperatingSystem.IsMacOS())
            return new(camera, camera, ["-f", "avfoundation", "-framerate", "30"], Live: true);

        return new(camera, camera, ["-f", "v4l2"], Live: true);
    }

    public static bool IsUrl(string text) =>
        Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "rtmp" or "rtsp";

    public bool IsFile => Format.Count == 0 && !IsUrl(Location);

    public string[] Arguments(TimeSpan start) =>
        [.. Format, .. Live ? [] : FFmpeg.StartAt(start), "-i", Location];

    public string[] AudioArguments(TimeSpan start) =>
        AudioLocation is null ? Arguments(start) : [.. FFmpeg.StartAt(start), "-i", AudioLocation];

    private static string NameFromUrl(string url)
    {
        var uri = new Uri(url);
        string file = Path.GetFileNameWithoutExtension(uri.AbsolutePath);

        return file.Length > 0 ? Uri.UnescapeDataString(file) : uri.Host;
    }
}
