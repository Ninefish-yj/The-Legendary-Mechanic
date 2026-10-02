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
            float ConvertToOnar(Actor a);  // 按原著欧纳校准
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
                catch
                {
                    // 反射类型检测：类型不存在或加载失败时静默返回false，这是预期行为
                }
            }
            return false;
        }

        public static IReadOnlyList<string> DisabledAdapters => _disabledAdapters;

        /// <summary>原著欧纳能级映射表（按境界等级索引，0~20级）
        /// 将各模组的境界/序列/神格等级统一映射到原著欧纳值
        /// 原著阈值：F=1~2, E=100, D=800, C=2000, B=6000, A=10000~33000, S=星系级, X=148800+
        /// </summary>
        public static readonly float[] RealmToOnarTable =
        {
            10f,      // 0: 入门/练气1层（F级）
            50f,      // 1: 初级/练气后期
            100f,     // 2: 筑基/正式（E级，原著E=100）
            300f,     // 3: 筑基后期
            600f,     // 4: 金丹初期（E+级，原著E+=600）
            800f,     // 5: 金丹圆满（D级，原著D=800）
            1200f,    // 6: 元婴初期
            1600f,    // 7: 元婴圆满（D+级，原著D+=1600）
            2000f,    // 8: 化神（C级，原著C=2000）
            4000f,    // 9: 炼虚
            6000f,    // 10: 合体（B级，原著B=6000+）
            10000f,   // 11: 大乘（A级下限，原著A=10000）
            15000f,   // 12: 渡劫初期
            20000f,   // 13: 渡劫中期（A+级下限，原著A+=20000）
            25000f,   // 14: 渡劫后期
            33000f,   // 15: 渡劫圆满/仙人（A级上限，原著A=33000）
            50000f,   // 16: 真仙/地仙（S级下限，星系级）
            80000f,   // 17: 金仙/天仙
            100000f,  // 18: 太乙/大神
            120000f,  // 19: 大罗/神王
            148800f   // 20: 道祖/至高（X级，原著X=148800+）
        };

        /// <summary>将境界等级转换为原著欧纳值（统一校准）</summary>
        public static float ConvertRealmToOnar(int realmLevel)
        {
            if (realmLevel <= 0) return 10f;
            if (realmLevel >= RealmToOnarTable.Length) return RealmToOnarTable[RealmToOnarTable.Length - 1];
            return RealmToOnarTable[realmLevel];
        }

        /// <summary>获取跨模组单位的原著欧纳能级（统一校准入口）</summary>
        public static float GetCrossModOnar(Actor a)
        {
            if (a == null) return 0f;
            var adapter = DetectAdapter(a);
            if (adapter == null) return 0f;
            int realmLevel = adapter.GetRealmLevel(a);
            return ConvertRealmToOnar(realmLevel);
        }

        /// <summary>检测单位所属的跨模组适配器</summary>
        public static IModAdapter DetectAdapter(Actor a)
        {
            if (a == null || _adapters == null) return null;
            foreach (var adapter in _adapters)
            {
                if (!adapter.IsAvailable) continue;
                if (adapter.Detect(a)) return adapter;
            }
            return null;
        }

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

        /// <summary>将跨模组单位的能量转换为原著欧纳值（统一校准）</summary>
        public static float ConvertModToOnar(Actor a)
        {
            var adapter = DetectMod(a);
            if (adapter != null) return adapter.ConvertToOnar(a);
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

        public float ConvertToOnar(Actor a)
        {
            int realm = GetRealmLevel(a);
            // 凡人修仙传境界映射：练气=2, 筑基=4, 金丹=5, 元婴=7, 化神=8, 炼虚=9, 合体=10, 大乘=11, 渡劫=13, 真仙=16
            int[] realmMap = { 0, 2, 4, 5, 7, 8, 9, 10, 11, 13, 16 };
            int mappedLevel = realm < realmMap.Length ? realmMap[realm] : 20;
            return SuperMechModAdapters.ConvertRealmToOnar(mappedLevel);
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

        public float ConvertToOnar(Actor a)
        {
            int realm = GetRealmLevel(a);
            // 香火神道境界映射：凡神=4, 地神=6, 天神=8, 主神=11, 神王=14, 至高神=18
            int[] realmMap = { 0, 4, 6, 8, 11, 14, 16, 18, 20 };
            int mappedLevel = realm < realmMap.Length ? realmMap[realm] : 20;
            return SuperMechModAdapters.ConvertRealmToOnar(mappedLevel);
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

        public float ConvertToOnar(Actor a)
        {
            int realm = GetRealmLevel(a);
            // 西幻等级映射：1-5级=2, 6-10级=4, 11-15级=6, 16-20级=8, 传奇=11, 半神=14, 神=18
            int mappedLevel = realm <= 0 ? 0 :
                              realm <= 5 ? 2 :
                              realm <= 10 ? 4 :
                              realm <= 15 ? 6 :
                              realm <= 20 ? 8 :
                              realm <= 25 ? 11 :
                              realm <= 30 ? 14 : 18;
            return SuperMechModAdapters.ConvertRealmToOnar(mappedLevel);
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

        public float ConvertToOnar(Actor a)
        {
            int realm = GetRealmLevel(a);
            // 诸天神座境界映射：凡人=2, 超凡=4, 圣域=6, 半神=8, 真神=11, 主神=14, 至高神=18
            int[] realmMap = { 0, 2, 4, 6, 8, 11, 14, 16, 18, 20 };
            int mappedLevel = realm < realmMap.Length ? realmMap[realm] : 20;
            return SuperMechModAdapters.ConvertRealmToOnar(mappedLevel);
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

        public float ConvertToOnar(Actor a)
        {
            int level = GetRealmLevel(a);
            if (level <= 0) return 10f;
            // 诡秘之主序列映射：序列9=2, 序列8=3, 序列7=4, 序列6=5, 序列5=7, 序列4=9, 序列3=11, 序列2=14, 序列1=17, 序列0=19, 旧日=20
            int[] sequenceMap = { 0, 20, 19, 17, 14, 11, 9, 7, 5, 4, 3, 2, 2, 2 };
            int idx = SequenceTraits.Length - level;
            int mappedLevel = idx >= 0 && idx < sequenceMap.Length ? sequenceMap[idx] : 20;
            return SuperMechModAdapters.ConvertRealmToOnar(mappedLevel);
        }
    }
}
