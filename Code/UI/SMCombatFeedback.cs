using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>战斗即时视觉反馈（v0.29.0 UI重构第三阶段）
    /// 在能级压制/精神穿甲/属性克制等触发时，调用WorldBox原生特效
    /// 参考现代战争M5模组的EffectsLibrary.spawnAtTileRandomScale用法
    /// </summary>
    public static class SMCombatFeedback
    {
        /// <summary>在目标单位所在tile生成特效</summary>
        private static void SpawnAtActor(Actor a, string effectId, float minScale = 0.8f, float maxScale = 1.2f)
        {
            if (a == null || a.current_tile == null) return;
            try
            {
                EffectsLibrary.spawnAtTileRandomScale(effectId, a.current_tile, minScale, maxScale);
            }
            catch (System.Exception e)
            {
                // 特效调用失败不影响战斗逻辑，仅记录首次
                if (!_effectErrorLogged)
                {
                    Debug.LogWarning($"[超神机械师] 战斗特效调用失败: {e.Message}");
                    _effectErrorLogged = true;
                }
            }
        }

        private static bool _effectErrorLogged = false;

        /// <summary>能级压制反馈：高能级攻击低能级时触发</summary>
        public static void OnSuppression(Actor target, float ratio)
        {
            if (target == null) return;
            // 压制等级越高，特效越强烈
            if (ratio >= 10f)
                SpawnAtActor(target, "fx_explosion_middle", 1.5f, 2.0f);
            else if (ratio >= 5f)
                SpawnAtActor(target, "fx_explosion_small", 1.2f, 1.6f);
            else if (ratio >= 2f)
                SpawnAtActor(target, "fx_hit", 1.0f, 1.4f);
            else
                SpawnAtActor(target, "fx_spark", 0.8f, 1.2f);
        }

        /// <summary>精神穿甲反馈：念力系/异能系攻击机械系时触发</summary>
        public static void OnSpiritPierce(Actor target)
        {
            SpawnAtActor(target, "fx_cast_top_purple", 0.8f, 1.2f);
        }

        /// <summary>属性克制反馈：气力属性克制环触发时</summary>
        public static void OnAttributeCounter(Actor target, bool isCounter)
        {
            if (isCounter)
                SpawnAtActor(target, "fx_slash", 1.0f, 1.4f);
            else
                SpawnAtActor(target, "fx_block", 0.8f, 1.2f);
        }

        /// <summary>闪避反馈：高能级单位闪避低能级攻击时</summary>
        public static void OnDodge(Actor target)
        {
            SpawnAtActor(target, "fx_dodge", 0.8f, 1.2f);
        }

        /// <summary>信息态护盾反馈：信息态单位格挡物理攻击时</summary>
        public static void OnInfoShield(Actor target)
        {
            SpawnAtActor(target, "fx_shield_hit", 1.0f, 1.4f);
        }

        /// <summary>信息态抹杀反馈：超神级之间战斗触发真正死亡标记时</summary>
        public static void OnInfoErase(Actor target)
        {
            SpawnAtActor(target, "fx_curse", 1.5f, 2.0f);
        }
    }
}
