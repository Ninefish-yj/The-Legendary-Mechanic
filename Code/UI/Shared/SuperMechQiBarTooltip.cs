using UnityEngine;
using UnityEngine.EventSystems;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.242.19 改用原版Tooltip系统：注册TooltipAsset到AssetManager.tooltips，
    ///   悬停时调用Tooltip.show(actor, "sm_qi_bar", data)，由原版Tooltip框架
    ///   自动处理位置、样式、层级和动画。
    /// v0.242.9 重构：不存Actor引用，直接存气力值（qi/qiMax/levelName），
    ///   由UpdateQiBarValue在更新条时写入，悬停时直接显示。
    /// </summary>
    public class SuperMechQiBarTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const string TooltipId = "sm_qi_bar";
        private static bool _registered;

        // v0.242.9 直接存储显示数据，由UpdateQiBarValue写入
        public float qi;
        public float qiMax;
        public string levelName = "";
        public bool awakened = false;
        // v0.242.19 临时存储Actor引用，仅用于Tooltip.show传参（悬停时有效）
        public Actor actor;

        /// <summary>v0.242.19 注册气力条TooltipAsset到原版系统（只需调用一次）</summary>
        public static void RegisterTooltipAsset()
        {
            if (_registered) return;
            _registered = true;

            var asset = new TooltipAsset
            {
                id = TooltipId,
                prefab_id = "tooltips/tooltip_normal",
                callback = ShowQiBarTooltip
            };
            AssetManager.tooltips.add(asset);
            Debug.Log("[超神机械师] 气力条Tooltip已注册到原版系统: " + TooltipId);
        }

        /// <summary>v0.242.19 原版Tooltip回调：填充气力数据到tooltip的两列布局</summary>
        private static void ShowQiBarTooltip(Tooltip pTooltip, string pType, TooltipData pData)
        {
            if (pTooltip == null || pData == null || pData.actor == null) return;

            Actor a = pData.actor;
            float curQi = SuperMechQi.GetQi(a);
            float maxQi = SuperMechQi.GetQiMax(a);
            bool isAwakened = SuperMechAwakened.IsAwakened(a);

            pTooltip.setTitle(LocalizedTextManager.getText("sm_ui_qi_tooltip_title"));

            if (!isAwakened || maxQi <= 0)
            {
                pTooltip.stats_description.text = LocalizedTextManager.getText("sm_ui_qi_tooltip_not_awakened");
                pTooltip.stats_values.text = "";
            }
            else
            {
                int lv = SuperMechQi.GetLevel(maxQi);
                string lvlName = lv > 0 ? SuperMechQi.LevelNames[lv - 1] : LocalizedTextManager.getText("sm_ui_qi_none");
                pTooltip.stats_description.text =
                    LocalizedTextManager.getText("sm_ui_qi_tooltip_current") + "\n" +
                    LocalizedTextManager.getText("sm_ui_qi_tooltip_level");
                pTooltip.stats_values.text =
                    ((int)curQi) + " / " + ((int)maxQi) + "\n" +
                    lvlName;
            }
            pTooltip.stats_container.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (actor == null) return;
            RegisterTooltipAsset();
            var data = new TooltipData { actor = actor };
            Tooltip.show(actor, TooltipId, data);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.hideTooltip();
        }

        private void OnDisable()
        {
            Tooltip.hideTooltip();
        }
    }
}
