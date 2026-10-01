using System.CommandLine;
using System.Globalization;
using System.Numerics;
using AsciiVideoPlayer.Ascii;
using AsciiVideoPlayer.Audio;
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
            "--fps", "-f", "Quadros por segundo exibidos, até o FPS do vídeo. Padrão: metade do FPS do vídeo.");

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

        var command = new RootCommand("Toca um vídeo no terminal em caracteres ASCII.")
        {
            video, width, fps, palette, noAudio, noColor,
        };

        command.SetAction(result => Run(new PlayerOptions(
            result.GetValue(video)!,
            result.GetValue(width),
            result.GetValue(fps),
            result.GetValue(palette)!,
            result.GetValue(noAudio),
            result.GetValue(noColor))));

        return command;
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

    private static int Run(PlayerOptions options)
    {
        string path = options.Video.FullName;

        if (Console.IsOutputRedirected)
        {
            Console.Error.WriteLine("O player precisa de um terminal para desenhar, e a saída está redirecionada.");
            return 1;
        }

        using var video = OpenCvVideoSource.TryOpen(path);

        if (video is null)
        {
            Console.Error.WriteLine($"Não foi possível abrir o vídeo: {path}");
            return 1;
        }

        Console.Title = Path.GetFileNameWithoutExtension(path);

        using var audio = !options.NoAudio && OperatingSystem.IsWindows() ? NAudioPlayer.TryOpen(path) : null;

        bool color = !options.NoColor && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        double fps = options.Fps ?? video.Fps / 2;

        var converter = new AsciiConverter(new CharacterPalette(options.Palette));
        var player = new Player(video, converter, new TerminalRenderer(color), audio, fps, options.Width);

        using var session = new TerminalSession();
        player.Play(session.Cancellation);
        return 0;
    }
}
