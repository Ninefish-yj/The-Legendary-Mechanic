using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>外交AI系统（v0.143.0）
    /// 文明间智能外交：结盟/宣战/停战/贸易协定
    /// 基于实力对比、地理位置、历史关系决策
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechDiplomacyAI
    {
        private static readonly Dictionary<string, float> _lastDiplomacy = new Dictionary<string, float>();
        private const float DiplomacyInterval = 400f;  // 每400时间单位进行一次外交决策

        // 外交关系类型
        public enum RelationType
        {
            Hostile,    // 敌对
            Cold,       // 冷淡
            Neutral,    // 中立
            Friendly,   // 友好
            Allied      // 同盟
        }

        private static readonly Dictionary<string, RelationType> _relations = new Dictionary<string, RelationType>();

        /// <summary>外交AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            var kingdoms = World.world.kingdoms.list;
            if (kingdoms == null || kingdoms.Count < 2) return;

            int processed = 0;
            for (int i = 0; i < kingdoms.Count; i++)
            {
                var k1 = kingdoms[i];
                if (k1 == null || k1.wild || !k1.isCiv()) continue;

                for (int j = i + 1; j < kingdoms.Count; j++)
                {
                    var k2 = kingdoms[j];
                    if (k2 == null || k2.wild || !k2.isCiv()) continue;

                    string key = GetRelationKey(k1, k2);
                    if (!_lastDiplomacy.TryGetValue(key, out float last) || now - last >= DiplomacyInterval)
                    {
                        if (processed >= 2) break; // 每tick最多2对外交决策
                        _lastDiplomacy[key] = (float)now;
                        MakeDiplomacyDecision(k1, k2);
                        processed++;
                    }
                }
                if (processed >= 2) break;
            }
        }

        /// <summary>外交决策</summary>
        private static void MakeDiplomacyDecision(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null) return;

            // 1. 计算实力对比
            float power1 = GetKingdomPower(k1);
            float power2 = GetKingdomPower(k2);
            float powerRatio = power1 > 0 ? Mathf.Max(power1, power2) / Mathf.Max(1f, Mathf.Min(power1, power2)) : 1f;

            // 2. 计算距离
            float dist = GetKingdomDistance(k1, k2);

            // 3. 获取当前关系
            string key = GetRelationKey(k1, k2);
            if (!_relations.TryGetValue(key, out var relation))
                relation = RelationType.Neutral;

            // 4. 决策逻辑
            if (relation == RelationType.Hostile)
            {
                // 敌对中：实力差距大或距离远时考虑停战
                if (powerRatio > 3f || dist > 100f || Random.value < 0.1f)
                {
                    SetRelation(k1, k2, RelationType.Cold);
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师]【外交AI】{k1.name} 与 {k2.name} 停战");
                }
            }
            else if (relation == RelationType.Allied)
            {
                // 同盟中：实力差距过大时可能破裂
                if (powerRatio > 5f && Random.value < 0.05f)
                {
                    SetRelation(k1, k2, RelationType.Neutral);
                    if (SuperMechConfig.LogVerbose)
                        Debug.Log($"[超神机械师]【外交AI】{k1.name} 与 {k2.name} 同盟破裂");
                }
            }
            else
            {
                // 非敌对非同盟：根据情况决定
                if (dist < 50f && powerRatio < 2.5f)
                {
                    // 邻近且实力相当：可能宣战
                    if (Random.value < 0.08f)
                    {
                        SetRelation(k1, k2, RelationType.Hostile);
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师]【外交AI】{k1.name} 向 {k2.name} 宣战");
                    }
                }
                else if (dist > 80f || powerRatio > 2f)
                {
                    // 距离远或实力差距大：可能结盟
                    if (Random.value < 0.05f)
                    {
                        SetRelation(k1, k2, RelationType.Allied);
                        if (SuperMechConfig.LogVerbose)
                            Debug.Log($"[超神机械师]【外交AI】{k1.name} 与 {k2.name} 结盟");
                    }
                }
                else if (Random.value < 0.1f)
                {
                    // 随机改善关系
                    var newRelation = relation == RelationType.Cold ? RelationType.Neutral : RelationType.Friendly;
                    SetRelation(k1, k2, newRelation);
                }
            }
        }

        /// <summary>计算文明实力（人口+超能者数量+科技等级）</summary>
        private static float GetKingdomPower(Kingdom k)
        {
            if (k == null) return 0f;
            float power = 10f; // 基础实力

            // 人口
            if (k.units != null) power += k.units.Count * 0.5f;

            // 超能者数量
            if (World.world != null && World.world.units != null)
            {
                int superCount = 0;
                foreach (var a in World.world.units.units_only_alive)
                {
                    if (a != null && a.isAlive() && a.kingdom == k && SuperMechAdvancement.GetExactRankIndex(a) >= 8)
                        superCount++;
                }
                power += superCount * 5f;
            }

            // 文明等级
            power += (int)(int)SuperMechCivilization.GetCivLevelFromKingdom(k) * 20f;

            return power;
        }

        /// <summary>计算文明间距离（首都距离）</summary>
        private static float GetKingdomDistance(Kingdom k1, Kingdom k2)
        {
            if (k1 == null || k2 == null || k1.capital == null || k2.capital == null) return 999f;
            return Mathf.Abs(k1.capital.getTile().x - k2.capital.getTile().x) + Mathf.Abs(k1.capital.getTile().y - k2.capital.getTile().y);
        }

        /// <summary>获取关系key</summary>
        private static string GetRelationKey(Kingdom k1, Kingdom k2)
        {
            string id1 = k1?.name ?? "k1";
            string id2 = k2?.name ?? "k2";
            return id1.CompareTo(id2) < 0 ? $"{id1}|{id2}" : $"{id2}|{id1}";
        }

        /// <summary>设置关系</summary>
        private static void SetRelation(Kingdom k1, Kingdom k2, RelationType relation)
        {
            string key = GetRelationKey(k1, k2);
            _relations[key] = relation;
        }

        /// <summary>获取两文明关系（供其他系统查询）</summary>
        public static RelationType GetRelation(Kingdom k1, Kingdom k2)
        {
            string key = GetRelationKey(k1, k2);
            return _relations.TryGetValue(key, out var r) ? r : RelationType.Neutral;
        }

        /// <summary>判断两文明是否敌对</summary>
        public static bool IsHostile(Kingdom k1, Kingdom k2)
        {
            return GetRelation(k1, k2) == RelationType.Hostile;
        }

        /// <summary>判断两文明是否同盟</summary>
        public static bool IsAllied(Kingdom k1, Kingdom k2)
        {
            return GetRelation(k1, k2) == RelationType.Allied;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _lastDiplomacy.Clear();
            _relations.Clear();
        }
    }
}
