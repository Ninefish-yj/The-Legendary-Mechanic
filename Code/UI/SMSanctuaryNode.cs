using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>圣所空间视图中的单个圣所节点（星图上的发光点）
    /// v0.35.0：圣所独立空间视图，节点以星图形式排列，不在地图上放置标记
    /// </summary>
    public class SMSanctuaryNode : MonoBehaviour
    {
        public int SanctuaryIndex { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Image _glowImage;
        private Image _coreImage;
        private Text _labelText;
        private Button _button;
        private System.Action<int> _onClick;

        private static readonly Color[] NodeColors =
        {
            new Color(0.3f, 0.7f, 1.0f),   // 第一圣所：机械系 - 冰蓝
            new Color(1.0f, 0.6f, 0.2f),   // 第二圣所：能量 - 橙
            new Color(0.3f, 1.0f, 0.5f),   // 第三圣所：生物基因 - 绿
            new Color(0.7f, 0.4f, 1.0f),   // 第四圣所：时空 - 紫
            new Color(0.9f, 0.3f, 0.7f),   // 第五圣所：维度 - 粉
            new Color(0.2f, 0.9f, 0.9f),   // 第六圣所：信息态 - 青
        };

        public static SMSanctuaryNode Create(Transform parent, int index, Vector2 position, System.Action<int> onClick)
        {
            var go = new GameObject($"SanctuaryNode_{index}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120, 80);
            rect.anchoredPosition = position;

            var node = go.AddComponent<SMSanctuaryNode>();
            node.SanctuaryIndex = index;
            node._onClick = onClick;
            node.Build();
            node.Refresh();
            return node;
        }

        private void Build()
        {
            // 外层光晕
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            var glowRect = glowGo.AddComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-20, -20);
            glowRect.offsetMax = new Vector2(20, 20);
            _glowImage = glowGo.AddComponent<Image>();
            _glowImage.color = new Color(1, 1, 1, 0.15f);
            _glowImage.raycastTarget = false;

            // 核心圆点
            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(transform, false);
            var coreRect = coreGo.AddComponent<RectTransform>();
            coreRect.anchorMin = new Vector2(0.5f, 0.5f);
            coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.pivot = new Vector2(0.5f, 0.5f);
            coreRect.sizeDelta = new Vector2(28, 28);
            coreRect.anchoredPosition = new Vector2(0, 10);
            _coreImage = coreGo.AddComponent<Image>();
            _coreImage.raycastTarget = true;

            // 按钮
            _button = coreGo.AddComponent<Button>();
            _button.targetGraphic = _coreImage;
            int idx = SanctuaryIndex;
            _button.onClick.AddListener(() => _onClick?.Invoke(idx));

            // 标签文字
            _labelText = SMUiSkin.MakeText(transform, "", 11, TextAnchor.UpperCenter);
            _labelText.supportRichText = true;
            var labelRect = _labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0);
            labelRect.pivot = new Vector2(0.5f, 0);
            labelRect.sizeDelta = new Vector2(0, 36);
            labelRect.anchoredPosition = new Vector2(0, 0);
        }

        public void Refresh()
        {
            int frags = SuperMechSanctuary.Data.sanctuary_fragments[SanctuaryIndex];
            IsUnlocked = frags >= SuperMechSanctuary.FragmentsToUnlock;

            Color baseColor = NodeColors[SanctuaryIndex];
            string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[SanctuaryIndex]);
            string typeName = SuperMechSanctuary.GetSanctuaryTypeName(SanctuaryIndex);

            if (IsUnlocked)
            {
                _coreImage.color = baseColor;
                _glowImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.25f);
                _labelText.text = $"<color=#{ColorToHex(baseColor)}>{name}</color>\n" +
                    $"<size=9><color=#aaa>{typeName}</color></size>\n" +
                    $"<size=8><color=#4f4>{LocalizedTextManager.getText("sm_ui_san_unlocked_tag")}</color></size>";
            }
            else
            {
                _coreImage.color = new Color(0.3f, 0.3f, 0.35f, 0.6f);
                _glowImage.color = new Color(0.3f, 0.3f, 0.35f, 0.1f);
                _labelText.text = $"<color=#666>{name}</color>\n" +
                    $"<size=9><color=#666>{typeName}</color></size>\n" +
                    $"<size=8><color=#f84>{frags}/{SuperMechSanctuary.FragmentsToUnlock}</color></size>";
            }

            var colors = _button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            _button.colors = colors;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                _coreImage.rectTransform.sizeDelta = new Vector2(36, 36);
                _glowImage.color = new Color(_coreImage.color.r, _coreImage.color.g, _coreImage.color.b, 0.4f);
            }
            else
            {
                _coreImage.rectTransform.sizeDelta = new Vector2(28, 28);
                Refresh();
            }
        }

        private static string ColorToHex(Color c)
        {
            return $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
        }
    }
}
