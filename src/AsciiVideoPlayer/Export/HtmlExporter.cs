using System.Globalization;
using System.IO.Compression;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Export;

public sealed class HtmlExporter(AsciiConverter converter, bool color)
{
    public const int BytesPerCell = 9;

    public int Export(IVideoSource video, double fps, int columns, string title, TextWriter output, Action<int>? onFrame = null)
    {
        double outputFps = Math.Min(fps, video.Fps);
        double step = video.Fps / outputFps;
        var layout = FrameLayout.Fit(video.Width, video.Height, columns, int.MaxValue, maxWidth: null);
        var frame = converter.CreateFrame(layout.Columns, layout.Rows);
        var image = new AsciiImage(layout.Columns, layout.Rows);
        var cells = new byte[layout.Columns * layout.Rows * BytesPerCell];
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
                EncodeCells(image, color, cells);
                gzip.Write(cells);
                frames++;
                onFrame?.Invoke(frames);
                next += step;
            }
        }

        WritePage(output, title, outputFps, layout, color, frames, compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
        return frames;
    }

    public static void EncodeCells(AsciiImage image, bool color, Span<byte> cells)
    {
        for (int i = 0, offset = 0; i < image.Characters.Length; i++, offset += BytesPerCell)
        {
            char character = image.Characters[i];
            int foreground = color ? image.Colors[i] : 0xCCCCCC;
            int background = color && image.Backgrounds[i] != AsciiImage.NoColor ? image.Backgrounds[i] : 0;
            bool hasBackground = color && image.Backgrounds[i] != AsciiImage.NoColor;

            cells[offset] = (byte)character;
            cells[offset + 1] = (byte)(character >> 8);
            cells[offset + 2] = (byte)(foreground >> 16);
            cells[offset + 3] = (byte)(foreground >> 8);
            cells[offset + 4] = (byte)foreground;
            cells[offset + 5] = (byte)(hasBackground ? 1 : 0);
            cells[offset + 6] = (byte)(background >> 16);
            cells[offset + 7] = (byte)(background >> 8);
            cells[offset + 8] = (byte)background;
        }
    }

    private static void WritePage(
        TextWriter output, string title, double fps, FrameLayout layout, bool color, int frames, ReadOnlySpan<byte> data)
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
        output.Write(string.Create(CultureInfo.InvariantCulture,
            $"const fps = {fps}, colunas = {layout.Columns}, linhas = {layout.Rows}, total = {frames}, celula = {BytesPerCell};\n"));
        output.Write("const dados = \"");
        output.Write(Convert.ToBase64String(data));
        output.Write("""
            ";
            const tela = document.getElementById("tela"), contexto = tela.getContext("2d");
            let quadros, atual = 0, tocando = true, relogio, largura, altura;

            function ajustar() {
              const fonte = Math.max(2, Math.min(innerWidth / (colunas * 0.6), (innerHeight - 24) / (linhas * 1.2)));
              const escala = devicePixelRatio || 1;
              largura = fonte * 0.6; altura = fonte * 1.2;
              tela.width = Math.round(colunas * largura * escala); tela.height = Math.round(linhas * altura * escala);
              tela.style.width = colunas * largura + "px"; tela.style.height = linhas * altura + "px";
              contexto.setTransform(escala, 0, 0, escala, 0, 0);
              contexto.font = fonte + 'px "Cascadia Mono", Consolas, Menlo, "DejaVu Sans Mono", monospace';
              contexto.textBaseline = "middle";
            }

            // Braille: bit de cada ponto como [coluna, linha] dentro da célula de 2×4.
            const posicoes = [[0, 0], [0, 1], [0, 2], [1, 0], [1, 1], [1, 2], [0, 3], [1, 3]];

            function pontos(bits, x, y) {
              const raio = Math.min(largura / 2, altura / 4) * 0.4;
              contexto.beginPath();
              for (let bit = 0; bit < 8; bit++) {
                if (!(bits & 1 << bit)) continue;
                const [coluna, linha] = posicoes[bit];
                const cx = x + (coluna + 0.5) * largura / 2, cy = y + (linha + 0.5) * altura / 4;
                contexto.moveTo(cx + raio, cy);
                contexto.arc(cx, cy, raio, 0, 2 * Math.PI);
              }
              contexto.fill();
            }

            function desenhar() {
              const inicio = atual * colunas * linhas * celula;
              contexto.fillStyle = "#0c0c0c";
              contexto.fillRect(0, 0, colunas * largura, linhas * altura);
              for (let linha = 0, c = inicio; linha < linhas; linha++) {
                for (let coluna = 0; coluna < colunas; coluna++, c += celula) {
                  const x = coluna * largura, y = linha * altura;
                  if (quadros[c + 5]) {
                    contexto.fillStyle = `rgb(${quadros[c + 6]},${quadros[c + 7]},${quadros[c + 8]})`;
                    contexto.fillRect(x, y, largura + 0.5, altura + 0.5);
                  }
                  const letra = quadros[c] | quadros[c + 1] << 8;
                  if (letra === 32) continue;
                  contexto.fillStyle = `rgb(${quadros[c + 2]},${quadros[c + 3]},${quadros[c + 4]})`;
                  if (letra === 0x2580) contexto.fillRect(x, y, largura + 0.5, altura / 2 + 0.5);
                  else if (letra === 0x2584) contexto.fillRect(x, y + altura / 2, largura + 0.5, altura / 2 + 0.5);
                  else if (letra === 0x2588) contexto.fillRect(x, y, largura + 0.5, altura + 0.5);
                  else if (letra > 0x2800 && letra <= 0x28FF) pontos(letra - 0x2800, x, y);
                  else contexto.fillText(String.fromCharCode(letra), x, y + altura / 2);
                }
              }
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
              addEventListener("resize", () => { ajustar(); });
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
