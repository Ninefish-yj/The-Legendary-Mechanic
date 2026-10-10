using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族注册中心 - 管理所有自定义种族的注册和查询
    /// 使用WB原生ActorAsset+ActorAssetLibrary.add()注册
    /// 美术资源通过ISpeciesRenderer接口抽象，当前用人类占位
    /// </summary>
    public static class SuperMechSpecies
    {
        private static Dictionary<string, SpeciesData> _species = new Dictionary<string, SpeciesData>();
        private static ISpeciesRenderer _renderer = new DefaultSpeciesRenderer();
        private static bool _initialized;

        /// <summary>初始化所有种族定义并注册到WB</summary>
        public static void Register()
        {
            if (_initialized) return;
            _initialized = true;

            RegisterSpeciesDefinitions();
            RegisterToWorldBox();
            Debug.Log($"[超神机械师] 种族系统初始化完成，共注册{_species.Count}个种族");
        }

        /// <summary>注册所有种族定义（对照原著）</summary>
        private static void RegisterSpeciesDefinitions()
        {
            // P0: 基础种族（自然生成）
            RegisterHumanCosmic();
            RegisterOrcLuhan();
            RegisterElf();

            // P1: 进阶种族（进化解锁）
            RegisterDwarf();
            RegisterZerg();
            RegisterSilverSpirit();

            // P2: 高级种族（特殊条件）
            RegisterMechLife();
            RegisterVoidDragon();
            RegisterStarBeast();

            // P3: 专属种族（超A诞生）
            RegisterBlackStar();
        }

        // ========== P0 基础种族 ==========

        /// <summary>Ⅰ型宇宙人族 - 原著ch265，人类长期星际生活进化形态</summary>
        private static void RegisterHumanCosmic()
        {
            var s = new SpeciesData("human_cosmic", "Ⅰ型宇宙人族", "P0")
            {
                Description = "人类种族长期在星际间生活，接触宇宙能量与不同星球环境后进化的形态，适应性极强",
                WildKingdomId = "humans",
                CivKingdomId = "humans",
                IsSpecial = false,
                RacialTalent = "适应性群体：全属性均衡，环境适应力强",
                EvolutionCondition = "地球人通过进化方块进化",
                NovelReference = "ch265 种族变更为Ⅰ型宇宙人族",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 5}, {"Agility", 5}, {"Endurance", 5},
                    {"Intelligence", 8}, {"Mystery", 3}, {"Charisma", 3}
                }
            };
            s.EvolutionTargets.Add("void_shadow");
            s.EvolutionTargets.Add("human_cosmic_2");
            _species[s.Id] = s;
        }

        /// <summary>卢翰兽人 - 原著战斗种族，身高三米多，浑身毛茸茸像熊人</summary>
        private static void RegisterOrcLuhan()
        {
            var s = new SpeciesData("orc_luhan", "卢翰兽人", "P0")
            {
                Description = "凶猛的战斗种族，身高三米多，浑身毛茸茸，看上去像熊人，长相还挺萌",
                WildKingdomId = "orcs",
                CivKingdomId = "orcs",
                IsSpecial = false,
                RacialTalent = "战斗种族：力量+30%，耐力+20%",
                NovelReference = "原著 哈蒙·崩岩·索诺丁 卢翰兽人",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 20}, {"Agility", 5}, {"Endurance", 15},
                    {"Intelligence", -5}, {"Mystery", 0}, {"Charisma", -3}
                },
                Traits = new List<string> {"Strong"}
            };
            _species[s.Id] = s;
        }

        /// <summary>精灵 - 原著中有精灵血统的种族，尖耳朵</summary>
        private static void RegisterElf()
        {
            var s = new SpeciesData("elf", "精灵", "P0")
            {
                Description = "具有精灵血统的种族，耳朵尖长，擅长神秘学与敏捷",
                WildKingdomId = "elves",
                CivKingdomId = "elves",
                IsSpecial = false,
                RacialTalent = "精灵血统：敏捷+25%，神秘+20%，魅力+15%",
                NovelReference = "原著 有精灵血统的角色 耳朵尖长",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", -3}, {"Agility", 15}, {"Endurance", -5},
                    {"Intelligence", 10}, {"Mystery", 15}, {"Charisma", 10}
                },
                Traits = new List<string> {"Fast"}
            };
            _species[s.Id] = s;
        }

        // ========== P1 进阶种族 ==========

        /// <summary>矮人 - 原著中的矮人佣兵，大胡子</summary>
        private static void RegisterDwarf()
        {
            var s = new SpeciesData("dwarf", "矮人", "P1")
            {
                Description = "大胡子矮人，擅长锻造和近战，耐力极强",
                WildKingdomId = "dwarves",
                CivKingdomId = "dwarves",
                IsSpecial = false,
                RacialTalent = "矮人坚韧：耐力+30%，力量+15%，敏捷-10%",
                NovelReference = "原著 矮人佣兵",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 12}, {"Agility", -8}, {"Endurance", 20},
                    {"Intelligence", 5}, {"Mystery", -5}, {"Charisma", 0}
                },
                Traits = new List<string> {"Tough"}
            };
            _species[s.Id] = s;
        }

        /// <summary>虫族 - 原著中海尔的种族，狰狞外貌</summary>
        private static void RegisterZerg()
        {
            var s = new SpeciesData("zerg", "虫族", "P1")
            {
                Description = "虫族，狰狞外貌，繁殖力强，擅长群体作战",
                WildKingdomId = "zergs",
                CivKingdomId = "",
                IsSpecial = false,
                RacialTalent = "虫群：力量+20%，敏捷+20%，繁殖+50%，智力-20%",
                NovelReference = "原著 海尔 虫族",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 12}, {"Agility", 12}, {"Endurance", 8},
                    {"Intelligence", -15}, {"Mystery", -5}, {"Charisma", -10}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>银灵人 - 原著ch364，星灵之海高级文明种族，繁殖力弱</summary>
        private static void RegisterSilverSpirit()
        {
            var s = new SpeciesData("silver_spirit", "银灵人", "P1")
            {
                Description = "星灵之海的高级文明种族，种族进化程度高，繁殖力比较弱，总数不庞大",
                WildKingdomId = "silver_spirits",
                CivKingdomId = "silver_spirits",
                IsSpecial = false,
                RacialTalent = "高进化种族：神秘+30%，智力+25%，繁殖力-50%",
                NovelReference = "ch364 银灵人 星灵之海高级文明种族",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", -5}, {"Agility", 5}, {"Endurance", -5},
                    {"Intelligence", 18}, {"Mystery", 22}, {"Charisma", 8}
                }
            };
            _species[s.Id] = s;
        }

        // ========== P2 高级种族 ==========

        /// <summary>机械族 - 韩萧的机械军团，机械生命形态</summary>
        private static void RegisterMechLife()
        {
            var s = new SpeciesData("mech_life", "机械族", "P2")
            {
                Description = "机械生命形态，韩萧机械军团的产物，无需食物，免疫疾病",
                WildKingdomId = "",
                CivKingdomId = "",
                IsSpecial = true,
                RacialTalent = "机械生命：力量+25%，耐力+25%，无需食物，免疫疾病",
                NovelReference = "原著 机械族群 暴兵是机械族强项",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 18}, {"Agility", 0}, {"Endurance", 18},
                    {"Intelligence", 10}, {"Mystery", -10}, {"Charisma", -5}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>虚空龙族 - 原著星际生物，身躯成千上万米，生活在宇宙</summary>
        private static void RegisterVoidDragon()
        {
            var s = new SpeciesData("void_dragon", "虚空龙族", "P2")
            {
                Description = "星际生物，身躯成千上万米，生活在宇宙中，暗能量如同空气对于人类",
                WildKingdomId = "void_dragons",
                CivKingdomId = "",
                IsSpecial = true,
                ActorSize = "S17_Dragon",
                RacialTalent = "虚空巨龙：全属性+40%，可飞行，体型巨大",
                NovelReference = "原著 虚空龙族 星际生物 身躯成千上万米",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 40}, {"Agility", 20}, {"Endurance", 40},
                    {"Intelligence", 15}, {"Mystery", 30}, {"Charisma", 10}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>星游兽 - 原著星际生物，能肉身穿行宇宙</summary>
        private static void RegisterStarBeast()
        {
            var s = new SpeciesData("star_beast", "星游兽", "P2")
            {
                Description = "能够肉身穿行宇宙的星际生物，别指望能轻松伤到它",
                WildKingdomId = "star_beasts",
                CivKingdomId = "",
                IsSpecial = true,
                RacialTalent = "宇宙生存：耐力+50%，可在宇宙生存，低智力",
                NovelReference = "原著 星游兽 肉身穿行宇宙",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 25}, {"Agility", 10}, {"Endurance", 35},
                    {"Intelligence", -10}, {"Mystery", 5}, {"Charisma", -5}
                }
            };
            _species[s.Id] = s;
        }

        // ========== P3 专属种族 ==========

        /// <summary>黑星族 - 韩萧超A时诞生的专属种族，ch770</summary>
        private static void RegisterBlackStar()
        {
            var s = new SpeciesData("black_star", "黑星族", "P3")
            {
                Description = "韩萧超A级时诞生的专属种族，可自命名，专属天赋可遗传后代",
                WildKingdomId = "",
                CivKingdomId = "",
                IsSpecial = true,
                RacialTalent = "机械天才：机械亲和度提升，专属天赋可遗传",
                EvolutionCondition = "机械系超A级自动诞生",
                NovelReference = "ch770 专属种族诞生 黑星族",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 30}, {"Agility", 25}, {"Endurance", 35},
                    {"Intelligence", 40}, {"Mystery", 30}, {"Charisma", 20}
                }
            };
            _species[s.Id] = s;
        }

        // ========== 注册到WB ==========

        /// <summary>将所有种族注册到WB的ActorAssetLibrary</summary>
        private static void RegisterToWorldBox()
        {
            foreach (var kvp in _species)
            {
                var species = kvp.Value;
                if (species.IsRegistered) continue;

                try
                {
                    var asset = new ActorAsset
                    {
                        id = species.Id,
                        kingdom_id_wild = species.WildKingdomId,
                        kingdom_id_civilization = species.CivKingdomId,
                        special = species.IsSpecial,
                        actor_size = (ActorSize)System.Enum.Parse(typeof(ActorSize), species.ActorSize),
                        icon = _renderer.GetIcon(species.Id),
                        avatar_prefab = _renderer.GetAvatarPrefab(species.Id),
                        traits = new List<string>(species.Traits)
                    };

                    // 注册本地化文本
                    LocalizedTextManager.add($"species_{species.Id}", species.Name, pReplace: true);
                    LocalizedTextManager.add($"species_{species.Id}_desc", species.Description, pReplace: true);

                    // 注册到WB资源库
                    AssetManager.actor_library.add(asset);

                    species.IsRegistered = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[超神机械师] 种族{species.Id}注册失败: {e.Message}");
                }
            }
        }

        // ========== 查询接口 ==========

        /// <summary>根据ID获取种族数据</summary>
        public static SpeciesData GetSpecies(string id)
        {
            _species.TryGetValue(id, out var species);
            return species;
        }

        /// <summary>获取所有种族</summary>
        public static Dictionary<string, SpeciesData> GetAllSpecies() => _species;

        /// <summary>按优先级获取种族</summary>
        public static List<SpeciesData> GetSpeciesByPriority(string priority)
        {
            var result = new List<SpeciesData>();
            foreach (var s in _species.Values)
            {
                if (s.Priority == priority) result.Add(s);
            }
            return result;
        }

        /// <summary>设置渲染器（后续补美术时调用）</summary>
        public static void SetRenderer(ISpeciesRenderer renderer)
        {
            _renderer = renderer ?? new DefaultSpeciesRenderer();
        }
    }
}
