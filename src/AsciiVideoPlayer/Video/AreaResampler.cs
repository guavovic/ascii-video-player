namespace AsciiVideoPlayer.Video;

public static class AreaResampler
{
    public static void Resize(byte[] source, int sourceWidth, int sourceHeight, VideoFrame target)
    {
        const int Channels = VideoFrame.BytesPerPixel;
        byte[] pixels = target.Pixels;

        for (int y = 0; y < target.Height; y++)
        {
            int top = y * sourceHeight / target.Height;
            int bottom = Math.Max(top + 1, (y + 1) * sourceHeight / target.Height);

            for (int x = 0; x < target.Width; x++)
            {
                int left = x * sourceWidth / target.Width;
                int right = Math.Max(left + 1, (x + 1) * sourceWidth / target.Width);

                int blue = 0, green = 0, red = 0;

                for (int sy = top; sy < bottom; sy++)
                {
                    int offset = (sy * sourceWidth + left) * Channels;

                    for (int sx = left; sx < right; sx++, offset += Channels)
                    {
                        blue += source[offset];
                        green += source[offset + 1];
                        red += source[offset + 2];
                    }
                }

                int count = (bottom - top) * (right - left);
                int index = (y * target.Width + x) * Channels;

                pixels[index] = (byte)(blue / count);
                pixels[index + 1] = (byte)(green / count);
                pixels[index + 2] = (byte)(red / count);
            }
        }
    }
}
