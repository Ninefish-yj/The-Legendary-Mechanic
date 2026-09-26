using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;
using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力分系用途（原著 ch49/ch50/ch269）：
    /// 气力是统一核心属性，不同系用途不同：
    ///   武道系 → 格斗能量（已在 SuperMechQi 里加 warfare/damage）
    ///   机械系 → 制造能量（磁环后叫械力）
    ///   异能系 → 基因链（通过基因树知识解锁提升，三维度：能级/操控/持久力）
    ///   魔法系 → 魔力池（通过魔法知识树解锁提升）
    ///   念力系 → 精神力（通过精神修炼树解锁提升）
    /// 阶段由知识树解锁数决定，不是气力阈值（原著：二阶基因链是进阶知识）。
    /// 不注册特质，用内部字典追踪，单位面板显示。
    /// </summary>
    public static class SuperMechCorePower
    {
        // 异能系：基因链5阶（原著：一阶→二阶→...，通过基因树知识提升）
        public static readonly string[] GeneChainNames = {
            "一阶基因链", "二阶基因链", "三阶基因链", "四阶基因链", "五阶基因链"
        };
        // 每阶需要的修炼进度（突破阈值）
        public static readonly int[] StageProgressReq = { 0, 100, 300, 600, 1000 };

        // 内部字典追踪当前阶段（unit.id -> stage 1-5）
        private static readonly Dictionary<long, int> _geneStage = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _manaStage = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _mindStage = new Dictionary<long, int>();

        // 修炼进度（unit.id -> progress）
        private static readonly Dictionary<long, int> _geneProgress = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _manaProgress = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _mindProgress = new Dictionary<long, int>();

        public static void Register()
        {
            // 独立修炼系统：基因链/魔力池/精神力通过修炼提升，不是知识树解锁
            Debug.Log("[超神机械师] 气力分系用途系统初始化：基因链/魔力池/精神力（独立修炼系统，原著ch49/ch50）");
        }

        /// <summary>独立修炼tick：被动增长修炼进度，进度满后自动突破。</summary>
        public static void TickCorePowers()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 判断是否在战斗中（攻击动画或最近被攻击）
                bool inCombat = a.data.blocked_action != null || a.data.in_duel;

                // 异能系：基因链修炼
                if (a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    TickCultivation(a, _geneStage, _geneProgress, "基因链", inCombat ? 3 : 1);
                }

                // 魔法系：魔力池修炼
                if (a.hasTrait(SuperMechTraits.ClassMage))
                {
                    TickCultivation(a, _manaStage, _manaProgress, "魔力池", inCombat ? 3 : 1);
                }

                // 念力系：精神力修炼
                if (a.hasTrait(SuperMechTraits.ClassMind))
                {
                    TickCultivation(a, _mindStage, _mindProgress, "精神力", inCombat ? 3 : 1);
                }
            }
        }

        // 每阶需要的气力等级（原著：气力是基础，决定基因链/魔力池/精神力的阶位上限）
        public static readonly int[] StageQiLevelReq = { 0, 1, 5, 10, 15, 21 };

        /// <summary>通用修炼tick：被动增长进度，满后突破（受气力等级限制，原著：气力是五系统一基础）。</summary>
        private static void TickCultivation(Actor a, Dictionary<long, int> stageDict, Dictionary<long, int> progDict, string name, int gain)
        {
            long id = a.id;
            if (!stageDict.TryGetValue(id, out int stage)) stage = 1;
            if (stage >= 5) return;  // 已满阶

            // 气力等级限制：下一阶需要的气力等级（原著：气力决定能量阶位上限）
            int nextStage = stage + 1;
            int qiLvReq = StageQiLevelReq[nextStage];
            float qi = SuperMechQi.GetQi(a);
            int qiLv = SuperMechQi.GetLevel(qi);
            if (qiLv < qiLvReq) return;  // 气力等级不够，无法突破

            if (!progDict.TryGetValue(id, out int prog)) prog = 0;
            // 修炼进度增长和气力修炼速度挂钩（气力涨得越快，能量修炼也越快）
            float qiMul = 1f + qiLv * 0.05f;
            prog += (int)(gain * qiMul);
            progDict[id] = prog;

            // 检查是否突破
            int req = StageProgressReq[stage];  // stage是1-4，对应下阶阈值
            if (prog >= req)
            {
                progDict[id] = 0;
                stageDict[id] = stage + 1;
                ApplyStageEffects(a);
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name} {name}突破到{GetStageName(name, stage + 1)}！（气力Lv{qiLv}）");
            }
        }

        /// <summary>获取阶段名称。</summary>
        private static string GetStageName(string type, int stage)
        {
            if (type == "基因链") return GeneChainNames[stage - 1];
            if (type == "魔力池") return ManaTierNames[stage - 1];
            if (type == "精神力") return MindTierNames[stage - 1];
            return "";
        }

        /// <summary>应用各系能量阶段的属性加成（原著：基因链提升异能威力，魔力池提升魔法威力，精神力提升念力威力）。</summary>
        private static void ApplyStageEffects(Actor a)
        {
            if (a == null || a.data == null) return;
            var stats = a.data.base_stats;
            if (stats == null) return;

            // 异能系：基因链提升伤害（每阶+10%伤害，+5%攻速）
            if (a.hasTrait(SuperMechTraits.ClassPsi) && _geneStage.TryGetValue(a.id, out int geneLv))
            {
                float dmgBonus = 1f + geneLv * 0.10f;
                float spdBonus = geneLv * 0.05f;
                stats["multiplier_damage"] = dmgBonus;
                stats["attack_speed"] = spdBonus;
            }

            // 魔法系：魔力池提升伤害和魔力上限（每阶+12%伤害，+20魔力）
            if (a.hasTrait(SuperMechTraits.ClassMage) && _manaStage.TryGetValue(a.id, out int manaLv))
            {
                float dmgBonus = 1f + manaLv * 0.12f;
                float manaBonus = manaLv * 20f;
                stats["multiplier_damage"] = dmgBonus;
                stats["mana"] = manaBonus;
            }

            // 念力系：精神力提升伤害和智力（每阶+10%伤害，+3智力）
            if (a.hasTrait(SuperMechTraits.ClassMind) && _mindStage.TryGetValue(a.id, out int mindLv))
            {
                float dmgBonus = 1f + mindLv * 0.10f;
                float intBonus = mindLv * 3f;
                stats["multiplier_damage"] = dmgBonus;
                stats["intelligence"] = intBonus;
            }
        }

        /// <summary>获取异能系当前基因链阶段名。</summary>
        public static string GetGeneStageName(Actor a)
        {
            if (a == null) return "基因未觉醒";
            if (_geneStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= GeneChainNames.Length)
                return GeneChainNames[lv - 1];
            return "一阶基因链";
        }

        /// <summary>获取魔法系当前魔力池阶段名。</summary>
        public static string GetManaStageName(Actor a)
        {
            if (a == null) return "魔力未觉醒";
            if (_manaStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= ManaTierNames.Length)
                return ManaTierNames[lv - 1];
            return "魔力初涌";
        }

        /// <summary>获取念力系当前精神力阶段名。</summary>
        public static string GetMindStageName(Actor a)
        {
            if (a == null) return "精神未觉醒";
            if (_mindStage.TryGetValue(a.id, out int lv) && lv >= 1 && lv <= MindTierNames.Length)
                return MindTierNames[lv - 1];
            return "精神觉醒";
        }

        /// <summary>获取异能系基因链阶段索引（1-5）。</summary>
        public static int GetGeneStage(Actor a)
        {
            if (a == null) return 1;
            return _geneStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        /// <summary>获取魔法系魔力池阶段索引（1-5）。</summary>
        public static int GetManaStage(Actor a)
        {
            if (a == null) return 1;
            return _manaStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        /// <summary>获取念力系精神力阶段索引（1-5）。</summary>
        public static int GetMindStage(Actor a)
        {
            if (a == null) return 1;
            return _mindStage.TryGetValue(a.id, out int lv) ? lv : 1;
        }

        /// <summary>获取异能系基因链修炼进度（0-100%）。</summary>
        public static float GetGeneProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetGeneStage(a);
            if (stage >= 5) return 100f;
            if (!_geneProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        /// <summary>获取魔法系魔力池修炼进度（0-100%）。</summary>
        public static float GetManaProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetManaStage(a);
            if (stage >= 5) return 100f;
            if (!_manaProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        /// <summary>获取念力系精神力修炼进度（0-100%）。</summary>
        public static float GetMindProgress(Actor a)
        {
            if (a == null) return 0f;
            int stage = GetMindStage(a);
            if (stage >= 5) return 100f;
            if (!_mindProgress.TryGetValue(a.id, out int prog)) prog = 0;
            int req = StageProgressReq[stage];
            return Mathf.Clamp01((float)prog / req) * 100f;
        }

        /// <summary>提升单位对应系别的核心能量阶段（基因链/魔力池/精神力）。</summary>
        public static void AdvanceStage(Actor a, int amount = 1)
        {
            if (a == null) return;
            string cls = SuperMechBranch.GetClass(a);
            if (cls == "异能系")
            {
                int cur = GetGeneStage(a);
                _geneStage[a.id] = Mathf.Min(cur + amount, GeneChainNames.Length);
            }
            else if (cls == "魔法系")
            {
                int cur = GetManaStage(a);
                _manaStage[a.id] = Mathf.Min(cur + amount, ManaTierNames.Length);
            }
            else if (cls == "念力系")
            {
                int cur = GetMindStage(a);
                _mindStage[a.id] = Mathf.Min(cur + amount, MindTierNames.Length);
            }
        }

        /// <summary>清空核心能量数据（世界切换用）。</summary>
        public static void Clear()
        {
            _geneStage.Clear();
            _manaStage.Clear();
            _mindStage.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_geneStage, alive);
            removed += SuperMechCleanup.CleanDict(_manaStage, alive);
            removed += SuperMechCleanup.CleanDict(_mindStage, alive);
            return removed;
        }
    }
}
