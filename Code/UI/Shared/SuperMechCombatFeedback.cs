using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>战斗即时视觉反馈（v0.201.8 移除原版EffectsLibrary.spawnAtTileRandomScale调用，避免保龄球图标等原版特效）
    /// 在能级压制/精神穿甲/属性克制等触发时的视觉反馈，后续可用SuperMechCombatVisuals扩展自定义特效
    /// </summary>
    public static class SuperMechCombatFeedback
    {
        /// <summary>能级压制反馈：高能级攻击低能级时触发（暂不使用原版特效）</summary>
        public static void OnSuppression(Actor target, float ratio)
        {
            if (target == null) return;
            // 后续可用SuperMechCombatVisuals实现自定义压制特效
        }

        /// <summary>精神穿甲反馈：念力系/异能系攻击机械系时触发</summary>
        public static void OnSpiritPierce(Actor target)
        {
            if (target == null) return;
        }

        /// <summary>属性克制反馈：气力属性克制环触发时</summary>
        public static void OnAttributeCounter(Actor target, bool isCounter)
        {
            if (target == null) return;
        }

        /// <summary>闪避反馈：高能级单位闪避低能级攻击时</summary>
        public static void OnDodge(Actor target)
        {
            if (target == null) return;
        }

        /// <summary>信息态护盾反馈：信息态单位格挡物理攻击时</summary>
        public static void OnInfoShield(Actor target)
        {
            if (target == null) return;
        }

        /// <summary>信息态抹杀反馈：超神级之间战斗触发真正死亡标记时</summary>
        public static void OnInfoErase(Actor target)
        {
            if (target == null) return;
        }
    }
}
