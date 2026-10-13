using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>文明战略AI系统（v0.141.0）
    /// 文明智能发展策略：根据职业分布确定战略倾向，提供对应加成
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechCivStrategyAI
    {
        private static readonly Dictionary<long, float> _lastStrategy = new Dictionary<long, float>();
        private const float StrategyInterval = 600f;  // 每600时间单位进行一次战略决策

        // 文明战略倾向
        private enum StrategyType
        {
            Expansion,    // 扩张型：优先军事和领土
            Technology,   // 科技型：优先科技研究
            Economy,      // 经济型：优先经济和贸易
            Cultural,     // 文化型：优先超能者培养
            Balanced      // 平衡型
        }

        private static readonly Dictionary<long, StrategyType> _civStrategy = new Dictionary<long, StrategyType>();
        private static readonly Dictionary<long, float> _strategyBonus = new Dictionary<long, float>(); // 战略加成倍率

        /// <summary>文明战略AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.kingdoms == null) return;
            double now = World.world.getCurWorldTime();

            int processed = 0;
            foreach (var kingdom in World.world.kingdoms.list)
            {
                if (kingdom == null || kingdom.id == null || kingdom.wild || !kingdom.isCiv()) continue;
                long kid = kingdom.id;

                if (!_lastStrategy.TryGetValue(kid, out float last) || now - last >= StrategyInterval)
                {
                    if (processed >= 3) break; // 每tick最多3个文明决策
                    _lastStrategy[kid] = (float)now;
                    MakeStrategyDecision(kingdom);
                    processed++;
                }

                // 应用战略加成
                ApplyStrategyBonus(kingdom);
            }
        }

        /// <summary>制定文明战略决策</summary>
        private static void MakeStrategyDecision(Kingdom kingdom)
        {
            if (kingdom == null) return;
            long kid = kingdom.id;

            // 确定文明战略倾向（基于职业分布）
            StrategyType strategy = DetermineStrategy(kingdom);
            _civStrategy[kid] = strategy;
            _strategyBonus[kid] = 1.1f; // 战略加成10%

            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师]【文明战略】{kingdom.name} 确定战略：{GetStrategyName(kid)}");
        }

        /// <summary>根据职业分布确定战略倾向</summary>
        private static StrategyType DetermineStrategy(Kingdom kingdom)
        {
            if (kingdom == null || World.world == null || World.world.units == null)
                return StrategyType.Balanced;

            int mechCount = 0, martialCount = 0, mageCount = 0, otherCount = 0;
            foreach (var a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive() || a.kingdom != kingdom) continue;
                string cls = SuperMechBranch.GetClass(a);
                if (cls == SuperMechTraits.ClassMech) mechCount++;
                else if (cls == SuperMechTraits.ClassMartial) martialCount++;
                else if (cls == SuperMechTraits.ClassMage) mageCount++;
                else otherCount++;
            }

            int total = mechCount + martialCount + mageCount + otherCount;
            if (total == 0) return StrategyType.Balanced;

            // 机械师多→科技型，武道多→扩张型，法师多→文化型
            if (mechCount > total * 0.4f) return StrategyType.Technology;
            if (martialCount > total * 0.4f) return StrategyType.Expansion;
            if (mageCount > total * 0.4f) return StrategyType.Cultural;

            return StrategyType.Balanced;
        }

        /// <summary>应用战略加成</summary>
        private static void ApplyStrategyBonus(Kingdom kingdom)
        {
            if (kingdom == null || World.world == null || World.world.units == null) return;
            long kid = kingdom.id;

            if (!_civStrategy.TryGetValue(kid, out var strategy)) return;
            if (!_strategyBonus.TryGetValue(kid, out float bonus)) bonus = 1.1f;

            // 根据战略类型给对应职业单位提供经验加成
            foreach (var a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive() || a.kingdom != kingdom) continue;
                string cls = SuperMechBranch.GetClass(a);

                bool applyBonus = false;
                switch (strategy)
                {
                    case StrategyType.Expansion:
                        applyBonus = (cls == SuperMechTraits.ClassMartial);
                        break;
                    case StrategyType.Technology:
                        applyBonus = (cls == SuperMechTraits.ClassMech);
                        break;
                    case StrategyType.Economy:
                        applyBonus = SuperMechSupermA.IsSuperA(a);
                        break;
                    case StrategyType.Cultural:
                        applyBonus = (cls == SuperMechTraits.ClassMage || cls == SuperMechTraits.ClassPsi);
                        break;
                    case StrategyType.Balanced:
                        applyBonus = true; // 平衡型所有单位少量加成
                        bonus = 1.05f;
                        break;
                }

                if (applyBonus && Random.value < 0.05f) // 5%概率每tick获得经验
                {
                    float xpGain = 1f * bonus;
                    SuperMechAwakened.SetXp(a, SuperMechAwakened.GetXp(a) + xpGain);
                }
            }
        }

        /// <summary>获取文明战略类型（供其他系统查询）</summary>
        public static string GetStrategyName(long kingdomId)
        {
            if (_civStrategy.TryGetValue(kingdomId, out var strategy))
            {
                switch (strategy)
                {
                    case StrategyType.Expansion: return LocalizedTextManager.getText("sm_civ_strategy_expansion");
                    case StrategyType.Technology: return LocalizedTextManager.getText("sm_civ_strategy_technology");
                    case StrategyType.Economy: return LocalizedTextManager.getText("sm_civ_strategy_economy");
                    case StrategyType.Cultural: return LocalizedTextManager.getText("sm_civ_strategy_cultural");
                    case StrategyType.Balanced: return LocalizedTextManager.getText("sm_civ_strategy_balanced");
                }
            }
            return LocalizedTextManager.getText("sm_civ_strategy_undefined");
        }

        /// <summary>获取文明战略加成倍率</summary>
        public static float GetStrategyBonus(long kingdomId)
        {
            return _strategyBonus.TryGetValue(kingdomId, out float bonus) ? bonus : 1.0f;
        }

        /// <summary>清理死亡文明数据</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> aliveKingdoms)
        {
            int removed = 0;
            var toRemove = new List<long>();
            foreach (var kv in _lastStrategy)
                if (!aliveKingdoms.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var id in toRemove)
            {
                _lastStrategy.Remove(id);
                _civStrategy.Remove(id);
                _strategyBonus.Remove(id);
                removed++;
            }
            return removed;
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _lastStrategy.Clear();
            _civStrategy.Clear();
            _strategyBonus.Clear();
        }
    }
}
