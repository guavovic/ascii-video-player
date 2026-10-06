namespace AsciiVideoPlayer.Ascii;

public enum ImageStyle
{
    Ascii,
    Blocks,
    Braille,
    Dither,
    Edges,
}

public static class ImageStyles
{
    public static IImageStyle Create(ImageStyle style, CharacterPalette palette, bool color) => style switch
    {
        ImageStyle.Blocks => new HalfBlockStyle(color),
        ImageStyle.Braille => new BrailleStyle(dither: false),
        ImageStyle.Dither => new BrailleStyle(dither: true),
        ImageStyle.Edges => new EdgeStyle(palette),
        _ => new AsciiStyle(palette),
    };
}
