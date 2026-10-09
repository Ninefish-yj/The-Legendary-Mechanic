using NeoModLoader.api;
using System.Collections.Generic;

namespace SuperMech.Code
{
    public static class SuperMechEquipBag
    {
        private static readonly Dictionary<long, List<string>> _bag = new Dictionary<long, List<string>>();

        public const int MaxBagSize = 12;

        public static List<string> GetBag(Actor a)
        {
            if (a == null) return new List<string>();
            if (_bag.TryGetValue(a.id, out var list)) return list;
            return new List<string>();
        }

        public static bool AddToBag(Actor a, string equipId)
        {
            if (a == null || string.IsNullOrEmpty(equipId)) return false;
            if (!_bag.TryGetValue(a.id, out var list))
            {
                list = new List<string>();
                _bag[a.id] = list;
            }
            if (list.Count >= MaxBagSize) return false;
            list.Add(equipId);
            return true;
        }

        public static bool RemoveFromBag(Actor a, string equipId)
        {
            if (a == null || string.IsNullOrEmpty(equipId)) return false;
            if (!_bag.TryGetValue(a.id, out var list)) return false;
            return list.Remove(equipId);
        }

        public static bool EquipFromBag(Actor a, string equipId)
        {
            if (a == null || string.IsNullOrEmpty(equipId)) return false;
            if (!RemoveFromBag(a, equipId)) return false;

            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx >= 0)
            {
                string currentId = SuperMechRelic.Equipments[currentIdx].id;
                AddToBag(a, currentId);
            }

            int newIdx = SuperMechRelic.GetEquipIndex(equipId);
            if (newIdx >= 0)
            {
                SuperMechRelic.EquipItem(a, newIdx);
                SuperMechEquipModify.OnEquip(a);
                return true;
            }
            return false;
        }

        public static bool UnequipToBag(Actor a)
        {
            if (a == null) return false;
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx < 0) return false;

            string currentId = SuperMechRelic.Equipments[currentIdx].id;
            if (!AddToBag(a, currentId)) return false;

            if (a.equipment != null)
            {
                var slot = a.equipment.getSlot(EquipmentType.Amulet);
                if (slot != null && !slot.isEmpty())
                {
                    slot.takeAwayItem();
                }
            }
            SuperMechEquipAffix.OnUnequip(a);
            SuperMechEquipModify.OnUnequip(a);
            return true;
        }

        public static void Clear()
        {
            _bag.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_bag, alive);
            return removed;
        }
    }
}
