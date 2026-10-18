using System;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>UI皮肤 - Sprite生成部分（程序化生成各种纹理：按钮/面板/光晕/六边形/角落装饰等）</summary>
    public static partial class SuperMechUiSkin
    {
        private static Sprite MakeSlicedSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    bool isCorner = ((x < borderWidth || x >= size - borderWidth) &&
                                     (y < borderWidth || y >= size - borderWidth));
                    bool isCutCorner = (x == 0 && y == 0) || (x == size - 1 && y == 0) ||
                                       (x == 0 && y == size - 1) || (x == size - 1 && y == size - 1);
                    if (isCutCorner)
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    else if (isBorder || isCorner)
                        pixels[y * size + x] = border;
                    else
                        pixels[y * size + x] = bg;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderWidth, borderWidth, borderWidth, borderWidth));
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.194 生成带垂直渐变+顶部高光+底部阴影的按钮Sprite</summary>
        private static Sprite MakeButtonSprite(Color bgTop, Color bgBottom, Color border,
            Color? highlight = null, Color? shadow = null, int size = 32, int borderWidth = 3, string name = "Btn")
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            int contentTop = borderWidth + 1;       // 高光行
            int contentBottom = size - borderWidth - 2; // 阴影行
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    bool isCutCorner = (x <= 1 && y <= 1) || (x >= size - 2 && y <= 1) ||
                                       (x <= 1 && y >= size - 2) || (x >= size - 2 && y >= size - 2);
                    if (isCutCorner)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (isBorder)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        // 垂直渐变
                        float t = (float)(y - borderWidth) / (size - 2 * borderWidth);
                        Color c = Color.Lerp(bgTop, bgBottom, t);
                        // 顶部高光（第borderWidth行）
                        if (y == borderWidth && highlight.HasValue)
                            c = Color.Lerp(c, highlight.Value, 0.7f);
                        // 次高光
                        else if (y == borderWidth + 1 && highlight.HasValue)
                            c = Color.Lerp(c, highlight.Value, 0.3f);
                        // 底部阴影（倒数第borderWidth行）
                        if (y == size - borderWidth - 1 && shadow.HasValue)
                            c = Color.Lerp(c, shadow.Value, 0.6f);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderWidth, borderWidth, borderWidth, borderWidth));
            sprite.name = name;
            return sprite;
        }

        /// <summary>生成纯色Sprite</summary>
        private static Sprite MakeSolidSprite(Color color, string name)
        {
            int size = 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>生成径向发光Sprite（用于标题装饰/选中高亮）</summary>
        private static Sprite MakeGlowSprite(Color color, string name)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist) * 0.6f;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 圆角+内发光边框Sprite（参考星穹铁道卡片风格）</summary>
        private static Sprite MakeRoundedGlowSprite(Color bg, Color border, Color? innerGlow, int size, int borderRadius, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            float radius = borderRadius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 圆角判断：四个角用圆形裁剪
                    bool inCorner = false;
                    float cornerDist = 0f;
                    if (x < borderRadius && y < borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((borderRadius - x) * (borderRadius - x) + (borderRadius - y) * (borderRadius - y));
                        inCorner = cornerDist > radius;
                    }
                    else if (x >= size - borderRadius && y < borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((x - (size - borderRadius - 1)) * (x - (size - borderRadius - 1)) + (borderRadius - y) * (borderRadius - y));
                        inCorner = cornerDist > radius;
                    }
                    else if (x < borderRadius && y >= size - borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((borderRadius - x) * (borderRadius - x) + (y - (size - borderRadius - 1)) * (y - (size - borderRadius - 1)));
                        inCorner = cornerDist > radius;
                    }
                    else if (x >= size - borderRadius && y >= size - borderRadius)
                    {
                        cornerDist = Mathf.Sqrt((x - (size - borderRadius - 1)) * (x - (size - borderRadius - 1)) + (y - (size - borderRadius - 1)) * (y - (size - borderRadius - 1)));
                        inCorner = cornerDist > radius;
                    }

                    if (inCorner)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    // 边框判断
                    bool isBorder = (x < borderWidth || x >= size - borderWidth ||
                                    y < borderWidth || y >= size - borderWidth);
                    // 圆角处的边框也要跟着圆
                    if (!isBorder && borderRadius > 0)
                    {
                        if ((x < borderRadius + borderWidth && y < borderRadius + borderWidth) ||
                            (x >= size - borderRadius - borderWidth && y < borderRadius + borderWidth) ||
                            (x < borderRadius + borderWidth && y >= size - borderRadius - borderWidth) ||
                            (x >= size - borderRadius - borderWidth && y >= size - borderRadius - borderWidth))
                        {
                            // 圆角区域内，距离边缘近的算边框
                            float edgeDist = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                            if (edgeDist < borderWidth) isBorder = true;
                        }
                    }

                    Color c = bg;
                    if (isBorder)
                    {
                        c = border;
                    }
                    else if (innerGlow.HasValue)
                    {
                        // 内发光：靠近边框的1-2像素混入发光色
                        float edgeDist = Mathf.Min(x - borderWidth, y - borderWidth, size - 1 - x - borderWidth, size - 1 - y - borderWidth);
                        if (edgeDist < 2f)
                        {
                            float glowAmount = (2f - edgeDist) / 2f * 0.35f;
                            c = Color.Lerp(c, innerGlow.Value, glowAmount);
                        }
                    }
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
                new Vector4(borderRadius, borderRadius, borderRadius, borderRadius));
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 圆形图标背景Sprite（参考原神/星穹铁道角色图标）</summary>
        private static Sprite MakeCircleSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center + 0.5f) * (x - center + 0.5f) + (y - center + 0.5f) * (y - center + 0.5f));
                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (dist > radius - borderWidth)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        // 径向渐变：中心亮边缘暗
                        float t = dist / radius;
                        Color c = Color.Lerp(new Color(bg.r * 1.3f, bg.g * 1.3f, bg.b * 1.3f, bg.a), bg, t);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.203 气力图标：蓝色能量球+青色闪电符号（原著机械系气力设定）</summary>
        private static Sprite MakeQiIconSprite()
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;
            Color bgCore = new Color(0.15f, 0.35f, 0.65f, 1f);
            Color bgEdge = new Color(0.05f, 0.12f, 0.25f, 1f);
            Color borderColor = new Color(0.3f, 0.75f, 0.95f, 1f);
            Color lightning = new Color(0.7f, 0.95f, 1f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - center + 0.5f) * (x - center + 0.5f) + (y - center + 0.5f) * (y - center + 0.5f));
                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (dist > radius - 2f)
                    {
                        pixels[y * size + x] = borderColor;
                    }
                    else
                    {
                        float t = dist / radius;
                        pixels[y * size + x] = Color.Lerp(bgCore, bgEdge, t);
                    }
                }
            }

            // 闪电符号（Z字形像素艺术）
            int[,] bolt = {
                {15, 6}, {16, 6}, {17, 7},
                {14, 8}, {15, 8}, {16, 8}, {17, 9},
                {13, 10}, {14, 10}, {15, 10}, {16, 10}, {17, 11},
                {14, 12}, {15, 12}, {16, 12}, {17, 13},
                {15, 14}, {16, 14}, {17, 15},
                {14, 16}, {15, 16}, {16, 16}, {17, 17},
                {15, 18}, {16, 18}, {17, 19},
                {16, 20}, {17, 21},
                {15, 22}, {16, 22}, {17, 23},
                {16, 24}, {17, 25}
            };
            for (int i = 0; i < bolt.GetLength(0); i++)
            {
                int px = bolt[i, 0];
                int py = bolt[i, 1];
                if (px >= 0 && px < size && py >= 0 && py < size)
                {
                    float dist = Mathf.Sqrt((px - center + 0.5f) * (px - center + 0.5f) + (py - center + 0.5f) * (py - center + 0.5f));
                    if (dist <= radius - 2f)
                    {
                        pixels[py * size + px] = lightning;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            var qiSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            qiSprite.name = "SM_QiIcon";
            return qiSprite;
        }

        /// <summary>v0.202 六边形图标背景Sprite（参考魔兽世界职业图标/科技感）</summary>
        private static Sprite MakeHexSprite(Color bg, Color border, int size, int borderWidth, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float cx = size / 2f, cy = size / 2f;
            float r = size / 2f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx + 0.5f, dy = y - cy + 0.5f;
                    // 六边形距离函数（pointy-top）
                    float q = (Mathf.Sqrt(3f) / 3f * dx - 1f / 3f * dy) / r;
                    float rcoord = (2f / 3f * dy) / r;
                    float hexDist = Mathf.Max(Mathf.Abs(q), Mathf.Abs(rcoord), Mathf.Abs(q + rcoord));
                    if (hexDist > 1f)
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                    else if (hexDist > 1f - (float)borderWidth / r)
                    {
                        pixels[y * size + x] = border;
                    }
                    else
                    {
                        float t = hexDist;
                        Color c = Color.Lerp(new Color(bg.r * 1.25f, bg.g * 1.25f, bg.b * 1.25f, bg.a), bg, t);
                        pixels[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>v0.202 装饰角标Sprite（L形金色角标，参考暗黑破坏神UI）</summary>
        private static Sprite MakeCornerDecorSprite(Color color, string name)
        {
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // L形：底部3像素+左侧3像素，右下角为原点
                    bool isL = (y < 3 && x < size) || (x < 3 && y < size);
                    // 末端渐细
                    if (y < 3 && x > size - 4) isL = false;
                    if (x < 3 && y > size - 4) isL = false;
                    if (isL)
                    {
                        // 末端淡出
                        float alpha = 1f;
                        if (y < 3 && x > size - 8) alpha = (float)(size - x) / 8f;
                        if (x < 3 && y > size - 8) alpha = Mathf.Min(alpha, (float)(size - y) / 8f);
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = new Color(0, 0, 0, 0);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0f, 0f), 100f);
            sprite.name = name;
            return sprite;
        }

        // ═════════════════════════ UI工具方法 ═════════════════════════

        /// <summary>创建Text组件（默认正文15号字，提高可读性）</summary>
    }
}
