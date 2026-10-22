using System.Collections.Generic;
using UnityEngine;

namespace Tylerland.Office
{
    /// <summary>
    /// Generates 100% 2D runtime placeholder Sprites (for Walls with transparent doorway cutouts,
    /// Doors, Corridors, Security Desk, Floor, Ceiling, Camera Room Stages, and Doorway Animatronics)
    /// whenever an artist PNG sprite has not yet been assigned in the Inspector.
    /// Ensures the entire project uses 2D Sprites exclusively with zero 3D primitive meshes.
    /// </summary>
    public static class Procedural2DSpriteFactory
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite GetOrCreateWallSprite(int directionIndex, Color tint)
        {
            string key = $"wall_{directionIndex}_{ColorUtility.ToHtmlStringRGBA(tint)}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            // 500x175 pixels (matches 10.0m x 3.5m aspect ratio at 50 PPU)
            int w = 500;
            int h = 175;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            // Doorway cutout in center: 2.0m wide (100px) x 2.8m tall (140px from bottom)
            int doorHalfW = 50;
            int doorHeight = 140;
            int centerX = w / 2;

            Color wallBase = tint;
            Color wainscot = Color.Lerp(tint, new Color(0.12f, 0.10f, 0.14f, 1f), 0.45f);
            Color stripeCol = new Color(0.75f, 0.18f, 0.18f, 1f);
            Color frameCol = new Color(0.18f, 0.20f, 0.24f, 1f);
            Color outlineCol = new Color(0.05f, 0.05f, 0.07f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = Mathf.Abs(x - centerX);

                    // Transparent doorway opening
                    if (dx < doorHalfW && y < doorHeight)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Doorway metallic frame border
                    if (dx < doorHalfW + 8 && y < doorHeight + 8)
                    {
                        bool isInnerEdge = (dx == doorHalfW || y == doorHeight);
                        tex.SetPixel(x, y, isInnerEdge ? outlineCol : frameCol);
                        continue;
                    }

                    // Hand-drawn style wall paneling
                    Color c = y < 48 ? wainscot : wallBase;
                    if (y >= 48 && y < 54) c = stripeCol; // FNAF pizzeria wall stripe
                    else if (y == 47 || y == 54 || y < 4 || y > h - 4) c = outlineCol;
                    else if (x % 50 == 0) c = Color.Lerp(c, outlineCol, 0.35f);

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
            sprite.name = $"2D_Wall_Placeholder_{directionIndex}";
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetOrCreateCorridorSprite(int directionIndex, Color accentColor)
        {
            string key = $"corridor_{directionIndex}_{ColorUtility.ToHtmlStringRGBA(accentColor)}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int w = 160;
            int h = 220;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 vanish = new Vector2(w * 0.5f, h * 0.48f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - vanish.x) / (w * 0.5f);
                    float ny = (y - vanish.y) / (h * 0.5f);
                    float distFromCenter = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                    // Deep dark end of corridor
                    if (distFromCenter < 0.32f)
                    {
                        tex.SetPixel(x, y, new Color(0.05f, 0.05f, 0.08f, 1f));
                        continue;
                    }

                    // Perspective floor, ceiling, and side walls
                    Color baseCol = new Color(0.32f, 0.34f, 0.40f, 1f);
                    if (y < vanish.y && Mathf.Abs(ny) > Mathf.Abs(nx))
                    {
                        // Perspective floor tiles
                        bool checker = ((x / 16) + (y / 12)) % 2 == 0;
                        baseCol = checker ? new Color(0.28f, 0.28f, 0.34f, 1f) : new Color(0.18f, 0.18f, 0.22f, 1f);
                    }
                    else if (Mathf.Abs(nx) >= Mathf.Abs(ny))
                    {
                        // Side corridor walls with subtle accent stripe
                        baseCol = Color.Lerp(new Color(0.25f, 0.26f, 0.32f, 1f), accentColor, 0.18f);
                    }

                    // Depth shading toward vanishing point
                    float shade = Mathf.Clamp01((distFromCenter - 0.30f) / 0.70f);
                    Color finalCol = Color.Lerp(new Color(0.06f, 0.06f, 0.09f, 1f), baseCol, shade);

                    // Hand-drawn perspective line borders
                    if (Mathf.Abs(Mathf.Abs(nx) - Mathf.Abs(ny)) < 0.03f || Mathf.Abs(distFromCenter - 0.32f) < 0.02f)
                    {
                        finalCol = new Color(0.04f, 0.04f, 0.06f, 1f);
                    }

                    tex.SetPixel(x, y, finalCol);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
            sprite.name = $"2D_Corridor_Placeholder_{directionIndex}";
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetOrCreateDoorShutterSprite(int doorIndex)
        {
            string key = $"door_shutter_{doorIndex}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int w = 120;
            int h = 168;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color steelLight = new Color(0.42f, 0.45f, 0.50f, 1f);
            Color steelDark = new Color(0.24f, 0.26f, 0.30f, 1f);
            Color hazardYellow = new Color(0.90f, 0.72f, 0.12f, 1f);
            Color hazardBlack = new Color(0.10f, 0.10f, 0.12f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < 3 || x >= w - 3 || y < 3 || y >= h - 3)
                    {
                        tex.SetPixel(x, y, hazardBlack);
                        continue;
                    }

                    // Bottom hazard stripe band
                    if (y < 22)
                    {
                        bool stripe = ((x + y) / 10) % 2 == 0;
                        tex.SetPixel(x, y, stripe ? hazardYellow : hazardBlack);
                        continue;
                    }

                    // Horizontal corrugated shutter slats
                    int slat = y % 16;
                    Color c = slat < 2 ? hazardBlack : (slat < 9 ? steelLight : steelDark);
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
            sprite.name = $"2D_DoorShutter_Placeholder_{doorIndex}";
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetOrCreateDeskSprite()
        {
            const string key = "office_desk_2d";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int w = 180;
            int h = 90;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color woodTop = new Color(0.36f, 0.24f, 0.16f, 1f);
            Color woodFront = new Color(0.24f, 0.16f, 0.10f, 1f);
            Color monitorBezel = new Color(0.18f, 0.20f, 0.24f, 1f);
            Color monitorScreen = new Color(0.08f, 0.28f, 0.22f, 1f);
            Color outline = new Color(0.05f, 0.04f, 0.04f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Desk lower half (y < 52)
                    if (y < 52)
                    {
                        // Leg cutout in middle bottom
                        if (y < 32 && x > 45 && x < w - 45)
                        {
                            tex.SetPixel(x, y, Color.clear);
                            continue;
                        }
                        bool border = x < 3 || x >= w - 3 || y < 3 || y >= 49 || (y == 32 && x > 45 && x < w - 45);
                        Color c = border ? outline : (y > 42 ? woodTop : woodFront);
                        tex.SetPixel(x, y, c);
                        continue;
                    }

                    // CRT Monitor sitting on desk center (x in 65..115, y in 52..85)
                    if (x >= 65 && x <= 115 && y < 85)
                    {
                        bool bezel = x < 70 || x > 110 || y < 56 || y > 80;
                        tex.SetPixel(x, y, bezel ? monitorBezel : monitorScreen);
                        continue;
                    }

                    tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 50f);
            sprite.name = "2D_Desk_Placeholder";
            _cache[key] = sprite;
            return sprite;
        }

        public static Sprite GetOrCreateCheckerFloorSprite(Color c1, Color c2)
        {
            string key = $"floor_{ColorUtility.ToHtmlStringRGBA(c1)}_{ColorUtility.ToHtmlStringRGBA(c2)}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool check = ((x / 16) + (y / 16)) % 2 == 0;
                    tex.SetPixel(x, y, check ? c1 : c2);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 12.8f);
            sprite.name = "2D_Floor_Placeholder";
            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a 100% 2D character sprite for an animatronic standing in the doorway blindspot
        /// or inside its camera room (with glowing eyes and visual damage cracks for Damage Levels 0-3).
        /// </summary>
        public static Sprite GetOrCreateAnimatronic2DSprite(string id, Color themeColor, int damageLevel, float scaleFactor = 1.0f)
        {
            string key = $"anim2d_{id}_{ColorUtility.ToHtmlStringRGBA(themeColor)}_dmg{damageLevel}_s{scaleFactor:F1}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            int w = 160;
            int h = 224;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bodyFill = Color.Lerp(themeColor, new Color(0.08f, 0.08f, 0.12f, 1f), 0.55f);
            Color outline = new Color(0.03f, 0.03f, 0.05f, 1f);
            Color eyeGlow = Color.Lerp(themeColor, Color.white, 0.35f);
            Color crackColor = new Color(0.02f, 0.02f, 0.02f, 1f);

            float cx = w * 0.5f;
            float headY = h * 0.74f;
            float headR = 30f * scaleFactor;
            float torsoW = 42f * scaleFactor;
            float torsoTop = h * 0.62f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = x - cx;
                    float dyHead = y - headY;
                    float distHead = Mathf.Sqrt(dx * dx + dyHead * dyHead);

                    bool inHead = distHead <= headR;
                    bool inEars = (y >= headY + headR * 0.5f && y <= headY + headR * 1.35f) && (Mathf.Abs(dx) >= headR * 0.35f && Mathf.Abs(dx) <= headR * 0.75f);
                    bool inTorso = y <= torsoTop && y >= 10 && Mathf.Abs(dx) <= torsoW * (1f - (y / h) * 0.2f);

                    if (!inHead && !inEars && !inTorso)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Glowing Eyes inside Head
                    float eyeOffsetX = 11f * scaleFactor;
                    float eyeRadius = 5.5f * scaleFactor;
                    float distLeftEye = Vector2.Distance(new Vector2(x, y), new Vector2(cx - eyeOffsetX, headY + 2f));
                    float distRightEye = Vector2.Distance(new Vector2(x, y), new Vector2(cx + eyeOffsetX, headY + 2f));

                    if (distLeftEye <= eyeRadius || distRightEye <= eyeRadius)
                    {
                        tex.SetPixel(x, y, distLeftEye <= eyeRadius * 0.45f || distRightEye <= eyeRadius * 0.45f ? Color.white : eyeGlow);
                        continue;
                    }

                    // Outline
                    bool isBorder = (inHead && distHead > headR - 3f) || (inTorso && Mathf.Abs(Mathf.Abs(dx) - torsoW * (1f - (y / h) * 0.2f)) < 3f);
                    Color pixel = isBorder ? outline : bodyFill;

                    // Damage cracks based on damageLevel (1=Cracked, 2=Broken, 3=Critical)
                    if (damageLevel >= 1 && Mathf.Abs((x + y * 2) % 37) < damageLevel)
                    {
                        pixel = crackColor;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 80f);
            sprite.name = $"2D_Animatronic_{id}_Dmg{damageLevel}";
            _cache[key] = sprite;
            return sprite;
        }
    }
}
