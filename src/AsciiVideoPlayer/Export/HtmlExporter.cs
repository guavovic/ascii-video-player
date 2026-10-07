using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Subtitles;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Export;

public sealed class HtmlExporter(AsciiConverter converter, bool color)
{
    private static readonly Lazy<string> Drawing = new(() =>
    {
        using var stream = typeof(HtmlExporter).Assembly.GetManifestResourceStream("desenho.js")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    public int Export(
        IVideoSource video,
        double fps,
        int columns,
        string title,
        TextWriter output,
        Action<int>? onFrame = null,
        SubtitleTrack? subtitles = null,
        TimeSpan start = default)
    {
        double outputFps = Math.Min(fps, video.Fps);
        double step = video.Fps / outputFps;
        var layout = FrameLayout.Fit(video.Width, video.Height, columns, int.MaxValue, maxWidth: null);
        var frame = converter.CreateFrame(layout.Columns, layout.Rows);
        var image = new AsciiImage(layout.Columns, layout.Rows);
        var cells = new byte[layout.Columns * layout.Rows * CellEncoding.BytesPerCell];
        using var compressed = new MemoryStream();
        int frames = 0;

        using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            long position = 0;
            double next = 0;

            while (true)
            {
                long wanted = (long)Math.Round(next);
                bool ended = false;

                for (; position < wanted && !ended; position++)
                    ended = !video.SkipFrame();

                if (ended || !video.TryReadFrame(frame))
                    break;

                position++;
                converter.Convert(frame, image);
                CellEncoding.Encode(image, color, cells);
                gzip.Write(cells);
                frames++;
                onFrame?.Invoke(frames);
                next += step;
            }
        }

        WritePage(output, title, outputFps, layout, frames, compressed.GetBuffer().AsSpan(0, (int)compressed.Length), Cues(subtitles, start));
        return frames;
    }

    // Cada legenda vira [início, fim, texto], em segundos a partir do começo do export.
    private static string Cues(SubtitleTrack? subtitles, TimeSpan start)
    {
        var json = new StringBuilder("[");

        foreach (var cue in subtitles?.Cues ?? [])
        {
            if (cue.End <= start)
                continue;

            if (json.Length > 1)
                json.Append(',');

            json.Append(CultureInfo.InvariantCulture,
                $"[{(cue.Start - start).TotalSeconds:0.###},{(cue.End - start).TotalSeconds:0.###},\"{JsonEncodedText.Encode(cue.Text)}\"]");
        }

        return json.Append(']').ToString();
    }

    private static void WritePage(
        TextWriter output, string title, double fps, FrameLayout layout, int frames, ReadOnlySpan<byte> data, string cues)
    {
        output.Write("""
            <!doctype html>
            <html lang="pt-BR">
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>
            """);
        output.Write(title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        output.Write("""
            </title>
            <style>
              html, body { margin: 0; height: 100%; background: #0c0c0c; }
              body { display: grid; place-items: center; overflow: hidden; }
              canvas { cursor: pointer; }
              p { position: fixed; bottom: 8px; width: 100%; margin: 0; text-align: center; font: 12px system-ui, sans-serif; color: #777777; }
            </style>
            <canvas id="tela" title="Clique ou aperte espaço para pausar"></canvas>
            <p>Feito com o ascii-video-player · clique ou aperte espaço para pausar</p>
            <script>

            """);
        output.Write(Drawing.Value);
        output.Write(string.Create(CultureInfo.InvariantCulture,
            $"\nconst fps = {fps}, colunas = {layout.Columns}, linhas = {layout.Rows}, total = {frames};\n"));
        output.Write($"const legendas = {cues};\n");
        output.Write("const dados = \"");
        output.Write(Convert.ToBase64String(data));
        output.Write("""
            ";
            const tela = document.getElementById("tela"), desenhista = criarDesenhista(tela);
            let quadros, atual = 0, tocando = true, relogio;

            function ajustar() {
              desenhista.ajustar(colunas, linhas, innerWidth, innerHeight - 24);
            }

            function desenhar() {
              desenhista.desenhar(quadros, atual * colunas * linhas * bytesPorCelula);
              const tempo = atual / fps;
              const ativa = legendas.find(([inicio, fim]) => tempo >= inicio && tempo < fim);
              desenhista.legenda(ativa?.[2]);
              atual = (atual + 1) % total;
            }

            function alternar() {
              tocando = !tocando;
              if (tocando) relogio = setInterval(desenhar, 1000 / fps); else clearInterval(relogio);
            }

            async function comecar() {
              const bytes = Uint8Array.from(atob(dados), letra => letra.charCodeAt(0));
              const fluxo = new Blob([bytes]).stream().pipeThrough(new DecompressionStream("gzip"));
              quadros = new Uint8Array(await new Response(fluxo).arrayBuffer());
              ajustar();
              addEventListener("resize", ajustar);
              addEventListener("keydown", evento => { if (evento.code === "Space") { evento.preventDefault(); alternar(); } });
              tela.addEventListener("click", alternar);
              desenhar();
              relogio = setInterval(desenhar, 1000 / fps);
            }

            comecar();
            </script>

            """);
    }
}
