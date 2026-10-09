using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>装备改装系统（原著：机械师核心能力，可改装装备部件提升属性）
    /// 挂接现有词缀系统：装备有改装等级(0-10)，每级提升词缀数值10%
    /// 改装消耗潜能点，有成功率，机械师有成功率加成
    /// </summary>
    public static class SuperMechEquipModify
    {
        public const int MaxModifyLevel = 10;
        public const float PerLevelBonus = 0.10f;  // 每级词缀数值+10%
        public const int BaseCost = 20;             // 基础改装消耗
        public const float BaseSuccessRate = 0.70f; // 基础成功率
        public const float MechSuccessBonus = 0.20f; // 机械师成功率加成

        private static readonly Dictionary<long, int> _modifyLevels = new Dictionary<long, int>();

        /// <summary>获取单位当前装备的改装等级</summary>
        public static int GetModifyLevel(Actor a)
        {
            if (a == null) return 0;
            if (_modifyLevels.TryGetValue(a.id, out var level)) return level;
            return 0;
        }

        /// <summary>获取改装带来的词缀数值倍率</summary>
        public static float GetAffixMultiplier(Actor a)
        {
            int level = GetModifyLevel(a);
            return 1f + level * PerLevelBonus;
        }

        /// <summary>获取改装消耗（随等级递增）</summary>
        public static int GetModifyCost(Actor a)
        {
            int level = GetModifyLevel(a);
            return BaseCost + level * 10;
        }

        /// <summary>获取改装成功率（随等级递减，机械师加成）</summary>
        public static float GetSuccessRate(Actor a)
        {
            int level = GetModifyLevel(a);
            float rate = BaseSuccessRate - level * 0.05f;
            if (a.hasTrait(SuperMechTraits.ClassMech)) rate += MechSuccessBonus;
            return Mathf.Clamp01(rate);
        }

        /// <summary>尝试改装当前装备</summary>
        public static bool TryModify(Actor a)
        {
            if (a == null || !a.isAlive()) return false;
            int level = GetModifyLevel(a);
            if (level >= MaxModifyLevel) return false;

            // 检查是否有装备
            int equipIdx = SuperMechRelic.GetCurrentEquipIndex(a);
            if (equipIdx < 0) return false;

            int cost = GetModifyCost(a);
            if (SuperMechPotential.GetPotential(a) < cost) return false;

            SuperMechPotential.SpendPotential(a, cost);

            float rate = GetSuccessRate(a);
            if (UnityEngine.Random.value <= rate)
            {
                _modifyLevels[a.id] = level + 1;
                // 重新应用词缀（改装后属性提升）
                SuperMechEquipAffix.ApplyAffixes(a);
                Debug.Log($"[超神机械师]【装备改装】{a.getName()} 改装成功！等级+1 → {level + 1}，词缀+{(int)((level + 1) * PerLevelBonus * 100)}%");
                return true;
            }
            else
            {
                Debug.Log($"[超神机械师]【装备改装】{a.getName()} 改装失败，等级保持{level}");
                return false;
            }
        }

        /// <summary>卸下装备时清除改装等级</summary>
        public static void OnUnequip(Actor a)
        {
            if (a == null) return;
            _modifyLevels.Remove(a.id);
        }

        /// <summary>装备时初始化改装等级为0</summary>
        public static void OnEquip(Actor a)
        {
            if (a == null) return;
            _modifyLevels[a.id] = 0;
        }

        public static void Clear()
        {
            _modifyLevels.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_modifyLevels, alive);
        }

        // === 存档 ===
        [System.Serializable]
        public class ModifySaveData
        {
            public long id;
            public int level;
        }

        public static List<ModifySaveData> Save()
        {
            var list = new List<ModifySaveData>();
            foreach (var kv in _modifyLevels)
            {
                list.Add(new ModifySaveData { id = kv.Key, level = kv.Value });
            }
            return list;
        }

        public static void Load(List<ModifySaveData> data)
        {
            _modifyLevels.Clear();
            if (data == null) return;
            foreach (var d in data)
            {
                _modifyLevels[d.id] = d.level;
            }
        }
    }
}
