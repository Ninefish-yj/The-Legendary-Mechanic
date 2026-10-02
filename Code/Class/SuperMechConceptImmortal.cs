using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超神级【资讯唯一·概念永生】专长机制（原著第1402-1403章）
    /// 达到X阶后自身信息态存在形式发生巨变，拥有自动复生力量：
    /// 只要全宇宙还有信息载体记录着你的事迹，只要还没被所有人遗忘，
    /// 只要还有人念诵你的名，就不会彻底死亡。
    /// 死了不需要圣所复苏，会自发引起信息态扰动重生，且不损害信息完整性。
    /// 只有被【信息态抹杀】才能真正杀死超神级。
    ///
    /// 游戏平衡：重生后进入"信息态稳定期"，期间再次死亡则信息态未稳定，真正死亡。
    /// </summary>
    public static class SuperMechConceptImmortal
    {
        /// <summary>信息态抹杀标记：被标记的单位死亡时不会触发概念重塑（真正死亡）</summary>
        private static readonly HashSet<long> _informationErased = new HashSet<long>();

        /// <summary>信息态稳定期记录：单位ID -> 重生时间戳，稳定期内再次死亡则真正死亡</summary>
        private static readonly Dictionary<long, float> _stabilizing = new Dictionary<long, float>();

        /// <summary>信息态稳定期时长（秒）：重生后这段时间内再次死亡则真正死亡</summary>
        private const float StabilizationPeriod = 180f;

        /// <summary>检查单位是否被信息态抹杀（真正死亡，无法重塑）</summary>
        public static bool IsInformationErased(Actor a)
        {
            if (a == null) return false;
            return _informationErased.Contains(a.data.id);
        }

        /// <summary>标记单位被信息态抹杀（真正死亡）</summary>
        public static void MarkInformationErased(Actor a)
        {
            if (a == null) return;
            _informationErased.Add(a.data.id);
            Debug.Log($"[超神机械师] {a.name} 被信息态抹杀，无法概念永生");
        }

        /// <summary>检查单位是否处于信息态稳定期（期间再次死亡则真正死亡）</summary>
        public static bool IsStabilizing(Actor a)
        {
            if (a == null) return false;
            if (_stabilizing.TryGetValue(a.data.id, out float reshapeTime))
            {
                if (Time.time - reshapeTime < StabilizationPeriod)
                    return true;
                _stabilizing.Remove(a.data.id);
            }
            return false;
        }

        /// <summary>检查超神级是否可以概念重塑（原著：未被信息态抹杀且不在稳定期内）</summary>
        public static bool CanReshape(Actor a)
        {
            if (a == null) return false;
            // 必须是X阶（超神级）
            if (SuperMechAdvancement.GetExactRankIndex(a) < 13) return false;
            // 必须有资讯唯一·概念永生特质
            if (!a.hasTrait(SuperMechRankSpecialty.ConceptImmortal)) return false;
            // 被信息态抹杀则无法重塑（真正死亡）
            if (IsInformationErased(a)) return false;
            // 处于信息态稳定期内再次死亡则真正死亡（信息态未稳定）
            if (IsStabilizing(a)) return false;
            // 只要未被抹杀且不在稳定期，总是可以重生（原著：还有人记得你）
            return true;
        }

        /// <summary>执行概念重塑：恢复生命，保留全部能力（原著：不损害信息完整性）</summary>
        public static void Reshape(Actor a)
        {
            if (a == null) return;
            // 恢复满血满耐力（信息态重塑，不损害完整性）
            a.data.health = a.getMaxHealth();
            a.data.stamina = a.getMaxStamina();
            // 进入信息态稳定期
            _stabilizing[a.data.id] = Time.time;
            // 移除抹杀标记（如果有的话，不应该有）
            _informationErased.Remove(a.data.id);
            Debug.Log($"[超神机械师] {a.name} 触发资讯唯一·概念永生，信息态扰动重塑完成，进入稳定期");
            // 推送事件日志
            SMEventLogger.LogConceptReshape(a);
        }

        /// <summary>清理死亡单位的数据</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var deadIds = new List<long>();
            foreach (var id in _stabilizing.Keys)
            {
                if (!alive.Contains(id)) deadIds.Add(id);
            }
            foreach (var id in deadIds)
            {
                _stabilizing.Remove(id);
                removed++;
            }
            _informationErased.RemoveWhere(id => !alive.Contains(id));
            return removed;
        }

        /// <summary>清空数据</summary>
        public static void Clear()
        {
            _informationErased.Clear();
            _stabilizing.Clear();
        }
    }

    /// <summary>
    /// 死亡补丁：拦截超神级普通死亡，触发资讯唯一·概念永生
    /// </summary>
    [HarmonyPatch]
    public static class ActorDiePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Actor), "die")]
        public static bool DiePrefix(Actor __instance, bool pDestroy, AttackType pType)
        {
            if (__instance == null || !__instance.isAlive()) return true;

            try
            {
                // 超神级资讯唯一·概念永生：普通死亡触发信息态扰动重塑
                if (SuperMechConceptImmortal.CanReshape(__instance))
                {
                    SuperMechConceptImmortal.Reshape(__instance);
                    return false; // 阻止真正死亡
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 概念永生补丁异常: {e.Message}");
            }

            return true; // 允许正常死亡
        }
    }
}
