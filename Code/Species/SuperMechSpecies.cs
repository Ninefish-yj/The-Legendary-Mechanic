using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族注册中心 - 管理所有自定义种族的注册和查询
    /// 使用WB原生ActorAsset+ActorAssetLibrary.add()注册
    /// 美术资源通过ISpeciesRenderer接口抽象，当前用人类占位
    /// 对照原著：仅保留原著明确出现的种族，原创种族标注IsOriginal=true
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
            // P0: 基础种族（自然生成，原著常见文明种族）
            RegisterHumanCosmic();
            RegisterKunde();
            RegisterSunir();
            RegisterBlackSpirit();

            // P1: 进阶种族（进化解锁，原著高级文明种族）
            RegisterStarPupilGod();
            RegisterTelkes();
            RegisterDemon();

            // P2: 高级种族（特殊条件，原著星际生物）
            RegisterVoidDragon();
            RegisterStarBeast();
            RegisterLuhanOrc();

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

        /// <summary>昆德族 - 原著高频种族，战争种族，繁衍能力强</summary>
        private static void RegisterKunde()
        {
            var s = new SpeciesData("kunde", "昆德族", "P0")
            {
                Description = "原著高频出现的战争种族，繁衍能力极强，个体战斗力中等但数量庞大，是星际战争的常见炮灰",
                WildKingdomId = "kundes",
                CivKingdomId = "kundes",
                IsSpecial = false,
                RacialTalent = "战争种族：力量+15%，耐力+15%，繁殖+40%，智力-10%",
                NovelReference = "原著 昆德族 高频出现 战争种族",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 12}, {"Agility", 8}, {"Endurance", 12},
                    {"Intelligence", -3}, {"Mystery", 0}, {"Charisma", -5}
                },
                Traits = new List<string> {"Strong"}
            };
            _species[s.Id] = s;
        }

        /// <summary>苏尼尔族 - 原著ch?，高级文明种族，擅长外交和商业</summary>
        private static void RegisterSunir()
        {
            var s = new SpeciesData("sunir", "苏尼尔族", "P0")
            {
                Description = "原著高级文明种族，擅长外交和商业，在星际政坛有重要影响力",
                WildKingdomId = "sunirs",
                CivKingdomId = "sunirs",
                IsSpecial = false,
                RacialTalent = "外交种族：魅力+30%，智力+15%，力量-10%",
                NovelReference = "原著 苏尼尔族 高级文明 外交商业",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", -5}, {"Agility", 5}, {"Endurance", 0},
                    {"Intelligence", 15}, {"Mystery", 10}, {"Charisma", 25}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>黑灵族 - 原著ch?，暗属性种族，擅长隐匿和暗杀</summary>
        private static void RegisterBlackSpirit()
        {
            var s = new SpeciesData("black_spirit", "黑灵族", "P0")
            {
                Description = "原著暗属性种族，擅长隐匿和暗杀，个体战斗力强但数量稀少",
                WildKingdomId = "black_spirits",
                CivKingdomId = "black_spirits",
                IsSpecial = false,
                RacialTalent = "暗影种族：敏捷+25%，神秘+20%，可隐匿，魅力-15%",
                NovelReference = "原著 黑灵族 暗属性 隐匿暗杀",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 5}, {"Agility", 20}, {"Endurance", 5},
                    {"Intelligence", 10}, {"Mystery", 18}, {"Charisma", -10}
                },
                Traits = new List<string> {"Fast"}
            };
            _species[s.Id] = s;
        }

        // ========== P1 进阶种族 ==========

        /// <summary>星瞳神族 - 原著ch?，顶级文明种族，瞳术能力极强</summary>
        private static void RegisterStarPupilGod()
        {
            var s = new SpeciesData("star_pupil_god", "星瞳神族", "P1")
            {
                Description = "原著顶级文明种族，拥有强大的瞳术能力，种族进化程度极高，繁殖力弱",
                WildKingdomId = "star_pupil_gods",
                CivKingdomId = "star_pupil_gods",
                IsSpecial = false,
                RacialTalent = "瞳术种族：神秘+35%，智力+25%，繁殖力-40%，瞳术伤害+50%",
                NovelReference = "原著 星瞳神族 顶级文明 瞳术",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 0}, {"Agility", 10}, {"Endurance", 5},
                    {"Intelligence", 20}, {"Mystery", 30}, {"Charisma", 15}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>泰尔克斯族 - 原著ch?，科技种族，擅长机械和工程</summary>
        private static void RegisterTelkes()
        {
            var s = new SpeciesData("telkes", "泰尔克斯族", "P1")
            {
                Description = "原著科技种族，擅长机械和工程学，与机械系相性极佳",
                WildKingdomId = "telkes",
                CivKingdomId = "telkes",
                IsSpecial = false,
                RacialTalent = "科技种族：智力+25%，机械亲和+30%，力量-5%",
                NovelReference = "原著 泰尔克斯族 科技种族 机械工程",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 0}, {"Agility", 5}, {"Endurance", 5},
                    {"Intelligence", 22}, {"Mystery", 8}, {"Charisma", 5}
                }
            };
            _species[s.Id] = s;
        }

        /// <summary>恶魔族 - 原著ch?，混乱种族，个体战斗力极强</summary>
        private static void RegisterDemon()
        {
            var s = new SpeciesData("demon", "恶魔族", "P1")
            {
                Description = "原著混乱种族，个体战斗力极强，性格暴躁，难以驯服",
                WildKingdomId = "demons",
                CivKingdomId = "",
                IsSpecial = false,
                RacialTalent = "恶魔血统：全属性+20%，狂暴时伤害+50%，智力-15%",
                NovelReference = "原著 恶魔族 混乱种族 个体战力强",
                BaseStats = new Dictionary<string, float>
                {
                    {"Strength", 20}, {"Agility", 15}, {"Endurance", 18},
                    {"Intelligence", -5}, {"Mystery", 15}, {"Charisma", -10}
                },
                Traits = new List<string> {"Strong", "Aggressive"}
            };
            _species[s.Id] = s;
        }

        // ========== P2 高级种族 ==========

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

        /// <summary>卢翰兽人 - 原著战斗种族，身高三米多，浑身毛茸茸像熊人</summary>
        private static void RegisterLuhanOrc()
        {
            var s = new SpeciesData("orc_luhan", "卢翰兽人", "P2")
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

                    LocalizedTextManager.add($"species_{species.Id}", species.Name, pReplace: true);
                    LocalizedTextManager.add($"species_{species.Id}_desc", species.Description, pReplace: true);

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
