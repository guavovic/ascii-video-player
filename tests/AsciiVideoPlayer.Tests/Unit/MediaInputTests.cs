using AsciiVideoPlayer.Media;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class MediaInputTests
{
    [Theory]
    [InlineData("https://exemplo.com/video.mp4", true)]
    [InlineData("http://exemplo.com/live", true)]
    [InlineData("rtsp://camera.local/stream", true)]
    [InlineData("filme.mp4", false)]
    [InlineData(@"C:\videos\filme.mp4", false)]
    [InlineData("/home/eu/filme.mp4", false)]
    public void Reconhece_link(string text, bool expected)
    {
        MediaInput.IsUrl(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://exemplo.com/videos/meu%20filme.mp4", "meu filme")]
    [InlineData("https://exemplo.com/", "exemplo.com")]
    public void Nome_do_link_vem_do_arquivo_ou_do_site(string url, string expected)
    {
        MediaInput.FromUrl(url).Name.ShouldBe(expected);
    }

    [Fact]
    public void Arquivo_comeca_adiante_com_ss_antes_da_entrada()
    {
        var input = MediaInput.FromFile("filme.mp4");

        input.Arguments(TimeSpan.FromSeconds(90)).ShouldBe(["-ss", "90", "-i", input.Location]);
        input.IsFile.ShouldBeTrue();
        input.Name.ShouldBe("filme");
    }

    [Fact]
    public void Camera_ao_vivo_ignora_o_inicio_e_usa_o_formato_do_sistema()
    {
        var camera = MediaInput.FromCamera("Minha Câmera");
        string[] arguments = camera.Arguments(TimeSpan.FromSeconds(90));

        camera.Live.ShouldBeTrue();
        camera.IsFile.ShouldBeFalse();
        arguments.ShouldNotContain("-ss");
        arguments[0].ShouldBe("-f");
        arguments[^1].ShouldEndWith("Minha Câmera");
    }

    [Fact]
    public void Lista_do_directshow_traz_so_as_cameras_pelo_nome()
    {
        const string Output = """
            [in#0 @ 0000] "HD Pro Webcam C920" (video)
            [in#0 @ 0000]   Alternative name "@device_pnp_\\?\usb#vid_046d"
            [in#0 @ 0000] "OBS Virtual Camera" (none)
            [in#0 @ 0000] "Microfone (HyperX Cloud III)" (audio)
            Error opening input file dummy.
            """;

        Cameras.ParseDirectShow(Output).ShouldBe(["HD Pro Webcam C920", "OBS Virtual Camera"]);
    }

    [Fact]
    public void Lista_do_avfoundation_para_antes_dos_microfones()
    {
        const string Output = """
            [AVFoundation indev @ 0x1] AVFoundation video devices:
            [AVFoundation indev @ 0x1] [0] FaceTime HD Camera
            [AVFoundation indev @ 0x1] [1] Capture screen 0
            [AVFoundation indev @ 0x1] AVFoundation audio devices:
            [AVFoundation indev @ 0x1] [0] MacBook Pro Microphone
            """;

        Cameras.ParseAvFoundation(Output).ShouldBe(["FaceTime HD Camera", "Capture screen 0"]);
    }
}
