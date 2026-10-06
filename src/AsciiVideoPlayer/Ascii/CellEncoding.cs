namespace AsciiVideoPlayer.Ascii;

// Cada célula em 9 bytes, para desenhar no navegador: caractere (2), cor da letra (3), se tem fundo (1) e cor do fundo (3).
public static class CellEncoding
{
    public const int BytesPerCell = 9;

    private const int Gray = 0xCCCCCC;

    public static void Encode(AsciiImage image, bool color, Span<byte> cells)
    {
        for (int i = 0, offset = 0; i < image.Characters.Length; i++, offset += BytesPerCell)
        {
            char character = image.Characters[i];
            int foreground = color ? image.Colors[i] : Gray;
            bool hasBackground = color && image.Backgrounds[i] != AsciiImage.NoColor;
            int background = hasBackground ? image.Backgrounds[i] : 0;

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
}
