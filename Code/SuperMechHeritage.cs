using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 土著传承度系统（原著：星海人没有面板，靠传承/流派/实战感悟学习知识）。
    ///
    /// 【双轨制】
    /// - 降临者（玩家）：潜能点系统，升级获得，手动点知识树
    /// - 土著（星海人）：传承度系统，随时间/战斗/师徒自动增长，自动解锁知识
    ///
    /// 原著设定：
    /// - 土著靠传承（师徒、流派、种族传承）学习技能
    /// - 实战中感悟突破
    /// - 没有量化的潜能点，知识是"学会"的不是"点出来"的
    /// - 高阶位土著传承度增长更快（阅历丰富）
    /// </summary>
    public static class SuperMechHeritage
    {
        // 传承度追踪（unit.id -> 传承度值）
        private static readonly Dictionary<long, float> _heritage = new Dictionary<long, float>();
        // 已自动解锁的知识节点数（unit.id -> count）
        private static readonly Dictionary<long, int> _autoUnlocked = new Dictionary<long, int>();

        // 每解锁一个知识节点需要的传承度（递增）
        private static float GetCost(int unlockedCount)
        {
            return 50f * Mathf.Pow(1.3f, unlockedCount);
        }

        /// <summary>获取传承度。</summary>
        public static float GetHeritage(Actor a)
        {
            if (a == null) return 0;
            float v; _heritage.TryGetValue(a.id, out v); return v;
        }

        /// <summary>增加传承度。</summary>
        public static void AddHeritage(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetHeritage(a);
            _heritage[a.id] = cur + amount;
        }

        /// <summary>直接设置传承度（存档恢复用）。</summary>
        public static void SetHeritage(Actor a, float value)
        {
            if (a == null) return;
            _heritage[a.id] = Mathf.Max(0, value);
        }

        /// <summary>
        /// Tick：土著传承度增长 + 自动解锁知识。
        /// 降临者不走这个系统（用潜能点）。
        /// </summary>
        public static void TickHeritage()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者不走传承度

                // 基础传承度增长（随时间阅历）
                float growth = 0.5f * tickInterval;

                // 阶位越高，阅历越丰富，传承度增长越快
                int rankIdx = SuperMechAdvancement.GetExactRankIndex(a);
                growth *= (1f + rankIdx * 0.15f);

                // 战斗中传承度增长加速（实战感悟）
                if (SuperMechQi.IsInCombat(a)) growth *= 3f;

                // 有提炼法的单位传承度增长更快（系统修炼）
                if (a.hasTrait("sm_refinement")) growth *= 1.5f;

                AddHeritage(a, growth);

                // —— 潜移默化属性成长（原著ch741：NPC水磨工夫潜移默化提升）——
                // 土著没有面板加点，属性按修炼方向自动偏向增长
                AutoGrowStats(a, growth);

                // 自动解锁知识节点
                int unlocked;
                _autoUnlocked.TryGetValue(a.id, out unlocked);
                float cost = GetCost(unlocked);
                float h = GetHeritage(a);

                while (h >= cost && unlocked < 50) // 最多自动解锁50个节点
                {
                    h -= cost;
                    unlocked++;
                    // 自动解锁对应知识树节点
                    string cls = SuperMechBranch.GetClass(a);
                    string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
                    string nodeId = $"{prefix}_{unlocked}";
                    SuperMechPotential.ForceUnlockNode(a, nodeId);
                    // 解锁知识给属性加成
                    var stats = SuperMechStats.Of(a);
                    if (stats != null)
                    {
                        stats["intelligence"] = (stats["intelligence"]) + 0.5f;
                        stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) + 0.01f;
                    }
                    _heritage[a.id] = h;
                    _autoUnlocked[a.id] = unlocked;
                    cost = GetCost(unlocked);
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师] {a.name}（土著）传承度突破，自动解锁知识{nodeId}（共{unlocked}个）");
                }
            }
        }

        /// <summary>获取土著已自动解锁的知识节点数。</summary>
        public static int GetAutoUnlockedCount(Actor a)
        {
            if (a == null) return 0;
            int v; _autoUnlocked.TryGetValue(a.id, out v); return v;
        }

        /// <summary>清除数据。</summary>
        public static void Clear() { _heritage.Clear(); _autoUnlocked.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_heritage, alive);
            removed += SuperMechCleanup.CleanDict(_autoUnlocked, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _heritage.Remove(a.id);
            _autoUnlocked.Remove(a.id);
        }

        /// <summary>
        /// 土著潜移默化属性成长（原著ch741：NPC没有面板，水磨工夫潜移默化提升）。
        /// 属性按所属系和分支自动偏向增长，不需要手动加点。
        /// </summary>
        private static void AutoGrowStats(Actor a, float growthAmount)
        {
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            string cls = SuperMechBranch.GetClass(a);
            string branch = SuperMechBranch.GetBranchTrait(a);
            if (string.IsNullOrEmpty(cls)) return;

            // 基础成长率（每点传承度转化为属性的比例，非常缓慢）
            float rate = growthAmount * 0.002f;

            // 各系基础属性偏向（原著：五系属性成长方向不同）
            switch (cls)
            {
                case "武道系":
                    // 武道：力量+耐力为主，速度为辅
                    stats["damage"] = (stats["damage"]) + rate * 1.5f;
                    stats["warfare"] = (stats["warfare"]) + rate * 1.2f;
                    stats["health"] = (stats["health"]) + rate * 2f;
                    stats["stamina"] = (stats["stamina"]) + rate * 1.5f;
                    stats["armor"] = (stats["armor"]) + rate * 0.5f;
                    stats["speed"] = (stats["speed"]) + rate * 0.3f;
                    // 分支细化（原著：敏捷/力量/防御等路线）
                    if (branch == SuperMechBranch.BranchMartialBody) // 体魄
                    {
                        stats["damage"] = (stats["damage"]) + rate * 1f;
                        stats["health"] = (stats["health"]) + rate * 1f;
                        stats["stamina"] = (stats["stamina"]) + rate * 0.5f;
                    }
                    else if (branch == SuperMechBranch.BranchMartialTactic) // 战术
                    {
                        stats["speed"] = (stats["speed"]) + rate * 1f;
                        stats["attack_speed"] = (stats["attack_speed"]) + rate * 1f;
                        stats["warfare"] = (stats["warfare"]) + rate * 0.8f;
                    }
                    else if (branch == SuperMechBranch.BranchMartialPower) // 超能
                    {
                        stats["damage"] = (stats["damage"]) + rate * 0.8f;
                        stats["mana"] = (stats["mana"]) + rate * 1f;
                        stats["intelligence"] = (stats["intelligence"]) + rate * 0.5f;
                    }
                    break;

                case "机械系":
                    // 机械：智力为主，耐力为辅
                    stats["intelligence"] = (stats["intelligence"]) + rate * 2f;
                    stats["health"] = (stats["health"]) + rate * 1f;
                    stats["stamina"] = (stats["stamina"]) + rate * 0.8f;
                    stats["damage"] = (stats["damage"]) + rate * 0.5f;
                    // 分支细化（原著ch50：枪炮师/机械师/械武者）
                    if (branch == SuperMechBranch.BranchGunner) // 枪炮师
                    {
                        stats["damage"] = (stats["damage"]) + rate * 1.5f;
                        stats["range"] = (stats["range"]) + rate * 0.5f;
                    }
                    else if (branch == SuperMechBranch.BranchMech) // 机械师
                    {
                        stats["intelligence"] = (stats["intelligence"]) + rate * 1f;
                        stats["health"] = (stats["health"]) + rate * 0.5f;
                    }
                    else if (branch == SuperMechBranch.BranchMartial) // 械武者
                    {
                        stats["damage"] = (stats["damage"]) + rate * 1f;
                        stats["speed"] = (stats["speed"]) + rate * 0.8f;
                        stats["armor"] = (stats["armor"]) + rate * 0.5f;
                    }
                    break;

                case "异能系":
                    // 异能：智力+精神为主
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.8f;
                    stats["mana"] = (stats["mana"]) + rate * 1.5f;
                    stats["damage"] = (stats["damage"]) + rate * 0.8f;
                    break;

                case "魔法系":
                    // 魔法：智力+精神+耐力
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.5f;
                    stats["mana"] = (stats["mana"]) + rate * 2f;
                    stats["health"] = (stats["health"]) + rate * 0.8f;
                    break;

                case "念力系":
                    // 念力：智力+精神+速度
                    stats["intelligence"] = (stats["intelligence"]) + rate * 1.5f;
                    stats["mana"] = (stats["mana"]) + rate * 1.5f;
                    stats["speed"] = (stats["speed"]) + rate * 0.8f;
                    break;
            }
        }
    }
}
