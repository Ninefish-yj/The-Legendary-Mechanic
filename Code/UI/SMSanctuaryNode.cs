using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>圣所空间视图中的单个圣所节点（星图上的方框节点）
    /// v0.38.9：参考道途树样式，缩小为方框节点，去掉大光晕
    /// </summary>
    public class SMSanctuaryNode : MonoBehaviour
    {
        public int SanctuaryIndex { get; private set; }
        public bool IsUnlocked { get; private set; }

        private Image _bgImage;
        private Image _borderImage;
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

        private const float NodeWidth = 90f;
        private const float NodeHeight = 32f;

        public static SMSanctuaryNode Create(Transform parent, int index, Vector2 position, System.Action<int> onClick)
        {
            var go = new GameObject($"SanctuaryNode_{index}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(NodeWidth, NodeHeight);
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
            // 背景（深色填充）
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            _bgImage = bgGo.AddComponent<Image>();
            _bgImage.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);
            _bgImage.raycastTarget = true;

            // 边框（用4个Image模拟，因为UGUI没有直接的边框组件）
            CreateBorder(bgGo.transform);

            // 按钮
            _button = bgGo.AddComponent<Button>();
            _button.targetGraphic = _bgImage;
            int idx = SanctuaryIndex;
            _button.onClick.AddListener(() => _onClick?.Invoke(idx));

            // 标签文字（居中）
            _labelText = SMUiSkin.MakeText(bgGo.transform, "", 11, TextAnchor.MiddleCenter);
            _labelText.supportRichText = true;
            var labelRect = _labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4, 0);
            labelRect.offsetMax = new Vector2(-4, 0);
        }

        private void CreateBorder(Transform parent)
        {
            // 用一个稍大的背景Image模拟边框
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(transform, false);
            borderGo.transform.SetAsFirstSibling();
            var borderRect = borderGo.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);
            _borderImage = borderGo.AddComponent<Image>();
            _borderImage.color = new Color(0.2f, 0.4f, 0.7f, 0.8f);
            _borderImage.raycastTarget = false;
        }

        public void Refresh()
        {
            int frags = SuperMechSanctuary.Data.sanctuary_fragments[SanctuaryIndex];
            IsUnlocked = frags >= SuperMechSanctuary.FragmentsToUnlock;

            Color baseColor = NodeColors[SanctuaryIndex];
            string name = LocalizedTextManager.getText(SuperMechSanctuary.SanctuaryNames[SanctuaryIndex]);

            if (IsUnlocked)
            {
                _borderImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f);
                _bgImage.color = new Color(baseColor.r * 0.15f, baseColor.g * 0.15f, baseColor.b * 0.2f, 0.9f);
                _labelText.text = $"<color=#{ColorToHex(baseColor)}>{name}</color>";
            }
            else
            {
                _borderImage.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
                _bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.8f);
                _labelText.text = $"<color=#888>{name}</color> <color=#f84><size=9>{frags}/{SuperMechSanctuary.FragmentsToUnlock}</size></color>";
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
                _borderImage.color = new Color(1f, 0.9f, 0.3f, 1f);
                _bgImage.color = new Color(0.15f, 0.12f, 0.05f, 0.95f);
            }
            else
            {
                Refresh();
            }
        }

        private static string ColorToHex(Color c)
        {
            return $"{Mathf.RoundToInt(c.r * 255):X2}{Mathf.RoundToInt(c.g * 255):X2}{Mathf.RoundToInt(c.b * 255):X2}";
        }
    }
}
