using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备背包系统：单位可以存多个装备，在单位面板切换装备。
    /// 参考天人武道神藏Tab的实现方式，在单位面板添加自定义Tab。
    /// 装备本身是原版EquipmentAsset，走amulet槽，属性自动merge。
    /// 背包只存装备ID，不存物品实例（需要时generateItem）。
    /// </summary>
    public static class SuperMechEquipBag
    {
        // 单位背包：unit.id -> 装备ID列表（sm_eq_gray等）
        private static readonly Dictionary<long, List<string>> _bag = new Dictionary<long, List<string>>();

        // 背包最大容量
        public const int MaxBagSize = 12;

        /// <summary>获取单位背包中的装备列表。</summary>
        public static List<string> GetBag(Actor a)
        {
            if (a == null) return new List<string>();
            if (_bag.TryGetValue(a.id, out var list)) return list;
            return new List<string>();
        }

        /// <summary>添加装备到背包。返回是否成功（满了返回false）。</summary>
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

        /// <summary>从背包移除装备。</summary>
        public static bool RemoveFromBag(Actor a, string equipId)
        {
            if (a == null || string.IsNullOrEmpty(equipId)) return false;
            if (!_bag.TryGetValue(a.id, out var list)) return false;
            return list.Remove(equipId);
        }

        /// <summary>从背包装备物品：先把当前装备放回背包，再装备选中的。</summary>
        public static bool EquipFromBag(Actor a, string equipId)
        {
            if (a == null || string.IsNullOrEmpty(equipId)) return false;
            if (!RemoveFromBag(a, equipId)) return false;

            // 当前装备放回背包
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx >= 0)
            {
                string currentId = SuperMechRelic.Equipments[currentIdx].id;
                AddToBag(a, currentId);
            }

            // 装备新的
            int newIdx = SuperMechRelic.GetEquipIndex(equipId);
            if (newIdx >= 0)
            {
                SuperMechRelic.EquipItem(a, newIdx);
                return true;
            }
            return false;
        }

        /// <summary>卸下当前装备到背包。</summary>
        public static bool UnequipToBag(Actor a)
        {
            if (a == null) return false;
            int currentIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (currentIdx < 0) return false;

            string currentId = SuperMechRelic.Equipments[currentIdx].id;
            if (!AddToBag(a, currentId)) return false;

            // 移除amulet槽装备
            if (a.equipment != null)
            {
                var slot = a.equipment.getSlot(EquipmentType.Amulet);
                if (slot != null && !slot.isEmpty())
                {
                    slot.takeAwayItem();
                }
            }
            return true;
        }

        /// <summary>清空背包数据（世界切换用）。</summary>
        public static void Clear()
        {
            _bag.Clear();
        }
    }
}
