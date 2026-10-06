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
    private const string FFmpegMissing = "O FFmpeg não foi encontrado. Instale o FFmpeg (com o ffmpeg e o ffprobe no PATH) e tente de novo.";

    public static RootCommand Create()
    {
        var video = new Argument<string?>("video")
        {
            Description = "Arquivo de vídeo ou link (http, https). Links de sites como o YouTube precisam do yt-dlp instalado.",
            Arity = ArgumentArity.ZeroOrOne,
        };

        var camera = new Option<string?>("--camera")
        {
            Description = "Toca a câmera com esse nome, ao vivo, em vez de um vídeo. Os nomes saem no --list-cameras.",
        };

        var listCameras = new Option<bool>("--list-cameras")
        {
            Description = "Lista os nomes das câmeras, sem abrir nenhuma, e sai.",
        };

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

        var stats = new Option<bool>("--stats")
        {
            Description = "Mostra no título da janela, uma vez por segundo, o FPS, os quadros pulados, os bytes por quadro e o tempo de desenho.",
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
            video, camera, listCameras, width, fps, palette, style, colorTolerance, colorSteps, noAudio, noColor, start, subtitles, stats, loop, export,
        };

        command.Validators.Add(result =>
        {
            bool hasVideo = result.GetValue(video) is not null;
            bool hasCamera = result.GetValue(camera) is not null;

            if (result.GetValue(listCameras))
                return;

            if (hasVideo == hasCamera)
                result.AddError("Informe um vídeo ou uma câmera (--camera), um dos dois.");
            else if (hasCamera && result.GetValue(export) is not null)
                result.AddError("--export não funciona com a câmera, porque ela não tem fim.");
        });

        command.SetAction(result => result.GetValue(listCameras) ? ListCameras() : Run(new PlayerOptions(
            result.GetValue(video),
            result.GetValue(camera),
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
            result.GetValue(stats),
            result.GetValue(loop),
            result.GetValue(export))));

        return command;
    }

    private static int ListCameras()
    {
        try
        {
            var cameras = Cameras.List();

            if (cameras.Count == 0)
                Console.WriteLine("Nenhuma câmera encontrada.");

            foreach (string camera in cameras)
                Console.WriteLine(camera);

            return 0;
        }
        catch (FFmpegNotFoundException)
        {
            Console.Error.WriteLine(FFmpegMissing);
            return 1;
        }
    }

    private static int Run(PlayerOptions options)
    {
        if (options.Export is null && Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("O player precisa de um terminal para desenhar, e a saída está redirecionada.");
            return 1;
        }

        MediaInput input;
        MediaInfo? info;

        try
        {
            if (Open(options) is not { } opened)
                return 1;

            (input, info) = opened;
        }
        catch (FFmpegNotFoundException)
        {
            Console.Error.WriteLine(FFmpegMissing);
            return 1;
        }

        bool color = !options.NoColor && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        var converter = new AsciiConverter(
            ImageStyles.Create(options.Style, new CharacterPalette(options.Palette), color), options.ColorSteps);

        var subtitles = options.Subtitles is not null
            ? LoadSubtitles(options.Subtitles.FullName)
            : input.IsFile && File.Exists(Path.ChangeExtension(input.Location, ".srt"))
                ? LoadSubtitles(Path.ChangeExtension(input.Location, ".srt"))
                : null;

        if (options.Export is not null)
            return Export(options, input, info, converter, color, subtitles);

        string name = input.Name;
        var renderer = new TerminalRenderer(new ConsoleTerminal(), color, options.ColorTolerance);
        var stats = options.Stats ? new PlaybackStats(name) : null;

        using var session = new TerminalSession();
        Console.Title = name;

        PlaybackLoop.Run((start, paused) =>
        {
            using var video = new FFmpegVideoSource(input, info, start);
            using var audio = !options.NoAudio && info.HasAudio ? OpenAlAudioPlayer.TryOpen(input, start) : null;

            IPlaybackClock clock = audio is null ? new StopwatchClock() : audio;
            var player = new Player(
                video, converter, renderer, clock, options.Fps ?? video.Fps, options.Width, start, info.Duration, paused, subtitles, stats);

            return player.Play(session.Cancellation);
        }, options.Start, options.Loop);

        return 0;
    }

    private static (MediaInput Input, MediaInfo Info)? Open(PlayerOptions options)
    {
        if (options.Camera is { } camera)
        {
            var cameraInput = MediaInput.FromCamera(camera);

            if (FFmpeg.Probe(cameraInput) is { } cameraInfo)
                return (cameraInput, cameraInfo);

            Console.Error.WriteLine($"Não foi possível abrir a câmera \"{camera}\". Veja os nomes com --list-cameras.");
            return null;
        }

        string video = options.Video!;

        if (!MediaInput.IsUrl(video))
        {
            if (!File.Exists(video))
            {
                Console.Error.WriteLine($"Arquivo não encontrado: {video}");
                return null;
            }

            var file = MediaInput.FromFile(video);

            if (FFmpeg.Probe(file) is { } fileInfo)
                return (file, fileInfo);

            Console.Error.WriteLine($"Não foi possível abrir o vídeo: {file.Location}");
            return null;
        }

        var url = MediaInput.FromUrl(video);

        if (FFmpeg.Probe(url) is { } urlInfo)
            return (url, urlInfo);

        if (YtDlp.Resolve(video) is { } resolved && FFmpeg.Probe(resolved) is { } resolvedInfo)
            return (resolved, resolvedInfo with { HasAudio = resolvedInfo.HasAudio || resolved.AudioLocation is not null });

        Console.Error.WriteLine($"Não foi possível abrir o link: {video}. Para sites como o YouTube, instale o yt-dlp.");
        return null;
    }

    private static SubtitleTrack? LoadSubtitles(string path)
    {
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
        PlayerOptions options, MediaInput input, MediaInfo info, AsciiConverter converter, bool color, SubtitleTrack? subtitles)
    {
        using var video = new FFmpegVideoSource(input, info, options.Start);
        using var output = new StreamWriter(options.Export!.FullName, append: false, new System.Text.UTF8Encoding(false));

        int frames = new HtmlExporter(converter, color).Export(
            video,
            options.Fps ?? video.Fps,
            options.Width ?? DefaultExportColumns,
            input.Name,
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
