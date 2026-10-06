using System.CommandLine;
using System.Globalization;
using System.Numerics;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
using AsciiVideoPlayer.Export;
using AsciiVideoPlayer.Media;
using AsciiVideoPlayer.Playback;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Cli;

public static class PlayerCommand
{
    public static RootCommand Create()
    {
        var video = new Argument<FileInfo>("video")
        {
            Description = "Arquivo de vídeo a ser tocado.",
        };
        video.AcceptExistingOnly();

        var width = PositiveNumberOption<int>(
            "--width", "-w", "Largura máxima em colunas. Padrão: a largura do terminal.");

        var fps = PositiveNumberOption<double>(
            "--fps", "-f", "Quadros por segundo exibidos, até o FPS do vídeo. Padrão: o FPS do vídeo.");

        var palette = new Option<string>("--palette", "-p")
        {
            Description = "Caracteres do mais escuro para o mais claro.",
            DefaultValueFactory = _ => CharacterPalette.DefaultCharacters,
        };
        palette.Validators.Add(result =>
        {
            if (string.IsNullOrEmpty(result.GetValue(palette)))
                result.AddError("A paleta precisa de pelo menos um caractere.");
        });

        var noAudio = new Option<bool>("--no-audio")
        {
            Description = "Toca só o vídeo, sem o áudio.",
        };

        var noColor = new Option<bool>("--no-color")
        {
            Description = "Desenha sem cores. A variável de ambiente NO_COLOR tem o mesmo efeito.",
        };

        var loop = new Option<bool>("--loop")
        {
            Description = "Recomeça o vídeo quando ele acaba, até apertar Esc ou Ctrl+C.",
        };

        var export = new Option<FileInfo?>("--export", "-o")
        {
            Description = "Em vez de tocar, salva o vídeo numa página HTML que toca o ASCII sozinha. A largura padrão é 120 colunas.",
        };
        export.Validators.Add(result =>
        {
            if (result.GetValue(export) is { } file && !file.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
                result.AddError("--export precisa de um arquivo .html.");
        });

        var command = new RootCommand("Toca um vídeo no terminal em caracteres ASCII.")
        {
            video, width, fps, palette, noAudio, noColor, loop, export,
        };

        command.SetAction(result => Run(new PlayerOptions(
            result.GetValue(video)!,
            result.GetValue(width),
            result.GetValue(fps),
            result.GetValue(palette)!,
            result.GetValue(noAudio),
            result.GetValue(noColor),
            result.GetValue(loop),
            result.GetValue(export))));

        return command;
    }

    private static int Export(PlayerOptions options, string path, MediaInfo info, AsciiConverter converter, bool color)
    {
        using var video = new FFmpegVideoSource(path, info);
        using var output = new StreamWriter(options.Export!.FullName, append: false, new System.Text.UTF8Encoding(false));

        int frames = new HtmlExporter(converter, color).Export(
            video,
            options.Fps ?? video.Fps,
            options.Width ?? DefaultExportColumns,
            Path.GetFileNameWithoutExtension(path),
            output,
            count => Console.Error.Write($"\rExportando: {count} quadros"));

        Console.Error.WriteLine($"\rPronto: {frames} quadros em {options.Export.FullName}");
        return 0;
    }

    private static Option<T?> PositiveNumberOption<T>(string name, string alias, string description)
        where T : struct, INumber<T>
    {
        return new Option<T?>(name, alias)
        {
            Description = description,
            CustomParser = result =>
            {
                string token = result.Tokens[0].Value;

                if (T.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out T value) && value > T.Zero)
                    return value;

                result.AddError($"{name} precisa ser um número maior que zero (recebido: '{token}').");
                return null;
            },
        };
    }

    private const int DefaultExportColumns = 120;

    private static int Run(PlayerOptions options)
    {
        string path = options.Video.FullName;

        if (options.Export is null && Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("O player precisa de um terminal para desenhar, e a saída está redirecionada.");
            return 1;
        }

        MediaInfo? info;

        try
        {
            info = FFmpeg.Probe(path);
        }
        catch (FFmpegNotFoundException)
        {
            Console.Error.WriteLine("O FFmpeg não foi encontrado. Instale o FFmpeg (com o ffmpeg e o ffprobe no PATH) e tente de novo.");
            return 1;
        }

        if (info is null)
        {
            Console.Error.WriteLine($"Não foi possível abrir o vídeo: {path}");
            return 1;
        }

        bool color = !options.NoColor && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        var converter = new AsciiConverter(new CharacterPalette(options.Palette));
        if (options.Export is not null)
            return Export(options, path, info, converter, color);

        Console.Title = Path.GetFileNameWithoutExtension(path);
        var renderer = new TerminalRenderer(new ConsoleTerminal(), color);

        using var session = new TerminalSession();

        PlaybackLoop.Run(() =>
        {
            using var video = new FFmpegVideoSource(path, info);
            using var audio = !options.NoAudio && info.HasAudio ? OpenAlAudioPlayer.TryOpen(path) : null;

            IPlaybackClock clock = audio is null ? new StopwatchClock() : audio;
            var player = new Player(video, converter, renderer, clock, options.Fps ?? video.Fps, options.Width);

            return player.Play(session.Cancellation);
        }, options.Loop);

        return 0;
    }
}
