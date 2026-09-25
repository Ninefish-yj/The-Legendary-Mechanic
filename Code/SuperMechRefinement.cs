using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 提炼法系统（原著ch50/ch51/ch172/ch237/ch277）：
    ///
    /// 【气力提炼法】ch50/ch51原文：
    /// - 刚获得时：总效果气力+10，锻炼次数0/80，每次消耗800经验、500体力
    /// - 每次锻炼按完美度获得额外气力：
    ///   完美度40%以下：气力+1
    ///   完美度40%~80%：气力+2
    ///   完美度80%以上：气力+3
    /// - 完美度取决于该职业的主要属性（机械系=智力，ch51原文）
    /// - 满80次最多+240，总计+250气力
    /// - 全系通用（ch172："就连异能系也屁颠颠来学"）
    ///
    /// 【电磁因子提炼法】ch237/ch277：
    /// - 机械师专属成长型技能
    /// - 限制100次提炼
    /// - 效果取决于智力属性
    /// </summary>
    public static class SuperMechRefinement
    {
        public const string RefinementTrait = "sm_refinement";
        public const string EmRefinementTrait = "sm_em_refinement";
        // 三系提炼法变种（原著：所有修炼气力的方法都叫提炼法）
        public const string PsiResonance  = "sm_pcult_resonance";   // 基因提炼法（异能）
        public const string ManaMeditation = "sm_pcult_meditation"; // 魔力提炼法（魔法）
        public const string MindTrain    = "sm_pcult_mind_train";   // 精神提炼法（念力）

        // 锻炼次数追踪
        private static readonly Dictionary<long, int> _refineCount = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _emRefineCount = new Dictionary<long, int>();
        // 累计获得的气力（基础+10 + 每次锻炼加成）
        private static readonly Dictionary<long, float> _refineQiBonus = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _emRefineQiBonus = new Dictionary<long, float>();

        // 原著常量
        public const int MaxRefineCount = 80;       // ch50：0/80
        public const int RefineXpCost = 800;         // ch50：每次消耗800经验
        public const int RefineStaminaCost = 500;    // ch50：每次消耗500体力
        public const float RefineBaseQi = 10f;       // ch50：基础总效果气力+10
        public const int MaxEmRefineCount = 100;     // ch237：限制100次

        public static void Register()
        {
            LocalizedTextManager.add("trait_" + RefinementTrait, "气力提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + RefinementTrait + "_info",
                "ch50/ch51原著。超能者自行领悟的基础气力修行法，全系通用。基础气力+10，锻炼0/80次，每次按完美度加1~3气力（完美度取决于职业主属性），每次消耗800经验、500体力。", pReplace: true);
            var t = new ActorTrait
            {
                id = RefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);

            LocalizedTextManager.add("trait_" + EmRefinementTrait, "电磁因子提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + EmRefinementTrait + "_info",
                "ch237/ch277原著。机械师专属成长型技能，限制100次提炼，效果取决于智力属性。", pReplace: true);
            var t2 = new ActorTrait
            {
                id = EmRefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t2);

            var givePower = new GodPower
            {
                id = "sm_give_refinement",
                name = "传授提炼法",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            givePower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a)
                {
                    if (!a.hasTrait(RefinementTrait))
                    {
                        a.addTrait(RefinementTrait);
                        _refineQiBonus[a.data.id] = RefineBaseQi; // 获得时基础+10
                        SuperMechQi.AddQiMax(a, RefineBaseQi);
                    }
                    if (a.hasTrait(SuperMechTraits.ClassMech) && !a.hasTrait(EmRefinementTrait))
                    {
                        a.addTrait(EmRefinementTrait);
                    }
                });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_refinement", "传授提炼法", pReplace: true);

            // 三系提炼法变种（原著ch172：就连异能系也屁颠颠来学）
            AddVariantTrait(PsiResonance, "基因提炼法（异能系）", "战斗中基因链共鸣，异能系单位智力额外增长。");
            AddVariantTrait(ManaMeditation, "魔力提炼法（魔法系）", "持续冥想提炼魔力，魔法系单位智力与魔力池额外增长。");
            AddVariantTrait(MindTrain, "精神提炼法（念力系）", "精神力日常锻炼提炼，念力系单位智力与精神力额外增长。");

            Debug.Log("[超神机械师] 提炼法系统注册完成（气力提炼法全系通用+电磁因子提炼法机械专属+三系变种）");
        }

        private static void AddVariantTrait(string id, string name, string desc)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);
        }

        /// <summary>三系提炼法变种tick效果（持续增长属性）。</summary>
        public static void TickCultivation()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                var s = SuperMechStats.Of(a);
                if (s == null) continue;

                if (a.hasTrait(PsiResonance) && a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
                if (a.hasTrait(ManaMeditation) && a.hasTrait(SuperMechTraits.ClassMage))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.6f;
                    float mana = s["mana"];
                    if (mana < 1000) s["mana"] = mana + 3f;
                }
                if (a.hasTrait(MindTrain) && a.hasTrait(SuperMechTraits.ClassMind))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
            }
        }

        /// <summary>计算完美度（取决于职业主属性，ch51原文）。</summary>
        private static float CalcPerfection(Actor a)
        {
            float mainStat = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                // 各职业主属性（ch51：机械系=智力）
                if (a.hasTrait(SuperMechTraits.ClassMech)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMartial)) mainStat = (stats["warfare"] + stats["stamina"]) / 2f;
                else if (a.hasTrait(SuperMechTraits.ClassPsi)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMage)) mainStat = stats["intelligence"];
                else if (a.hasTrait(SuperMechTraits.ClassMind)) mainStat = stats["intelligence"];
            }
            // 完美度 = 基础50% + 主属性×2%，上限95%
            float perfection = 50f + mainStat * 2f;
            return Mathf.Clamp(perfection, 10f, 95f);
        }

        /// <summary>根据完美度计算气力加成（ch51：40%以下+1，40~80%+2，80%以上+3）。</summary>
        private static int CalcQiGain(float perfection)
        {
            if (perfection >= 80f) return 3;
            if (perfection >= 40f) return 2;
            return 1;
        }

        /// <summary>主动锻炼一次提炼法。</summary>
        public static bool TryRefine(Actor a)
        {
            if (a == null || !a.hasTrait(RefinementTrait)) return false;
            long id = a.data.id;
            int count = GetRefineCount(a);
            if (count >= MaxRefineCount) return false;

            // 降临者消耗经验
            if (SuperMechAwakened.IsAwakened(a))
            {
                float xp = SuperMechAwakened.GetXp(a);
                if (xp < RefineXpCost) return false;
                SuperMechAwakened.AddXp(a, -RefineXpCost);
            }

            // 计算完美度和气力加成
            float perfection = CalcPerfection(a);
            int qiGain = CalcQiGain(perfection);

            _refineCount[id] = count + 1;
            if (!_refineQiBonus.ContainsKey(id)) _refineQiBonus[id] = RefineBaseQi;
            _refineQiBonus[id] += qiGain;

            // 增加气力上限
            SuperMechQi.AddQiMax(a, qiGain);
            SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 提炼法锻炼：完美度{perfection:F0}%，气力+{qiGain}（{count + 1}/{MaxRefineCount}）");

            return true;
        }

        /// <summary>电磁因子提炼（机械师专属）。</summary>
        public static bool TryEmRefine(Actor a)
        {
            if (a == null || !a.hasTrait(EmRefinementTrait)) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMech)) return false;
            long id = a.data.id;
            int count = GetEmRefineCount(a);
            if (count >= MaxEmRefineCount) return false;

            _emRefineCount[id] = count + 1;

            // 效果取决于智力（ch237）
            float intel = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null) intel = stats["intelligence"];
            float qiGain = 0.5f * (1f + intel * 0.02f);
            if (!_emRefineQiBonus.ContainsKey(id)) _emRefineQiBonus[id] = 0;
            _emRefineQiBonus[id] += qiGain;
            SuperMechQi.AddQiMax(a, qiGain);

            if (count + 1 >= MaxEmRefineCount && SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 电磁因子提炼法圆满！");

            return true;
        }

        /// <summary>Tick：自动锻炼。</summary>
        public static void TickRefinement()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                bool hasRefine = a.hasTrait(RefinementTrait);
                bool hasEmRefine = a.hasTrait(EmRefinementTrait);
                if (!hasRefine && !hasEmRefine) continue;

                // 土著自动锻炼（每tick10%概率）
                if (!SuperMechAwakened.IsAwakened(a))
                {
                    if (hasRefine && Random.value < 0.1f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.05f) TryEmRefine(a);
                }
                // 降临者小概率自动锻炼（2%，模拟挂机）
                else
                {
                    if (hasRefine && Random.value < 0.02f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.01f) TryEmRefine(a);
                }
            }
        }

        public static int GetRefineCount(Actor a)
        {
            if (a == null) return 0;
            _refineCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        public static int GetEmRefineCount(Actor a)
        {
            if (a == null) return 0;
            _emRefineCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        public static float GetRefineQiBonus(Actor a)
        {
            if (a == null) return 0;
            _refineQiBonus.TryGetValue(a.data.id, out float v);
            return v;
        }

        /// <summary>获取提炼法状态文本（用于单位面板）。</summary>
        public static string GetStatusText(Actor a)
        {
            if (a == null) return null;
            bool hasRefine = a.hasTrait(RefinementTrait);
            bool hasEm = a.hasTrait(EmRefinementTrait);
            if (!hasRefine && !hasEm) return null;

            string text = "";
            if (hasRefine)
            {
                int c = GetRefineCount(a);
                float bonus = GetRefineQiBonus(a);
                text += $"提炼法 {c}/{MaxRefineCount}（气力+{bonus:F0}）";
                if (c >= MaxRefineCount) text += " 圆满";
            }
            if (hasEm)
            {
                int c = GetEmRefineCount(a);
                if (text.Length > 0) text += " | ";
                text += $"电磁因子 {c}/{MaxEmRefineCount}";
                if (c >= MaxEmRefineCount) text += " 圆满";
            }
            return text;
        }

        public static void Clear() { _refineCount.Clear(); _emRefineCount.Clear(); _refineQiBonus.Clear(); _emRefineQiBonus.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_cultivation, alive);
            removed += SuperMechCleanup.CleanDict(_perfectLevel, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null)
            {
                _refineCount.Remove(a.data.id);
                _emRefineCount.Remove(a.data.id);
                _refineQiBonus.Remove(a.data.id);
                _emRefineQiBonus.Remove(a.data.id);
            }
        }
    }
}
