using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code.Core
{
    /// <summary>
    /// 拦截PowerButton.clickOpenWindow，对模组的窗口按钮打开UGUI窗口而不是ScrollWindow
    /// </summary>
    [HarmonyPatch(typeof(PowerButton), "clickOpenWindow")]
    public static class SMPowerButtonWindowPatch
    {
        static bool Prefix(PowerButton __instance)
        {
            if (__instance == null) return true;
            string id = __instance.gameObject.name;

            if (id == SuperMechPowers.OpenSanctuary)
            {
                SMWindowManager.OpenSanctuary();
                return false; // 阻止原方法打开ScrollWindow
            }
            if (id == SuperMechPowers.OpenRank)
            {
                SMWindowManager.OpenRank();
                return false;
            }
            return true; // 其他按钮走原版逻辑
        }
    }
}
