using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>文化AI系统（v0.144.0）
    /// 文明文化影响力传播、文化同化、文化冲突
    /// 基于文明等级、超能者数量、距离传播文化
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechCultureAI
    {
        private static readonly Dictionary<long, float> _culturePower = new Dictionary<long, float>();
        private static readonly Dictionary<long, string> _dominantCulture = new Dictionary<long, string>();
        private const float CultureInterval = 300f;  // 每300时间单位传播一次文化

        /// <summary>文化AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            // 1. 计算各文明文化实力
            CalculateCulturePower();

            // 2. 文化传播（每300时间单位）
            if ((int)(now / CultureInterval) != (int)((now - 1) / CultureInterval))
            {
                SpreadCulture();
            }

            // 3. 文化同化（低文化文明被高文化文明影响）
            AssimilateCulture();
        }

        /// <summary>计算文明文化实力</summary>
        private static void CalculateCulturePower()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            foreach (var k in World.world.kingdoms.list)
            {
                if (k == null || k.wild || !k.isCiv()) continue;
                long kid = k.id;

                float power = 5f; // 基础文化力
                power += (int)SuperMechCivilization.GetLevel(k) * 10f; // 文明等级加成

                // 超能者数量加成
                if (World.world.units != null)
                {
                    int superCount = 0;
                    foreach (var a in World.world.units.units_only_alive)
                    {
                        if (a != null && a.isAlive() && a.kingdom == k)
                        {
                            int rank = SuperMechAdvancement.GetExactRankIndex(a);
                            if (rank >= 10) superCount += 3; // S阶+3
                            else if (rank >= 8) superCount += 1; // A阶+1
                        }
                    }
                    power += superCount * 2f;
                }

                // 圣所访问加成（简化：文明等级已包含）
                // power += SuperMechSanctuary.GetVisitedCount(kid) * 0.5f;

                _culturePower[kid] = power;
            }
        }

        /// <summary>文化传播</summary>
        private static void SpreadCulture()
        {
            if (World.world == null || World.world.kingdoms == null) return;

            var kingdoms = World.world.kingdoms.list;
            for (int i = 0; i < kingdoms.Count; i++)
            {
                var k1 = kingdoms[i];
                if (k1 == null || k1.wild || !k1.isCiv()) continue;
                if (!_culturePower.TryGetValue(k1.id, out float p1)) continue;

                for (int j = 0; j < kingdoms.Count; j++)
                {
                    if (i == j) continue;
                    var k2 = kingdoms[j];
                    if (k2 == null || k2.wild || !k2.isCiv()) continue;

                    // 距离衰减
                    float dist = GetCapitalDistance(k1, k2);
                    if (dist > 150f) continue;

                    float influence = p1 / Mathf.Max(1f, dist * 0.5f);
                    if (influence > 1f && Random.value < influence * 0.01f)
                    {
                        // 文化传播成功：k2的主导文化可能变为k1
                        if (!_dominantCulture.ContainsKey(k2.id) || Random.value < 0.3f)
                        {
                            _dominantCulture[k2.id] = k1.name;
                            if (SuperMechConfig.LogVerbose && Random.value < 0.1f)
                                Debug.Log($"[超神机械师]【文化AI】{k1.name} 文化传播到 {k2.name}");
                        }
                    }
                }
            }
        }

        /// <summary>文化同化</summary>
        private static void AssimilateCulture()
        {
            if (World.world == null || World.world.units == null) return;

            // 低文化文明的单位有概率被高文化文明同化（改变职业倾向）
            foreach (var a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive() || a.kingdom == null) continue;
                if (!_culturePower.TryGetValue(a.kingdom.id, out float myPower)) continue;
                if (myPower > 20f) continue; // 文化力足够的文明不会被同化

                // 寻找邻近的高文化文明
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.id == a.kingdom.id || k.wild || !k.isCiv()) continue;
                    if (!_culturePower.TryGetValue(k.id, out float otherPower)) continue;
                    if (otherPower < myPower * 2f) continue;

                    float dist = GetUnitKingdomDistance(a, k);
                    if (dist < 30f && Random.value < 0.001f)
                    {
                        // 文化同化：单位获得知识加成
                        SuperMechAwakened.SetXp(a, SuperMechAwakened.GetXp(a) + 5f);
                        break;
                    }
                }
            }
        }

        /// <summary>获取首都距离</summary>
        private static float GetCapitalDistance(Kingdom k1, Kingdom k2)
        {
            if (k1?.capital == null || k2?.capital == null) return 999f;
            return Mathf.Abs(k1.capital.x - k2.capital.x) + Mathf.Abs(k1.capital.y - k2.capital.y);
        }

        /// <summary>获取单位到文明首都距离</summary>
        private static float GetUnitKingdomDistance(Actor a, Kingdom k)
        {
            if (a?.current_tile == null || k?.capital == null) return 999f;
            return Mathf.Abs(a.current_tile.x - k.capital.x) + Mathf.Abs(a.current_tile.y - k.capital.y);
        }

        /// <summary>获取文明文化实力（供其他系统查询）</summary>
        public static float GetCulturePower(long kingdomId)
        {
            return _culturePower.TryGetValue(kingdomId, out float p) ? p : 0f;
        }

        /// <summary>获取文明主导文化（供其他系统查询）</summary>
        public static string GetDominantCulture(long kingdomId)
        {
            return _dominantCulture.TryGetValue(kingdomId, out string c) ? c : LocalizedTextManager.getText("sm_culture_native");
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _culturePower.Clear();
            _dominantCulture.Clear();
        }
    }
}
