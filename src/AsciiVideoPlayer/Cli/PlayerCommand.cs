using System.CommandLine;
using System.Globalization;
using System.Numerics;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
using AsciiVideoPlayer.Export;
using AsciiVideoPlayer.Media;
using AsciiVideoPlayer.Playback;
using AsciiVideoPlayer.Subtitles;
using AsciiVideoPlayer.Terminal;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Cli;

public static class PlayerCommand
{
    private const int DefaultExportColumns = 120;

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

        var style = new Option<ImageStyle>("--style", "-s")
        {
            Description = "Como desenhar a imagem: ascii (letras pelo brilho), blocks (meio bloco, o dobro de resolução vertical), " +
                "braille (2×4 pontos por caractere), dither (braille pontilhado) ou edges (ASCII com contornos).",
            DefaultValueFactory = _ => ImageStyle.Ascii,
        };

        var colorTolerance = new Option<int>("--color-tolerance")
        {
            Description = "Reaproveita a cor anterior quando a nova difere menos que isso (soma das diferenças de R, G e B), " +
                "para mandar menos bytes ao terminal. 0 só reaproveita a cor idêntica.",
            DefaultValueFactory = _ => TerminalRenderer.DefaultColorTolerance,
        };
        colorTolerance.Validators.Add(result =>
        {
            if (result.GetValue(colorTolerance) < 0)
                result.AddError("--color-tolerance precisa ser 0 ou mais.");
        });

        var colorSteps = PositiveNumberOption<int>(
            "--color-steps", "-c", "Arredonda cada canal de cor para múltiplos desse passo, com menos cores no total. Padrão: 1 (todas as cores).");

        var noAudio = new Option<bool>("--no-audio")
        {
            Description = "Toca só o vídeo, sem o áudio.",
        };

        var noColor = new Option<bool>("--no-color")
        {
            Description = "Desenha sem cores. A variável de ambiente NO_COLOR tem o mesmo efeito.",
        };

        var start = new Option<TimeSpan>("--start")
        {
            Description = "Começa a partir desse ponto, em segundos (90) ou com dois-pontos (1:30, 1:02:03).",
            CustomParser = result =>
            {
                string token = result.Tokens[0].Value;

                if (TimeFormat.TryParse(token, out var time))
                    return time;

                result.AddError($"--start precisa ser um tempo como 90, 1:30 ou 1:02:03 (recebido: '{token}').");
                return TimeSpan.Zero;
            },
        };

        var subtitles = new Option<FileInfo>("--subtitles")
        {
            Description = "Legenda .srt desenhada por cima do vídeo. Padrão: um .srt com o mesmo nome do vídeo, se houver.",
        };
        subtitles.AcceptExistingOnly();

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
            video, width, fps, palette, style, colorTolerance, colorSteps, noAudio, noColor, start, subtitles, loop, export,
        };

        command.SetAction(result => Run(new PlayerOptions(
            result.GetValue(video)!,
            result.GetValue(width),
            result.GetValue(fps),
            result.GetValue(palette)!,
            result.GetValue(style),
            result.GetValue(colorTolerance),
            result.GetValue(colorSteps) ?? 1,
            result.GetValue(noAudio),
            result.GetValue(noColor),
            result.GetValue(start),
            result.GetValue(subtitles),
            result.GetValue(loop),
            result.GetValue(export))));

        return command;
    }

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
        var converter = new AsciiConverter(
            ImageStyles.Create(options.Style, new CharacterPalette(options.Palette), color), options.ColorSteps);

        var subtitles = LoadSubtitles(options.Subtitles?.FullName ?? Path.ChangeExtension(path, ".srt"), options.Subtitles is not null);

        if (options.Export is not null)
            return Export(options, path, info, converter, color, subtitles);

        Console.Title = Path.GetFileNameWithoutExtension(path);
        var renderer = new TerminalRenderer(new ConsoleTerminal(), color, options.ColorTolerance);

        using var session = new TerminalSession();

        PlaybackLoop.Run((start, paused) =>
        {
            using var video = new FFmpegVideoSource(path, info, start);
            using var audio = !options.NoAudio && info.HasAudio ? OpenAlAudioPlayer.TryOpen(path, start) : null;

            IPlaybackClock clock = audio is null ? new StopwatchClock() : audio;
            var player = new Player(
                video, converter, renderer, clock, options.Fps ?? video.Fps, options.Width, start, info.Duration, paused, subtitles);

            return player.Play(session.Cancellation);
        }, options.Start, options.Loop);

        return 0;
    }

    private static SubtitleTrack? LoadSubtitles(string path, bool requested)
    {
        if (!requested && !File.Exists(path))
            return null;

        try
        {
            return SubtitleTrack.Load(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Não foi possível ler a legenda, o vídeo toca sem ela: {ex.Message}");
            return null;
        }
    }

    private static int Export(
        PlayerOptions options, string path, MediaInfo info, AsciiConverter converter, bool color, SubtitleTrack? subtitles)
    {
        using var video = new FFmpegVideoSource(path, info, options.Start);
        using var output = new StreamWriter(options.Export!.FullName, append: false, new System.Text.UTF8Encoding(false));

        int frames = new HtmlExporter(converter, color).Export(
            video,
            options.Fps ?? video.Fps,
            options.Width ?? DefaultExportColumns,
            Path.GetFileNameWithoutExtension(path),
            output,
            count => Console.Error.Write($"\rExportando: {count} quadros"),
            subtitles,
            options.Start);

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
}
