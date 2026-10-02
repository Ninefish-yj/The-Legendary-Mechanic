using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechQiAttribute
    {
        public const string AttrMagnetic = "sm_qiattribute_938";
        public const string AttrSpirit   = "sm_qiattribute_939";
        public const string AttrFire     = "sm_qiattribute_940";
        public const string AttrWind     = "sm_qiattribute_941";
        public const string AttrIron     = "sm_qiattribute_942";
        public const string AttrWater    = "sm_qiattribute_943";
        public const string AttrLightning = "sm_qiattribute_944";
        public const string AttrDark     = "sm_qiattribute_945";
        public const string AttrLight    = "sm_qiattribute_946";
        public const string AttrNone     = "sm_qiattribute_947";

        public static readonly Dictionary<string, string> AttrDesc = new Dictionary<string, string>
        {
            { AttrMagnetic, "sm_qiattribute_948" },
            { AttrSpirit,   "sm_qiattribute_949" },
            { AttrFire,     "sm_qiattribute_950" },
            { AttrWind,     "sm_qiattribute_951" },
            { AttrIron,     "sm_qiattribute_952" },
            { AttrWater,    "sm_qiattribute_953" },
            { AttrLightning, "sm_qiattribute_954" },
            { AttrDark,     "sm_qiattribute_955" },
            { AttrLight,    "sm_qiattribute_956" },
        };

        private static readonly Dictionary<long, string> _attr = new Dictionary<long, string>();

        public static string GetAttribute(Actor a)
        {
            if (a == null) return AttrNone;
            if (_attr.TryGetValue(a.data.id, out string v)) return v;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return AttrSpirit;
            return AttrNone;
        }

        public static void SetAttribute(Actor a, string attr)
        {
            if (a == null) return;
            _attr[a.data.id] = attr;
            ApplyBonus(a);
        }

        public static void AutoAssign(Actor a)
        {
            if (a == null) return;
            string attr = AttrNone;

            if (a.hasTrait(SuperMechTraits.ClassMech))
            {
                if (SuperMechStage.GetStage(a) >= 4) attr = AttrMagnetic;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMind))
            {
                attr = AttrSpirit;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMartial))
            {
                // 武道系无固定分支，随机分配气力属性
                string[] martialAttrs = { AttrIron, AttrFire, AttrWind };
                attr = martialAttrs[Mathf.Abs(a.data.id.GetHashCode()) % martialAttrs.Length];
            }
            else if (a.hasTrait(SuperMechTraits.ClassPsi))
            {
                // 念力系按四大分支分配属性
                if (a.hasTrait(SuperMechBranch.BranchPsiMind)) attr = AttrSpirit;
                else if (a.hasTrait(SuperMechBranch.BranchPsiKinesis)) attr = AttrWind;
                else if (a.hasTrait(SuperMechBranch.BranchPsiSense)) attr = AttrLight;
                else if (a.hasTrait(SuperMechBranch.BranchPsiPotential)) attr = AttrFire;
                else attr = AttrSpirit;
            }
            else if (a.hasTrait(SuperMechTraits.ClassMage))
            {
                // 魔法系无固定分支（法术可兼修），随机分配元素属性
                string[] mageAttrs = { AttrFire, AttrWater, AttrLightning, AttrLight, AttrDark };
                attr = mageAttrs[Mathf.Abs(a.data.id.GetHashCode()) % mageAttrs.Length];
            }

            if (attr != AttrNone) SetAttribute(a, attr);
        }

        private static void ApplyBonus(Actor a)
        {
            string attr = GetAttribute(a);
            var stats = SuperMechStats.Of(a);
            if (stats == null) return;

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

        public static void Clear() { _attr.Clear(); }

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
