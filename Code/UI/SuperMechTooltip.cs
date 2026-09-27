using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechTooltip
    {
        public static void Register()
        {
            if (AssetManager.tooltips.get("sm_unit_resource") != null) return;

            TooltipAsset asset = new TooltipAsset
            {
                id = "sm_unit_resource",
                prefab_id = "tooltips/tooltip_normal",
                callback = ShowUnitResource,
                callback_text_animated = ShowUnitResource
            };
            AssetManager.tooltips.add(asset);
        }

        private static void ShowUnitResource(Tooltip pTooltip, string pType, TooltipData pData)
        {
            ResourceAsset res = pData.resource;
            Actor actor = pData.actor;
            if (res == null) return;

            pTooltip.name.text = res.getTranslatedName();
            pTooltip.clearTextRows();

            if (actor != null && actor.inventory != null)
            {
                int amount = actor.inventory.getResource(res.id);
                pTooltip.addLineIntText("amount", amount);
            }

            pTooltip.addLineBreak();

            if (res.restore_health != 0f)
            {
                pTooltip.addLineText("health", res.restore_health.ToText());
            }
            if (res.restore_mana != 0)
            {
                pTooltip.addLineIntText("mana", res.restore_mana);
            }
            if (res.restore_stamina != 0)
            {
                pTooltip.addLineIntText("stamina", res.restore_stamina);
            }
            if (res.restore_nutrition != 0)
            {
                pTooltip.addLineIntText("nutrition", res.restore_nutrition);
            }
            if (res.restore_happiness != 0)
            {
                pTooltip.addLineIntText("happiness", res.restore_happiness);
            }

            if (!string.IsNullOrEmpty(res.tooltip) && res.tooltip != "city_resource" && res.tooltip != "city_resource_food")
            {
                pTooltip.description.text = res.tooltip;
            }

            if (res.type == ResType.Food && actor != null && actor.inventory != null && actor.inventory.getResource(res.id) > 0)
            {
                pTooltip.addLineBreak();
                pTooltip.addLineText(LocalizedTextManager.getText("sm_ui_click_to_eat"), "");
            }
        }
    }
}
