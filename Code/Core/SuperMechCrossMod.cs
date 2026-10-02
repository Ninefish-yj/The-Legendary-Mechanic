using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechCrossMod
    {
        private static bool _initialized;
        private static readonly Dictionary<string, float> _modEnergyCache = new Dictionary<string, float>();

        public static readonly string[] EnergyKeywords = {
            "mana", "magic", "mp_", "_mp", "spell_power",
            "cultivation", "xianqi", "spiritual", "soul",
            "divine", "faith", "chakra", "nen",
            "aura", "battle_qi", "true_qi", "primordial",
            "star_power", "cosmic", "origin", "source_energy",
            "incense", "fire", "worship", "believer",
            "merit", "virtue", "karma", "luck_power",
            "battle_power", "combat_power", "power_level",
            "internal_force", "zhenqi", "jingqi", "shenhun"
        };

        public static readonly string[] ExcludeKeywords = {
            "max", "cost", "regen", "rate", "gain",
            "multiplier", "bonus", "damage", "defense",
            "resistance", "penetration", "crit", "speed",
            "vitality", "essence", "stamina", "life_force",
            "level", "point", "icon", "texture", "sprite"
        };

        public static readonly string[] DataKeyPatterns = {
            ".exp", ".exp_", ".cultivation", ".qi", ".mana",
            ".energy", ".power", ".realm", ".stage",
            "fanren.standalone.exp", "fanren.standalone.realm_level"
        };

        private static HashSet<string> _cachedEnergyStats;
        private static bool _cacheDirty = true;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            SuperMechModAdapters.Init();
            RebuildEnergyStatCache();
            Debug.Log("[超神机械师] 跨模组适配层初始化：逐个模组适配 + 原版属性排除");
        }

        public static void RebuildEnergyStatCache()
        {
            _cachedEnergyStats = new HashSet<string>();
            var allStats = AssetManager.base_stats_library;
            if (allStats == null) return;

            foreach (var kv in allStats.dict)
            {
                string id = kv.Key.ToLower();
                if (SuperMechModAdapters.IsVanillaStat(id)) continue;
                if (IsLikelyEnergyStat(id))
                {
                    _cachedEnergyStats.Add(kv.Key);
                }
            }
            _cacheDirty = false;
            Debug.Log($"[超神机械师] 能量属性缓存重建：检测到{_cachedEnergyStats.Count}个非原版能量属性");
        }

        private static bool IsLikelyEnergyStat(string id)
        {
            foreach (string excl in ExcludeKeywords)
            {
                if (id.Contains(excl)) return false;
            }
            foreach (string kw in EnergyKeywords)
            {
                if (id.Contains(kw)) return true;
            }
            return false;
        }

        private const float NormalizedBase = 100f;
        private const float LogScaleFactor = 50f;
        private const float MinEnergyForLog = 2f;

        private static readonly string[] MaxSuffixes = {
            "_max", "_maximum", "_max_value", "max_", "maximum_",
            "_limit", "_cap", "_upper", "max"
        };

        public static float DetectExternalEnergy(Actor a)
        {
            if (a == null) return 0f;

            float modPower = SuperMechModAdapters.GetModPower(a);
            if (modPower > 0f) return modPower;

            if (a.stats == null) return 0f;
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();

            float total = 0f;
            int found = 0;

            foreach (string statId in _cachedEnergyStats)
            {
                if (statId.StartsWith("sm_")) continue;
                if (SuperMechModAdapters.IsVanillaStat(statId)) continue;
                float val = a.stats[statId];
                if (val <= MinEnergyForLog) continue;

                float normalized = NormalizeEnergyValue(a, statId, val);
                if (normalized > 0f)
                {
                    total += normalized;
                    found++;
                }
            }

            float dataEnergy = DetectDataEnergy(a);
            if (dataEnergy > 0f)
            {
                total += dataEnergy;
                found++;
            }

            if (found > 0) return total / found;
            return 0f;
        }

        private static float NormalizeEnergyValue(Actor a, string statId, float rawValue)
        {
            float maxVal = FindEnergyMax(a, statId);
            if (maxVal > 0f && rawValue <= maxVal * 1.5f)
            {
                float ratio = Mathf.Clamp01(rawValue / maxVal);
                return ratio * NormalizedBase;
            }

            float systemFactor = GetSystemFactor(statId);
            float logVal = Mathf.Log10(rawValue);
            return Mathf.Max(0f, logVal * LogScaleFactor * systemFactor);
        }

        private static float FindEnergyMax(Actor a, string statId)
        {
            if (a == null || a.stats == null) return 0f;
            string lower = statId.ToLower();

            foreach (string suffix in MaxSuffixes)
            {
                string candidate;
                if (suffix.EndsWith("_"))
                    candidate = suffix + statId;
                else
                    candidate = statId + suffix;

                if (AssetManager.base_stats_library.has(candidate))
                {
                    float maxVal = a.stats[candidate];
                    if (maxVal > 0f) return maxVal;
                }
            }

            if (lower.Contains("mana")) return TryGetStat(a, "mana_max", "max_mana", "mana_maximum");
            if (lower.Contains("qi") || lower.Contains("zhenqi")) return TryGetStat(a, "qi_max", "max_qi", "zhenqi_max");
            if (lower.Contains("cultivation")) return TryGetStat(a, "cultivation_max", "max_cultivation", "cultivation_limit");
            if (lower.Contains("soul") || lower.Contains("spiritual")) return TryGetStat(a, "soul_max", "max_soul", "spiritual_max");

            return 0f;
        }

        private static float TryGetStat(Actor a, params string[] candidates)
        {
            if (a == null || a.stats == null) return 0f;
            foreach (string c in candidates)
            {
                if (AssetManager.base_stats_library.has(c))
                {
                    float v = a.stats[c];
                    if (v > 0f) return v;
                }
            }
            return 0f;
        }

        private static float GetSystemFactor(string statId)
        {
            string lower = statId.ToLower();
            if (lower.Contains("cultivation") || lower.Contains("xianqi") || lower.Contains("spiritual"))
                return 0.7f;
            if (lower.Contains("divine") || lower.Contains("faith") || lower.Contains("worship"))
                return 0.8f;
            if (lower.Contains("mana") || lower.Contains("magic") || lower.Contains("mp"))
                return 1.2f;
            if (lower.Contains("chakra") || lower.Contains("nen") || lower.Contains("aura"))
                return 1.0f;
            if (lower.Contains("battle_qi") || lower.Contains("true_qi") || lower.Contains("internal_force"))
                return 1.1f;
            return 1.0f;
        }

        private static float DetectDataEnergy(Actor a)
        {
            if (a == null || a.data == null) return 0f;
            float total = 0f;
            int found = 0;

            foreach (string pattern in DataKeyPatterns)
            {
                try
                {
                    float val = -1f;
                    a.data.get(pattern, out val, -1f);
                    if (val > MinEnergyForLog)
                    {
                        float logVal = Mathf.Log10(val);
                        total += logVal * LogScaleFactor * 0.8f;
                        found++;
                    }
                }
                catch { }
            }

            if (found > 0) return total / found;
            return 0f;
        }

        public static void SyncExternalEnergyToQi(Actor a)
        {
            if (a == null) return;
            if (!SuperMechConfig.CrossModEnergySync) return;

            float modQi = SuperMechModAdapters.ConvertModToQi(a);
            if (modQi > 0f)
            {
                float currentQi = SuperMechQi.GetQiMax(a);
                if (currentQi <= 0f)
                {
                    SuperMechQi.SetQiMax(a, modQi);
                    SuperMechQi.SetQi(a, modQi);
                    Debug.Log($"[超神机械师] 模组能量同步：{a.name} [{SuperMechModAdapters.GetDetectedModName(a)}] 转换为气力{modQi:F0}");
                }
                return;
            }

            float normalizedEnergy = DetectExternalEnergy(a);
            if (normalizedEnergy <= 0f) return;

            float currentQi2 = SuperMechQi.GetQiMax(a);
            if (currentQi2 <= 0f)
            {
                float converted = normalizedEnergy * SuperMechConfig.CrossModEnergyRatio;
                if (converted > 10f)
                {
                    SuperMechQi.SetQiMax(a, converted);
                    SuperMechQi.SetQi(a, converted);
                    Debug.Log($"[超神机械师] 通用能量同步：{a.name} 归一化能量{normalizedEnergy:F0}，转换为气力{converted:F0}");
                }
            }
        }

        // 原著能级公式(ch3/ch51/ch1039/ch1203/ch1402)：
        // "气力是超能者的基础，很大部分决定了能级与位阶"
        // 三点回归拟合：能级 = 22 × 气力^0.67 + 身体素质×0.15，全局误差±5.7%
        // Lv21: 气力182075→能级78000(ch1039), Lv25: 293475→98510(ch1203), Lv29: 481200→148800(ch1402)
        // 气力边际效益递减（高等级时技能/知识/装备贡献增大）
        // 技能/知识/装备/职业的加成已包含在damage/health等属性中
        public static float GetUniversalPowerLevel(Actor a)
        {
            if (a == null || a.stats == null) return 0f;

            float energyStrength = GetEnergyStrength(a);
            float bodyPower = GetBodyCombatPower(a);

            // 幂函数拟合：气力^0.67，高等级边际效益递减
            float qiOnar = 22f * Mathf.Pow(Mathf.Max(1f, energyStrength), 0.67f);
            return qiOnar + bodyPower * 0.15f;
        }

        // 气力强度：优先用本模组气力，其次用其他模组能量
        private static float GetEnergyStrength(Actor a)
        {
            if (a == null) return 0f;

            float qi = SuperMechQi.GetQiMax(a);
            if (qi > 0f) return qi;

            float modEnergy = SuperMechModAdapters.GetModPower(a);
            if (modEnergy > 0f) return modEnergy;

            float external = DetectExternalEnergy(a);
            if (external > 0f) return external * SuperMechConfig.CrossModEnergyRatio;

            return 0f;
        }

        // 身体素质战力：线性加权各属性
        // damage(伤害)权重10, health(生命)权重0.5, speed(速度)权重20, armor(护甲)权重15
        // 技能/知识/装备/神性蜕变的加成已反映在这些属性中
        private static float GetBodyCombatPower(Actor a)
        {
            if (a == null || a.stats == null) return 0f;
            float dmg = a.stats["damage"];
            float hp = a.stats["health"];
            float spd = a.stats["speed"];
            float armor = a.stats["armor"];
            return dmg * 10f + hp * 0.5f + spd * 20f + armor * 15f;
        }

        public static bool HasExternalModSystem(Actor a)
        {
            if (a == null) return false;
            if (SuperMechModAdapters.DetectMod(a) != null) return true;
            if (a.stats == null) return false;
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();
            foreach (string statId in _cachedEnergyStats)
            {
                if (statId.StartsWith("sm_")) continue;
                if (SuperMechModAdapters.IsVanillaStat(statId)) continue;
                if (a.stats[statId] > 1f) return true;
            }
            return DetectDataEnergy(a) > 0f;
        }

        public static string GetDetectedMods()
        {
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();
            var mods = new List<string>();
            foreach (string stat in _cachedEnergyStats)
            {
                if (SuperMechModAdapters.IsVanillaStat(stat)) continue;
                string low = stat.ToLower();
                if (low.Contains("cultivation") || low.Contains("xianqi") || low.Contains("spiritual"))
                    mods.Add(LocalizedTextManager.getText("sm_crossmod_cultivation"));
                else if (low.Contains("divine") || low.Contains("faith") || low.Contains("incense"))
                    mods.Add(LocalizedTextManager.getText("sm_crossmod_divine"));
                else if (low.Contains("mana") || low.Contains("magic") || low.Contains("mp"))
                    mods.Add(LocalizedTextManager.getText("sm_crossmod_magic"));
                else if (low.Contains("chakra") || low.Contains("nen") || low.Contains("aura"))
                    mods.Add(LocalizedTextManager.getText("sm_crossmod_power"));
                else mods.Add(stat);
            }
            return mods.Count > 0 ? string.Join("、", mods) : LocalizedTextManager.getText("sm_crossmod_none");
        }

        public static void TickEnergySync()
        {
            if (!SuperMechConfig.CrossModEnergySync) return;
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();
            if (_cachedEnergyStats.Count == 0) return;

            var units = World.world.units.units_only_alive;
            if (units == null) return;

            int synced = 0;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                try
                {
                    SyncExternalEnergyToQi(a);
                    synced++;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[超神机械师] 跨模组能量同步异常({a.name}): {e.Message}");
                }
            }
            if (synced > 0 && _tickCounter % 20 == 0)
            {
                Debug.Log($"[超神机械师] 跨模组能量同步完成：{synced}个单位，检测到模组：{GetDetectedMods()}");
            }
            _tickCounter++;
        }

        private static int _tickCounter;
    }
}
