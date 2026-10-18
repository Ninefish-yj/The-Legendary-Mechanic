using NeoModLoader.api;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位面板气力条：参考原版消耗条（体力/法力），在单位面板添加气力条
    /// 原著：超能者修炼气力，气力是核心资源，应像生命值/体力一样直观显示
    /// v0.242.3 统一气力条创建/更新逻辑：单位面板和超能详情面板共用同一套方法，
    /// 避免两处样式/数据不一致；彻底清理复制法力条带来的"法力"tooltip残留
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

            _qiBar.gameObject.SetActive(true);
            UpdateQiBarValue(_qiBar, actor);
        }

        /// <summary>
        /// v0.242.3 公共：从模板StatBar创建气力条，统一处理颜色/图标/Tooltip清理
        /// 单位面板和超能详情面板都调用此方法，确保两处完全一致
        /// </summary>
        public static StatBar CreateQiBarFromTemplate(StatBar template, Transform parent, string name)
        {
            if (template == null || parent == null) return null;

            // 检查是否已存在
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<StatBar>();
            }

            // 复制模板（worldPositionStays=false确保相对坐标正确）
            StatBar bar = Object.Instantiate(template, parent, false);
            if (bar == null) return null;
            bar.name = name;

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
                    int replaced = 0;
                    foreach (Image img in bar.GetComponentsInChildren<Image>(true))
                    {
                        if (img == barImage) continue;
                        if (img.gameObject == bar.gameObject) continue;
                        RectTransform ir = img.GetComponent<RectTransform>();
                        if (ir != null && ir.rect.width > 0 && ir.rect.width < 40f)
                        {
                            img.sprite = qiSprite;
                            replaced++;
                        }
                    }
                    if (replaced == 0)
                    {
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

            // v0.242.3 彻底清理复制过来的法力tooltip残留：
            // 1. 销毁所有Tooltip相关组件
            // 2. 清空所有子物体Text中可能残留的"法力"文本（除了bar.textField，它会被setBar覆盖）
            CleanManaResidue(bar);

            // 重建布局
            RectTransform parentRect = parent as RectTransform;
            if (parentRect != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            // v0.242.5 挂载气力条悬停tooltip组件（actor在UpdateQiBarValue时设置）
            bar.gameObject.AddComponent<SuperMechQiBarTooltip>();

            return bar;
        }

        /// <summary>
        /// v0.242.3 彻底清理复制法力条带来的残留：Tooltip组件 + "法力"文本
        /// </summary>
        private static void CleanManaResidue(StatBar bar)
        {
            if (bar == null) return;
            try
            {
                // 销毁所有Tooltip相关组件（匹配Tooltip/Tip/WorldTooltip/SimpleTooltip等）
                var allMono = bar.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in allMono)
                {
                    if (mb == null) continue;
                    string typeName = mb.GetType().Name;
                    if (typeName.Contains("Tooltip") || typeName.Contains("Tip") ||
                        typeName.Contains("Hover") || typeName.Contains("Description"))
                    {
                        Object.Destroy(mb);
                    }
                }

                // 清空所有子物体Text中可能残留的"法力"/"Mana"文本（保留bar.textField，它会被setBar覆盖）
                var allTexts = bar.GetComponentsInChildren<Text>(true);
                foreach (var t in allTexts)
                {
                    if (t == null) continue;
                    if (t == bar.textField) continue; // 数值文本由setBar管理
                    if (t.text.Contains("法力") || t.text.Contains("Mana") || t.text.Contains("mana"))
                    {
                        t.text = "";
                    }
                }
            }
            catch (System.Exception)
            {
                // 清理失败不影响功能
            }
        }

        /// <summary>
        /// v0.242.3 公共：统一更新气力条数值，单位面板和超能详情面板共用
        /// v0.242.5 同时设置tooltip的actor引用
        /// </summary>
        public static void UpdateQiBarValue(StatBar bar, Actor actor)
        {
            if (bar == null || actor == null) return;

            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            bool awakened = SuperMechAwakened.IsAwakened(actor);

            // v0.242.9 直接写入tooltip显示数据，不再存actor引用（避免引用失效导致0/0）
            // v0.242.19 改用原版Tooltip系统，需要临时存actor用于Tooltip.show传参
            var qiTooltip = bar.GetComponent<SuperMechQiBarTooltip>();
            if (qiTooltip != null)
            {
                qiTooltip.qi = qi;
                qiTooltip.qiMax = qiMax;
                qiTooltip.awakened = awakened;
                qiTooltip.actor = actor;
                if (qiMax > 0)
                {
                    int lv = SuperMechQi.GetLevel(qiMax);
                    qiTooltip.levelName = lv > 0 ? SuperMechQi.LevelNames[lv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
                }
                else
                {
                    qiTooltip.levelName = LocalizedTextManager.getText("sm_ui_qi_none");
                }
            }

            if (qiMax <= 0) qiMax = 1f;

            if (SuperMechQi.GetQiMax(actor) > 0)
            {
                bar.setBar(qi, qiMax, "/" + ((int)qiMax).ToString(), pReset: false, pFloat: false, pUpdateText: true, 0.25f);
            }
            else
            {
                bar.setBar(0, 1, LocalizedTextManager.getText("sm_ui_qi_none"), pReset: false, pFloat: false, pUpdateText: true, 0.25f);
            }
        }

        /// <summary>
        /// 创建气力条：复制魔力条（消耗条样式），放在所有原版条的最后
        /// v0.242.3 重构为调用公共CreateQiBarFromTemplate
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

                _qiBar = CreateQiBarFromTemplate(template, parent, BarName);
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
