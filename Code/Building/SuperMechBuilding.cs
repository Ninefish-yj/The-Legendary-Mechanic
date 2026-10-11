using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义建筑注册中心
    /// 对照原著：机械工厂/佣兵公会/星际银行等特殊设施
    /// 参考诡秘之主实现：clone原生建筑再修改属性
    /// </summary>
    public static class SuperMechBuilding
    {
        private static bool _registered = false;

        // 建筑ID常量
        public const string MechFactory = "sm_mech_factory";        // 机械工厂
        public const string MercGuild = "sm_merc_guild";           // 佣兵公会
        public const string StarBank = "sm_star_bank";             // 星际银行
        public const string BlackStarPalace = "sm_blackstar_palace"; // 黑星行宫
        public const string CompanyHQ = "sm_company_hq";           // 降临者公司总部

        /// <summary>注册所有自定义建筑</summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            RegisterMechFactory();
            RegisterMercGuild();
            RegisterStarBank();
            RegisterBlackStarPalace();
            RegisterCompanyHQ();

            Debug.Log("[超神机械师] 自定义建筑注册完成：5个建筑");
        }

        /// <summary>机械工厂 - 原著韩萧制造机械军团的基地</summary>
        private static void RegisterMechFactory()
        {
            BuildingAsset source = AssetManager.buildings.get("house")
                ?? AssetManager.buildings.get("well")
                ?? AssetManager.buildings.get("bonfire");
            if (source == null) return;

            BuildingAsset asset = AssetManager.buildings.get(MechFactory)
                ?? AssetManager.buildings.clone(MechFactory, source.id);
            if (asset == null) return;

            asset.id = MechFactory;
            asset.city_building = true;
            asset.kingdom = "neutral";
            asset.civ_kingdom = string.Empty;
            asset.building_type = BuildingType.Building_Civ;
            asset.type = "type_mech_factory";
            asset.can_be_abandoned = true;
            asset.can_be_upgraded = true;
            asset.destroy_on_liquid = true;
            asset.can_be_placed_on_liquid = false;
            asset.removed_by_sponge = true;
            asset.spawn_units = false;
            asset.spawn_drops = false;
            asset.can_units_live_here = false;
            asset.can_be_living_house = false;
            asset.max_houses = 0;
            asset.burnable = true;
            asset.construction_progress_needed = 50;

            if (asset.base_stats == null)
                asset.base_stats = new BaseStats();
            asset.base_stats["health"] = 500f;
            asset.base_stats["armor"] = 20f;

            // 本地化
            LocalizedTextManager.add($"building_{MechFactory}", "机械工厂", pReplace: true);
            LocalizedTextManager.add($"building_{MechFactory}_desc", "机械系超能者制造机械军团和装备的基地", pReplace: true);
        }

        /// <summary>佣兵公会 - 原著发布任务/招募佣兵的机构</summary>
        private static void RegisterMercGuild()
        {
            BuildingAsset source = AssetManager.buildings.get("house")
                ?? AssetManager.buildings.get("well")
                ?? AssetManager.buildings.get("bonfire");
            if (source == null) return;

            BuildingAsset asset = AssetManager.buildings.get(MercGuild)
                ?? AssetManager.buildings.clone(MercGuild, source.id);
            if (asset == null) return;

            asset.id = MercGuild;
            asset.city_building = true;
            asset.kingdom = "neutral";
            asset.civ_kingdom = string.Empty;
            asset.building_type = BuildingType.Building_Civ;
            asset.type = "type_merc_guild";
            asset.can_be_abandoned = true;
            asset.can_be_upgraded = true;
            asset.destroy_on_liquid = true;
            asset.can_be_placed_on_liquid = false;
            asset.removed_by_sponge = true;
            asset.spawn_units = false;
            asset.spawn_drops = false;
            asset.can_units_live_here = false;
            asset.can_be_living_house = false;
            asset.max_houses = 0;
            asset.burnable = true;
            asset.construction_progress_needed = 40;

            if (asset.base_stats == null)
                asset.base_stats = new BaseStats();
            asset.base_stats["health"] = 400f;
            asset.base_stats["armor"] = 15f;

            LocalizedTextManager.add($"building_{MercGuild}", "佣兵公会", pReplace: true);
            LocalizedTextManager.add($"building_{MercGuild}_desc", "发布任务、招募佣兵的星际机构", pReplace: true);
        }

        /// <summary>星际银行 - 原著存储伊纳尔/转账的机构</summary>
        private static void RegisterStarBank()
        {
            BuildingAsset source = AssetManager.buildings.get("house")
                ?? AssetManager.buildings.get("well")
                ?? AssetManager.buildings.get("bonfire");
            if (source == null) return;

            BuildingAsset asset = AssetManager.buildings.get(StarBank)
                ?? AssetManager.buildings.clone(StarBank, source.id);
            if (asset == null) return;

            asset.id = StarBank;
            asset.city_building = true;
            asset.kingdom = "neutral";
            asset.civ_kingdom = string.Empty;
            asset.building_type = BuildingType.Building_Civ;
            asset.type = "type_star_bank";
            asset.can_be_abandoned = true;
            asset.can_be_upgraded = true;
            asset.destroy_on_liquid = true;
            asset.can_be_placed_on_liquid = false;
            asset.removed_by_sponge = true;
            asset.spawn_units = false;
            asset.spawn_drops = false;
            asset.can_units_live_here = false;
            asset.can_be_living_house = false;
            asset.max_houses = 0;
            asset.burnable = true;
            asset.construction_progress_needed = 60;

            if (asset.base_stats == null)
                asset.base_stats = new BaseStats();
            asset.base_stats["health"] = 600f;
            asset.base_stats["armor"] = 30f;

            LocalizedTextManager.add($"building_{StarBank}", "星际银行", pReplace: true);
            LocalizedTextManager.add($"building_{StarBank}_desc", "存储伊纳尔、转账贷款的星际金融机构", pReplace: true);
        }

        /// <summary>黑星行宫 - 原著韩萧的专属基地</summary>
        private static void RegisterBlackStarPalace()
        {
            BuildingAsset source = AssetManager.buildings.get("house")
                ?? AssetManager.buildings.get("well")
                ?? AssetManager.buildings.get("bonfire");
            if (source == null) return;

            BuildingAsset asset = AssetManager.buildings.get(BlackStarPalace)
                ?? AssetManager.buildings.clone(BlackStarPalace, source.id);
            if (asset == null) return;

            asset.id = BlackStarPalace;
            asset.city_building = true;
            asset.kingdom = "neutral";
            asset.civ_kingdom = string.Empty;
            asset.building_type = BuildingType.Building_Civ;
            asset.type = "type_blackstar_palace";
            asset.can_be_abandoned = false;
            asset.can_be_upgraded = true;
            asset.destroy_on_liquid = true;
            asset.can_be_placed_on_liquid = false;
            asset.removed_by_sponge = false;
            asset.spawn_units = false;
            asset.spawn_drops = false;
            asset.can_units_live_here = true;
            asset.can_be_living_house = true;
            asset.max_houses = 5;
            asset.burnable = false;
            asset.construction_progress_needed = 200;

            if (asset.base_stats == null)
                asset.base_stats = new BaseStats();
            asset.base_stats["health"] = 5000f;
            asset.base_stats["armor"] = 100f;

            LocalizedTextManager.add($"building_{BlackStarPalace}", "黑星行宫", pReplace: true);
            LocalizedTextManager.add($"building_{BlackStarPalace}_desc", "黑星韩萧的专属基地，军团驻扎与研发中心", pReplace: true);
        }

        /// <summary>降临者公司总部 - 新设定，黑星公司+地球商会发行伊纳尔</summary>
        private static void RegisterCompanyHQ()
        {
            BuildingAsset source = AssetManager.buildings.get("house")
                ?? AssetManager.buildings.get("well")
                ?? AssetManager.buildings.get("bonfire");
            if (source == null) return;

            BuildingAsset asset = AssetManager.buildings.get(CompanyHQ)
                ?? AssetManager.buildings.clone(CompanyHQ, source.id);
            if (asset == null) return;

            asset.id = CompanyHQ;
            asset.city_building = true;
            asset.kingdom = "neutral";
            asset.civ_kingdom = string.Empty;
            asset.building_type = BuildingType.Building_Civ;
            asset.type = "type_company_hq";
            asset.can_be_abandoned = false;
            asset.can_be_upgraded = true;
            asset.destroy_on_liquid = true;
            asset.can_be_placed_on_liquid = false;
            asset.removed_by_sponge = false;
            asset.spawn_units = false;
            asset.spawn_drops = false;
            asset.can_units_live_here = false;
            asset.can_be_living_house = false;
            asset.max_houses = 0;
            asset.burnable = false;
            asset.construction_progress_needed = 150;

            if (asset.base_stats == null)
                asset.base_stats = new BaseStats();
            asset.base_stats["health"] = 3000f;
            asset.base_stats["armor"] = 50f;

            LocalizedTextManager.add($"building_{CompanyHQ}", "降临者公司总部", pReplace: true);
            LocalizedTextManager.add($"building_{CompanyHQ}_desc", "黑星公司与地球商会的联合总部，伊纳尔发行机构", pReplace: true);
        }
    }
}
