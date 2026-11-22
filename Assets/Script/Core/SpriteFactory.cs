using System.Collections.Generic;
using UnityEngine;

namespace Pulling
{
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite CreateForTile(int tileId, Color color, int size = 32)
        {
            switch (tileId)
            {
                case TileIds.SlopeNE: return CreateSlope(SlopeVariant.NE, color, size);
                case TileIds.SlopeNW: return CreateSlope(SlopeVariant.NW, color, size);
                case TileIds.SlopeSE: return CreateSlope(SlopeVariant.SE, color, size);
                case TileIds.SlopeSW: return CreateSlope(SlopeVariant.SW, color, size);
                case TileIds.MirrorNE: return CreateMirrorSlope(SlopeVariant.NE, color, size);
                case TileIds.MirrorNW: return CreateMirrorSlope(SlopeVariant.NW, color, size);
                case TileIds.MirrorSE: return CreateMirrorSlope(SlopeVariant.SE, color, size);
                case TileIds.MirrorSW: return CreateMirrorSlope(SlopeVariant.SW, color, size);
                case TileIds.Breakable: return CreateCracked(color, size);
                case TileIds.BreakableStrong: return CreateSturdy(color, size);
                case TileIds.Bomb: return CreateBomb(color, size);
                case TileIds.BlastWall: return CreateBlastWall(color, size);
                case TileIds.Switch: return CreateSwitch(color, size, false);
                case TileIds.SwitchToggle: return CreateSwitch(color, size, true);
                case TileIds.SwitchLight: return CreateLightSensor(color, size);
                case TileIds.Door: return CreateDoor(color, size);
                case TileIds.OneWay: return CreateOneWay(color, size);
                case TileIds.Sunlight: return CreateSunlight(color, size);
                case TileIds.Bullet: return CreateCircle(color, size);
                case TileIds.Fran:
                case TileIds.Meiling:
                case TileIds.Patchouli:
                case TileIds.Sakuya: return CreateCircle(color, size);
                case TileIds.Goal: return CreateFlag(color, size);
                case TileIds.Hazard: return CreatePit(color, size);
                case TileIds.HighBounce: return CreateHighBounce(color, size);
                case TileIds.EnemyWeak:
                case TileIds.EnemyMid:
                case TileIds.EnemyHeavy:
                case TileIds.EnemyBoss: return CreateSolid(color, size);
                default: return CreateSolid(color, size);
            }
        }

        public static bool UsesTextLabel(int tileId)
        {
            return tileId == TileIds.Fran
                   || tileId == TileIds.Meiling
                   || tileId == TileIds.Patchouli
                   || tileId == TileIds.Sakuya
                   || tileId == TileIds.EnemyWeak
                   || tileId == TileIds.EnemyMid
                   || tileId == TileIds.EnemyHeavy
                   || tileId == TileIds.EnemyBoss;
        }

        public static Sprite CreateSolid(Color color, int size = 32)
        {
            string key = $"solid:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateCircle(Color color, int size = 32)
        {
            string key = $"circle:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            float radius = size * 0.5f - 0.5f;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, dist <= radius ? color : Color.clear);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateCracked(Color color, int size = 32)
        {
            string key = $"cracked:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            var crack = new Color(color.r * 0.38f, color.g * 0.38f, color.b * 0.38f, 1f);
            var grout = new Color(color.r * 0.72f, color.g * 0.72f, color.b * 0.72f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    bool mortar = x == size / 2 || y == size / 2;
                    bool slash = Mathf.Abs(x - y) <= 1 || Mathf.Abs(x + y - (size - 1)) <= 1;
                    bool chip = (x == 6 && y >= 4 && y <= 10) || (x >= 20 && x <= 26 && y == 22);
                    Color pixel = color;
                    if (edge || mortar)
                    {
                        pixel = grout;
                    }

                    if (slash || chip)
                    {
                        pixel = crack;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateSturdy(Color color, int size = 32)
        {
            string key = $"sturdy:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = CreateTexture(key, size);
            PaintSturdy(tex, color, size, false);
            return Finish(tex, key, size);
        }

        public static Sprite CreateFractured(Color color, int size = 32)
        {
            string key = $"fractured:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = CreateTexture(key, size);
            PaintSturdy(tex, color, size, true);
            var crack = new Color(color.r * 0.18f, color.g * 0.18f, color.b * 0.18f, 1f);
            float scale = size / 32f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (IsCrackPixel(x, y, scale))
                    {
                        tex.SetPixel(x, y, crack);
                    }
                }
            }

            return Finish(tex, key, size);
        }

        static void PaintSturdy(Texture2D tex, Color color, int size, bool damaged)
        {
            var body = damaged
                ? new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.82f, 1f)
                : color;
            var edge = new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.55f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    tex.SetPixel(x, y, border ? edge : body);
                }
            }
        }

        static bool IsCrackPixel(int x, int y, float scale)
        {
            return DistToSegment(x, y, 5f * scale, 27f * scale, 14f * scale, 16f * scale) <= 1.2f
                   || DistToSegment(x, y, 14f * scale, 16f * scale, 7f * scale, 3f * scale) <= 1.2f
                   || DistToSegment(x, y, 14f * scale, 16f * scale, 27f * scale, 22f * scale) <= 1.15f
                   || DistToSegment(x, y, 14f * scale, 16f * scale, 21f * scale, 8f * scale) <= 1.05f
                   || DistToSegment(x, y, 21f * scale, 8f * scale, 29f * scale, 5f * scale) <= 1.05f
                   || DistToSegment(x, y, 22f * scale, 24f * scale, 30f * scale, 17f * scale) <= 1f;
        }

        static float DistToSegment(float px, float py, float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len2 = dx * dx + dy * dy;
            if (len2 < 0.001f)
            {
                return Vector2.Distance(new Vector2(px, py), new Vector2(x0, y0));
            }

            float t = Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / len2);
            return Vector2.Distance(new Vector2(px, py), new Vector2(x0 + t * dx, y0 + t * dy));
        }

        public static Sprite CreateBorder(Color color, int size = 32, int thickness = 2)
        {
            string key = $"border:{color}:{size}:{thickness}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < thickness || y < thickness || x >= size - thickness || y >= size - thickness;
                    tex.SetPixel(x, y, edge ? color : Color.clear);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateSlope(SlopeVariant variant, Color color, int size = 32)
        {
            string key = $"slope:{variant}:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var edge = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            int last = size - 1;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool fill = IsSlopeFill(variant, x, y, last);
                    bool onEdge = IsSlopeEdge(variant, x, y, last);
                    if (!fill && !onEdge)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    tex.SetPixel(x, y, onEdge ? edge : color);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        static bool IsSlopeFill(SlopeVariant variant, int x, int y, int last)
        {
            switch (variant)
            {
                case SlopeVariant.NE: return y <= x;
                case SlopeVariant.NW: return y <= last - x;
                case SlopeVariant.SE: return y >= last - x;
                case SlopeVariant.SW: return y >= x;
                default: return false;
            }
        }

        static bool IsSlopeEdge(SlopeVariant variant, int x, int y, int last)
        {
            switch (variant)
            {
                case SlopeVariant.NE: return Mathf.Abs(y - x) <= 1;
                case SlopeVariant.NW: return Mathf.Abs(y - (last - x)) <= 1;
                case SlopeVariant.SE: return Mathf.Abs(y - (last - x)) <= 1;
                case SlopeVariant.SW: return Mathf.Abs(y - x) <= 1;
                default: return false;
            }
        }

        public static Sprite CreateMirrorSlope(SlopeVariant variant, Color color, int size = 32)
        {
            string key = $"mirrorslope:{variant}:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var shine = new Color(0.97f, 0.99f, 1f, 1f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            int last = size - 1;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool fill = IsSlopeFill(variant, x, y, last);
                    bool onEdge = IsSlopeEdge(variant, x, y, last);
                    if (!fill && !onEdge)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    tex.SetPixel(x, y, onEdge ? shine : color);
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateOneWay(Color color, int size = 32)
        {
            string key = $"oneway:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var top = new Color(Mathf.Min(1f, color.r + 0.2f), Mathf.Min(1f, color.g + 0.2f), Mathf.Min(1f, color.b + 0.2f), 1f);
            var tex = CreateTexture(key, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool body = y < size - 4;
                    bool topBand = y >= size - 4;
                    bool dash = topBand && (x / 3) % 2 == 0;
                    if (body)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else if (dash)
                    {
                        tex.SetPixel(x, y, top);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateSunlight(Color color, int size = 32)
        {
            string key = $"sunlight:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var core = new Color(1f, 0.98f, 0.55f, color.a);
            var tex = CreateTexture(key, size);
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = 1f - Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / (size * 0.55f);
                    t = Mathf.Clamp01(t);
                    tex.SetPixel(x, y, Color.Lerp(color, core, t));
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateFlag(Color color, int size = 32)
        {
            string key = $"flag:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var pole = new Color(0.55f, 0.4f, 0.25f, 1f);
            var tex = CreateTexture(key, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isPole = x <= 3;
                    bool isFlag = x > 3 && x < size - 2 && y >= size / 3 && y <= size * 2 / 3;
                    if (isPole)
                    {
                        tex.SetPixel(x, y, pole);
                    }
                    else if (isFlag)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreatePit(Color color, int size = 32)
        {
            string key = $"pit:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var dark = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var tex = CreateTexture(key, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    tex.SetPixel(x, y, border ? color : dark);
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateHighBounce(Color color, int size = 32)
        {
            string key = $"highbounce:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var highlight = new Color(Mathf.Min(1f, color.r + 0.25f), Mathf.Min(1f, color.g + 0.25f), Mathf.Min(1f, color.b + 0.25f), 1f);
            var tex = CreateTexture(key, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool sparkle = (x == size / 4 && y == size * 3 / 4)
                                   || (x == size * 3 / 4 && y == size / 4);
                    tex.SetPixel(x, y, sparkle ? highlight : color);
                }
            }

            return Finish(tex, key, size);
        }

        static Material _pipelineMaterial;

        public static void AssignPipelineMaterial(SpriteRenderer renderer, Material template = null)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = template != null ? template : PipelineMaterial();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        static Material PipelineMaterial()
        {
            if (_pipelineMaterial != null)
            {
                return _pipelineMaterial;
            }

            var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material shared = renderers[i].sharedMaterial;
                if (shared == null || shared.shader == null)
                {
                    continue;
                }

                if (shared.shader.name.Contains("Universal Render Pipeline/2D/Sprite"))
                {
                    _pipelineMaterial = shared;
                    return _pipelineMaterial;
                }
            }

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }

            if (shader == null)
            {
                return null;
            }

            _pipelineMaterial = new Material(shader)
            {
                name = "Pipeline Sprite (runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _pipelineMaterial;
        }

        public static Sprite CreateLightSensor(Color color, int size = 32)
        {
            string key = $"light-sensor:v2:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var bracket = new Color(color.r, color.g, color.b, 1f);
            var ray = new Color(1f, 0.9f, 0.28f, 1f);
            var ring = new Color(1f, 0.95f, 0.45f, 1f);
            var core = new Color(1f, 0.99f, 0.72f, 1f);
            var tex = CreateTexture(key, size);
            float center = (size - 1) * 0.5f;
            float arm = size * 0.34f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool left = x >= 1 && x < 4;
                    bool right = x >= size - 4 && x < size - 1;
                    bool bottom = y >= 1 && y < 4;
                    bool top = y >= size - 4 && y < size - 1;
                    bool inwardX = x >= 1 && x < size - 1 && (x < arm || x >= size - arm);
                    bool inwardY = y >= 1 && y < size - 1 && (y < arm || y >= size - arm);
                    bool bracketPixel = (top || bottom) && inwardX || (left || right) && inwardY;
                    bool rayPixel = dist > size * 0.32f && dist < size * 0.44f
                                    && (Mathf.Abs(dx) <= 1.2f || Mathf.Abs(dy) <= 1.2f);
                    bool ringPixel = dist >= size * 0.16f && dist <= size * 0.3f;
                    bool corePixel = dist <= size * 0.1f;
                    Color pixel = Color.clear;
                    if (bracketPixel)
                    {
                        pixel = bracket;
                    }

                    if (rayPixel)
                    {
                        pixel = ray;
                    }

                    if (ringPixel)
                    {
                        pixel = ring;
                    }

                    if (corePixel)
                    {
                        pixel = core;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateSwitch(Color color, int size = 32, bool toggle = false)
        {
            string key = $"switch:v3:{toggle}:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var plate = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var cap = new Color(Mathf.Min(1f, color.r + 0.12f), Mathf.Min(1f, color.g + 0.08f), Mathf.Min(1f, color.b + 0.08f), 1f);
            var shine = new Color(1f, 0.92f, 0.88f, 1f);
            var tex = CreateTexture(key, size);
            float center = (size - 1) * 0.5f;
            float radius = size * 0.26f;
            Vector2 hub = new Vector2(center, size * 0.30f);
            Vector2 tip = hub + new Vector2(-Mathf.Sin(38f * Mathf.Deg2Rad), Mathf.Cos(38f * Mathf.Deg2Rad)) * (size * 0.42f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool frame = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    float dist = Vector2.Distance(new Vector2(x, y), toggle ? hub : new Vector2(center, center));
                    bool handle = toggle && (DistToSegment(x, y, hub.x, hub.y, tip.x, tip.y) <= 1.6f
                                              || Vector2.Distance(new Vector2(x, y), tip) <= size * 0.09f);
                    bool hubDot = toggle && dist <= size * 0.07f;
                    if (!toggle && dist <= radius)
                    {
                        bool highlight = x > center - radius * 0.2f && y > center && dist < radius * 0.72f;
                        tex.SetPixel(x, y, highlight ? shine : cap);
                    }
                    else if (handle || hubDot)
                    {
                        tex.SetPixel(x, y, cap);
                    }
                    else
                    {
                        tex.SetPixel(x, y, frame ? plate : color);
                    }
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateLeverBase(Color color, int size = 32)
        {
            string key = $"lever-base:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var plate = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var hub = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 1f);
            var tex = CreateTexture(key, size);
            float center = (size - 1) * 0.5f;
            Vector2 pivot = new Vector2(center, size * 0.30f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool frame = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    bool pin = Vector2.Distance(new Vector2(x, y), pivot) <= size * 0.07f;
                    tex.SetPixel(x, y, pin ? hub : frame ? plate : color);
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateLeverHandle(Color color)
        {
            string key = $"lever-handle:{color}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            const int width = 6;
            const int height = 16;
            var cap = new Color(Mathf.Min(1f, color.r + 0.18f), Mathf.Min(1f, color.g + 0.16f), Mathf.Min(1f, color.b + 0.12f), 1f);
            var edge = new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 1f);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };

            Vector2 tip = new Vector2((width - 1) * 0.5f, height - 3f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool bar = x >= 2 && x <= 3 && y >= 1 && y <= height - 4;
                    bool knob = Vector2.Distance(new Vector2(x, y), tip) <= 2.2f;
                    if (bar || knob)
                    {
                        tex.SetPixel(x, y, knob ? Color.Lerp(cap, Color.white, 0.35f) : edge);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), 32f);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        public static Sprite CreateDoor(Color color, int size = 32)
        {
            string key = $"door:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var frame = new Color(color.r * 0.42f, color.g * 0.42f, color.b * 0.5f, 1f);
            var bar = new Color(0.72f, 0.58f, 0.28f, 1f);
            var gap = new Color(color.r * 0.2f, color.g * 0.22f, color.b * 0.28f, 1f);
            var tex = CreateTexture(key, size);
            int mid = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 3 || y < 3 || x >= size - 3 || y >= size - 3;
                    bool seam = Mathf.Abs(x - mid) <= 1;
                    bool brace = y > size * 0.42f && y < size * 0.58f && x >= 3 && x < size - 3;
                    if (border || seam)
                    {
                        tex.SetPixel(x, y, frame);
                    }
                    else if (brace)
                    {
                        tex.SetPixel(x, y, bar);
                    }
                    else if ((x / 4) % 2 == 0)
                    {
                        tex.SetPixel(x, y, gap);
                    }
                    else
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateBomb(Color color, int size = 32)
        {
            string key = $"bomb:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = CreateTexture(key, size);
            var edge = new Color(color.r * 0.28f, color.g * 0.28f, color.b * 0.28f, 1f);
            var hot = new Color(1f, 0.78f, 0.22f, 1f);
            var core = new Color(1f, 0.96f, 0.82f, 1f);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float hotRadius = size * 0.22f;
            float coreRadius = size * 0.1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    Color pixel = border ? edge : color;
                    if (dist <= coreRadius)
                    {
                        pixel = core;
                    }
                    else if (dist <= hotRadius)
                    {
                        pixel = hot;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            return Finish(tex, key, size);
        }

        public static Sprite CreateBlastWall(Color color, int size = 32)
        {
            string key = $"blastwall:{color}:{size}";
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = CreateTexture(key, size);
            var edge = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            var mortar = new Color(color.r * 0.72f, color.g * 0.72f, color.b * 0.72f, 1f);
            int course = Mathf.Max(1, size / 3);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    bool seam = y % course == 0 || (x + (y / course) * (size / 2)) % size == 0;
                    Color pixel = seam ? mortar : color;
                    if (border)
                    {
                        pixel = edge;
                    }

                    tex.SetPixel(x, y, pixel);
                }
            }

            return Finish(tex, key, size);
        }

        static Texture2D CreateTexture(string key, int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = key
            };
        }

        static Sprite Finish(Texture2D tex, string key, int size)
        {
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
