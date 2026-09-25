using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力属性系统（原著ch48/ch49）。
    /// 气力有属性，表示该职业对气力的用途：
    /// - 机械系磁环阶段觉醒【磁】属性（+机械亲和）
    /// - 念力系气力=【精神】属性
    /// - 武道系可通过流派改变属性（火的爆裂/风的速度/铁的坚韧）
    /// - 异能系取决于异能类型
    /// - 魔法系取决于魔法类型
    /// 属性通过stats反射施加属性加成，不做特质。
    /// </summary>
    public static class SuperMechQiAttribute
    {
        public const string AttrMagnetic = "磁";
        public const string AttrSpirit   = "精神";
        public const string AttrFire     = "火";
        public const string AttrWind     = "风";
        public const string AttrIron     = "铁";
        public const string AttrWater    = "水";
        public const string AttrLightning = "雷";
        public const string AttrDark     = "暗";
        public const string AttrLight    = "光";
        public const string AttrNone     = "无";

        // 属性→描述
        public static readonly Dictionary<string, string> AttrDesc = new Dictionary<string, string>
        {
            { AttrMagnetic, "电磁属性气力，增加机械亲和度与制造效率" },
            { AttrSpirit,   "精神属性气力，强化精神攻击与幻术" },
            { AttrFire,     "火属性气力，爆裂伤害，攻速+暴击" },
            { AttrWind,     "风属性气力，速度与闪避，灵动作战" },
            { AttrIron,     "铁属性气力，坚韧防御，护甲+生命" },
            { AttrWater,    "水属性气力，持续恢复，持久战" },
            { AttrLightning, "雷属性气力，高速暴击，瞬时爆发" },
            { AttrDark,     "暗属性气力，潜行与暴击伤害" },
            { AttrLight,    "光属性气力，治疗与祝福效果" },
        };

        private static readonly Dictionary<long, string> _attr = new Dictionary<long, string>();

        /// <summary>获取单位气力属性。</summary>
        public static string GetAttribute(Actor a)
        {
            if (a == null) return AttrNone;
            if (_attr.TryGetValue(a.data.id, out string v)) return v;
            // 自动推断：念力系默认精神
            if (a.hasTrait(SuperMechTraits.ClassMind)) return AttrSpirit;
            return AttrNone;
        }

        /// <summary>设置单位气力属性。</summary>
        public static void SetAttribute(Actor a, string attr)
        {
            if (a == null) return;
            _attr[a.data.id] = attr;
            ApplyBonus(a);
        }

        /// <summary>根据系/分支/阶段自动推断并设置属性。</summary>
        public static void AutoAssign(Actor a)
        {
            if (a == null) return;
            string attr = AttrNone;

            // 机械系：磁环阶段觉醒【磁】
            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                if (SuperMechStage.GetStage(a) >= 4) attr = AttrMagnetic;
            }
            // 念力系：默认【精神】
            else if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                attr = AttrSpirit;
            }
            // 武道系：按分支定属性
            else if (a.hasTrait(SuperMechTraits.ClassMartial))
            {
                if (a.hasTrait(SuperMechBranch.BranchMartialPower)) attr = AttrFire;       // 超能→火
                else if (a.hasTrait(SuperMechBranch.BranchMartialTactic)) attr = AttrWind; // 战术→风
                else if (a.hasTrait(SuperMechBranch.BranchMartialBody)) attr = AttrIron;   // 体魄→铁
            }
            // 异能系：按异能类型（简化：随机火/水/雷/暗/光）
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                string[] psiAttrs = { AttrFire, AttrWater, AttrLightning, AttrDark, AttrLight };
                attr = psiAttrs[Mathf.Abs(a.data.id.GetHashCode()) % psiAttrs.Length];
            }
            // 魔法系：按专精方向
            else if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                if (a.hasTrait(SuperMechBranch.BranchMageSpecialist)) attr = AttrFire;  // 专精→火/雷
                else if (a.hasTrait(SuperMechBranch.BranchMageWeave)) attr = AttrLight;  // 魔网→光
                else attr = AttrLight;
            }

            if (attr != AttrNone) SetAttribute(a, attr);
        }

        /// <summary>通过stats反射施加属性加成。</summary>
        private static void ApplyBonus(Actor a)
        {
            string attr = GetAttribute(a);
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

            // 先清除旧加成（简化：每次重新设置时由调用方处理）
            switch (attr)
            {
                case AttrMagnetic:
                    stats["intelligence"] = (stats["intelligence"]) + 5f;
                    stats["experience"] = ((stats["experience"] == 0f ? 1f : stats["experience"])) * 1.1f;
                    break;
                case AttrSpirit:
                    stats["intelligence"] = (stats["intelligence"]) + 8f;
                    stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) * 1.1f;
                    break;
                case AttrFire:
                    stats["damage"] = (stats["damage"]) + 5f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.1f;
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.05f;
                    break;
                case AttrWind:
                    stats["speed"] = (stats["speed"]) + 0.5f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.15f;
                    break;
                case AttrIron:
                    stats["armor"] = (stats["armor"]) + 5f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.15f;
                    break;
                case AttrWater:
                    stats["stamina"] = (stats["stamina"]) + 20f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.05f;
                    break;
                case AttrLightning:
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.1f;
                    stats["attack_speed"] = (stats["attack_speed"]) + 0.2f;
                    break;
                case AttrDark:
                    stats["critical_chance"] = (stats["critical_chance"]) + 0.08f;
                    stats["damage"] = (stats["damage"]) + 3f;
                    break;
                case AttrLight:
                    stats["intelligence"] = (stats["intelligence"]) + 3f;
                    stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.08f;
                    break;
            }
        }

        /// <summary>清除单位属性数据。</summary>
        public static void Clear() { _attr.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_attr, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a != null) _attr.Remove(a.data.id);
        }

        /// <summary>定期为已觉醒但未分配属性的单位自动分配。</summary>
        public static void TickAutoAssign()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (GetAttribute(a) == AttrNone) AutoAssign(a);
            }
        }
    }
}
