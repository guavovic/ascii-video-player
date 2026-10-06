namespace AsciiVideoPlayer.Terminal;

// Texto por cima da imagem: a linha de status fica na última linha do terminal, e a legenda logo acima.
public readonly record struct Overlay(string? Subtitle = null, string? Status = null)
{
    public bool IsEmpty => Subtitle is null && Status is null;
}
