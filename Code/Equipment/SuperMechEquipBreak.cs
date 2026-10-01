using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechEquipBreak
    {
        public static readonly Dictionary<long, float> _durability = new Dictionary<long, float>();

        public static readonly Dictionary<long, float> _breakCooldown = new Dictionary<long, float>();

        public static void OnTakeDamage(Actor a, float damage)
        {
            if (a == null || !a.isAlive()) return;
            if (!SuperMechAdvancement.IsSuperMechUnit(a)) return;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx < 0) return;

            if (_breakCooldown.TryGetValue(a.id, out var cd) && Time.time < cd) return;

            if (!_durability.TryGetValue(a.id, out var dur))
            {
                dur = 100f;
                _durability[a.id] = dur;
            }

            float maxHp = a.getMaxHealth();
            if (maxHp <= 0) return;
            float damageRatio = damage / maxHp;

            if (damageRatio > 0.1f)
            {
                float qualityProtection = 1f - currentIdx * 0.08f;
                float durabilityLoss = damageRatio * 15f * qualityProtection;
                dur -= durabilityLoss;
                _durability[a.id] = dur;

                if (dur <= 0f)
                {
                    BreakEquip(a, currentIdx);
                }
            }
        }

        public static void BreakEquip(Actor a, int qualityIdx)
        {
            if (a == null || a.equipment == null) return;

            var slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null || slot.isEmpty()) return;

            string equipId = SuperMechRelic.Equipments[qualityIdx].id;
            SuperMechEquipBag.AddToBag(a, equipId);

            slot.takeAwayItem();

            SuperMechEquipAffix.OnUnequip(a);

            _durability[a.id] = 100f;

            _breakCooldown[a.id] = Time.time + 30f;

            Debug.Log($"[超神机械师] {a.name} 的{SuperMechRelic.Equipments[qualityIdx].name}装备被打掉了！");
        }

        public static void OnEquip(Actor a)
        {
            if (a == null) return;
            _durability[a.id] = 100f;
        }

        public static float GetDurability(Actor a)
        {
            if (a != null && _durability.TryGetValue(a.id, out var dur)) return dur;
            return 100f;
        }

        public static bool IsOnCooldown(Actor a)
        {
            if (a != null && _breakCooldown.TryGetValue(a.id, out var cd))
                return Time.time < cd;
            return false;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = SuperMechCleanup.CleanDict(_durability, alive);
            removed += SuperMechCleanup.CleanDict(_breakCooldown, alive);
            return removed;
        }

        public static void Clear() { _durability.Clear(); _breakCooldown.Clear(); }
    }
}
