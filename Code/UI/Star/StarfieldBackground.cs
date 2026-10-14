using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>星空背景生成器：程序化生成深空背景+三层星星+星云+银河带（纯静态工厂）</summary>
    internal static class StarfieldBackground
    {
        /// <summary>生成结果：包含需要闪烁的星星和对应相位</summary>
        public class Result
        {
            public List<Image> TwinkleStars = new List<Image>();
            public List<float> TwinklePhases = new List<float>();
        }

        /// <summary>在指定父节点下生成程序化星空背景，返回需要闪烁的星星列表</summary>
        public static Result Generate(Transform parent, float mapSize)
        {
            var result = new Result();
            var starfieldGo = new GameObject("Starfield");
            starfieldGo.transform.SetParent(parent, false);
            var sfRect = starfieldGo.AddComponent<RectTransform>();
            sfRect.anchorMin = Vector2.zero;
            sfRect.anchorMax = Vector2.one;
            sfRect.offsetMin = Vector2.zero;
            sfRect.offsetMax = Vector2.zero;
            sfRect.SetAsFirstSibling();

            System.Random rng = new System.Random(20240501);

            // 深空背景渐变
            var bgGo = new GameObject("DeepSpaceBG");
            bgGo.transform.SetParent(starfieldGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            bgImg.color = new Color(0.01f, 0.015f, 0.04f, 1f);

            // 宇宙尘埃层
            var dustSprite = PlanetSpriteGenerator.GetNebulaSprite();
            for (int i = 0; i < 15; i++)
            {
                var dustGo = new GameObject($"Dust_{i}");
                dustGo.transform.SetParent(starfieldGo.transform, false);
                var dustRect = dustGo.AddComponent<RectTransform>();
                dustRect.anchorMin = new Vector2(0.5f, 0.5f);
                dustRect.anchorMax = new Vector2(0.5f, 0.5f);
                dustRect.pivot = new Vector2(0.5f, 0.5f);
                float dustSize = (float)(600 + rng.NextDouble() * 1000);
                dustRect.sizeDelta = new Vector2(dustSize, dustSize * 0.6f);
                dustRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * mapSize * 1.5f,
                    (float)(rng.NextDouble() - 0.5) * mapSize * 1.5f
                );
                dustRect.localRotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 360));
                var dustImg = dustGo.AddComponent<Image>();
                dustImg.sprite = dustSprite;
                dustImg.raycastTarget = false;
                dustImg.color = new Color(0.15f, 0.1f, 0.08f, 0.08f);
            }

            // 第一层：远景星星（2000颗）
            int farStarCount = 0;
            int farStarAttempts = 0;
            while (farStarCount < 2000 && farStarAttempts < 15000)
            {
                farStarAttempts++;
                float sx = (float)(rng.NextDouble() - 0.5) * mapSize * 1.8f;
                float sy = (float)(rng.NextDouble() - 0.5) * mapSize * 1.8f;
                float density = SuperMechStarSystem.GetStarDensity(sx, -sy);
                if (rng.NextDouble() > density * 0.35f) continue;
                farStarCount++;

                var starGo = new GameObject($"FarStar_{farStarCount}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float size = (float)(0.8 + rng.NextDouble() * 1.2);
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(sx, sy);
                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;
                int colorRoll = rng.Next(100);
                if (colorRoll < 75) starImg.color = new Color(0.7f, 0.75f, 0.9f, (float)(0.15 + rng.NextDouble() * 0.25));
                else if (colorRoll < 92) starImg.color = new Color(0.9f, 0.85f, 0.7f, (float)(0.15 + rng.NextDouble() * 0.2));
                else starImg.color = new Color(0.9f, 0.6f, 0.5f, (float)(0.15 + rng.NextDouble() * 0.2));
            }

            // 第二层：中景星星（800颗）
            int midStarCount = 0;
            int midStarAttempts = 0;
            while (midStarCount < 800 && midStarAttempts < 8000)
            {
                midStarAttempts++;
                float sx = (float)(rng.NextDouble() - 0.5) * mapSize * 1.5f;
                float sy = (float)(rng.NextDouble() - 0.5) * mapSize * 1.5f;
                float density = SuperMechStarSystem.GetStarDensity(sx, -sy);
                if (rng.NextDouble() > density * 0.45f) continue;
                midStarCount++;

                var starGo = new GameObject($"MidStar_{midStarCount}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float size = (float)(1.5 + rng.NextDouble() * 2.5);
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(sx, sy);
                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;
                int colorRoll = rng.Next(100);
                if (colorRoll < 65) starImg.color = new Color(0.85f, 0.9f, 1f, (float)(0.3 + rng.NextDouble() * 0.4));
                else if (colorRoll < 85) starImg.color = new Color(1f, 0.95f, 0.8f, (float)(0.3 + rng.NextDouble() * 0.35));
                else if (colorRoll < 95) starImg.color = new Color(0.7f, 0.85f, 1f, (float)(0.3 + rng.NextDouble() * 0.3));
                else starImg.color = new Color(1f, 0.65f, 0.5f, (float)(0.3 + rng.NextDouble() * 0.3));
                if (rng.NextDouble() < 0.4f)
                {
                    result.TwinkleStars.Add(starImg);
                    result.TwinklePhases.Add((float)rng.NextDouble() * Mathf.PI * 2f);
                }
            }

            // 第三层：近景亮星（200颗，带十字光芒）
            int nearStarCount = 0;
            int nearStarAttempts = 0;
            while (nearStarCount < 200 && nearStarAttempts < 5000)
            {
                nearStarAttempts++;
                float sx = (float)(rng.NextDouble() - 0.5) * mapSize * 1.2f;
                float sy = (float)(rng.NextDouble() - 0.5) * mapSize * 1.2f;
                float density = SuperMechStarSystem.GetStarDensity(sx, -sy);
                if (rng.NextDouble() > density * 0.55f) continue;
                nearStarCount++;

                var starGo = new GameObject($"NearStar_{nearStarCount}");
                starGo.transform.SetParent(starfieldGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float size = (float)(3 + rng.NextDouble() * 6);
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(sx, sy);
                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;
                Color starColor;
                int colorRoll = rng.Next(100);
                if (colorRoll < 55) starColor = new Color(1f, 1f, 1f, (float)(0.7 + rng.NextDouble() * 0.3));
                else if (colorRoll < 75) starColor = new Color(0.7f, 0.85f, 1f, (float)(0.6 + rng.NextDouble() * 0.35));
                else if (colorRoll < 90) starColor = new Color(1f, 0.9f, 0.7f, (float)(0.6 + rng.NextDouble() * 0.35));
                else starColor = new Color(1f, 0.6f, 0.5f, (float)(0.5 + rng.NextDouble() * 0.3));
                starImg.color = starColor;

                if (size > 5f && rng.NextDouble() < 0.5f)
                {
                    var hFlareGo = new GameObject("HFlare");
                    hFlareGo.transform.SetParent(starGo.transform, false);
                    var hFlareRect = hFlareGo.AddComponent<RectTransform>();
                    hFlareRect.anchorMin = new Vector2(0.5f, 0.5f);
                    hFlareRect.anchorMax = new Vector2(0.5f, 0.5f);
                    hFlareRect.pivot = new Vector2(0.5f, 0.5f);
                    hFlareRect.sizeDelta = new Vector2(size * 6, 1.5f);
                    var hFlareImg = hFlareGo.AddComponent<Image>();
                    hFlareImg.raycastTarget = false;
                    hFlareImg.color = new Color(starColor.r, starColor.g, starColor.b, 0.3f);

                    var vFlareGo = new GameObject("VFlare");
                    vFlareGo.transform.SetParent(starGo.transform, false);
                    var vFlareRect = vFlareGo.AddComponent<RectTransform>();
                    vFlareRect.anchorMin = new Vector2(0.5f, 0.5f);
                    vFlareRect.anchorMax = new Vector2(0.5f, 0.5f);
                    vFlareRect.pivot = new Vector2(0.5f, 0.5f);
                    vFlareRect.sizeDelta = new Vector2(1.5f, size * 6);
                    var vFlareImg = vFlareGo.AddComponent<Image>();
                    vFlareImg.raycastTarget = false;
                    vFlareImg.color = new Color(starColor.r, starColor.g, starColor.b, 0.3f);
                }

                result.TwinkleStars.Add(starImg);
                result.TwinklePhases.Add((float)rng.NextDouble() * Mathf.PI * 2f);
            }

            // 星云（25团）
            var nebulaSprite = PlanetSpriteGenerator.GetNebulaSprite();
            Color[] nebulaColors = {
                new Color(0.4f, 0.15f, 0.6f, 0.22f),
                new Color(0.15f, 0.25f, 0.6f, 0.22f),
                new Color(0.1f, 0.45f, 0.45f, 0.18f),
                new Color(0.5f, 0.2f, 0.4f, 0.18f),
                new Color(0.6f, 0.3f, 0.15f, 0.15f),
                new Color(0.2f, 0.4f, 0.2f, 0.12f),
                new Color(0.5f, 0.15f, 0.3f, 0.15f),
            };
            for (int i = 0; i < 25; i++)
            {
                var nebulaGo = new GameObject($"Nebula_{i}");
                nebulaGo.transform.SetParent(starfieldGo.transform, false);
                var nebulaRect = nebulaGo.AddComponent<RectTransform>();
                nebulaRect.anchorMin = new Vector2(0.5f, 0.5f);
                nebulaRect.anchorMax = new Vector2(0.5f, 0.5f);
                nebulaRect.pivot = new Vector2(0.5f, 0.5f);
                float nebulaSize = (float)(500 + rng.NextDouble() * 1000);
                nebulaRect.sizeDelta = new Vector2(nebulaSize, nebulaSize * (0.7f + (float)rng.NextDouble() * 0.6f));
                nebulaRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * mapSize * 1.3f,
                    (float)(rng.NextDouble() - 0.5) * mapSize * 1.3f
                );
                nebulaRect.localRotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 360));
                var nebulaImg = nebulaGo.AddComponent<Image>();
                nebulaImg.sprite = nebulaSprite;
                nebulaImg.raycastTarget = false;
                nebulaImg.color = nebulaColors[rng.Next(nebulaColors.Length)];
            }

            // 银河带（8条）
            for (int i = 0; i < 8; i++)
            {
                var bandGo = new GameObject($"GalaxyBand_{i}");
                bandGo.transform.SetParent(starfieldGo.transform, false);
                var bandRect = bandGo.AddComponent<RectTransform>();
                bandRect.anchorMin = new Vector2(0.5f, 0.5f);
                bandRect.anchorMax = new Vector2(0.5f, 0.5f);
                bandRect.pivot = new Vector2(0.5f, 0.5f);
                bandRect.sizeDelta = new Vector2(mapSize * 1.8f, 100 + rng.Next(150));
                bandRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * 500,
                    (float)(rng.NextDouble() - 0.5) * 500
                );
                bandRect.localRotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 25 - 12.5));
                var bandImg = bandGo.AddComponent<Image>();
                bandImg.sprite = nebulaSprite;
                bandImg.raycastTarget = false;
                bandImg.color = new Color(0.25f, 0.3f, 0.45f, 0.08f);
            }

            return result;
        }
    }
}
