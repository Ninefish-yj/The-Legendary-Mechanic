using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 械力融合系统：机械系专属，将装备与身体融合。
    /// 原著：械武者分支将机械与身体融合，使徒兵器与使用者融合。
    /// 融合后装备属性加成提升，不会被打掉，但持续消耗气力。
    /// 参考天人武道"道化/合一"的融合玩法，但用原著科幻术语。
    /// </summary>
    public static class SuperMechMechFusion
    {
        /// <summary>融合状态（actorId → 融合等级 0=未融合, 1-5=融合深度）。</summary>
        private static readonly Dictionary<long, int> _fusionLevel = new Dictionary<long, int>();

        /// <summary>融合的装备ID（actorId → equipId）。</summary>
        private static readonly Dictionary<long, string> _fusedEquip = new Dictionary<long, string>();

        /// <summary>融合最低阶段要求（磁环阶段=tier3）。</summary>
        public const int MinStageForFusion = 3;

        /// <summary>融合最低品质要求（蓝色=qualityLevel2）。</summary>
        public const int MinQualityForFusion = 2;

        /// <summary>尝试融合装备与身体。</summary>
        public static bool TryFuse(Actor a)
        {
            if (a == null) return false;
            if (!SuperMechBranch.GetClass(a).Contains("机械")) return false;

            int stage = SuperMechStage.GetStage(a);
            if (stage < MinStageForFusion) return false; // 需磁环阶段以上

            int eqIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (eqIdx < MinQualityForFusion) return false; // 需蓝色以上装备

            if (_fusionLevel.ContainsKey(a.id) && _fusionLevel[a.id] > 0) return false; // 已融合

            // 融合等级由装备品质决定：蓝=1, 淡紫=2, 紫=3, 粉=4, 橙+=5
            int level = Mathf.Clamp(eqIdx - 1, 1, 5);
            _fusionLevel[a.id] = level;
            _fusedEquip[a.id] = SuperMechRelic.Equipments[eqIdx].id;

            Debug.Log($"[超神机械师] {a.name} 完成械力融合！融合等级{level}，装备{SuperMechRelic.Equipments[eqIdx].name}");
            return true;
        }

        /// <summary>解除融合。</summary>
        public static bool Unfuse(Actor a)
        {
            if (a == null) return false;
            if (!_fusionLevel.ContainsKey(a.id) || _fusionLevel[a.id] == 0) return false;

            _fusionLevel.Remove(a.id);
            _fusedEquip.Remove(a.id);
            return true;
        }

        /// <summary>tick消耗气力维持融合。</summary>
        public static void TickFusion()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!_fusionLevel.TryGetValue(a.id, out int level) || level <= 0) continue;

                // 每秒消耗气力维持融合（等级越高消耗越大）
                float qiCost = level * 2f;
                SuperMechQi.SpendQi(a, qiCost);

                // 气力不足自动解除融合
                if (SuperMechQi.GetQi(a) <= 0)
                {
                    Unfuse(a);
                    Debug.Log($"[超神机械师] {a.name} 气力耗尽，械力融合解除！");
                }
            }
        }

        /// <summary>获取融合加成倍率。</summary>
        public static float GetFusionMultiplier(Actor a)
        {
            if (a == null || !_fusionLevel.TryGetValue(a.id, out int level) || level <= 0) return 1f;
            // 融合等级1=1.2x, 2=1.4x, 3=1.6x, 4=1.8x, 5=2.0x
            return 1f + level * 0.2f;
        }

        /// <summary>是否融合中。</summary>
        public static bool IsFused(Actor a)
        {
            return a != null && _fusionLevel.TryGetValue(a.id, out int level) && level > 0;
        }

        /// <summary>获取融合等级。</summary>
        public static int GetFusionLevel(Actor a)
        {
            if (a != null && _fusionLevel.TryGetValue(a.id, out int level)) return level;
            return 0;
        }

        /// <summary>融合中的装备不会被打掉。</summary>
        public static bool IsEquipProtected(Actor a)
        {
            return IsFused(a);
        }

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_fusionLevel, alive);
            removed += SuperMechCleanup.CleanDict(_fusedEquip, alive);
            return removed;
        }

        /// <summary>清空。</summary>
        public static void Clear() { _fusionLevel.Clear(); _fusedEquip.Clear(); }
    }
}
