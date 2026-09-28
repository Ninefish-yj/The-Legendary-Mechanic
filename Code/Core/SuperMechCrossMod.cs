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
            "star_power", "cosmic", "origin", "source_energy"
        };

        public static readonly string[] ExcludeKeywords = {
            "max", "cost", "regen", "rate", "gain",
            "multiplier", "bonus", "damage", "defense",
            "resistance", "penetration", "crit", "speed",
            "vitality", "essence", "stamina", "life_force",
            "level", "exp", "experience", "point"
        };

        private static HashSet<string> _cachedEnergyStats;
        private static bool _cacheDirty = true;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            RebuildEnergyStatCache();
            Debug.Log("[超神机械师] 跨模组适配层初始化：动态扫描能量属性");
        }

        public static void RebuildEnergyStatCache()
        {
            _cachedEnergyStats = new HashSet<string>();
            var allStats = AssetManager.base_stats_library;
            if (allStats == null) return;

            foreach (var kv in allStats.dict)
            {
                string id = kv.Key.ToLower();
                if (IsLikelyEnergyStat(id))
                {
                    _cachedEnergyStats.Add(kv.Key);
                }
            }
            _cacheDirty = false;
            Debug.Log($"[超神机械师] 能量属性缓存重建：检测到{_cachedEnergyStats.Count}个可能的能量属性");
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

        public static float DetectExternalEnergy(Actor a)
        {
            if (a == null || a.stats == null) return 0f;
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();

            float total = 0f;
            int found = 0;

            foreach (string statId in _cachedEnergyStats)
            {
                if (statId.StartsWith("sm_")) continue;
                float val = a.stats[statId];
                if (val > 1f)
                {
                    total += val;
                    found++;
                }
            }

            if (found > 0)
            {
                return total / found;
            }
            return 0f;
        }

        public static void SyncExternalEnergyToQi(Actor a)
        {
            if (a == null) return;
            if (!SuperMechConfig.CrossModEnergySync) return;

            float externalEnergy = DetectExternalEnergy(a);
            if (externalEnergy <= 0f) return;

            float currentQi = SuperMechQi.GetQiMax(a);
            if (currentQi <= 0f)
            {
                float converted = externalEnergy * SuperMechConfig.CrossModEnergyRatio;
                if (converted > 10f)
                {
                    SuperMechQi.SetQiMax(a, converted);
                    SuperMechQi.SetQi(a, converted);
                    Debug.Log($"[超神机械师] 跨模组能量同步：{a.name} 检测到外部能量{externalEnergy:F0}，转换为气力{converted:F0}");
                }
            }
        }

        public static float GetUniversalPowerLevel(Actor a)
        {
            if (a == null || a.stats == null) return 0f;

            float qi = SuperMechQi.GetQiMax(a);
            if (qi > 0f) return qi;

            float external = DetectExternalEnergy(a);
            if (external > 0f) return external * SuperMechConfig.CrossModEnergyRatio;

            float dmg = a.stats["damage"];
            float hp = a.stats["health"];
            float basePower = Mathf.Sqrt(dmg * dmg + hp * hp * 0.1f);
            return basePower * 10f;
        }

        public static bool HasExternalModSystem(Actor a)
        {
            if (a == null || a.stats == null) return false;
            foreach (string statId in KnownEnergyStats)
            {
                if (a.stats[statId] > 0f) return true;
            }
            return false;
        }

        public static string GetDetectedMods()
        {
            if (_cacheDirty || _cachedEnergyStats == null) RebuildEnergyStatCache();
            var mods = new List<string>();
            foreach (string stat in _cachedEnergyStats)
            {
                string low = stat.ToLower();
                if (low.Contains("mana") || low.Contains("magic")) mods.Add("魔法类");
                else if (low.Contains("cultivation") || low.Contains("xianqi") || low.Contains("spiritual")) mods.Add("修仙类");
                else if (low.Contains("divine") || low.Contains("faith")) mods.Add("神权类");
                else if (low.Contains("chakra") || low.Contains("nen") || low.Contains("aura")) mods.Add("异能类");
                else mods.Add(stat);
            }
            return mods.Count > 0 ? string.Join("、", mods) : "无";
        }

        public static void TickEnergySync()
        {
            if (!SuperMechConfig.CrossModEnergySync) return;
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
