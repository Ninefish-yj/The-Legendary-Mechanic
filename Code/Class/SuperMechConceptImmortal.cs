using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 超神级概念不朽机制（原著第1417、1450章）
    /// 超神级是概念级存在，普通死亡不会摧毁信息态，可通过概念重塑复活
    /// 只有被其他超神级用"信息态抹杀"才能真正杀死
    /// </summary>
    public static class SuperMechConceptImmortal
    {
        /// <summary>信息态抹杀标记：被标记的单位死亡时不会触发概念重塑（真正死亡）</summary>
        private static readonly HashSet<long> _informationErased = new HashSet<long>();

        /// <summary>概念重塑冷却时间（秒），避免连续死亡无限重塑</summary>
        private const float ReshapingCooldown = 300f;

        /// <summary>上次重塑时间记录</summary>
        private static readonly Dictionary<long, float> _lastReshaping = new Dictionary<long, float>();

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
            Debug.Log($"[超神机械师] {a.name} 被信息态抹杀，无法概念重塑");
        }

        /// <summary>检查超神级是否可以概念重塑</summary>
        public static bool CanReshape(Actor a)
        {
            if (a == null) return false;
            // 必须是X阶（超神级）
            if (SuperMechAdvancement.GetExactRankIndex(a) < 13) return false;
            // 必须有概念不朽特质
            if (!a.hasTrait(SuperMechRankSpecialty.ConceptImmortal)) return false;
            // 被信息态抹杀则无法重塑
            if (IsInformationErased(a)) return false;
            // 冷却时间检查
            if (_lastReshaping.TryGetValue(a.data.id, out float lastTime))
            {
                if (Time.time - lastTime < ReshapingCooldown) return false;
            }
            return true;
        }

        /// <summary>执行概念重塑：恢复生命，保留全部能力（原著：超神级普通死亡后信息态重塑）</summary>
        public static void Reshape(Actor a)
        {
            if (a == null) return;
            // 恢复满血
            a.data.health = a.getMaxHealth();
            a.data.stamina = a.getMaxStamina();
            // 记录重塑时间
            _lastReshaping[a.data.id] = Time.time;
            // 移除抹杀标记（如果有的话，不应该有）
            _informationErased.Remove(a.data.id);
            Debug.Log($"[超神机械师] {a.name} 触发概念不朽，信息态重塑完成");
            // 推送事件日志
            SMEventLogger.LogConceptReshape(a);
        }

        /// <summary>清理死亡单位的数据</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var deadIds = new List<long>();
            foreach (var id in _lastReshaping.Keys)
            {
                if (!alive.Contains(id)) deadIds.Add(id);
            }
            foreach (var id in deadIds)
            {
                _lastReshaping.Remove(id);
                removed++;
            }
            _informationErased.RemoveWhere(id => !alive.Contains(id));
            return removed;
        }

        /// <summary>清空数据</summary>
        public static void Clear()
        {
            _informationErased.Clear();
            _lastReshaping.Clear();
        }
    }

    /// <summary>
    /// 死亡补丁：拦截超神级普通死亡，触发概念重塑
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
                // 超神级概念不朽：普通死亡触发重塑
                if (SuperMechConceptImmortal.CanReshape(__instance))
                {
                    SuperMechConceptImmortal.Reshape(__instance);
                    return false; // 阻止真正死亡
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 概念不朽补丁异常: {e.Message}");
            }

            return true; // 允许正常死亡
        }
    }
}
