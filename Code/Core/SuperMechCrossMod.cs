using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechCrossMod
    {
        private static bool _initialized;
        private static readonly Dictionary<string, float> _modEnergyCache = new Dictionary<string, float>();

        public static readonly string[] KnownEnergyStats = {
            "mana", "magic", "mp", "sp", "stamina",
            "cultivation", "qi", "xianqi", "spiritual_energy",
            "divine_power", "faith", "soul_power",
            "chakra", "nen", "ki", "aura",
            "essence", "vitality", "life_force"
        };

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Debug.Log("[超神机械师] 跨模组适配层初始化：检测其他模组能量体系");
        }

        public static float DetectExternalEnergy(Actor a)
        {
            if (a == null || a.stats == null) return 0f;
            float total = 0f;
            int found = 0;

            foreach (string statId in KnownEnergyStats)
            {
                float val = a.stats[statId];
                if (val > 0f)
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
            var mods = new List<string>();
            if (AssetManager.base_stats_library.get("mana") != null) mods.Add("西幻/魔法类");
            if (AssetManager.base_stats_library.get("cultivation") != null) mods.Add("修仙类");
            if (AssetManager.base_stats_library.get("divine_power") != null) mods.Add("神权类");
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
