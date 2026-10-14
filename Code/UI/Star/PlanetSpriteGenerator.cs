using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>星球精灵生成器：程序化生成星云/光晕/星球纹理（纯静态，无实例依赖）</summary>
    internal static class PlanetSpriteGenerator
    {
        private static readonly Dictionary<string, Sprite> _planetSpriteCache = new Dictionary<string, Sprite>();
        private static Sprite _glowSprite;
        private static Sprite _nebulaSprite;

        /// <summary>生成圆形星云渐变纹理（中心亮边缘透明）</summary>
        public static Sprite GetNebulaSprite()
        {
            if (_nebulaSprite != null) return _nebulaSprite;
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                    float alpha = dist < 1f ? Mathf.Pow(1f - dist, 1.5f) * 0.4f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _nebulaSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _nebulaSprite;
        }

        /// <summary>生成圆形光晕纹理（中心亮边缘透明的径向渐变）</summary>
        public static Sprite GetGlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
                    float alpha = dist < 1f ? Mathf.Pow(1f - dist, 2f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _glowSprite;
        }

        /// <summary>生成圆形星球纹理（带光照渐变+表面噪声，纯代码实现）</summary>
        public static Sprite GetPlanetSprite(Color baseColor, int seed, bool isUnknown = false, PlanetType planetType = PlanetType.Rocky)
        {
            string key = $"{baseColor.r:F2}_{baseColor.g:F2}_{baseColor.b:F2}_{seed}_{isUnknown}_{planetType}";
            if (_planetSpriteCache.TryGetValue(key, out var cached)) return cached;

            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var rng = new System.Random(seed);

            // 预生成大陆噪声（低频）
            float[,] continentNoise = new float[size, size];
            for (int i = 0; i < 5; i++)
            {
                float cx = (float)rng.NextDouble() * size;
                float cy = (float)rng.NextDouble() * size;
                float radius = 15 + (float)rng.NextDouble() * 35;
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        float dx = x - cx, dy = y - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < radius)
                        {
                            continentNoise[x, y] += (1f - d / radius) * (0.5f + (float)rng.NextDouble() * 0.5f);
                        }
                    }
                }
            }

            // 预生成云层噪声
            float[,] cloudNoise = new float[size, size];
            for (int i = 0; i < 8; i++)
            {
                float cx = (float)rng.NextDouble() * size;
                float cy = (float)rng.NextDouble() * size;
                float radius = 10 + (float)rng.NextDouble() * 25;
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        float dx = x - cx, dy = y - cy;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        if (d < radius)
                        {
                            cloudNoise[x, y] += (1f - d / radius) * 0.4f;
                        }
                    }
                }
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);

                    if (dist > 1f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (isUnknown)
                    {
                        tex.SetPixel(x, y, new Color(0.3f, 0.3f, 0.35f, 0.4f * (1f - dist * 0.3f)));
                        continue;
                    }

                    // 光照：左上光源
                    float nx = dx / (size / 2f);
                    float ny = dy / (size / 2f);
                    float light = 1f - (nx * 0.5f + ny * 0.5f) * 0.45f;
                    light = Mathf.Clamp01(light);

                    // 边缘暗化（球体感）
                    float edge = 1f - dist * dist * 0.6f;

                    Color c = baseColor;

                    // 根据行星类型添加表面特征
                    float cn = continentNoise[x, y];
                    float cl = cloudNoise[x, y];
                    float lat = Mathf.Abs(ny); // 纬度（用于极冠）

                    switch (planetType)
                    {
                        case PlanetType.Ocean:
                            if (cn > 0.6f) c = Color.Lerp(c, new Color(0.3f, 0.5f, 0.35f), 0.7f);
                            else c = Color.Lerp(c, new Color(0.1f, 0.25f, 0.6f), 0.5f);
                            break;
                        case PlanetType.Forest:
                            if (cn > 0.4f) c = Color.Lerp(c, new Color(0.2f, 0.55f, 0.25f), 0.6f);
                            else c = Color.Lerp(c, new Color(0.15f, 0.3f, 0.55f), 0.4f);
                            break;
                        case PlanetType.Desert:
                            c = Color.Lerp(c, new Color(0.85f, 0.7f, 0.4f), 0.5f);
                            if (cn > 0.5f) c = Color.Lerp(c, new Color(0.9f, 0.8f, 0.55f), 0.4f);
                            break;
                        case PlanetType.Ice:
                            c = Color.Lerp(c, new Color(0.85f, 0.95f, 1f), 0.6f);
                            if (cn > 0.5f) c = Color.Lerp(c, new Color(0.6f, 0.8f, 0.95f), 0.5f);
                            break;
                        case PlanetType.Lava:
                            c = Color.Lerp(c, new Color(0.15f, 0.1f, 0.1f), 0.5f);
                            if (cn > 0.45f) c = Color.Lerp(c, new Color(1f, 0.4f, 0.15f), 0.8f);
                            break;
                        case PlanetType.Gas:
                            float band = Mathf.Sin(ny * 8 + cn * 2) * 0.5f + 0.5f;
                            c = Color.Lerp(baseColor * 0.7f, baseColor * 1.2f, band);
                            if (cn > 0.7f) c = Color.Lerp(c, new Color(0.9f, 0.5f, 0.3f), 0.5f);
                            break;
                        default:
                            if (cn > 0.55f) c = Color.Lerp(c, new Color(0.5f, 0.45f, 0.4f), 0.4f);
                            break;
                    }

                    // 极冠（高纬度地区变白）
                    if (lat > 0.75f && planetType != PlanetType.Lava && planetType != PlanetType.Gas)
                    {
                        c = Color.Lerp(c, new Color(0.95f, 0.97f, 1f), (lat - 0.75f) * 3f);
                    }

                    // 云层覆盖
                    if (cl > 0.5f && planetType != PlanetType.Lava)
                    {
                        c = Color.Lerp(c, new Color(1f, 1f, 1f), (cl - 0.5f) * 0.6f);
                    }

                    // 应用光照和边缘
                    c = c * light * edge;

                    // 细微噪声
                    float noise = (float)(rng.NextDouble() - 0.5) * 0.08f;
                    c.r = Mathf.Clamp01(c.r + noise);
                    c.g = Mathf.Clamp01(c.g + noise);
                    c.b = Mathf.Clamp01(c.b + noise);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            _planetSpriteCache[key] = sprite;
            return sprite;
        }
    }
}
