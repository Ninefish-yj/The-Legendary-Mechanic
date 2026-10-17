using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>v0.204 2.5D星空背景生成器：5层视差滚动+星云+银河带（纯静态工厂）</summary>
    internal static class StarfieldBackground
    {
        /// <summary>生成结果：包含需要闪烁的星星和各层视差容器</summary>
        public class Result
        {
            public List<Image> TwinkleStars = new List<Image>();
            public List<float> TwinklePhases = new List<float>();
            // v0.204 视差滚动各层容器
            public RectTransform FarLayer;      // 远景层：微小暗星，速度×0.2
            public RectTransform MidFarLayer;   // 中远层：小星星，速度×0.4
            public RectTransform MidLayer;      // 中景层：中等星星，速度×0.7
            public RectTransform NearLayer;     // 近景层：大亮星，速度×1.0
            public RectTransform NebulaLayer;   // 星云层：彩色星云，速度×0.1
        }

        /// <summary>在指定父节点下生成2.5D程序化星空背景，返回各层引用</summary>
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
            // v0.224 深空背景：蓝紫色调，更符合大众认知的宇宙
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.raycastTarget = false;
            bgImg.color = new Color(0.008f, 0.012f, 0.035f, 1f);

            // v0.204 创建5层视差容器（从远到近，后创建的在上层）
            result.NebulaLayer = CreateLayer(starfieldGo.transform, "NebulaLayer");
            result.FarLayer = CreateLayer(starfieldGo.transform, "FarLayer");
            result.MidFarLayer = CreateLayer(starfieldGo.transform, "MidFarLayer");
            result.MidLayer = CreateLayer(starfieldGo.transform, "MidLayer");
            result.NearLayer = CreateLayer(starfieldGo.transform, "NearLayer");

            // 宇宙尘埃层（放在星云层）
            var dustSprite = PlanetSpriteGenerator.GetNebulaSprite();
            for (int i = 0; i < 15; i++)
            {
                var dustGo = new GameObject($"Dust_{i}");
                dustGo.transform.SetParent(result.NebulaLayer, false);
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

            // 第一层：远景星星（1500颗，微小暗星）
            GenerateStars(result.FarLayer, rng, mapSize, 1500, 0.35f,
                0.8f, 1.2f, 0.15f, 0.25f, false, "FarStar");

            // 第二层：中远星星（800颗，小星星）
            GenerateStars(result.MidFarLayer, rng, mapSize, 800, 0.40f,
                1.2f, 2.0f, 0.25f, 0.35f, false, "MidFarStar");

            // 第三层：中景星星（500颗，中等星星，40%闪烁）
            GenerateStars(result.MidLayer, rng, mapSize, 500, 0.45f,
                1.8f, 3.0f, 0.35f, 0.5f, true, "MidStar", result);

            // v0.224 近景亮星（60颗，少量大亮星带十字光芒，全部闪烁）
            GenerateNearStars(result.NearLayer, rng, mapSize, 60, 0.55f, result);

            // v0.224 星云（40团，更鲜艳更明显，参考哈勃望远镜照片）
            var nebulaSprite = PlanetSpriteGenerator.GetNebulaSprite();
            Color[] nebulaColors = {
                new Color(0.5f, 0.2f, 0.7f, 0.35f),   // 紫红星云（猎户座）
                new Color(0.2f, 0.35f, 0.7f, 0.35f),  // 蓝色星云
                new Color(0.15f, 0.55f, 0.5f, 0.3f),  // 青绿星云（鹰星云）
                new Color(0.6f, 0.25f, 0.45f, 0.3f),  // 粉红星云
                new Color(0.7f, 0.35f, 0.2f, 0.25f),  // 橙色星云
                new Color(0.25f, 0.45f, 0.25f, 0.2f), // 绿色星云
                new Color(0.55f, 0.2f, 0.35f, 0.3f),  // 玫红星云
                new Color(0.3f, 0.3f, 0.6f, 0.3f),    // 靛蓝星云
            };
            for (int i = 0; i < 40; i++)
            {
                var nebulaGo = new GameObject($"Nebula_{i}");
                nebulaGo.transform.SetParent(result.NebulaLayer, false);
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

            // v0.224 银河带（10条，更明显的淡蓝白色）
            for (int i = 0; i < 10; i++)
            {
                var bandGo = new GameObject($"GalaxyBand_{i}");
                bandGo.transform.SetParent(result.NebulaLayer.transform, false);
                var bandRect = bandGo.AddComponent<RectTransform>();
                bandRect.anchorMin = new Vector2(0.5f, 0.5f);
                bandRect.anchorMax = new Vector2(0.5f, 0.5f);
                bandRect.pivot = new Vector2(0.5f, 0.5f);
                bandRect.sizeDelta = new Vector2(mapSize * 1.8f, 80 + rng.Next(120));
                bandRect.anchoredPosition = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * 400,
                    (float)(rng.NextDouble() - 0.5) * 400
                );
                bandRect.localRotation = Quaternion.Euler(0, 0, (float)(rng.NextDouble() * 20 - 10));
                var bandImg = bandGo.AddComponent<Image>();
                bandImg.sprite = nebulaSprite;
                bandImg.raycastTarget = false;
                bandImg.color = new Color(0.35f, 0.4f, 0.55f, 0.12f);
            }

            return result;
        }

        /// <summary>v0.204 创建视差层容器</summary>
        private static RectTransform CreateLayer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>v0.204 生成普通星星层</summary>
        private static void GenerateStars(RectTransform layer, System.Random rng, float mapSize,
            int count, float densityFactor, float minSize, float maxSize,
            float minAlpha, float maxAlpha, bool canTwinkle, string prefix, Result result = null)
        {
            int starCount = 0;
            int attempts = 0;
            while (starCount < count && attempts < count * 8)
            {
                attempts++;
                float sx = (float)(rng.NextDouble() - 0.5) * mapSize * 1.8f;
                float sy = (float)(rng.NextDouble() - 0.5) * mapSize * 1.8f;
                float density = SuperMechStarSystem.GetStarDensity(sx, -sy);
                if (rng.NextDouble() > density * densityFactor) continue;
                starCount++;

                var starGo = new GameObject($"{prefix}_{starCount}");
                starGo.transform.SetParent(layer, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin = new Vector2(0.5f, 0.5f);
                starRect.anchorMax = new Vector2(0.5f, 0.5f);
                starRect.pivot = new Vector2(0.5f, 0.5f);
                float size = (float)(minSize + rng.NextDouble() * (maxSize - minSize));
                starRect.sizeDelta = new Vector2(size, size);
                starRect.anchoredPosition = new Vector2(sx, sy);
                var starImg = starGo.AddComponent<Image>();
                starImg.raycastTarget = false;
                int colorRoll = rng.Next(100);
                if (colorRoll < 70) starImg.color = new Color(0.85f, 0.9f, 1f, (float)(minAlpha + rng.NextDouble() * (maxAlpha - minAlpha)));
                else if (colorRoll < 88) starImg.color = new Color(1f, 0.95f, 0.8f, (float)(minAlpha + rng.NextDouble() * (maxAlpha - minAlpha)));
                else if (colorRoll < 96) starImg.color = new Color(0.7f, 0.85f, 1f, (float)(minAlpha + rng.NextDouble() * (maxAlpha - minAlpha)));
                else starImg.color = new Color(1f, 0.65f, 0.5f, (float)(minAlpha + rng.NextDouble() * (maxAlpha - minAlpha)));

                if (canTwinkle && result != null && rng.NextDouble() < 0.4f)
                {
                    result.TwinkleStars.Add(starImg);
                    result.TwinklePhases.Add((float)rng.NextDouble() * Mathf.PI * 2f);
                }
            }
        }

        /// <summary>v0.204 生成近景亮星（带十字光芒）</summary>
        private static void GenerateNearStars(RectTransform layer, System.Random rng, float mapSize,
            int count, float densityFactor, Result result)
        {
            int starCount = 0;
            int attempts = 0;
            while (starCount < count && attempts < count * 10)
            {
                attempts++;
                float sx = (float)(rng.NextDouble() - 0.5) * mapSize * 1.2f;
                float sy = (float)(rng.NextDouble() - 0.5) * mapSize * 1.2f;
                float density = SuperMechStarSystem.GetStarDensity(sx, -sy);
                if (rng.NextDouble() > density * densityFactor) continue;
                starCount++;

                var starGo = new GameObject($"NearStar_{starCount}");
                starGo.transform.SetParent(layer, false);
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

                // v0.224 十字光芒：只有最大最亮的星才有，更小更淡
                if (size > 6f && rng.NextDouble() < 0.15f)
                {
                    var hFlareGo = new GameObject("HFlare");
                    hFlareGo.transform.SetParent(starGo.transform, false);
                    var hFlareRect = hFlareGo.AddComponent<RectTransform>();
                    hFlareRect.anchorMin = new Vector2(0.5f, 0.5f);
                    hFlareRect.anchorMax = new Vector2(0.5f, 0.5f);
                    hFlareRect.pivot = new Vector2(0.5f, 0.5f);
                    hFlareRect.sizeDelta = new Vector2(size * 3.5f, 1f);
                    var hFlareImg = hFlareGo.AddComponent<Image>();
                    hFlareImg.raycastTarget = false;
                    hFlareImg.color = new Color(starColor.r, starColor.g, starColor.b, 0.2f);

                    var vFlareGo = new GameObject("VFlare");
                    vFlareGo.transform.SetParent(starGo.transform, false);
                    var vFlareRect = vFlareGo.AddComponent<RectTransform>();
                    vFlareRect.anchorMin = new Vector2(0.5f, 0.5f);
                    vFlareRect.anchorMax = new Vector2(0.5f, 0.5f);
                    vFlareRect.pivot = new Vector2(0.5f, 0.5f);
                    vFlareRect.sizeDelta = new Vector2(1f, size * 3.5f);
                    var vFlareImg = vFlareGo.AddComponent<Image>();
                    vFlareImg.raycastTarget = false;
                    vFlareImg.color = new Color(starColor.r, starColor.g, starColor.b, 0.2f);
                }

                result.TwinkleStars.Add(starImg);
                result.TwinklePhases.Add((float)rng.NextDouble() * Mathf.PI * 2f);
            }
        }

        /// <summary>v0.204 视差滚动：根据地图内容偏移量移动各层</summary>
        public static void ApplyParallax(Result result, Vector2 contentOffset)
        {
            if (result == null) return;
            // 各层速度系数：远景慢，近景快
            if (result.NebulaLayer != null) result.NebulaLayer.anchoredPosition = contentOffset * 0.1f;
            if (result.FarLayer != null) result.FarLayer.anchoredPosition = contentOffset * 0.2f;
            if (result.MidFarLayer != null) result.MidFarLayer.anchoredPosition = contentOffset * 0.4f;
            if (result.MidLayer != null) result.MidLayer.anchoredPosition = contentOffset * 0.7f;
            if (result.NearLayer != null) result.NearLayer.anchoredPosition = contentOffset * 1.0f;
        }
    }
}
