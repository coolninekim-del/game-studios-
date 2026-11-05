using UnityEngine;

namespace KnightOrder.UI
{
    /// <summary>
    /// Generated placeholder pixel sprites of a mounted rider, shared by the battle, the duel and the stable
    /// until the pixel-art assets exist. Both sprites use the same 24x22 canvas with the pivot at the hooves, so
    /// a rider placed at the horse's position sits in the saddle.
    /// Implements: Story 004 (duel riders), Story 005 (mounted protagonist in battle and stable).
    ///
    /// Usage:
    /// <code>
    /// Sprite horse = MountedSprites.MakeHorse(20f);   // untinted brown
    /// Sprite rider = MountedSprites.MakeRider(20f);   // white and light grey, tint it with the side colour
    /// </code>
    /// </summary>
    public static class MountedSprites
    {
        private const int WIDTH = 24;
        private const int HEIGHT = 22;

        /// <summary>A 24x22 pixel horse facing right, pivot at the hooves (untinted brown).</summary>
        /// <param name="pixelsPerUnit">Sprite pixels per world unit.</param>
        /// <returns>A new sprite with its own texture.</returns>
        public static Sprite MakeHorse(float pixelsPerUnit)
        {
            var pixels = new Color32[WIDTH * HEIGHT];
            var coat = new Color32(120, 82, 48, 255);
            var dark = new Color32(70, 46, 26, 255);
            Fill(pixels, 4, 6, 18, 10, coat);
            Fill(pixels, 17, 9, 20, 13, coat);
            Fill(pixels, 19, 12, 22, 14, coat);
            Fill(pixels, 21, 12, 23, 13, dark);
            Fill(pixels, 17, 14, 18, 15, dark);
            Fill(pixels, 2, 7, 3, 10, dark);
            Fill(pixels, 5, 0, 5, 5, dark);
            Fill(pixels, 7, 0, 7, 5, coat);
            Fill(pixels, 15, 0, 15, 5, dark);
            Fill(pixels, 17, 0, 17, 5, coat);
            return MakePixelSprite(pixels, pixelsPerUnit);
        }

        /// <summary>
        /// A 24x22 pixel armoured rider seated on the horse, same canvas as the horse. The helmet is white and
        /// the armour light grey, so the side tint shows on both.
        /// </summary>
        /// <param name="pixelsPerUnit">Sprite pixels per world unit.</param>
        /// <returns>A new sprite with its own texture.</returns>
        public static Sprite MakeRider(float pixelsPerUnit)
        {
            var pixels = new Color32[WIDTH * HEIGHT];
            var armour = new Color32(200, 200, 200, 255);
            var helmet = new Color32(255, 255, 255, 255);
            var visor = new Color32(40, 40, 40, 255);
            Fill(pixels, 9, 11, 12, 16, armour);
            Fill(pixels, 10, 7, 11, 10, armour);
            Fill(pixels, 9, 17, 12, 20, helmet);
            Fill(pixels, 11, 18, 12, 18, visor);
            Fill(pixels, 10, 21, 11, 21, helmet);
            Fill(pixels, 12, 13, 13, 14, armour);
            return MakePixelSprite(pixels, pixelsPerUnit);
        }

        private static void Fill(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 color)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    pixels[y * WIDTH + x] = color;
                }
            }
        }

        private static Sprite MakePixelSprite(Color32[] pixels, float pixelsPerUnit)
        {
            var texture = new Texture2D(WIDTH, HEIGHT) { filterMode = FilterMode.Point };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, WIDTH, HEIGHT), new Vector2(0.5f, 0f), pixelsPerUnit);
        }
    }
}
