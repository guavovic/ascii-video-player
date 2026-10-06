using AsciiVideoPlayer.Media;
using AsciiVideoPlayer.Video;

namespace AsciiVideoPlayer.Tests.Integration;

public sealed class FFmpegTests : IDisposable
{
    private const string RedVideo = "color=c=red:s=64x36:r=10:d=2";
    private const string Tone = "sine=f=440:d=2";

    private readonly TestVideos _videos = new();

    public FFmpegTests()
    {
        if (Environment.GetEnvironmentVariable("CI") == "true")
            TestVideos.FFmpegInstalled.ShouldBeTrue("O CI precisa do FFmpeg para os testes de integração.");

        Assert.SkipUnless(TestVideos.FFmpegInstalled, "FFmpeg não está instalado.");
    }

    [Fact]
    public void Probe_le_o_tamanho_o_fps_a_duracao_e_se_tem_audio()
    {
        string path = _videos.Create("com-audio.mp4", "-f", "lavfi", "-i", RedVideo, "-f", "lavfi", "-i", Tone, "-shortest");

        FFmpeg.Probe(path).ShouldBe(new MediaInfo(Width: 64, Height: 36, Fps: 10, HasAudio: true, Duration: TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void Probe_identifica_video_sem_audio()
    {
        string path = _videos.Create("sem-audio.mp4", "-f", "lavfi", "-i", RedVideo);

        FFmpeg.Probe(path)!.HasAudio.ShouldBeFalse();
    }

    [Fact]
    public void Probe_troca_largura_e_altura_de_video_gravado_em_pe()
    {
        string original = _videos.Create("deitado.mp4", "-f", "lavfi", "-i", RedVideo);
        string rotated = _videos.Create("em-pe.mp4", "-display_rotation", "90", "-i", original, "-c", "copy");

        var info = FFmpeg.Probe(rotated)!;

        (info.Width, info.Height).ShouldBe((36, 64));
    }

    [Fact]
    public void Probe_devolve_nulo_para_arquivo_que_nao_e_video()
    {
        string path = _videos.CreateText("texto.mp4", "isto não é um vídeo");

        FFmpeg.Probe(path).ShouldBeNull();
    }

    [Fact]
    public void Fonte_de_video_le_todos_os_quadros_e_depois_para()
    {
        string path = _videos.Create("vinte-quadros.mp4", "-f", "lavfi", "-i", RedVideo);
        var info = FFmpeg.Probe(path)!;
        using var source = new FFmpegVideoSource(path, info);
        var frame = new VideoFrame(8, 4);

        int frames = 0;
        while (source.TryReadFrame(frame))
            frames++;

        frames.ShouldBe(20);
    }

    [Fact]
    public void Fonte_de_video_comeca_na_posicao_pedida()
    {
        string path = _videos.Create("comecar-adiante.mp4", "-f", "lavfi", "-i", RedVideo);
        using var source = new FFmpegVideoSource(path, FFmpeg.Probe(path)!, start: TimeSpan.FromSeconds(1.5));
        var frame = new VideoFrame(8, 4);

        int frames = 0;
        while (source.TryReadFrame(frame))
            frames++;

        frames.ShouldBe(5);
    }

    [Fact]
    public void Fonte_de_video_entrega_as_cores_em_bgr()
    {
        string path = _videos.Create("vermelho.mp4", "-f", "lavfi", "-i", RedVideo);
        using var source = new FFmpegVideoSource(path, FFmpeg.Probe(path)!);
        var frame = new VideoFrame(8, 4);

        source.TryReadFrame(frame).ShouldBeTrue();

        frame.Pixels[0].ShouldBeLessThan((byte)20);
        frame.Pixels[1].ShouldBeLessThan((byte)20);
        frame.Pixels[2].ShouldBeGreaterThan((byte)235);
    }

    [Fact]
    public void Fonte_de_video_pula_quadros()
    {
        string path = _videos.Create("pular.mp4", "-f", "lavfi", "-i", RedVideo);
        using var source = new FFmpegVideoSource(path, FFmpeg.Probe(path)!);

        int skipped = 0;
        while (source.SkipFrame())
            skipped++;

        skipped.ShouldBe(20);
        source.TryReadFrame(new VideoFrame(8, 4)).ShouldBeFalse();
    }

    public void Dispose() => _videos.Dispose();
}
