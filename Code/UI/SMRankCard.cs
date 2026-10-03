using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>排行榜卡片组件（v0.40.0重设计）
    /// 布局：[排名徽章] [头像] [名称·阶位·体系] [能级]
    /// 点击选中单位，前三名高亮
    /// </summary>
    public class SMRankCard : MonoBehaviour
    {
        private Actor _actor;
        private Button _button;

        private static readonly Color[] RankColors = {
            new Color(0.53f, 0.53f, 0.53f), // F 灰
            new Color(0.80f, 0.80f, 0.80f), // E 银白
            new Color(0.31f, 0.80f, 0.44f), // D 绿
            new Color(0.23f, 0.62f, 1.00f), // C 蓝
            new Color(0.66f, 0.33f, 0.97f), // B 紫
            new Color(0.96f, 0.62f, 0.04f), // A 橙
            new Color(1.00f, 0.42f, 0.21f), // S 橙红
            new Color(1.00f, 0.20f, 0.20f), // X 红
        };

        public void Build(Actor actor, int rank, float onar, string className)
        {
            _actor = actor;
            string displayName = actor?.name ?? "?";
            if (displayName.Length > 10) displayName = displayName.Substring(0, 10) + "..";

            int rankIdx = SuperMechAdvancement.GetRankIndex(actor);
            Color rankColor = rankIdx >= 0 && rankIdx < RankColors.Length ? RankColors[rankIdx] : Color.white;
            string rankName = SuperMechRanks.GetRankName(actor);

            // 背景
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            if (rank <= 3)
                bg.color = new Color(rankColor.r, rankColor.g, rankColor.b, 0.12f);
            else
                bg.color = rank % 2 == 0 ? new Color(1, 1, 1, 0.02f) : new Color(1, 1, 1, 0.00f);

            float cardH = 34f;

            // === 排名徽章 ===
            var rankGo = new GameObject("Rank");
            rankGo.transform.SetParent(transform, false);
            var rankRect = rankGo.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f);
            rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.pivot = new Vector2(0, 0.5f);
            rankRect.sizeDelta = new Vector2(30, cardH);
            rankRect.anchoredPosition = new Vector2(2, 0);
            var rankText = rankGo.AddComponent<Text>();
            rankText.font = SMUiSkin.DefaultFont;
            rankText.fontSize = 12;
            rankText.alignment = TextAnchor.MiddleCenter;
            rankText.text = $"#{rank}";
            rankText.color = rank <= 3 ? rankColor : new Color(0.70f, 0.62f, 0.50f);

            // === 头像 ===
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(transform, false);
            var avatarRect = avatarGo.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.sizeDelta = new Vector2(24, 24);
            avatarRect.anchoredPosition = new Vector2(32, 0);
            var avatarImg = avatarGo.AddComponent<Image>();
            avatarImg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            try
            {
                if (actor?.asset?.getSpriteIcon() != null)
                {
                    avatarImg.sprite = actor.asset.getSpriteIcon();
                    avatarImg.color = Color.white;
                }
            }
            catch { }

            // === 名称+阶位+体系（中间区域）===
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0.5f);
            infoRect.anchorMax = new Vector2(1, 0.5f);
            infoRect.pivot = new Vector2(0, 0.5f);
            infoRect.sizeDelta = new Vector2(-100, cardH);
            infoRect.anchoredPosition = new Vector2(58, 0);
            var infoText = infoGo.AddComponent<Text>();
            infoText.font = SMUiSkin.DefaultFont;
            infoText.fontSize = 12;
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
            infoText.verticalOverflow = VerticalWrapMode.Truncate;
            // 名称米白 + 阶位带色 + 体系暗金
            infoText.text = $"<color=#ebe0cc>{displayName}</color>  <color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>{rankName}</color>  <color=#9a8a6e>{className}</color>";

            // === 能级（右侧）===
            var onarGo = new GameObject("Onar");
            onarGo.transform.SetParent(transform, false);
            var onarRect = onarGo.AddComponent<RectTransform>();
            onarRect.anchorMin = new Vector2(1, 0.5f);
            onarRect.anchorMax = new Vector2(1, 0.5f);
            onarRect.pivot = new Vector2(1, 0.5f);
            onarRect.sizeDelta = new Vector2(70, cardH);
            onarRect.anchoredPosition = new Vector2(-4, 0);
            var onarText = onarGo.AddComponent<Text>();
            onarText.font = SMUiSkin.DefaultFont;
            onarText.fontSize = 11;
            onarText.alignment = TextAnchor.MiddleRight;
            onarText.text = onar >= 10000 ? $"{onar / 10000f:F1}w" : $"{onar:F0}";
            onarText.color = new Color(0.83f, 0.72f, 0.50f);

            // === 点击按钮 ===
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = bg;
            _button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (_actor == null || !_actor.isAlive()) return;
            try
            {
                ActionLibrary.openUnitWindow(_actor);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 排行榜点击单位失败: " + e.Message);
            }
        }
    }
}
