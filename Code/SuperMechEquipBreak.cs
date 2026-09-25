using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备损坏系统：战斗中受到高额伤害时装备可能被打掉。
    /// 原著ch229："如果玩家使用武器损坏了"——装备会在战斗中损坏。
    /// 高阶战斗中装备更容易被破坏，特别是超A级战斗。
    /// </summary>
    public static class SuperMechEquipBreak
    {
        /// <summary>装备耐久度（actorId → 当前耐久0-100）。</summary>
        private static readonly Dictionary<long, float> _durability = new Dictionary<long, float>();

        /// <summary>被打掉的冷却（actorId → 下次可装备时间）。</summary>
        private static readonly Dictionary<long, float> _breakCooldown = new Dictionary<long, float>();

        /// <summary>受到伤害时检查装备是否损坏。</summary>
        public static void OnTakeDamage(Actor a, float damage)
        {
            if (a == null || !a.isAlive()) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(a)) return;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx < 0) return; // 没装备

            // 检查冷却
            if (_breakCooldown.TryGetValue(a.id, out var cd) && Time.time < cd) return;

            // 耐久度初始化
            if (!_durability.TryGetValue(a.id, out var dur))
            {
                dur = 100f;
                _durability[a.id] = dur;
            }

            // 伤害超过最大生命10%时，装备耐久下降
            float maxHp = a.getMaxHealth();
            if (maxHp <= 0) return;
            float damageRatio = damage / maxHp;

            if (damageRatio > 0.1f)
            {
                // 高品质装备更耐久
                float qualityProtection = 1f - currentIdx * 0.08f;
                float durabilityLoss = damageRatio * 15f * qualityProtection;
                dur -= durabilityLoss;
                _durability[a.id] = dur;

                // 耐久归零，装备被打掉
                if (dur <= 0f)
                {
                    BreakEquip(a, currentIdx);
                }
            }
        }

        /// <summary>装备被打掉：卸下装备，放回背包，设置冷却。</summary>
        public static void BreakEquip(Actor a, int qualityIdx)
        {
            if (a == null || a.equipment == null) return;

            var slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null || slot.isEmpty()) return;

            // 旧装备放回背包
            string equipId = SuperMechRelic.Equipments[qualityIdx].id;
            SuperMechEquipBag.AddToBag(a, equipId);

            // 移除装备
            slot.takeAwayItem();

            // 清除词条
            SuperMechEquipAffix.OnUnequip(a);

            // 重置耐久
            _durability[a.id] = 100f;

            // 设置冷却（30秒内不能装备同品质以上装备）
            _breakCooldown[a.id] = Time.time + 30f;

            Debug.Log($"[超神机械师] {a.name} 的{SuperMechRelic.Equipments[qualityIdx].name}装备被打掉了！");
        }

        /// <summary>装备新装备时重置耐久。</summary>
        public static void OnEquip(Actor a)
        {
            if (a == null) return;
            _durability[a.id] = 100f;
        }

        /// <summary>获取装备耐久度（用于面板显示）。</summary>
        public static float GetDurability(Actor a)
        {
            if (a != null && _durability.TryGetValue(a.id, out var dur)) return dur;
            return 100f;
        }

        /// <summary>是否在损坏冷却中。</summary>
        public static bool IsOnCooldown(Actor a)
        {
            if (a != null && _breakCooldown.TryGetValue(a.id, out var cd))
                return Time.time < cd;
            return false;
        }

        /// <summary>清理死亡单位。</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_durability, alive);
            removed += SuperMechCleanup.CleanDict(_breakCooldown, alive);
            return removed;
        }

        /// <summary>清空。</summary>
        public static void Clear() { _durability.Clear(); _breakCooldown.Clear(); }
    }
}
