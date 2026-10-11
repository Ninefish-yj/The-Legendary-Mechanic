namespace SuperMech.Code
{
    /// <summary>
    /// 战斗属性加成计算器
    /// 单一职责：只负责计算气力/分支/专精/专长/装备/法师塔的属性加成
    /// 从CombatPatches中提取，遵循单一职责原则
    /// </summary>
    public static class CombatBonusCalculator
    {
        /// <summary>
        /// 应用所有模组属性加成到单位stats
        /// 由Actor_UpdateStats_Postfix调用
        /// </summary>
        public static void ApplyAllBonuses(Actor actor, BaseStats stats)
        {
            if (actor == null || stats == null) return;

            // 1. 气力层次属性加成（原著：气力层次提升带来力量/敏捷/耐力/智力/神秘全面提升）
            ApplyQiLayerBonus(actor, stats);

            // 2. 职业方向属性加成（分支不再注册为特质，需手动应用）
            SuperMechBranch.ApplyBranchBonus(actor, stats);

            // 3. 专精属性加成
            SuperMechBranchMastery.ApplySpecBonus(actor, stats);

            // 4. 通用专长属性加成
            SuperMechElement.ApplyAllSpecBonus(actor, stats);

            // 5. 职业专精属性加成
            SuperMechSpecialty.ApplySpecialtyBonus(actor, stats);

            // 6. 装备/宝物属性加成
            SuperMechCosmicRelic.ApplyEquipBonus(actor, stats);

            // 7. 装备词缀属性加成（对照原著：装备词缀提供伤害/生命/速度/护甲等加成）
            SuperMechEquipAffix.ApplyAffixesToStats(actor, stats);

            // 8. 法师塔属性加成
            SuperMechMageTower.ApplyTowerBonus(actor, stats);

            // 9. 文明科技树加成（v0.99.1 对照原著：三大文明科技路线给全文明属性加成）
            ApplyTechBonus(actor, stats);
        }

        /// <summary>v0.99.1 文明科技树加成：已研究科技给全文明单位提供伤害/生命/速度加成</summary>
        private static void ApplyTechBonus(Actor actor, BaseStats stats)
        {
            if (actor == null || actor.kingdom == null) return;
            long kid = actor.kingdom.getID();
            var (dmg, hp, spd) = SuperMechTech.GetBonuses(kid);
            if (dmg > 0) stats["damage"] *= (1f + dmg);
            if (hp > 0) stats["health"] *= (1f + hp);
            if (spd > 0) stats["speed"] *= (1f + spd);
        }

        /// <summary>
        /// 气力层次加成映射到原版战斗属性
        /// 映射规则：力量→伤害，敏捷→速度/攻速，耐力→生命/护甲，智力→原版智力，神秘→伤害加成，幸运→暴击
        /// </summary>
        private static void ApplyQiLayerBonus(Actor actor, BaseStats stats)
        {
            if (!SuperMechAwakened.IsAwakened(actor)) return;

            float strBonus = SuperMechQiLayer.GetStrengthBonus(actor);
            float agiBonus = SuperMechQiLayer.GetAgilityBonus(actor);
            float endBonus = SuperMechQiLayer.GetEnduranceBonus(actor);
            float intBonus = SuperMechQiLayer.GetIntelligenceBonus(actor);
            float mysBonus = SuperMechQiLayer.GetMysteryBonus(actor);
            float luckBonus = SuperMechCustomStats.GetStat(actor, SuperMechCustomStats.StatLuck);

            stats["damage"] += strBonus * 0.1f;
            stats["speed"] += agiBonus * 0.05f;
            stats["attack_speed"] += agiBonus * 0.02f;
            stats["health"] += endBonus * 2f;
            stats["armor"] += endBonus * 0.1f;
            stats["intelligence"] += intBonus;
            stats["damage"] += mysBonus * 0.05f;
            stats["critical_chance"] += luckBonus * 0.001f;
        }
    }
}
