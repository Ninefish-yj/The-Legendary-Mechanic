using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>排行榜卡片组件（v0.239.7 对齐天人武道天榜中间栏美术）
    /// 卡片背景：原版切片 ui/special/backgroundKingdomElement（Sliced 九切片）
    /// 布局：[排名#] [原版头像] [名字·阶位·体系] [能级] [伊纳尔]
    /// 前三名排名金/银/铜，点击选中单位
    /// </summary>
    public class SuperMechLeaderboardCard : MonoBehaviour
    {
        private Actor _actor;
        private Button _button;

        private static Sprite _panelSlice;
        private static UiUnitAvatarElement _avatarPrefab;

        /// <summary>天人武道同款原版切片（王国元素背景，Sliced）</summary>
        private static Sprite GetPanelSlice()
        {
            if (_panelSlice != null) return _panelSlice;
            try { _panelSlice = SpriteTextureLoader.getSprite("ui/special/backgroundKingdomElement"); }
            catch (System.Exception e) { Debug.LogWarning("[超神机械师] 排行榜切片加载失败: " + e.Message); }
            return _panelSlice;
        }

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

            const float cardH = 38f; // v0.239.7 对齐天人武道 CardHeight=38

            // === 卡片背景：原版切片（天人武道同款 backgroundKingdomElement，Sliced 九切片）===
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            Sprite slice = GetPanelSlice();
            if (slice != null)
            {
                bg.sprite = slice;
                bg.type = Image.Type.Sliced;
            }
            bg.color = rank <= 3 ? new Color(rankColor.r, rankColor.g, rankColor.b, 0.18f) : Color.white;

            // === 排名（左 26×26，前三名金/银/铜，对齐天人武道）===
            var rankGo = new GameObject("Rank");
            rankGo.transform.SetParent(transform, false);
            var rankRect = rankGo.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f);
            rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.pivot = new Vector2(0, 0.5f);
            rankRect.sizeDelta = new Vector2(26, 26);
            rankRect.anchoredPosition = new Vector2(4, 0);
            var rankText = rankGo.AddComponent<Text>();
            rankText.font = SuperMechUiSkin.DefaultFont;
            rankText.fontSize = 12;
            rankText.alignment = TextAnchor.MiddleCenter;
            rankText.text = $"#{rank}";
            rankText.color = rank == 1 ? new Color(1f, 0.84f, 0f)   // 金
                            : rank == 2 ? new Color(0.75f, 0.75f, 0.75f) // 银
                            : rank == 3 ? new Color(0.8f, 0.5f, 0.2f)   // 铜
                            : new Color(0.85f, 0.80f, 0.70f);

            // === 头像：原版 UiUnitAvatarElement（天人武道同款 ui/UnitAvatarElement，scale 0.6）===
            CreateAvatar(transform, actor);

            // === 名字（上排，x=64 y=+7，白色 fontSize9）===
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(64, 7);
            nameRect.sizeDelta = new Vector2(130, 16);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = SuperMechUiSkin.DefaultFont;
            nameText.fontSize = 9;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.text = displayName;
            nameText.color = Color.white;

            // === 阶位+体系（下排，x=64 y=-7，富文本）===
            var subGo = new GameObject("Sub");
            subGo.transform.SetParent(transform, false);
            var subRect = subGo.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0.5f);
            subRect.anchorMax = new Vector2(0, 0.5f);
            subRect.pivot = new Vector2(0, 0.5f);
            subRect.anchoredPosition = new Vector2(64, -7);
            subRect.sizeDelta = new Vector2(150, 16);
            var subText = subGo.AddComponent<Text>();
            subText.font = SuperMechUiSkin.DefaultFont;
            subText.fontSize = 8;
            subText.alignment = TextAnchor.MiddleLeft;
            subText.supportRichText = true;
            subText.horizontalOverflow = HorizontalWrapMode.Overflow;
            subText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(rankColor)}>{rankName}</color>  <color=#A6947A>{className}</color>";

            // === 能级（右上，金色 fontSize10，对齐天人武道 detailText 金色）===
            var onarGo = new GameObject("Onar");
            onarGo.transform.SetParent(transform, false);
            var onarRect = onarGo.AddComponent<RectTransform>();
            onarRect.anchorMin = new Vector2(1, 0.5f);
            onarRect.anchorMax = new Vector2(1, 0.5f);
            onarRect.pivot = new Vector2(1, 0.5f);
            onarRect.sizeDelta = new Vector2(60, 16);
            onarRect.anchoredPosition = new Vector2(-6, 5);
            var onarText = onarGo.AddComponent<Text>();
            onarText.font = SuperMechUiSkin.DefaultFont;
            onarText.fontSize = 10;
            onarText.alignment = TextAnchor.MiddleRight;
            onarText.text = onar >= 10000 ? $"{onar / 10000f:F1}w" : $"{onar:F0}";
            onarText.color = new Color(1f, 0.84f, 0f);

            // === 伊纳尔（右下，仅超A或有存款时显示）===
            int inal = SuperMechInal.GetInal(actor);
            if (inal > 0 || SuperMechSupermA.IsSuperA(actor))
            {
                var inalGo = new GameObject("Inal");
                inalGo.transform.SetParent(transform, false);
                var inalRect = inalGo.AddComponent<RectTransform>();
                inalRect.anchorMin = new Vector2(1, 0.5f);
                inalRect.anchorMax = new Vector2(1, 0.5f);
                inalRect.pivot = new Vector2(1, 0.5f);
                inalRect.sizeDelta = new Vector2(60, 14);
                inalRect.anchoredPosition = new Vector2(-6, -8);
                var inalText = inalGo.AddComponent<Text>();
                inalText.font = SuperMechUiSkin.DefaultFont;
                inalText.fontSize = 8;
                inalText.alignment = TextAnchor.MiddleRight;
                inalText.text = inal >= 10000 ? $"{inal / 10000f:F1}w" : inal.ToString();
                inalText.color = new Color(1f, 0.85f, 0.3f);
            }

            // === 点击按钮 ===
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = bg;
            _button.onClick.AddListener(OnClick);
        }

        /// <summary>v0.239.7 原版头像组件：UiUnitAvatarElement（Resources.Load ui/UnitAvatarElement），失败回退静态图标</summary>
        private void CreateAvatar(Transform parent, Actor actor)
        {
            try
            {
                if (_avatarPrefab == null)
                    _avatarPrefab = Resources.Load<UiUnitAvatarElement>("ui/UnitAvatarElement");
                if (_avatarPrefab != null)
                {
                    UiUnitAvatarElement avatar = Instantiate(_avatarPrefab, parent);
                    RectTransform rect = avatar.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    rect.anchoredPosition = new Vector2(30, 0);
                    rect.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                    avatar.show_banner_kingdom = false;
                    avatar.show_banner_clan = false;
                    if (avatar.kingdomBanner != null) avatar.kingdomBanner.gameObject.SetActive(false);
                    if (avatar.clanBanner != null) avatar.clanBanner.gameObject.SetActive(false);
                    if (actor != null) avatar.show(actor);
                    return;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[超神机械师] 排行榜原版头像异常: " + e.Message); }

            // 回退：静态图标
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(parent, false);
            var avatarRect = avatarGo.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.sizeDelta = new Vector2(28, 28);
            avatarRect.anchoredPosition = new Vector2(30, 0);
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
            catch (System.Exception e) { Debug.LogWarning("[超神机械师] 排行榜头像回退异常: " + e.Message); }
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
