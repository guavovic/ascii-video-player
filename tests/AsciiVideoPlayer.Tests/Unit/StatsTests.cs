using AsciiVideoPlayer.Playback;

namespace AsciiVideoPlayer.Tests.Unit;

public sealed class StatsTests
{
    [Fact]
    public void Resume_o_ultimo_segundo_com_virgula_decimal()
    {
        var stats = new PlaybackStats("filme");
        stats.Report(TimeSpan.Zero).ShouldBeNull();

        for (int i = 0; i < 24; i++)
            stats.RecordFrame(bytes: 30 * 1024, TimeSpan.FromMilliseconds(0.25));

        stats.RecordSkipped(6);

        stats.Report(TimeSpan.FromSeconds(1)).ShouldBe("filme · 24,0 fps · 6 pulados · 30,0 KB por quadro · desenho 0,25 ms");
    }

    [Fact]
    public void So_informa_depois_de_um_segundo_e_recomeca_a_contagem()
    {
        var stats = new PlaybackStats("filme");
        stats.Report(TimeSpan.Zero);
        stats.RecordFrame(1024, TimeSpan.Zero);

        stats.Report(TimeSpan.FromSeconds(0.5)).ShouldBeNull();
        stats.Report(TimeSpan.FromSeconds(1)).ShouldNotBeNull();

        stats.RecordFrame(1024, TimeSpan.Zero);
        stats.Report(TimeSpan.FromSeconds(1.5)).ShouldBeNull();
        stats.Report(TimeSpan.FromSeconds(2))!.ShouldStartWith("filme · 1,0 fps · 0 pulados");
    }

    [Fact]
    public void Depois_de_pular_para_tras_recomeca_sem_numero_errado()
    {
        var stats = new PlaybackStats("filme");
        stats.Report(TimeSpan.FromSeconds(10));
        stats.RecordFrame(1024, TimeSpan.Zero);

        stats.Report(TimeSpan.FromSeconds(2)).ShouldBeNull();
    }
}
