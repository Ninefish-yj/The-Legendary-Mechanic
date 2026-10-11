using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 自定义生物注册中心 - 管理所有自定义生物的ActorAsset注册
    /// 对照原著：星际生物/机械军团/异化怪物/世界树单位
    /// 美术资源预留接口：texturePath指向GameResources/actors/，后续补图后自动加载
    /// 实现方式：克隆原版ActorAsset，替换属性和特质，纹理路径预留
    /// </summary>
    public static class SuperMechCustomCreatures
    {
        private static Dictionary<string, CreatureDef> _creatures = new Dictionary<string, CreatureDef>();
        private static bool _initialized;

        /// <summary>生物定义</summary>
        public class CreatureDef
        {
            public string Id;
            public string Name;
            public string CloneFrom;      // 克隆模板（原版ActorAsset id）
            public string TexturePath;    // 预留美术路径（GameResources/actors/下）
            public string KingdomWild;    // 野生王国
            public string KingdomCiv;     // 文明王国
            public bool IsSpecial;        // 是否特殊生物（不自然生成）
            public ActorSize Size;        // 体型
            public Dictionary<string, float> BaseStats = new Dictionary<string, float>();
            public List<string> Traits = new List<string>();
            public string NovelReference; // 原著依据
            public bool IsRegistered;
        }

        /// <summary>初始化所有自定义生物</summary>
        public static void Register()
        {
            if (_initialized) return;
            _initialized = true;

            RegisterDefinitions();
            RegisterToWorldBox();
            Debug.Log($"[超神机械师] 自定义生物系统初始化完成，共注册{_creatures.Count}个生物");
        }

        /// <summary>注册所有生物定义（对照原著）</summary>
        private static void RegisterDefinitions()
        {
            // === 星际生物（P2高级种族，独立ActorAsset） ===
            RegisterVoidDragon();
            RegisterStarBeast();

            // === 机械军团单位（克隆human，预留突变皮肤） ===
            RegisterMechUnits();

            // === 异化怪物（克隆zombie） ===
            RegisterInfectedCreature();

            // === 世界树单位（克隆human，预留纹理） ===
            RegisterWorldTreeUnit();
        }

        // ========== 星际生物 ==========

        /// <summary>虚空巨龙 - 原著星际生物，身躯成千上万米</summary>
        private static void RegisterVoidDragon()
        {
            var c = new CreatureDef
            {
                Id = "sm_void_dragon",
                Name = "虚空巨龙",
                CloneFrom = "dragon",
                TexturePath = "actors/sm_creatures/void_dragon",
                KingdomWild = "void_dragons",
                KingdomCiv = "",
                IsSpecial = true,
                Size = ActorSize.S17_Dragon,
                NovelReference = "原著 虚空龙族 身躯成千上万米 暗能量如同空气",
                BaseStats = new Dictionary<string, float>
                {
                    {"damage", 50f}, {"health", 200f}, {"armor", 20f},
                    {"speed", 1.5f}, {"attack_speed", 0.8f}
                },
                Traits = new List<string> { "Strong", "Aggressive", "sm_awakened" }
            };
            _creatures[c.Id] = c;
        }

        /// <summary>星游兽 - 原著能肉身穿行宇宙的星际生物</summary>
        private static void RegisterStarBeast()
        {
            var c = new CreatureDef
            {
                Id = "sm_star_beast",
                Name = "星游兽",
                CloneFrom = "orc",
                TexturePath = "actors/sm_creatures/star_beast",
                KingdomWild = "star_beasts",
                KingdomCiv = "",
                IsSpecial = true,
                Size = ActorSize.S15_Bear,
                NovelReference = "原著 星游兽 肉身穿行宇宙 别指望能轻松伤到它",
                BaseStats = new Dictionary<string, float>
                {
                    {"damage", 30f}, {"health", 150f}, {"armor", 30f},
                    {"speed", 1.2f}, {"attack_speed", 0.9f}
                },
                Traits = new List<string> { "Strong", "Tough" }
            };
            _creatures[c.Id] = c;
        }

        // ========== 机械军团单位 ==========

        /// <summary>机械军团单位（8种，对照原著韩萧的机械军团）</summary>
        private static void RegisterMechUnits()
        {
            var mechUnits = new[]
            {
                new { Id = "sm_mech_ranger", Name = "游骑兵", Clone = "human", Size = ActorSize.S13_Human, Dmg = 8f, Hp = 30f, Desc = "低端多功能机器人，全面性高，改造潜力大" },
                new { Id = "sm_mech_soldier", Name = "械兵", Clone = "human", Size = ActorSize.S13_Human, Dmg = 12f, Hp = 50f, Desc = "标准机械战士，量产型" },
                new { Id = "sm_mech_elite", Name = "精英械兵", Clone = "human", Size = ActorSize.S14_Cow, Dmg = 18f, Hp = 80f, Desc = "精英机械战士，经过改装强化" },
                new { Id = "sm_mech_heavy", Name = "重装械兵", Clone = "orc", Size = ActorSize.S15_Bear, Dmg = 25f, Hp = 120f, Desc = "重装甲机械单位，防御力极高" },
                new { Id = "sm_mech_immortal", Name = "不灭械兵", Clone = "skeleton", Size = ActorSize.S14_Cow, Dmg = 20f, Hp = 100f, Desc = "具备自我修复能力的机械单位" },
                new { Id = "sm_mech_fighter", Name = "战斗机甲", Clone = "human", Size = ActorSize.S15_Bear, Dmg = 35f, Hp = 90f, Desc = "高机动战斗机甲，空中优势单位" },
                new { Id = "sm_mech_ship", Name = "星际战舰", Clone = "UFO", Size = ActorSize.S16_Buffalo, Dmg = 60f, Hp = 200f, Desc = "星际战舰，远程火力支援" },
                new { Id = "sm_mech_god", Name = "机械战神", Clone = "dragon", Size = ActorSize.S17_Dragon, Dmg = 100f, Hp = 500f, Desc = "机械军团终极单位，韩萧的王牌" }
            };

            foreach (var u in mechUnits)
            {
                var c = new CreatureDef
                {
                    Id = u.Id,
                    Name = u.Name,
                    CloneFrom = u.Clone,
                    TexturePath = $"actors/sm_mech/{u.Id.Replace("sm_mech_", "")}",
                    KingdomWild = "mech_legion",
                    KingdomCiv = "",
                    IsSpecial = true,
                    Size = u.Size,
                    NovelReference = $"原著 机械军团 {u.Name} {u.Desc}",
                    BaseStats = new Dictionary<string, float>
                    {
                        {"damage", u.Dmg}, {"health", u.Hp}, {"armor", u.Dmg * 0.5f},
                        {"attack_speed", 1.2f}
                    },
                    Traits = new List<string> { "Strong", "sm_mech_unit" }
                };
                _creatures[c.Id] = c;
            }
        }

        // ========== 异化怪物 ==========

        /// <summary>异化怪物 - 原著第513章，异化原体影响微生物，生物异化暴走</summary>
        private static void RegisterInfectedCreature()
        {
            var c = new CreatureDef
            {
                Id = "sm_infected",
                Name = "异化怪物",
                CloneFrom = "zombie",
                TexturePath = "actors/sm_creatures/infected",
                KingdomWild = "infected",
                KingdomCiv = "",
                IsSpecial = true,
                Size = ActorSize.S14_Cow,
                NovelReference = "原著ch513 异化原体影响微生物 生物异化暴走",
                BaseStats = new Dictionary<string, float>
                {
                    {"damage", 15f}, {"health", 60f}, {"armor", 5f},
                    {"speed", 1.5f}, {"attack_speed", 1.3f}
                },
                Traits = new List<string> { "Aggressive", "sm_disaster_infected" }
            };
            _creatures[c.Id] = c;
        }

        // ========== 世界树单位 ==========

        /// <summary>世界树战士 - 原著世界树文明的基础单位</summary>
        private static void RegisterWorldTreeUnit()
        {
            var c = new CreatureDef
            {
                Id = "sm_worldtree_soldier",
                Name = "世界树战士",
                CloneFrom = "human",
                TexturePath = "actors/sm_creatures/worldtree_soldier",
                KingdomWild = "worldtree",
                KingdomCiv = "worldtree",
                IsSpecial = true,
                Size = ActorSize.S14_Cow,
                NovelReference = "原著 世界树文明 母体 世界树战士",
                BaseStats = new Dictionary<string, float>
                {
                    {"damage", 20f}, {"health", 100f}, {"armor", 15f},
                    {"speed", 1.0f}, {"attack_speed", 1.0f}
                },
                Traits = new List<string> { "Strong", "Tough", "sm_worldtree" }
            };
            _creatures[c.Id] = c;
        }

        // ========== 注册到WB ==========

        /// <summary>将所有自定义生物注册到WB的ActorAssetLibrary</summary>
        private static void RegisterToWorldBox()
        {
            foreach (var kvp in _creatures)
            {
                var creature = kvp.Value;
                if (creature.IsRegistered) continue;

                try
                {
                    // 克隆模板ActorAsset（参数顺序：clone(新ID, 源ID)）
                    ActorAsset template = AssetManager.actor_library.get(creature.CloneFrom);
                    if (template == null)
                    {
                        Debug.LogWarning($"[超神机械师] 自定义生物{creature.Id}模板{creature.CloneFrom}不存在，跳过");
                        continue;
                    }

                    ActorAsset asset = AssetManager.actor_library.clone(creature.Id, creature.CloneFrom);
                    if (asset == null)
                    {
                        Debug.LogWarning($"[超神机械师] 自定义生物{creature.Id}克隆失败，跳过");
                        continue;
                    }

                    // 设置属性
                    asset.id = creature.Id;
                    asset.name_locale = $"species_{creature.Id}";
                    asset.kingdom_id_wild = creature.KingdomWild;
                    asset.kingdom_id_civilization = creature.KingdomCiv;
                    asset.special = creature.IsSpecial;
                    asset.actor_size = creature.Size;
                    asset.unit_other = true;       // 非文明单位
                    asset.use_phenotypes = false;  // 不使用表型颜色

                    // 应用基础属性
                    foreach (var stat in creature.BaseStats)
                    {
                        ApplyStat(asset, stat.Key, stat.Value);
                    }

                    // 添加特质
                    foreach (var trait in creature.Traits)
                    {
                        if (!asset.traits.Contains(trait))
                        {
                            asset.traits.Add(trait);
                        }
                    }

                    // 本地化名称
                    LocalizedTextManager.add($"species_{creature.Id}", creature.Name, pReplace: true);
                    LocalizedTextManager.add($"species_{creature.Id}_desc", creature.NovelReference, pReplace: true);

                    creature.IsRegistered = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[超神机械师] 自定义生物{creature.Id}注册失败: {e.Message}");
                }
            }
        }

        /// <summary>应用属性到ActorAsset</summary>
        private static void ApplyStat(ActorAsset asset, string key, float value)
        {
            try
            {
                switch (key)
                {
                    case "damage":
                        asset.base_stats["damage"] = value;
                        break;
                    case "health":
                        asset.base_stats["health"] = value;
                        break;
                    case "armor":
                        asset.base_stats["armor"] = value;
                        break;
                    case "speed":
                        asset.base_stats["speed"] = value;
                        break;
                    case "attack_speed":
                        asset.base_stats["attack_speed"] = value;
                        break;
                }
            }
            catch { /* 属性不存在时忽略 */ }
        }

        // ========== 查询接口 ==========

        /// <summary>根据ID获取生物定义</summary>
        public static CreatureDef GetCreature(string id)
        {
            _creatures.TryGetValue(id, out var creature);
            return creature;
        }

        /// <summary>获取所有自定义生物</summary>
        public static Dictionary<string, CreatureDef> GetAllCreatures() => _creatures;

        /// <summary>生成自定义生物</summary>
        public static Actor SpawnCreature(string id, WorldTile tile)
        {
            if (!_creatures.ContainsKey(id)) return null;
            if (World.world == null || World.world.units == null) return null;

            try
            {
                Actor actor = World.world.units.createNewUnit(id, tile);
                return actor;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 生成生物{id}失败: {e.Message}");
                return null;
            }
        }
    }
}
