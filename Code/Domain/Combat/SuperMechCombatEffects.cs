using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.197 战斗特效系统
    /// 按职业/阶位在攻击和击杀时触发符合原著的视觉特效
    /// 机械师=蓝金科技感、武道=红色血气、异能=紫色念动力、法师=绿蓝奥术、念力=青色精神力</summary>
    public static class SuperMechCombatEffects
    {
        // 各职业对应的施法特效ID（按ProfessionType顺序：None/Mechanical/Martial/Psi/Mage/Mind）
        private static readonly string[] _castGroundByClass =
        {
            "fx_cast_ground_blue",   // None
            "fx_cast_ground_blue",   // Mechanical 机械=蓝金科技
            "fx_cast_ground_red",    // Martial 武道=红色血气
            "fx_cast_ground_purple", // Psi 异能=紫色念动力
            "fx_cast_ground_green",  // Mage 法师=绿色奥术
            "fx_cast_ground_blue",   // Mind 念力=青色精神
        };

        private static readonly string[] _castTopByClass =
        {
            "fx_cast_top_blue",
            "fx_cast_top_blue",
            "fx_cast_top_red",
            "fx_cast_top_purple",
            "fx_cast_top_green",
            "fx_cast_top_blue",
        };

        /// <summary>攻击命中时触发特效</summary>
        public static void OnAttackHit(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(attacker)) return;

            int classId = (int)SuperMechProfession.GetProfession(attacker);
            int rankIdx = SuperMechAdvancement.GetExactRankIndex(attacker);

            // B阶及以上才显示施法特效
            if (rankIdx < 3) return;

            // 节流：避免频繁生成特效
            float throttle = 0.3f + (rankIdx * 0.02f);
            if (Time.unscaledTime < _lastEffectTime + throttle) return;
            _lastEffectTime = Time.unscaledTime;

            try
            {
                var tile = target.currentTile;
                if (tile == null) return;

                string groundFx = classId >= 0 && classId < _castGroundByClass.Length
                    ? _castGroundByClass[classId] : "fx_cast_ground_blue";
                string topFx = classId >= 0 && classId < _castTopByClass.Length
                    ? _castTopByClass[classId] : "fx_cast_top_blue";

                // 地面施法特效
                EffectsLibrary.spawn(groundFx, tile, null, null, 0f, -1f, -1f, attacker);

                // A阶及以上额外显示顶部特效
                if (rankIdx >= 8)
                {
                    EffectsLibrary.spawn(topFx, tile, null, null, 0f, -1f, -1f, attacker);
                }

                // S阶及以上有概率触发爆炸
                if (rankIdx >= 10 && Random.value < 0.3f)
                {
                    EffectsLibrary.spawn("fx_explosion_huge", tile, null, null, 0f, -1f, -1f, attacker);
                }
            }
            catch (System.Exception e)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[超神机械师] 战斗特效异常(仅首次): " + e.Message); }
            }
        }

        /// <summary>击杀时触发特效</summary>
        public static void OnKill(Actor killer, Actor target)
        {
            if (killer == null || target == null) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(killer)) return;

            int killerRank = SuperMechAdvancement.GetExactRankIndex(killer);
            int targetRank = SuperMechAdvancement.GetExactRankIndex(target);

            try
            {
                var tile = target.currentTile;
                if (tile == null) return;

                // 击杀S阶及以上目标触发大爆炸
                if (targetRank >= 10)
                {
                    EffectsLibrary.spawn("fx_explosion_huge", tile, null, null, 0f, -1f, -1f, killer);
                    EffectsLibrary.spawn("fx_experience_gain", tile, null, null, 0f, -1f, -1f, killer);
                }
                // 击杀A阶目标触发中等爆炸
                else if (targetRank >= 8)
                {
                    EffectsLibrary.spawn("fx_explosion_meteorite", tile, null, null, 0f, -1f, -1f, killer);
                }
                // 击杀者是超A时触发经验特效
                if (killerRank >= 10)
                {
                    EffectsLibrary.spawn("fx_experience_gain", tile, null, null, 0f, -1f, -1f, killer);
                }
            }
            catch (System.Exception e)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[超神机械师] 击杀特效异常(仅首次): " + e.Message); }
            }
        }

        private static float _lastEffectTime = 0f;
        private static bool _warned = false;
    }
}
