using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 法师塔系统（百度百科/原著ch730）：
    /// 超A级法师都有自己的法师塔，在塔中获得大量加成，有炼金人偶、符文阵列等辅助道具。
    /// 在法师塔中的超A级法师被称为"完全状态"。
    /// 秘法之殿是法师塔的极致（固化48000个符文法阵，投影遍及数百个次级维度）。
    /// 实现：A级以上法师自动建造法师塔，塔等级随阶位提升，提供伤害/生命/智力加成。
    /// </summary>
    public static class SuperMechMageTower
    {
        // 法师塔等级特质（1-5级）
        public const string Tower1 = "sm_mage_tower_1"; // 初级法师塔
        public const string Tower2 = "sm_mage_tower_2"; // 中级法师塔
        public const string Tower3 = "sm_mage_tower_3"; // 高级法师塔
        public const string Tower4 = "sm_mage_tower_4"; // 顶级法师塔
        public const string Tower5 = "sm_mage_tower_5"; // 秘法之殿级（宇宙宝物）

        public static readonly string[] TowerIds = { Tower1, Tower2, Tower3, Tower4, Tower5 };
        public static readonly string[] TowerNames = {
            "初级法师塔", "中级法师塔", "高级法师塔", "顶级法师塔", "秘法之殿"
        };
        // 每级加成（完全状态）
        public static readonly (float dmg, float hp, int intel, float mana)[] TowerBonus = {
            (0.20f, 0.20f, 10, 50f),   // 初级
            (0.40f, 0.40f, 20, 100f),  // 中级
            (0.70f, 0.70f, 35, 200f),  // 高级
            (1.00f, 1.00f, 50, 400f),  // 顶级
            (2.00f, 2.00f, 80, 800f),  // 秘法之殿（ch730：48000符文法阵）
        };

        private static readonly Dictionary<long, int> _towerLevel = new Dictionary<long, int>();

        public static void Register()
        {
            for (int i = 0; i < TowerIds.Length; i++)
            {
                var b = TowerBonus[i];
                LocalizedTextManager.add("trait_" + TowerIds[i], TowerNames[i], pReplace: true);
                LocalizedTextManager.add("trait_" + TowerIds[i] + "_info",
                    $"法师塔（完全状态）。伤害+{b.dmg * 100:F0}%，生命+{b.hp * 100:F0}%，智力+{b.intel}，魔力+{b.mana}。塔内有炼金人偶与符文阵列辅助。", pReplace: true);
                var t = new ActorTrait
                {
                    id = TowerIds[i],
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = "sm_mage_tower",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = 1f + b.dmg;
                t.base_stats["multiplier_health"] = 1f + b.hp;
                t.base_stats["intelligence"] = b.intel;
                t.base_stats["mana"] = b.mana;
                AssetManager.traits.add(t);
            }

            Debug.Log("[超神机械师] 法师塔系统注册完成：5级（初级→秘法之殿）");
        }

        /// <summary>Tick：A级以上法师自动建造/升级法师塔。</summary>
        public static void TickMageTowers()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                if (!a.hasTrait(SuperMechTraits.ClassMage)) continue;

                int rank = SuperMechAdvancement.GetRankIndex(a);
                if (rank < 8) continue; // A级以上才能建法师塔

                // 法师塔等级：A=1, A+=2, S=3, S+=4, SS/X=5
                int targetLevel = 1;
                if (rank >= 9) targetLevel = 2;  // A+
                if (rank >= 10) targetLevel = 3; // S
                if (rank >= 11) targetLevel = 4; // S+
                if (rank >= 12) targetLevel = 5; // SS/X = 秘法之殿级

                int current = GetTowerLevel(a);
                if (current < targetLevel)
                {
                    SetTowerLevel(a, targetLevel);
                    Debug.Log($"[超神机械师] {a.Name} 法师塔升级：{TowerNames[current]} → {TowerNames[targetLevel - 1]}");
                }
            }
        }

        /// <summary>获取法师塔等级（0=无）。</summary>
        public static int GetTowerLevel(Actor a)
        {
            if (a == null) return 0;
            if (_towerLevel.TryGetValue(a.data.id, out int lv)) return lv;
            // 从特质反查
            for (int i = TowerIds.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(TowerIds[i])) { _towerLevel[a.data.id] = i + 1; return i + 1; }
            }
            return 0;
        }

        /// <summary>设置法师塔等级。</summary>
        public static void SetTowerLevel(Actor a, int level)
        {
            if (a == null || level < 0 || level > TowerIds.Length) return;
            // 移除旧塔
            for (int i = 0; i < TowerIds.Length; i++)
            {
                if (a.hasTrait(TowerIds[i])) a.removeTrait(TowerIds[i]);
            }
            if (level > 0)
            {
                a.addTrait(TowerIds[level - 1]);
            }
            _towerLevel[a.data.id] = level;
        }

        /// <summary>获取法师塔名。</summary>
        public static string GetTowerName(Actor a)
        {
            int lv = GetTowerLevel(a);
            return lv > 0 ? TowerNames[lv - 1] : "无";
        }
    }
}
