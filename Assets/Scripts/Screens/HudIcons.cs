using UnityEngine;

/*
 * Draws the HUD's icons in code, so no image files are needed: a heart (health), a bullet (ammo), a bottle (sanity) and an
 * exit key, both solid and as an outline. The key is drawn diagonally, bow at the bottom left and teeth at the top right.
 * Each icon is a white shape on a transparent square, so an Image can tint it to any colour.
 * Edges are smoothed by sampling every pixel several times and keeping the share of samples inside the shape.
 * Each sprite is drawn once and reused.
 */
public static class HudIcons
{
    private const int Size = 64;
    private const int KeySize = 128;
    private const float KeyOutlineWidth = 0.028f;
    // The key's shape is described at 1 and drawn this much larger, so it reaches out to the edges of its square.
    private const float KeyScale = 1.15f;
    private const int Samples = 4;

    private static Sprite heart;
    private static Sprite bullet;
    private static Sprite bottle;
    private static Sprite key;
    private static Sprite keyOutline;

    public static Sprite Heart => heart != null ? heart : heart = Draw("HudHeart", InHeart);
    public static Sprite Bullet => bullet != null ? bullet : bullet = Draw("HudBullet", InBullet);
    public static Sprite Bottle => bottle != null ? bottle : bottle = Draw("HudBottle", InBottle);
    public static Sprite Key => key != null ? key : key = Draw("HudKey", InKey, KeySize);
    public static Sprite KeyOutline => keyOutline != null ? keyOutline : keyOutline = Draw("HudKeyOutline", OnKeyEdge, KeySize);

    // Fills a square texture from a shape test that takes coordinates from -1 to 1, with y pointing up.
    private static Sprite Draw(string name, System.Func<float, float, bool> inside) => Draw(name, inside, Size);

    // The same at a chosen texture size; larger icons get a larger texture so they stay sharp at high resolutions.
    private static Sprite Draw(string name, System.Func<float, float, bool> inside, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];

        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = (px + (sx + 0.5f) / Samples) / size * 2f - 1f;
                        float y = (py + (sy + 0.5f) / Samples) / size * 2f - 1f;
                        if (inside(x, y)) hits++;
                    }
                }
                byte alpha = (byte)(255 * hits / (Samples * Samples));
                pixels[py * size + px] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // The classic heart curve (x² + y² - 1)³ - x²y³ ≤ 0, scaled and lowered slightly to fill the square.
    private static bool InHeart(float x, float y)
    {
        float hx = x * 1.25f;
        float hy = (y + 0.08f) * 1.25f;
        float a = hx * hx + hy * hy - 1f;
        return a * a * a - hx * hx * hy * hy * hy <= 0f;
    }

    // An upright round: a rounded tip on a straight casing, with a slightly wider rim at the base.
    private static bool InBullet(float x, float y)
    {
        const float halfWidth = 0.3f;
        const float tipStart = 0.15f;
        const float tipTop = 0.9f;

        // Rim at the base.
        if (y >= -0.9f && y < -0.72f) return Mathf.Abs(x) <= halfWidth + 0.06f;
        // Straight casing.
        if (y >= -0.72f && y < tipStart) return Mathf.Abs(x) <= halfWidth;
        // Tip: half an ellipse from the casing's width up to the point.
        if (y >= tipStart && y <= tipTop)
        {
            float t = (y - tipStart) / (tipTop - tipStart);
            return Mathf.Abs(x) <= halfWidth * Mathf.Sqrt(1f - t * t);
        }
        return false;
    }

    // A medicine bottle: a cap, a narrow neck and a wide body with rounded corners.
    private static bool InBottle(float x, float y)
    {
        // Cap.
        if (y >= 0.66f && y <= 0.92f) return Mathf.Abs(x) <= 0.26f;
        // Neck.
        if (y >= 0.42f && y < 0.66f) return Mathf.Abs(x) <= 0.16f;
        // Body.
        return InRoundedBox(x, y, 0f, -0.22f, 0.5f, 0.66f, 0.18f);
    }

    /*
     * A classic key laid diagonally: the point is turned 45 degrees (and shrunk by KeyScale, which draws the key larger)
     * so the key can be described lying flat, along u.
     * A bow of three round lobes, each with a hole, a collar where the bow meets the shaft, a straight shaft and two teeth
     * hanging below its far end.
     */
    private static bool InKey(float x, float y)
    {
        const float s = 0.70710678f / KeyScale;
        float u = (x + y) * s;
        float v = (y - x) * s;

        // Bow: three lobes around a centre, each lobe with a round hole.
        bool inBow = InCircle(u, v, -0.5f, 0f, 0.19f) || InCircle(u, v, -0.64f, 0f, 0.17f) ||
                     InCircle(u, v, -0.48f, 0.17f, 0.17f) || InCircle(u, v, -0.48f, -0.17f, 0.17f);
        if (inBow)
            return !(InCircle(u, v, -0.64f, 0f, 0.055f) || InCircle(u, v, -0.48f, 0.17f, 0.055f) || InCircle(u, v, -0.48f, -0.17f, 0.055f));

        // Shaft, then the collar near the bow.
        if (u >= -0.42f && u <= 0.86f && Mathf.Abs(v) <= 0.08f) return true;
        if (u >= -0.38f && u <= -0.27f && Mathf.Abs(v) <= 0.12f) return true;

        // Teeth.
        if (u >= 0.54f && u <= 0.66f && v >= -0.3f && v <= 0f) return true;
        return u >= 0.72f && u <= 0.86f && v >= -0.24f && v <= 0f;
    }

    // Inside the key but within the outline width of its edge, so only a thin line around the shape is drawn.
    private static bool OnKeyEdge(float x, float y)
    {
        if (!InKey(x, y)) return false;

        const float w = KeyOutlineWidth;
        return !InKey(x + w, y) || !InKey(x - w, y) || !InKey(x, y + w) || !InKey(x, y - w) ||
               !InKey(x + w, y + w) || !InKey(x - w, y - w) || !InKey(x + w, y - w) || !InKey(x - w, y + w);
    }

    private static bool InCircle(float x, float y, float cx, float cy, float r)
    {
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
    }

    // Inside a box centred at (cx, cy) with the given half sizes and corner radius.
    private static bool InRoundedBox(float x, float y, float cx, float cy, float halfW, float halfH, float radius)
    {
        float dx = Mathf.Abs(x - cx) - (halfW - radius);
        float dy = Mathf.Abs(y - cy) - (halfH - radius);
        if (dx <= 0f && dy <= 0f) return true;
        if (dx > radius || dy > radius) return false;
        return Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f) <= radius * radius;
    }
}
