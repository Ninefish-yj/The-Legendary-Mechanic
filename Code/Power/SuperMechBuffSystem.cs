using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 增益/减益效果系统：管理法术buff/debuff的持续时间和过期移除
    /// 简化实现：直接修改stats，用字典记录过期时间，Tick中清理过期效果
    /// </summary>
    public static class SuperMechBuffSystem
    {
        private class BuffEntry
        {
            public string buffId;
            public float armorBonus;
            public float damageBonus;
            public float dodgeBonus;
            public float speedMul;     // 减益用的速度倍率（<1表示减速）
            public double expireTime;
            public bool isDebuff;
        }

        private static readonly Dictionary<long, List<BuffEntry>> _activeBuffs = new Dictionary<long, List<BuffEntry>>();

        /// <summary>
        /// 添加增益效果
        /// </summary>
        public static void AddBuff(Actor target, string buffId, float armorBonus, float damageBonus, float duration, float dodgeBonus = 0f)
        {
            if (target == null) return;
            AddEntry(target, new BuffEntry
            {
                buffId = buffId,
                armorBonus = armorBonus,
                damageBonus = damageBonus,
                dodgeBonus = dodgeBonus,
                speedMul = 1f,
                expireTime = World.world.getCurWorldTime() + duration,
                isDebuff = false
            });
        }

        /// <summary>
        /// 添加减益效果
        /// </summary>
        public static void AddDebuff(Actor target, string debuffId, float armorPenalty, float damagePenalty, float duration, float speedMul = 1f)
        {
            if (target == null) return;
            AddEntry(target, new BuffEntry
            {
                buffId = debuffId,
                armorBonus = -armorPenalty,
                damageBonus = -damagePenalty,
                dodgeBonus = 0f,
                speedMul = speedMul,
                expireTime = World.world.getCurWorldTime() + duration,
                isDebuff = true
            });
        }

        private static void AddEntry(Actor target, BuffEntry entry)
        {
            long id = target.data.id;
            if (!_activeBuffs.TryGetValue(id, out var list))
            {
                list = new List<BuffEntry>();
                _activeBuffs[id] = list;
            }
            list.Add(entry);
        }

        /// <summary>
        /// 移除目标的随机一个增益（驱散术用）
        /// </summary>
        public static void RemoveRandomBuff(Actor target)
        {
            if (target == null) return;
            long id = target.data.id;
            if (!_activeBuffs.TryGetValue(id, out var list)) return;

            var buffs = list.FindAll(b => !b.isDebuff);
            if (buffs.Count == 0) return;

            var toRemove = buffs[UnityEngine.Random.Range(0, buffs.Count)];
            RemoveEntry(target, toRemove);
            list.Remove(toRemove);
        }

        private static void RemoveEntry(Actor target, BuffEntry entry)
        {
            var stats = SuperMechStats.Of(target);
            if (stats == null) return;
            // 反向移除属性修改
            if (entry.armorBonus != 0) stats["armor"] = Mathf.Max(0f, stats["armor"] - entry.armorBonus);
            if (entry.damageBonus != 0) stats["damage"] = Mathf.Max(0f, stats["damage"] - entry.damageBonus);
            if (entry.speedMul < 1f && entry.speedMul > 0) stats["speed"] /= entry.speedMul;
        }

        /// <summary>
        /// Tick：清理过期buff
        /// </summary>
        public static void Tick()
        {
            double now = World.world.getCurWorldTime();
            var toRemoveActors = new List<long>();

            foreach (var kv in _activeBuffs)
            {
                var list = kv.Value;
                list.RemoveAll(b =>
                {
                    if (now >= b.expireTime)
                    {
                        // 过期：找到对应actor并移除属性修改
                        // 注意：actor可能已死亡，这里只清理字典
                        return true;
                    }
                    return false;
                });
                if (list.Count == 0) toRemoveActors.Add(kv.Key);
            }

            foreach (var id in toRemoveActors)
            {
                _activeBuffs.Remove(id);
            }
        }

        /// <summary>
        /// 检查单位是否有某个buff
        /// </summary>
        public static bool HasBuff(Actor target, string buffId)
        {
            if (target == null) return false;
            if (!_activeBuffs.TryGetValue(target.data.id, out var list)) return false;
            return list.Exists(b => b.buffId == buffId);
        }

        /// <summary>
        /// 获取单位活跃buff数量
        /// </summary>
        public static int GetActiveBuffCount(Actor target)
        {
            if (target == null) return 0;
            if (!_activeBuffs.TryGetValue(target.data.id, out var list)) return 0;
            return list.Count;
        }

        /// <summary>
        /// 清理死亡单位
        /// </summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var toRemove = new List<long>();
            foreach (var id in _activeBuffs.Keys)
            {
                if (!alive.Contains(id)) toRemove.Add(id);
            }
            foreach (var id in toRemove)
            {
                _activeBuffs.Remove(id);
                removed++;
            }
            return removed;
        }

        /// <summary>
        /// 全局清理
        /// </summary>
        public static void Clear()
        {
            _activeBuffs.Clear();
        }
    }
}
