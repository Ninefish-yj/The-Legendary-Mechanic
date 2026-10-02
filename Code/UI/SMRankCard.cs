using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>排行榜卡片组件（v0.33.0 UI重构）
    /// 显示头像、排名、名称、阶位、体系、能级，点击可选中单位
    /// </summary>
    public class SMRankCard : MonoBehaviour
    {
        private Image _bg;
        private Image _avatar;
        private Text _rankText;
        private Text _nameText;
        private Text _infoText;
        private Button _button;
        private Actor _actor;

        public void Build(Actor actor, int rank, float score, string sortLabel)
        {
            _actor = actor;

            // 背景
            _bg = gameObject.GetComponent<Image>();
            if (_bg == null) _bg = gameObject.AddComponent<Image>();

            if (rank <= 3)
                _bg.color = new Color(SMUiSkin.AccentColor.r, SMUiSkin.AccentColor.g, SMUiSkin.AccentColor.b, 0.15f);
            else
                _bg.color = rank % 2 == 0 ? new Color(1, 1, 1, 0.03f) : new Color(1, 1, 1, 0.015f);

            // 排名徽章
            var rankGo = new GameObject("Rank");
            rankGo.transform.SetParent(transform, false);
            var rankRect = rankGo.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.5f);
            rankRect.anchorMax = new Vector2(0, 0.5f);
            rankRect.pivot = new Vector2(0, 0.5f);
            rankRect.sizeDelta = new Vector2(30, 20);
            rankRect.anchoredPosition = new Vector2(4, 0);
            _rankText = SMUiSkin.MakeText(rankGo.transform, $"#{rank}", 12, TextAnchor.MiddleCenter);
            if (rank <= 3) _rankText.color = SMUiSkin.AccentColor;

            // 头像
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(transform, false);
            var avatarRect = avatarGo.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.sizeDelta = new Vector2(24, 24);
            avatarRect.anchoredPosition = new Vector2(34, 0);
            _avatar = avatarGo.AddComponent<Image>();
            _avatar.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            // 尝试加载单位头像
            try
            {
                if (actor?.asset?.getSpriteIcon() != null)
                {
                    _avatar.sprite = actor.asset.getSpriteIcon();
                    _avatar.color = Color.white;
                }
            }
            catch { }

            // 名称+信息
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(62, 0);
            infoRect.offsetMax = new Vector2(-4, 0);

            _nameText = SMUiSkin.MakeText(infoGo.transform, actor?.name ?? "?", 13, TextAnchor.MiddleLeft);
            var nameRect = _nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            string rankName = SuperMechRanks.GetRankName(actor);
            string className = GetClassName(actor);
            string scoreText = sortLabel == null ? "" : $"{sortLabel}:{score:F0}";
            _infoText = SMUiSkin.MakeText(infoGo.transform, $"{rankName} · {className} {scoreText}", 10, TextAnchor.MiddleLeft);
            _infoText.color = new Color(0.7f, 0.7f, 0.7f);
            var infoTextRect = _infoText.GetComponent<RectTransform>();
            infoTextRect.anchorMin = new Vector2(0, 0);
            infoTextRect.anchorMax = new Vector2(1, 0.5f);
            infoTextRect.offsetMin = Vector2.zero;
            infoTextRect.offsetMax = Vector2.zero;

            // 点击按钮
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = _bg;
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

        private static string GetClassName(Actor a)
        {
            if (a == null) return "?";
            if (a.hasTrait(SuperMechTraits.ClassMech)) return LocalizedTextManager.getText("sm_class_mech");
            if (a.hasTrait(SuperMechTraits.ClassMage)) return LocalizedTextManager.getText("sm_class_mage");
            if (a.hasTrait(SuperMechTraits.ClassMind)) return LocalizedTextManager.getText("sm_class_power");
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return LocalizedTextManager.getText("sm_class_wudan");
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return LocalizedTextManager.getText("sm_class_psi");
            return LocalizedTextManager.getText("sm_ui_wild");
        }
    }
}
