namespace AsciiVideoPlayer.Media;

// Links de sites de vídeo (YouTube e outros) não são um arquivo de vídeo. Se o yt-dlp estiver instalado,
// ele devolve o título e os endereços diretos da imagem e do som, que esses sites costumam entregar separados.
// A imagem vira poucas colunas no terminal, então 480p já sobra.
public static class YtDlp
{
    private const string Format = "bv*[height<=480]+ba/b[height<=480]/b";

    public static MediaInput? Resolve(string url)
    {
        try
        {
            using var process = FFmpeg.Start("yt-dlp", "--no-playlist", "--no-warnings", "-f", Format, "--print", "title", "--print", "urls", url);
            string[] lines = process.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            process.WaitForExit();

            if (process.ExitCode != 0 || lines.Length < 2)
                return null;

            return MediaInput.FromUrl(lines[1], title: lines[0], audioUrl: lines.Length > 2 ? lines[2] : null);
        }
        catch (FFmpegNotFoundException)
        {
            return null;
        }
    }
}
