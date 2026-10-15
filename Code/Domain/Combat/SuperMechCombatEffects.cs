using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.197 战斗特效系统（v0.201.8 移除原版EffectsLibrary.spawn调用，避免保龄球图标等原版特效，统一用SuperMechCombatVisuals）
    /// 按职业/阶位在攻击和击杀时触发符合原著的视觉特效
    /// 机械师=蓝金科技感、武道=红色血气、异能=紫色念动力、法师=绿蓝奥术、念力=青色精神力</summary>
    public static class SuperMechCombatEffects
    {
        /// <summary>攻击命中时触发特效（视觉特效已由SuperMechCombatVisuals.OnAttack统一处理）</summary>
        public static void OnAttackHit(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(attacker)) return;
        }

        /// <summary>击杀时触发特效（可后续用SuperMechCombatVisuals扩展）</summary>
        public static void OnKill(Actor killer, Actor target)
        {
            if (killer == null || target == null) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(killer)) return;
        }
    }
}
