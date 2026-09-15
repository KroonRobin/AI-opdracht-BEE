using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AI_opdracht_BEE.Core;

public static class TextureFactory
{
    public static Texture2D CreateRoundedRect(GraphicsDevice device, int width, int height, int radius, Color color)
    {
        var data = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                data[y * width + x] = IsInsideRoundedRect(x, y, width, height, radius)
                    ? color
                    : Color.Transparent;

        var texture = new Texture2D(device, width, height);
        texture.SetData(data);
        return texture;
    }

    public static Texture2D CreateRoundedRectBorder(GraphicsDevice device, int width, int height, int radius, int thickness, Color color)
    {
        var data = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool insideOuter = IsInsideRoundedRect(x, y, width, height, radius);
                bool insideInner = IsInsideRoundedRect(
                    x - thickness, y - thickness,
                    width - thickness * 2, height - thickness * 2,
                    System.Math.Max(0, radius - thickness));

                data[y * width + x] = (insideOuter && !insideInner) ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(device, width, height);
        texture.SetData(data);
        return texture;
    }

    private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return false;

        if (x < radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
        if (x >= width - radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
        if (x < radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
        if (x >= width - radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;

        return true;
    }
}