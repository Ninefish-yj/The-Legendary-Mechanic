using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位面板气力条：参考原版消耗条（体力/法力），在单位面板添加气力条
    /// 原著：超能者修炼气力，气力是核心资源，应像生命值/体力一样直观显示
    /// </summary>
    public static class SuperMechUnitBars
    {
        private static StatBar _qiBar;
        private static bool _initialized = false;
        private static readonly Color QiBarColor = new Color(0.95f, 0.78f, 0.25f, 1f); // 金色，代表气力/超能能量

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

            // 首次创建：复制mana条作为模板
            if (_qiBar == null)
            {
                CreateQiBar(tab);
            }

            if (_qiBar == null) return;

            // 动态调整位置：基于当前显示的最后一个条
            UpdateQiBarPosition(tab);

            float qi = SuperMechQi.GetQi(actor);
            float qiMax = SuperMechQi.GetQiMax(actor);
            if (qiMax <= 0) qiMax = 1f;

            _qiBar.gameObject.SetActive(true);
            _qiBar.setBar(qi, qiMax, "/" + ((int)qiMax).ToString(), pReset: false, pFloat: false, pUpdateText: true, 0.25f);
        }

        /// <summary>
        /// 动态调整气力条位置：放在当前显示的最后一个条下方
        /// </summary>
        private static void UpdateQiBarPosition(SelectedUnitTab tab)
        {
            try
            {
                var manaField = typeof(SelectedUnitTab).GetField("_bar_mana",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var staminaField = typeof(SelectedUnitTab).GetField("_bar_stamina",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hungerField = typeof(SelectedUnitTab).GetField("_bar_hunger",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                StatBar manaBar = manaField?.GetValue(tab) as StatBar;
                StatBar staminaBar = staminaField?.GetValue(tab) as StatBar;
                StatBar hungerBar = hungerField?.GetValue(tab) as StatBar;

                // 找到当前显示的最下方的条作为参考
                StatBar reference = null;
                if (manaBar != null && manaBar.gameObject.activeSelf) reference = manaBar;
                else if (staminaBar != null && staminaBar.gameObject.activeSelf) reference = staminaBar;
                else if (hungerBar != null && hungerBar.gameObject.activeSelf) reference = hungerBar;

                if (reference == null) return;

                RectTransform refRect = reference.GetComponent<RectTransform>();
                RectTransform qiRect = _qiBar.GetComponent<RectTransform>();
                qiRect.anchoredPosition = new Vector2(refRect.anchoredPosition.x,
                    refRect.anchoredPosition.y - refRect.sizeDelta.y - 4f);
            }
            catch (System.Exception)
            {
                // 位置调整失败不影响功能
            }
        }

        private static void CreateQiBar(SelectedUnitTab tab)
        {
            try
            {
                // 找到mana条作为模板和位置参考（法力条通常是最后一个，放在它下方最安全）
                var manaField = typeof(SelectedUnitTab).GetField("_bar_mana",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (manaField == null) return;

                StatBar template = manaField.GetValue(tab) as StatBar;
                if (template == null) return;

                // 复制模板
                GameObject qiBarObj = Object.Instantiate(template.gameObject, template.transform.parent);
                qiBarObj.name = "bar_qi";
                _qiBar = qiBarObj.GetComponent<StatBar>();

                // 设置位置：在mana条下方
                RectTransform templateRect = template.GetComponent<RectTransform>();
                RectTransform qiRect = qiBarObj.GetComponent<RectTransform>();
                qiRect.anchoredPosition = new Vector2(templateRect.anchoredPosition.x,
                    templateRect.anchoredPosition.y - templateRect.sizeDelta.y - 4f);

                // 修改填充条颜色为紫色
                Image barImage = _qiBar.bar.GetComponent<Image>();
                if (barImage != null) barImage.color = QiBarColor;

                // 修改文本颜色
                if (_qiBar.textField != null) _qiBar.textField.color = QiBarColor;

                // 替换图标为自定义气力图标
                try
                {
                    Sprite qiSprite = SpriteTextureLoader.getSprite("ui/Icons/iconQi");
                    if (qiSprite != null)
                    {
                        // StatBar的图标通常是名为"icon"或"Icon"的子对象
                        Transform iconTransform = qiBarObj.transform.Find("icon") ?? qiBarObj.transform.Find("Icon");
                        if (iconTransform != null)
                        {
                            Image iconImage = iconTransform.GetComponent<Image>();
                            if (iconImage != null) iconImage.sprite = qiSprite;
                        }
                        else
                        {
                            // 尝试找所有Image子对象，第一个非bar的就是图标
                            foreach (Image img in qiBarObj.GetComponentsInChildren<Image>())
                            {
                                if (img != barImage && img.gameObject != qiBarObj)
                                {
                                    img.sprite = qiSprite;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // 图标替换失败不影响功能
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
