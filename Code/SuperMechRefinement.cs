using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 提炼法系统（原著ch50/ch172/ch237/ch277）：
    ///
    /// 【气力提炼法】ch50原文：
    /// "超能者自行领悟的基础气力修行法，总效果：气力+10，目前锻炼次数：0/80，每次锻炼消耗800经验、500体力。"
    /// - 全系通用（ch172："就连异能系也屁颠颠来学"）
    /// - 成长型技能，需要主动锻炼
    /// - 满80次锻炼后获得气力+10
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

        // 锻炼次数追踪（unit.id -> 已锻炼次数）
        private static readonly Dictionary<long, int> _refineCount = new Dictionary<long, int>();
        private static readonly Dictionary<long, int> _emRefineCount = new Dictionary<long, int>();

        // 原著常量
        public const int MaxRefineCount = 80;      // ch50：0/80
        public const int RefineXpCost = 800;        // ch50：每次消耗800经验
        public const int RefineStaminaCost = 500;   // ch50：每次消耗500体力
        public const float RefineQiBonus = 10f;     // ch50：总效果气力+10
        public const int MaxEmRefineCount = 100;    // ch237：限制100次

        public static void Register()
        {
            // 注册气力提炼法特质（全系通用）
            LocalizedTextManager.add("trait_" + RefinementTrait, "气力提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + RefinementTrait + "_info",
                "ch50原著。超能者自行领悟的基础气力修行法，全系通用。总效果气力+10，锻炼0/80次，每次消耗800经验、500体力。", pReplace: true);
            var t = new ActorTrait
            {
                id = RefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t);

            // 注册电磁因子提炼法特质（ch237/ch277原著：机械师专属，效果取决于智力）
            LocalizedTextManager.add("trait_" + EmRefinementTrait, "电磁因子提炼法", pReplace: true);
            LocalizedTextManager.add("trait_" + EmRefinementTrait + "_info",
                "ch237/ch277原著。机械师专属成长型技能，限制100次提炼，效果取决于智力属性。", pReplace: true);
            var t2 = new ActorTrait
            {
                id = EmRefinementTrait, path_icon = "ui/Icons/actor_traits/iconFireBlood", group_id = "sm_refinement",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            AssetManager.traits.add(t2);

            // 注册传授提炼法神权
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
                    a.addTrait(RefinementTrait);
                    if (a.hasTrait(SuperMechTraits.ClassMech))
                        a.addTrait(EmRefinementTrait);
                });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_refinement", "传授提炼法", pReplace: true);

            Debug.Log("[超神机械师] 提炼法系统注册完成（气力提炼法全系通用+电磁因子提炼法机械专属）");
        }

        /// <summary>主动锻炼一次提炼法（消耗经验+体力，成功则次数+1）。</summary>
        public static bool TryRefine(Actor a)
        {
            if (a == null || !a.hasTrait(RefinementTrait)) return false;
            long id = a.data.id;
            int count = GetRefineCount(a);
            if (count >= MaxRefineCount) return false; // 已满

            // 检查经验和体力（降临者有经验系统，土著简化为自动锻炼）
            if (SuperMechAwakened.IsAwakened(a))
            {
                float xp = SuperMechAwakened.GetXp(a);
                if (xp < RefineXpCost) return false; // 经验不足
                SuperMechAwakened.AddXp(a, -RefineXpCost);
            }

            // 消耗体力（简化：不直接扣stamina，因为原版stamina恢复快）
            _refineCount[id] = count + 1;

            // 满80次：获得气力+10
            if (_refineCount[id] >= MaxRefineCount)
            {
                SuperMechQi.AddQiMax(a, RefineQiBonus);
                SuperMechQi.SetQi(a, SuperMechQi.GetQiMax(a));
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name} 气力提炼法圆满！气力上限+{RefineQiBonus}");
            }
            return true;
        }

        /// <summary>电磁因子提炼（机械师专属，效果取决于智力）。</summary>
        public static bool TryEmRefine(Actor a)
        {
            if (a == null || !a.hasTrait(EmRefinementTrait)) return false;
            if (!a.hasTrait(SuperMechTraits.ClassMech)) return false;
            long id = a.data.id;
            int count = GetEmRefineCount(a);
            if (count >= MaxEmRefineCount) return false;

            _emRefineCount[id] = count + 1;

            // 效果取决于智力（ch237：智力越高，提炼效果越强）
            float intel = 5f;
            var stats = SuperMechStats.Of(a);
            if (stats != null) intel = stats["intelligence"];
            float qiGain = 0.5f * (1f + intel * 0.02f);
            SuperMechQi.AddQiMax(a, qiGain);

            // 满100次：额外奖励
            if (_emRefineCount[id] >= MaxEmRefineCount)
            {
                SuperMechQi.AddQiMax(a, 20f); // 圆满奖励
                if (SuperMechConfig.LogVerbose)
                    Debug.Log($"[超神机械师] {a.name} 电磁因子提炼法圆满！");
            }
            return true;
        }

        /// <summary>Tick：土著自动锻炼（降临者需要手动/面板锻炼）。</summary>
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

                // 土著自动锻炼（每tick有概率锻炼一次）
                if (!SuperMechAwakened.IsAwakened(a))
                {
                    if (hasRefine && Random.value < 0.1f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.05f) TryEmRefine(a);
                }
                // 降临者：每tick小概率自动锻炼（模拟玩家挂机修炼）
                else
                {
                    if (hasRefine && Random.value < 0.02f) TryRefine(a);
                    if (hasEmRefine && Random.value < 0.01f) TryEmRefine(a);
                }
            }
        }

        /// <summary>获取提炼法锻炼次数。</summary>
        public static int GetRefineCount(Actor a)
        {
            if (a == null) return 0;
            _refineCount.TryGetValue(a.data.id, out int c);
            return c;
        }

        /// <summary>获取电磁因子提炼次数。</summary>
        public static int GetEmRefineCount(Actor a)
        {
            if (a == null) return 0;
            _emRefineCount.TryGetValue(a.data.id, out int c);
            return c;
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
                text += $"提炼法 {c}/{MaxRefineCount}";
                if (c >= MaxRefineCount) text += "（圆满，气力+10）";
            }
            if (hasEm)
            {
                int c = GetEmRefineCount(a);
                if (text.Length > 0) text += " | ";
                text += $"电磁因子 {c}/{MaxEmRefineCount}";
                if (c >= MaxEmRefineCount) text += "（圆满）";
            }
            return text;
        }

        public static void Clear() { _refineCount.Clear(); _emRefineCount.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) { _refineCount.Remove(a.data.id); _emRefineCount.Remove(a.data.id); }
        }
    }
}
