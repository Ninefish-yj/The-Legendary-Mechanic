using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义种族注册中心
    /// 对照原著：Ⅰ型宇宙人族/卢翰兽人/精灵/矮人/虫族/银灵人/机械族/虚空龙族/星游兽/黑星族
    /// 美术通过ISpeciesRenderer接口预留，当前用人类精灵图占位
    /// </summary>
    public static class SuperMechSpecies
    {
        private static bool _registered = false;
        private static readonly Dictionary<string, SpeciesDef> _species = new Dictionary<string, SpeciesDef>();
        private static readonly Dictionary<string, ISpeciesRenderer> _renderers = new Dictionary<string, ISpeciesRenderer>();

        // === 种族ID常量 ===
        public const string HumanCosmic = "sm_species_human_cosmic";   // Ⅰ型宇宙人族
        public const string OrcLuhan    = "sm_species_orc_luhan";      // 卢翰兽人
        public const string Elf         = "sm_species_elf";            // 精灵
        public const string Dwarf       = "sm_species_dwarf";          // 矮人
        public const string Zerg        = "sm_species_zerg";           // 虫族
        public const string SilverSpirit= "sm_species_silver_spirit";  // 银灵人
        public const string MechLife    = "sm_species_mech_life";      // 机械族
        public const string VoidDragon  = "sm_species_void_dragon";    // 虚空龙族
        public const string StarBeast   = "sm_species_star_beast";     // 星游兽
        public const string BlackStar   = "sm_species_black_star";     // 黑星族（韩萧专属）

        /// <summary>注册所有自定义种族（模组加载时调用一次）</summary>
        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            // 定义种族数据
            DefineSpecies();

            // 注册到WB ActorAssetLibrary
            foreach (var kvp in _species)
            {
                RegisterActorAsset(kvp.Value);
            }

            Debug.Log($"[超神机械师] 自定义种族注册完成：{_species.Count}个种族");
        }

        /// <summary>定义所有种族数据（对照原著）</summary>
        private static void DefineSpecies()
        {
            // P0：Ⅰ型宇宙人族（原著ch265，人类长期星际生活进化形态）
            _species[HumanCosmic] = new SpeciesDef {
                id = HumanCosmic,
                nameKey = "sm_species_human_cosmic_name",
                descKey = "sm_species_human_cosmic_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.1f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#FFD700",
                baseStats = new Dictionary<string, float> {
                    {"health", 120f}, {"damage", 12f}, {"speed", 45f},
                    {"attack_speed", 2.5f}, {"stamina", 120f}, {"armor", 5f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 1,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P0：卢翰兽人（原著，身高三米多，毛茸茸像熊人，凶猛战斗种族）
            _species[OrcLuhan] = new SpeciesDef {
                id = OrcLuhan,
                nameKey = "sm_species_orc_luhan_name",
                descKey = "sm_species_orc_luhan_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S15_Large,
                scale = 0.13f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#8B4513",
                baseStats = new Dictionary<string, float> {
                    {"health", 200f}, {"damage", 20f}, {"speed", 35f},
                    {"attack_speed", 1.8f}, {"stamina", 180f}, {"armor", 15f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 1,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P0：精灵（原著，有精灵血统，尖耳朵，紫色眼眸）
            _species[Elf] = new SpeciesDef {
                id = Elf,
                nameKey = "sm_species_elf_name",
                descKey = "sm_species_elf_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.095f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#9370DB",
                baseStats = new Dictionary<string, float> {
                    {"health", 90f}, {"damage", 10f}, {"speed", 55f},
                    {"attack_speed", 3f}, {"stamina", 100f}, {"armor", 3f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 2,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P1：矮人（原著，大胡子矮人佣兵）
            _species[Dwarf] = new SpeciesDef {
                id = Dwarf,
                nameKey = "sm_species_dwarf_name",
                descKey = "sm_species_dwarf_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S10_Medium,
                scale = 0.085f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#CD853F",
                baseStats = new Dictionary<string, float> {
                    {"health", 160f}, {"damage", 15f}, {"speed", 30f},
                    {"attack_speed", 2f}, {"stamina", 160f}, {"armor", 20f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 1,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P1：虫族（原著，海尔是虫族，狰狞外貌，地底异种有虫族特征）
            _species[Zerg] = new SpeciesDef {
                id = Zerg,
                nameKey = "sm_species_zerg_name",
                descKey = "sm_species_zerg_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.1f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#228B22",
                baseStats = new Dictionary<string, float> {
                    {"health", 140f}, {"damage", 18f}, {"speed", 50f},
                    {"attack_speed", 2.8f}, {"stamina", 140f}, {"armor", 12f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 2,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P1：银灵人（原著ch364，星灵之海高级文明种族，繁殖力弱，高进化种族）
            _species[SilverSpirit] = new SpeciesDef {
                id = SilverSpirit,
                nameKey = "sm_species_silver_spirit_name",
                descKey = "sm_species_silver_spirit_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.1f,
                isCivilized = true,
                canReproduce = false,  // 繁殖力弱
                hasSoul = true,
                flying = false,
                colorHex = "#C0C0C0",
                baseStats = new Dictionary<string, float> {
                    {"health", 100f}, {"damage", 12f}, {"speed", 48f},
                    {"attack_speed", 2.5f}, {"stamina", 110f}, {"armor", 8f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 3,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P2：机械族（原著，韩萧机械军团，机械族群暴兵强项）
            _species[MechLife] = new SpeciesDef {
                id = MechLife,
                nameKey = "sm_species_mech_life_name",
                descKey = "sm_species_mech_life_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.1f,
                isCivilized = false,
                canReproduce = false,  // 无性繁殖/量产
                hasSoul = false,       // 机械生命无灵魂
                flying = false,
                colorHex = "#708090",
                baseStats = new Dictionary<string, float> {
                    {"health", 180f}, {"damage", 16f}, {"speed", 40f},
                    {"attack_speed", 2.2f}, {"stamina", 200f}, {"armor", 25f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 2,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P2：虚空龙族（原著，星际生物，身躯成千上万米，生活在宇宙）
            _species[VoidDragon] = new SpeciesDef {
                id = VoidDragon,
                nameKey = "sm_species_void_dragon_name",
                descKey = "sm_species_void_dragon_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S17_Dragon,
                scale = 0.15f,
                isCivilized = false,
                canReproduce = true,
                hasSoul = true,
                flying = true,
                colorHex = "#4B0082",
                baseStats = new Dictionary<string, float> {
                    {"health", 500f}, {"damage", 40f}, {"speed", 60f},
                    {"attack_speed", 1.5f}, {"stamina", 400f}, {"armor", 30f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 3,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P2：星游兽（原著，星际生物，能肉身穿行宇宙）
            _species[StarBeast] = new SpeciesDef {
                id = StarBeast,
                nameKey = "sm_species_star_beast_name",
                descKey = "sm_species_star_beast_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S15_Large,
                scale = 0.12f,
                isCivilized = false,
                canReproduce = true,
                hasSoul = true,
                flying = true,
                colorHex = "#00CED1",
                baseStats = new Dictionary<string, float> {
                    {"health", 350f}, {"damage", 25f}, {"speed", 55f},
                    {"attack_speed", 1.8f}, {"stamina", 300f}, {"armor", 20f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 2,
                evolutionFrom = null,
                evolutionTo = null
            };

            // P3：黑星族（原著ch770，韩萧超A时诞生的专属种族，可自命名，天赋可遗传）
            _species[BlackStar] = new SpeciesDef {
                id = BlackStar,
                nameKey = "sm_species_black_star_name",
                descKey = "sm_species_black_star_desc",
                templateId = "human",
                texturePath = "",
                actorSize = ActorSize.S13_Human,
                scale = 0.1f,
                isCivilized = true,
                canReproduce = true,
                hasSoul = true,
                flying = false,
                colorHex = "#000000",
                baseStats = new Dictionary<string, float> {
                    {"health", 250f}, {"damage", 30f}, {"speed", 50f},
                    {"attack_speed", 2.5f}, {"stamina", 250f}, {"armor", 15f}
                },
                defaultTraits = new List<string>(),
                defaultSkills = new List<string>(),
                rarity = 3,
                evolutionFrom = HumanCosmic,  // 从宇宙人族进化
                evolutionTo = null
            };
        }

        /// <summary>注册单个种族到WB ActorAssetLibrary</summary>
        private static void RegisterActorAsset(SpeciesDef def)
        {
            if (string.IsNullOrEmpty(def.id) || AssetManager.actor_library == null) return;
            if (AssetManager.actor_library.get(def.id) != null) return;

            // 克隆模板生物
            ActorAsset asset = AssetManager.actor_library.clone(def.id, def.templateId);
            if (asset == null)
            {
                asset = AssetManager.actor_library.clone(def.id, "human");
                if (asset == null) return;
            }

            // 基础设置
            asset.name_locale = def.id;
            asset.use_phenotypes = false;
            asset.actor_size = (ActorSize)def.actorSize;
            asset.can_be_inspected = true;
            asset.can_edit_traits = true;
            asset.has_soul = def.hasSoul;
            asset.flying = def.flying;
            asset.color_hex = def.colorHex;
            asset.color = Toolbox.makeColor(def.colorHex);

            // 文明/繁殖设置
            asset.auto_civ = def.isCivilized;
            asset.can_reproduce = def.canReproduce;

            // 预留美术渲染器接入
            ISpeciesRenderer renderer = GetRenderer(def.id);
            if (renderer != null && renderer.HasCustomArt)
            {
                // 后续补美术时走这里
                asset.texture_asset = new ActorTextureSubAsset(renderer.TexturePath, false);
                asset.texture_asset.texture_path_main = renderer.TexturePath.TrimEnd('/', '\\');
                asset.animation_idle = renderer.IdleFrames;
                asset.animation_walk = renderer.WalkFrames;
                asset.animation_swim = renderer.SwimFrames;
            }
            // else: 用克隆模板的默认纹理（人类占位）

            // 基础属性覆盖
            if (asset.base_stats != null && def.baseStats != null)
            {
                foreach (var stat in def.baseStats)
                {
                    asset.base_stats[stat.Key] = stat.Value;
                }
                asset.base_stats["scale"] = def.scale;
            }

            // 默认特质
            if (def.defaultTraits != null && def.defaultTraits.Count > 0)
            {
                asset.default_subspecies_traits = new List<string>(def.defaultTraits);
            }

            // 加载纹理
            AssetManager.actor_library.loadTexturesAndSprites(asset);
        }

        /// <summary>注册种族美术渲染器（后续补美术时调用）</summary>
        public static void RegisterRenderer(string speciesId, ISpeciesRenderer renderer)
        {
            _renderers[speciesId] = renderer;
        }

        /// <summary>获取种族渲染器（未注册时返回默认人类占位）</summary>
        public static ISpeciesRenderer GetRenderer(string speciesId)
        {
            if (_renderers.TryGetValue(speciesId, out var r))
                return r;
            return new DefaultSpeciesRenderer(speciesId);
        }

        /// <summary>获取种族定义</summary>
        public static SpeciesDef GetSpecies(string id)
        {
            _species.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>获取所有种族</summary>
        public static IReadOnlyDictionary<string, SpeciesDef> GetAllSpecies()
        {
            return _species;
        }

        /// <summary>获取指定稀有度的种族</summary>
        public static List<SpeciesDef> GetSpeciesByRarity(int rarity)
        {
            var result = new List<SpeciesDef>();
            foreach (var def in _species.Values)
            {
                if (def.rarity == rarity) result.Add(def);
            }
            return result;
        }
    }
}
