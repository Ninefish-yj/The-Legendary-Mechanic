using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 副职业系统（原著 ch233）：主职业之外的第二职业槽。
    /// 每个单位可同时拥有主职业 + 一个副职业。
    /// 副职业有等级（原著：特工lv9/黑夜潜行者lv10），升级给额外加成。
    /// 副职业经验在战斗中获取。
    /// </summary>
    public static class SuperMechSubClass
    {
        // 副职业列表
        public const string SubAgent       = "sm_sub_agent";       // 特工
        public const string SubNinja      = "sm_sub_ninja";       // 黑夜潜行者
        public const string SubHacker     = "sm_sub_hacker";      // 黑客
        public const string SubMerchant  = "sm_sub_merchant";    // 商人
        public const string SubDoctor     = "sm_sub_doctor";      // 医生
        public const string SubEngineer  = "sm_sub_engineer";    // 工程师
        public const string SubScribe     = "sm_sub_scribe";      // 学者
        public const string SubScout      = "sm_sub_scout";       // 侦察兵

        public const int MaxSubLevel = 10;  // 副职业最高10级

        // 副职业经验追踪（unit.id -> subclass_id -> xp）
        private static readonly Dictionary<long, Dictionary<string, float>> _subXp = new Dictionary<long, Dictionary<string, float>>();
        // 副职业等级追踪（unit.id -> subclass_id -> level）
        private static readonly Dictionary<long, Dictionary<string, int>> _subLevel = new Dictionary<long, Dictionary<string, int>>();
        // 上次生命值（检测战斗）
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();

        // 升级经验阈值（10级）
        public static readonly int[] LevelThresholds = { 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5500, 7500 };

        public static string[] AllSubClasses = {
            SubAgent, SubNinja, SubHacker, SubMerchant, SubDoctor, SubEngineer, SubScribe, SubScout
        };

        public static void Register()
        {
            AddSubClass(SubAgent,     "特工",     0, 0, 0.05f,  "情报与暗杀专精，近战伤害提升。");
            AddSubClass(SubNinja,    "黑夜潜行者", 0, 0, 0.08f,  "夜间作战，隐蔽与暴击。");
            AddSubClass(SubHacker,   "黑客",     5, 0, 0f,       "电子战专精，智力提升。");
            AddSubClass(SubMerchant, "商人",     2, 0, 0f,       "交易与资源经营。");
            AddSubClass(SubDoctor,   "医生",     3, 0, 0f,       "治疗与急救。");
            AddSubClass(SubEngineer, "工程师",   4, 0, 0.05f,    "制造与维护。");
            AddSubClass(SubScribe,   "学者",     6, 0, 0f,       "知识研究，智力提升。");
            AddSubClass(SubScout,    "侦察兵",   0, 2, 0.03f,    "侦察与追踪。");

            // 副职业学习已移到知识Tab「◆ 操作」区域，不再注册神权
            // AddGivePower 方法保留供未来使用

            Debug.Log("[超神机械师] 副职业系统注册完成：8个副职业（含等级系统，知识Tab学习）");
        }

        /// <summary>每tick：战斗中获取副职业经验，自动升级。</summary>
        public static void TickSubLevels()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 检测战斗（血量变化）
                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                bool inCombat = curHealth < lastH - 0.5f || SuperMechQi.IsInCombat(a);
                _lastHealth[a.id] = curHealth;

                if (!inCombat) continue;

                // 给所有已拥有的副职业加经验
                foreach (string subId in AllSubClasses)
                {
                    if (!a.hasTrait(subId)) continue;

                    float xpGain = (2f + SuperMechQi.GetLevel(SuperMechQi.GetQi(a)) * 0.3f) * tickInterval;
                    AddSubXp(a, subId, xpGain);
                }
            }
        }

        /// <summary>增加副职业经验，自动检查升级。</summary>
        public static void AddSubXp(Actor a, string subId, float xp)
        {
            if (a == null) return;
            Dictionary<string, float> xpMap;
            if (!_subXp.TryGetValue(a.id, out xpMap))
            {
                xpMap = new Dictionary<string, float>();
                _subXp[a.id] = xpMap;
            }
            float curXp;
            xpMap.TryGetValue(subId, out curXp);
            xpMap[subId] = curXp + xp;

            // 检查升级
            int curLv = GetSubLevel(a, subId);
            if (curLv < MaxSubLevel && xpMap[subId] >= LevelThresholds[curLv])
            {
                SetSubLevel(a, subId, curLv + 1);
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name} 副职业{subId}升级到Lv{curLv + 1}");
            }
        }

        /// <summary>获取副职业等级。</summary>
        public static int GetSubLevel(Actor a, string subId)
        {
            if (a == null) return 0;
            Dictionary<string, int> lvMap;
            if (_subLevel.TryGetValue(a.id, out lvMap))
            {
                int lv;
                if (lvMap.TryGetValue(subId, out lv)) return lv;
            }
            return a.hasTrait(subId) ? 1 : 0;
        }

        /// <summary>设置副职业等级，给属性加成。</summary>
        private static void SetSubLevel(Actor a, string subId, int level)
        {
            Dictionary<string, int> lvMap;
            if (!_subLevel.TryGetValue(a.id, out lvMap))
            {
                lvMap = new Dictionary<string, int>();
                _subLevel[a.id] = lvMap;
            }
            lvMap[subId] = level;

            // 每级给属性加成
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                if (subId == SubAgent || subId == SubNinja || subId == SubScout)
                {
                    stats["damage"] = (stats["damage"]) + 2f;
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.01f;
                }
                else if (subId == SubHacker || subId == SubScribe)
                {
                    stats["intelligence"] = (stats["intelligence"]) + 2f;
                    stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.02f;
                }
                else if (subId == SubDoctor)
                {
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) + 0.03f;
                }
                else if (subId == SubEngineer)
                {
                    stats["intelligence"] = (stats["intelligence"]) + 1f;
                    stats["armor"] = (stats["armor"]) + 1f;
                }
                else
                {
                    stats["intelligence"] = (stats["intelligence"]) + 1f;
                }
            }
        }

        /// <summary>获取单位所有副职业等级描述。</summary>
        public static string GetSubLevelText(Actor a)
        {
            if (a == null) return "";
            var parts = new List<string>();
            foreach (string subId in AllSubClasses)
            {
                if (a.hasTrait(subId))
                {
                    int lv = GetSubLevel(a, subId);
                    string name = subId.Replace("sm_sub_", "");
                    parts.Add($"{name}Lv{lv}");
                }
            }
            return string.Join(" ", parts);
        }

        private static void AddSubClass(string id, string name, int intell, int dmgAdd, float dmgMul, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconAmbitious", group_id = "sm_subclass",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["intelligence"] = intell;
            if (dmgAdd > 0) t.base_stats["damage"] = dmgAdd;
            if (dmgMul > 0) t.base_stats["multiplier_damage"] = 1f + dmgMul;
            AssetManager.traits.add(t);
        }

        // AddGivePower 方法已移除（副职业学习移到知识Tab◆操作区域）

        /// <summary>清空所有副职业数据（世界切换用）。</summary>
        public static void Clear()
        {
            _subXp.Clear();
            _subLevel.Clear();
            _lastHealth.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_subLevel, alive);
            return removed;
        }
    }
}
