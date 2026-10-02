using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechModAdapters
    {
        private static bool _initialized;
        private static HashSet<string> _vanillaStats;
        private static List<IModAdapter> _adapters;
        private static readonly List<string> _disabledAdapters = new();

        public interface IModAdapter
        {
            string ModName { get; }
            bool IsAvailable { get; }
            bool Detect(Actor a);
            int GetRealmLevel(Actor a);
            float GetEnergyStrength(Actor a);
            float ConvertToQi(Actor a);
        }

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            BuildVanillaBlacklist();
            BuildAdapters();
        }

        /// <summary>通过反射检测目标模组的关键类型是否存在于当前AppDomain</summary>
        public static bool IsTypeLoaded(string typeName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.GetType(typeName) != null) return true;
                }
                catch { }
            }
            return false;
        }

        public static IReadOnlyList<string> DisabledAdapters => _disabledAdapters;

        private static void BuildVanillaBlacklist()
        {
            _vanillaStats = new HashSet<string>
            {
                "personality_aggression", "personality_administration", "personality_diplomatic",
                "personality_rationality", "diplomacy", "warfare", "stewardship", "intelligence",
                "lifespan", "mutation", "offspring", "multiplier_offspring", "army", "cities",
                "range", "bonus_towers", "damage", "speed", "health", "armor", "stamina", "mana",
                "accuracy", "targets", "projectiles", "experience", "happiness", "critical_chance",
                "critical_damage_multiplier", "size", "area_of_effect", "attack_speed",
                "throwing_range", "construction_speed", "loyalty_traits", "birth_rate",
                "maturation", "age_adult", "age_breeding", "max_nutrition", "nutrition",
                "multiplier_health", "multiplier_mana", "multiplier_stamina", "multiplier_damage",
                "multiplier_speed", "multiplier_armor", "regen_health", "regen_mana",
                "regen_stamina", "max_health", "max_mana", "max_stamina"
            };
        }

        private static void BuildAdapters()
        {
            var all = new IModAdapter[]
            {
                new FanrenXiuxianAdapter(),
                new IncenseDivineAdapter(),
                new WesternFantasyAdapter(),
                new ZhutianAdapter(),
                new GuimiAdapter()
            };
            _adapters = new List<IModAdapter>();
            foreach (var adapter in all)
            {
                if (adapter.IsAvailable)
                {
                    _adapters.Add(adapter);
                }
                else
                {
                    _disabledAdapters.Add(adapter.ModName);
                    Debug.LogWarning($"[超神机械师] 跨模组适配器已禁用: {adapter.ModName}（目标模组未加载）");
                }
            }
            Debug.Log($"[超神机械师] 跨模组适配器: {_adapters.Count}个可用, {_disabledAdapters.Count}个禁用");
        }

        public static bool IsVanillaStat(string statId)
        {
            return _vanillaStats.Contains(statId.ToLower());
        }

        public static IModAdapter DetectMod(Actor a)
        {
            if (!_initialized) Init();
            if (a == null) return null;
            foreach (var adapter in _adapters)
            {
                if (adapter.Detect(a)) return adapter;
            }
            return null;
        }

        public static float GetModPower(Actor a)
        {
            var adapter = DetectMod(a);
            if (adapter != null) return adapter.GetEnergyStrength(a);
            return 0f;
        }

        public static float ConvertModToQi(Actor a)
        {
            var adapter = DetectMod(a);
            if (adapter != null) return adapter.ConvertToQi(a);
            return 0f;
        }

        public static string GetDetectedModName(Actor a)
        {
            var adapter = DetectMod(a);
            return adapter?.ModName ?? "未知";
        }
    }

    public class FanrenXiuxianAdapter : SuperMechModAdapters.IModAdapter
    {
        public string ModName => "凡人修仙传";
        public bool IsAvailable => SuperMechModAdapters.IsTypeLoaded("FanRenStandalone.Core.FanRenStandalone")
            || SuperMechModAdapters.IsTypeLoaded("FanRenStandalone.Main");

        private static readonly float[] RealmEnergyMap = {
            0f,       // 0 凡人
            100f,     // 1 炼气期
            500f,     // 2 筑基期
            2000f,    // 3 结丹期
            8000f,    // 4 元婴期
            20000f,   // 5 化神期
            40000f,   // 6 炼虚期
            70000f,   // 7 合体期
            100000f,  // 8 大乘期
            130000f,  // 9 渡劫期
            160000f   // 10 真仙（≈X阶148800）
        };

        public bool Detect(Actor a)
        {
            if (a == null || a.data == null) return false;
            int realm = GetRealmLevel(a);
            return realm > 0;
        }

        public int GetRealmLevel(Actor a)
        {
            if (a == null || a.data == null) return 0;
            try
            {
                float realm = 0f;
                a.data.get("fanren_standalone.realm_level", out realm, 0f);
                return (int)realm;
            }
            catch { return 0; }
        }

        public float GetEnergyStrength(Actor a)
        {
            int realm = GetRealmLevel(a);
            if (realm <= 0) return 0f;
            if (realm >= RealmEnergyMap.Length) return RealmEnergyMap[RealmEnergyMap.Length - 1];
            return RealmEnergyMap[realm];
        }

        public float ConvertToQi(Actor a)
        {
            return GetEnergyStrength(a);
        }
    }

    public class IncenseDivineAdapter : SuperMechModAdapters.IModAdapter
    {
        public string ModName => "香火神道";
        public bool IsAvailable => SuperMechModAdapters.IsTypeLoaded("IncenseAndFire.IncenseDivine")
            || SuperMechModAdapters.IsTypeLoaded("IncenseAndFire.Main");

        public bool Detect(Actor a)
        {
            if (a == null || a.stats == null) return false;
            return a.stats["Incenseandfire"] > 0f || a.stats["Believers"] > 0f;
        }

        public int GetRealmLevel(Actor a)
        {
            if (a == null || a.stats == null) return 0;
            float incense = a.stats["Incenseandfire"];
            if (incense >= 50000f) return 10;
            if (incense >= 10000f) return 9;
            if (incense >= 5000f) return 8;
            if (incense >= 2000f) return 7;
            if (incense >= 1000f) return 6;
            if (incense >= 500f) return 5;
            if (incense >= 200f) return 4;
            if (incense >= 100f) return 3;
            if (incense >= 50f) return 2;
            if (incense >= 10f) return 1;
            return 0;
        }

        public float GetEnergyStrength(Actor a)
        {
            int realm = GetRealmLevel(a);
            if (realm <= 0) return 0f;
            float[] energyMap = {
                0f, 50f, 200f, 500f, 1000f, 3000f,
                8000f, 20000f, 50000f, 100000f, 150000f
            };
            if (realm >= energyMap.Length) return energyMap[energyMap.Length - 1];
            return energyMap[realm];
        }

        public float ConvertToQi(Actor a)
        {
            return GetEnergyStrength(a);
        }
    }

    public class WesternFantasyAdapter : SuperMechModAdapters.IModAdapter
    {
        public string ModName => "西幻世界";
        public bool IsAvailable => SuperMechModAdapters.IsTypeLoaded("WesternFantasy.WesternFantasy")
            || SuperMechModAdapters.IsTypeLoaded("WesternFantasy.Main");

        private static readonly string[] CareerPrefixes = {
            "enchanter", "pastor", "Paladin", "valiantgeneral",
            "Ranger", "Assassin", "Summoner", "minstrel",
            "warlock", "alchemist", "barbarian"
        };

        private static readonly float[] GradeEnergyMap = {
            0f,       // 0 非职业者
            100f,     // 1 一阶
            500f,     // 2 二阶
            2000f,    // 3 三阶
            8000f,    // 4 四阶
            25000f,   // 5 五阶
            70000f,   // 6 六阶
            150000f   // 7 七阶（≈X阶148800）
        };

        public bool Detect(Actor a)
        {
            if (a == null) return false;
            return GetRealmLevel(a) > 0 || (a.stats != null && a.stats["MagicalEnergy"] > 0f);
        }

        public int GetRealmLevel(Actor a)
        {
            if (a == null) return 0;
            int maxGrade = 0;
            foreach (string prefix in CareerPrefixes)
            {
                for (int grade = 7; grade >= 1; grade--)
                {
                    if (a.hasTrait(prefix + grade))
                    {
                        if (grade > maxGrade) maxGrade = grade;
                        break;
                    }
                }
            }
            return maxGrade;
        }

        public float GetEnergyStrength(Actor a)
        {
            int grade = GetRealmLevel(a);
            if (grade <= 0)
            {
                if (a != null && a.stats != null && a.stats["MagicalEnergy"] > 0f)
                    return 50f;
                return 0f;
            }
            if (grade >= GradeEnergyMap.Length) return GradeEnergyMap[GradeEnergyMap.Length - 1];
            return GradeEnergyMap[grade];
        }

        public float ConvertToQi(Actor a)
        {
            return GetEnergyStrength(a);
        }
    }

    public class ZhutianAdapter : SuperMechModAdapters.IModAdapter
    {
        public string ModName => "诸天神座";
        public bool IsAvailable => SuperMechModAdapters.IsTypeLoaded("WanXiang.WanXiangEnergy")
            || SuperMechModAdapters.IsTypeLoaded("ZhuTian.Main");

        public bool Detect(Actor a)
        {
            if (a == null || a.stats == null) return false;
            return a.stats["WanXiangEnergyMax"] > 0f || a.stats["WanXiangEnergy"] > 0f;
        }

        public int GetRealmLevel(Actor a)
        {
            if (a == null || a.stats == null) return 0;
            float maxEnergy = a.stats["WanXiangEnergyMax"];
            if (maxEnergy >= 10000f) return 10;
            if (maxEnergy >= 5000f) return 9;
            if (maxEnergy >= 2000f) return 8;
            if (maxEnergy >= 1000f) return 7;
            if (maxEnergy >= 500f) return 6;
            if (maxEnergy >= 300f) return 5;
            if (maxEnergy >= 200f) return 4;
            if (maxEnergy >= 150f) return 3;
            if (maxEnergy >= 120f) return 2;
            if (maxEnergy >= 100f) return 1;
            return 0;
        }

        public float GetEnergyStrength(Actor a)
        {
            int realm = GetRealmLevel(a);
            if (realm <= 0) return 0f;
            float[] energyMap = {
                0f, 100f, 300f, 800f, 2000f, 5000f,
                12000f, 30000f, 70000f, 120000f, 160000f
            };
            if (realm >= energyMap.Length) return energyMap[energyMap.Length - 1];
            return energyMap[realm];
        }

        public float ConvertToQi(Actor a)
        {
            return GetEnergyStrength(a);
        }
    }

    public class GuimiAdapter : SuperMechModAdapters.IModAdapter
    {
        public string ModName => "诡秘之主";
        public bool IsAvailable => SuperMechModAdapters.IsTypeLoaded("Guimi.GuimiMain")
            || SuperMechModAdapters.IsTypeLoaded("LordOfMysteries.Main");

        private static readonly string[] SequenceTraits = {
            "XuLie94", "XuLie93", "XuLie92", "XuLie91",
            "XuLie9", "XuLie8", "XuLie7", "XuLie6",
            "XuLie5", "XuLie4", "XuLie3", "XuLie2", "XuLie1"
        };

        private static readonly float[] SequenceEnergyMap = {
            160000f, // 旧日尊主（≈X阶148800）
            150000f, // 神明本尊
            135000f, // 天使之王
            120000f, // 大天使
            100000f, // 序列1
            60000f,  // 序列2
            30000f,  // 序列3
            12000f,  // 序列4
            5000f,   // 序列5
            2000f,   // 序列6
            800f,    // 序列7
            300f,    // 序列8
            100f     // 序列9
        };

        public bool Detect(Actor a)
        {
            if (a == null) return false;
            return GetRealmLevel(a) > 0 || (a.stats != null && a.stats["XuLie"] > 0f);
        }

        public int GetRealmLevel(Actor a)
        {
            if (a == null) return 0;
            for (int i = 0; i < SequenceTraits.Length; i++)
            {
                if (a.hasTrait(SequenceTraits[i])) return SequenceTraits.Length - i;
            }
            return 0;
        }

        public float GetEnergyStrength(Actor a)
        {
            int level = GetRealmLevel(a);
            if (level <= 0)
            {
                if (a != null && a.stats != null && a.stats["XuLie"] > 0f)
                    return 50f;
                return 0f;
            }
            int idx = SequenceTraits.Length - level;
            if (idx >= 0 && idx < SequenceEnergyMap.Length)
                return SequenceEnergyMap[idx];
            return SequenceEnergyMap[SequenceEnergyMap.Length - 1];
        }

        public float ConvertToQi(Actor a)
        {
            return GetEnergyStrength(a);
        }
    }
}
