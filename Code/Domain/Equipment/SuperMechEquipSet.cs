using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.198 装备套装效果系统
    /// 原著风格：机械师'使徒套装'、武道'霸体套装'、法师'奥术套装'
    /// 2件激活基础效果，4件激活完整效果</summary>
    public static class SuperMechEquipSet
    {
        public class SetDef
        {
            public string id;
            public string nameKey;       // 本地化key
            public string[] partIds;     // 套装部件装备ID
            public int twoPieceDmgMul;   // 2件伤害加成(%)
            public int twoPieceHpMul;    // 2件生命加成(%)
            public int fourPieceDmgMul;  // 4件额外伤害加成(%)
            public int fourPieceHpMul;   // 4件额外生命加成(%)
            public string specialEffect; // 4件特殊效果ID
        }

        public static readonly List<SetDef> Sets = new List<SetDef>
        {
            // 机械师·使徒套装（原著：韩萧的机械军团装备）
            new SetDef
            {
                id = "set_apostle",
                nameKey = "sm_set_apostle",
                partIds = new[] { "sm_eq_blue_helmet", "sm_eq_lp_armor", "sm_eq_purple_weapon", "sm_eq_gold_amulet" },
                twoPieceDmgMul = 15, twoPieceHpMul = 10,
                fourPieceDmgMul = 30, fourPieceHpMul = 20,
                specialEffect = "mech_swarm"
            },
            // 武道·霸体套装（原著：武道系炼体强者装备）
            new SetDef
            {
                id = "set_overlord",
                nameKey = "sm_set_overlord",
                partIds = new[] { "sm_eq_green_boots", "sm_eq_lp_armor", "sm_eq_orange_weapon", "sm_eq_so_armor" },
                twoPieceDmgMul = 20, twoPieceHpMul = 15,
                fourPieceDmgMul = 35, fourPieceHpMul = 30,
                specialEffect = "body_refine"
            },
            // 法师·奥术套装（原著：法师系施法者装备）
            new SetDef
            {
                id = "set_arcane",
                nameKey = "sm_set_arcane",
                partIds = new[] { "sm_eq_gray_ring", "sm_eq_pink_amulet", "sm_eq_purple_weapon", "sm_eq_gold_amulet" },
                twoPieceDmgMul = 25, twoPieceHpMul = 5,
                fourPieceDmgMul = 45, fourPieceHpMul = 10,
                specialEffect = "spell_amp"
            },
        };

        private static readonly Dictionary<string, SetDef> _setById = new Dictionary<string, SetDef>();
        private static readonly Dictionary<string, string> _equipToSet = new Dictionary<string, string>();

        static SuperMechEquipSet()
        {
            foreach (var set in Sets)
            {
                _setById[set.id] = set;
                foreach (var partId in set.partIds)
                {
                    _equipToSet[partId] = set.id;
                }
            }
        }

        /// <summary>获取装备所属套装ID，无则返回null</summary>
        public static string GetSetId(string equipId)
        {
            return _equipToSet.TryGetValue(equipId, out string setId) ? setId : null;
        }

        /// <summary>统计单位装备的某套装部件数</summary>
        public static int CountSetParts(Actor a, string setId)
        {
            if (a == null || a.equipment == null) return 0;
            if (!_setById.TryGetValue(setId, out var set)) return 0;

            int count = 0;
            foreach (var partId in set.partIds)
            {
                if (HasEquipped(a, partId)) count++;
            }
            return count;
        }

        /// <summary>检查单位是否装备了指定ID的装备</summary>
        private static bool HasEquipped(Actor a, string equipId)
        {
            if (a.equipment == null) return false;
            // 遍历所有装备槽
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                var slot = a.equipment.getSlot(type);
                if (slot == null || slot.isEmpty()) continue;
                var item = slot.getItem();
                if (item != null && item.asset != null && item.asset.id == equipId) return true;
            }
            return false;
        }

        /// <summary>获取单位所有套装加成（伤害%，生命%）</summary>
        public static (int dmgBonus, int hpBonus, string activeSets) GetSetBonuses(Actor a)
        {
            int totalDmg = 0, totalHp = 0;
            var active = new List<string>();

            foreach (var set in Sets)
            {
                int parts = CountSetParts(a, set.id);
                if (parts >= 2)
                {
                    totalDmg += set.twoPieceDmgMul;
                    totalHp += set.twoPieceHpMul;
                    active.Add($"{LocalizedTextManager.getText(set.nameKey)}(2件)");
                }
                if (parts >= 4)
                {
                    totalDmg += set.fourPieceDmgMul;
                    totalHp += set.fourPieceHpMul;
                    active[active.Count - 1] = $"{LocalizedTextManager.getText(set.nameKey)}(4件)";
                }
            }

            return (totalDmg, totalHp, string.Join(", ", active));
        }

        /// <summary>获取套装显示信息（用于UI）</summary>
        public static string GetSetInfo(Actor a)
        {
            if (a == null) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var set in Sets)
            {
                int parts = CountSetParts(a, set.id);
                if (parts > 0)
                {
                    string setName = LocalizedTextManager.getText(set.nameKey);
                    sb.Append(setName).Append(": ").Append(parts).Append("/").Append(set.partIds.Length);
                    if (parts >= 2) sb.Append(" <color=#81c784>[2件激活]</color>");
                    if (parts >= 4) sb.Append(" <color=#ffd700>[4件激活]</color>");
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }
    }
}
