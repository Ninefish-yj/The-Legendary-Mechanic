namespace SuperMech.Code
{
    /// <summary>
    /// 系统启动引导器：统一注册所有模组系统
    /// 遵循开闭原则：新增系统只需在此添加一行RegisterAction
    /// 替代Main.cs中29个手动Register()调用
    /// </summary>
    public static class SystemBootstrapper
    {
        private static bool _initialized = false;

        /// <summary>注册所有系统（模组加载时调用一次）</summary>
        public static void Bootstrap()
        {
            if (_initialized) return;
            _initialized = true;

            // === 基础设施层 ===
            SystemRegistry.RegisterAction("TraitGroups", () => SuperMechTraitGroups.Register());
            SystemRegistry.RegisterAction("CustomStats", () => SuperMechCustomStats.Register());

            // === 核心数据层 ===
            SystemRegistry.RegisterAction("Traits", () => SuperMechTraits.Register());
            SystemRegistry.RegisterAction("Skills", () => SuperMechSkills.Register());
            SystemRegistry.RegisterAction("Knowledge", () => SuperMechKnowledge.Register());
            SystemRegistry.RegisterAction("KnowledgeSynergy", () => SuperMechKnowledgeSynergy.Register());
            SystemRegistry.RegisterAction("KnowledgeFusion", () => SuperMechKnowledgeFusion.Register());
            SystemRegistry.RegisterAction("KnowledgeRecipes", () => SuperMechKnowledgeRecipe.RegisterBaseRecipes());

            // === 修炼体系 ===
            SystemRegistry.RegisterAction("Qi", () => SuperMechQi.Register());
            SystemRegistry.RegisterAction("CorePower", () => SuperMechCorePower.Register());
            SystemRegistry.RegisterAction("Meditation", () => SuperMechMeditation.Register());
            SystemRegistry.RegisterAction("QiRefine", () => SuperMechQiRefine.Register());

            // === 职业体系 ===
            SystemRegistry.RegisterAction("SubClass", () => SuperMechSubClass.Register());
            SystemRegistry.RegisterAction("Branch", () => SuperMechBranch.Register());
            SystemRegistry.RegisterAction("BranchMastery", () => SuperMechBranchMastery.Register());
            SystemRegistry.RegisterAction("Specialty", () => SuperMechSpecialty.Register());
            SystemRegistry.RegisterAction("ExclusiveTrait", () => SuperMechExclusiveTrait.Register());
            SystemRegistry.RegisterAction("MageType", () => SuperMechMageType.Register());
            SystemRegistry.RegisterAction("Spell", () => SuperMechSpell.RegisterAll());
            SystemRegistry.RegisterAction("StageTask", () => SuperMechStageTask.Register());
            SystemRegistry.RegisterAction("Element", () => SuperMechElement.Register());
            SystemRegistry.RegisterAction("RankTrait", () => SuperMechRankTrait.Register());
            SystemRegistry.RegisterAction("Race", () => SuperMechRace.Register());

            // === 装备与宝物 ===
            SystemRegistry.RegisterAction("Relic", () => SuperMechRelic.Register());
            SystemRegistry.RegisterAction("CosmicRelic", () => SuperMechCosmicRelic.Register());
            SystemRegistry.RegisterAction("MageTower", () => SuperMechMageTower.Register());
            SystemRegistry.RegisterAction("Crafting", () => SuperMechCrafting.Register());

            // === 高阶系统 ===
            SystemRegistry.RegisterAction("Awakened", () => SuperMechAwakened.Register());
            SystemRegistry.RegisterAction("Sanctuary", () => SuperMechSanctuary.Register());
            SystemRegistry.RegisterAction("Powers", () => SuperMechPowers.Register());
            SystemRegistry.RegisterAction("Company", () => SuperMechCompany.Register());

            // 执行所有注册
            SystemRegistry.RegisterAll();
        }
    }
}
