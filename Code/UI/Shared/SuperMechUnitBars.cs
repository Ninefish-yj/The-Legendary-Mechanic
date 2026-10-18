using NeoModLoader.api;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位面板气力条：参考原版消耗条（体力/法力），在单位面板添加气力条
    /// 原著：超能者修炼气力，气力是核心资源，应像生命值/体力一样直观显示
    /// 实现参考诡秘之主灵性条：使用LayoutElement+LayoutRebuilder确保布局正确
    /// </summary>
    public static class SuperMechUnitBars
    {
        private const string BarName = "bar_qi";
        private static StatBar _qiBar;
        private static bool _initialized = false;
        // v0.240.6 气力条统一为帝国盒子暖金色（对齐全局UI配色）
        private static readonly Color QiBarColor = SuperMechUiSkin.CyanColor; // 金色

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
        }

        /// <summary>
        /// 在showStatBars Postfix中调用：确保气力条存在并更新值
        /// </summary>
        public static void UpdateQiBar(SelectedUnitTab tab, Actor actor)
        {
            if (tab == null || actor == null) return;
            if (!SuperMechAwakened.IsAwakened(actor))
            {
                if (_qiBar != null) _qiBar.gameObject.SetActive(false);
                return;
            }

            // 首次创建：复制health条作为模板（health条一定存在且布局最稳定）
            if (_qiBar == null)
            {
                CreateQiBar(tab);
            }

            if (_qiBar == null) return;

            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            if (qiMax <= 0) qiMax = 1f;

            _qiBar.gameObject.SetActive(true);
            _qiBar.setBar(qi, qiMax, "/" + ((int)qiMax).ToString(), pReset: false, pFloat: false, pUpdateText: true, 0.25f);
        }

        /// <summary>
        /// 创建气力条：复制魔力条（消耗条样式），放在所有原版条的最后
        /// v0.239.5 模板由health改为mana：与原版饥饿/体力/魔力条同属消耗条样式，
        /// 且与超能面板气力条（SuperMechUnitWindow）统一模板，避免两处样式不一致
        /// 使用LayoutElement确保布局槽位，LayoutRebuilder重建布局
        /// </summary>
        private static void CreateQiBar(SelectedUnitTab tab)
        {
            try
            {
                // 用反射获取魔力条（消耗条样式；health/饥饿/幸福度条结构与样式不同源）
                var manaField = typeof(SelectedUnitTab).GetField("_bar_mana",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (manaField == null) return;

                StatBar template = manaField.GetValue(tab) as StatBar;
                if (template == null) return;

                Transform parent = template.transform.parent;
                if (parent == null) return;

                // 检查是否已存在
                Transform existing = parent.Find(BarName);
                if (existing != null)
                {
                    _qiBar = existing.GetComponent<StatBar>();
                    return;
                }

                // 复制模板（worldPositionStays=false确保相对坐标正确）
                StatBar bar = Object.Instantiate(template, parent, false);
                if (bar == null) return;
                bar.name = BarName;
                _qiBar = bar;

                // 设置兄弟索引：放在最后（所有原版条之后）
                bar.transform.SetSiblingIndex(parent.childCount);

                // 复制大小
                RectTransform sourceRect = template.GetComponent<RectTransform>();
                RectTransform targetRect = bar.GetComponent<RectTransform>();
                if (sourceRect != null && targetRect != null)
                {
                    targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sourceRect.rect.width);
                    targetRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sourceRect.rect.height);
                }

                // 添加LayoutElement确保布局槽位（关键：否则Layout Group不会给它留位置）
                LayoutElement layout = bar.GetComponent<LayoutElement>();
                if (layout == null) layout = bar.gameObject.AddComponent<LayoutElement>();
                if (sourceRect != null)
                {
                    layout.minWidth = sourceRect.rect.width;
                    layout.preferredWidth = sourceRect.rect.width;
                    layout.flexibleWidth = 0f;
                    layout.minHeight = sourceRect.rect.height;
                    layout.preferredHeight = sourceRect.rect.height;
                    layout.flexibleHeight = 0f;
                }

                // 修改填充条颜色为金色
                Image barImage = bar.bar.GetComponent<Image>();
                if (barImage != null) barImage.color = QiBarColor;

                // 修改文本颜色
                if (bar.textField != null) bar.textField.color = QiBarColor;

                // 替换图标为自定义气力图标（统一使用动态生成的UiSkin.QiIcon）
                try
                {
                    Sprite qiSprite = SuperMechUiSkin.QiIcon;
                    if (qiSprite != null)
                    {
                        // v0.240.1 强制替换所有子物体图标：复制mana条时可能有多个Image子物体
                        int replaced = 0;
                        foreach (Image img in bar.GetComponentsInChildren<Image>(true))
                        {
                            if (img == barImage) continue;
                            if (img.gameObject == bar.gameObject) continue;
                            // 跳过背景框（通常是bar的父级或mask），只替换小图标
                            RectTransform ir = img.GetComponent<RectTransform>();
                            if (ir != null && ir.rect.width > 0 && ir.rect.width < 40f)
                            {
                                img.sprite = qiSprite;
                                replaced++;
                            }
                        }
                        if (replaced == 0)
                        {
                            // 兜底：按名字找icon
                            Transform iconTransform = bar.transform.Find("icon") ?? bar.transform.Find("Icon");
                            if (iconTransform != null)
                            {
                                Image iconImage = iconTransform.GetComponent<Image>();
                                if (iconImage != null) iconImage.sprite = qiSprite;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // 图标替换失败不影响功能
                }

                // v0.240.1 清理复制过来的法力tooltip：遍历所有子物体移除Tooltip组件
                try
                {
                    var tooltips = bar.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var mb in tooltips)
                    {
                        if (mb == null) continue;
                        string typeName = mb.GetType().Name;
                        if (typeName.Contains("Tooltip") || typeName.Contains("Tip"))
                        {
                            Object.Destroy(mb);
                        }
                    }
                }
                catch (System.Exception)
                {
                    // tooltip清理失败不影响功能
                }

                // 重建布局（关键：让Layout Group重新计算所有子对象位置）
                RectTransform parentRect = parent as RectTransform;
                if (parentRect != null)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }

                Debug.Log("[超神机械师] 气力条创建成功");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 气力条创建失败: " + e.Message);
            }
        }

        public static void ClearStaticState()
        {
            _qiBar = null;
            _initialized = false;
        }
    }
}
