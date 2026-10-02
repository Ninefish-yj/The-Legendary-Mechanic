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
    /// </summary>
    public static class SuperMechConceptImmortal
    {
        /// <summary>信息态抹杀标记：被标记的单位死亡时不会触发概念重塑（真正死亡）</summary>
        private static readonly HashSet<long> _informationErased = new HashSet<long>();

        /// <summary>每个单位的重塑次数（影响遗忘程度）</summary>
        private static readonly Dictionary<long, int> _reshapeCount = new Dictionary<long, int>();

        /// <summary>基础重塑成功率</summary>
        private const float BaseReshapeChance = 0.85f;

        /// <summary>每次重塑后成功率衰减</summary>
        private const float ReshapeDecay = 0.12f;

        /// <summary>成功率下限（只要还有一个人记住就有机会）</summary>
        private const float MinReshapeChance = 0.1f;

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

        /// <summary>计算单位的"被记住程度"（年龄+阶位+职业等级）</summary>
        private static float GetRemembrance(Actor a)
        {
            if (a == null) return 0f;
            float remembrance = 0f;
            // 年龄：活得越久，事迹越多，被记住的人越多
            if (a.data != null)
            {
                remembrance += Mathf.Min(a.data.getAge() / 200f, 0.2f);
            }
            // 阶位：阶位越高，知名度越高
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            remembrance += Mathf.Min((rank - 10) * 0.03f, 0.15f);
            return Mathf.Min(remembrance, 0.3f); // 上限30%加成
        }

        /// <summary>检查超神级是否可以概念重塑（原著：未被遗忘且未被信息态抹杀）</summary>
        public static bool CanReshape(Actor a)
        {
            if (a == null) return false;
            // 必须是X阶（超神级）
            if (SuperMechAdvancement.GetExactRankIndex(a) < 13) return false;
            // 必须有资讯唯一·概念永生特质
            if (!a.hasTrait(SuperMechRankSpecialty.ConceptImmortal)) return false;
            // 被信息态抹杀则无法重塑（真正死亡）
            if (IsInformationErased(a)) return false;
            // 计算重塑成功率（基础 - 衰减 + 被记住程度）
            int count = 0;
            _reshapeCount.TryGetValue(a.data.id, out count);
            float chance = BaseReshapeChance - count * ReshapeDecay + GetRemembrance(a);
            chance = Mathf.Max(chance, MinReshapeChance);
            return Random.value < chance;
        }

        /// <summary>执行概念重塑：恢复生命，保留全部能力（原著：不损害信息完整性）</summary>
        public static void Reshape(Actor a)
        {
            if (a == null) return;
            // 恢复满血满耐力（信息态重塑，不损害完整性）
            a.data.health = a.getMaxHealth();
            a.data.stamina = a.getMaxStamina();
            // 记录重塑次数
            int count = 0;
            _reshapeCount.TryGetValue(a.data.id, out count);
            count++;
            _reshapeCount[a.data.id] = count;
            // 移除抹杀标记（如果有的话，不应该有）
            _informationErased.Remove(a.data.id);
            Debug.Log($"[超神机械师] {a.name} 触发资讯唯一·概念永生，信息态扰动重塑完成（第{count}次）");
            // 推送事件日志
            SMEventLogger.LogConceptReshape(a);
        }

        /// <summary>清理死亡单位的数据</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var deadIds = new List<long>();
            foreach (var id in _reshapeCount.Keys)
            {
                if (!alive.Contains(id)) deadIds.Add(id);
            }
            foreach (var id in deadIds)
            {
                _reshapeCount.Remove(id);
                removed++;
            }
            _informationErased.RemoveWhere(id => !alive.Contains(id));
            return removed;
        }

        /// <summary>清空数据</summary>
        public static void Clear()
        {
            _informationErased.Clear();
            _reshapeCount.Clear();
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
