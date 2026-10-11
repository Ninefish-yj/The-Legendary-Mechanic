using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 机械军团系统
    /// 对照原著：韩萧的机械军团是机械系核心特色
    /// 包含多种机械单位类型，通过机械工厂制造或虚拟创世召唤
    /// </summary>
    public static class SuperMechMechLegion
    {
        /// <summary>机械单位类型定义</summary>
        public class MechUnitDef
        {
            public string id;          // 单位类型ID
            public string name;        // 名称本地化key
            public string desc;        // 描述本地化key
            public int tier;           // 等级（1-5，对应原著机械单位品级）
            public int baseRank;       // 基础阶位（0-13）
            public float hpMult;       // 生命倍率
            public float dmgMult;      // 伤害倍率
            public float spdMult;      // 速度倍率
            public string[] traits;    // 附加特质
            public int cost;           // 制造成本（潜能点）
            public string icon;        // 图标
        }

        /// <summary>原著机械单位类型（对照原著）</summary>
        public static readonly MechUnitDef[] UnitTypes =
        {
            // === Tier1：低端机械单位 ===
            new MechUnitDef
            {
                id = "mech_ranger",
                name = "sm_mech_ranger_name",
                desc = "sm_mech_ranger_desc",
                tier = 1,
                baseRank = 1, // E阶
                hpMult = 0.8f,
                dmgMult = 0.9f,
                spdMult = 1.3f,
                traits = new[] { "aggressive" },
                cost = 50,
                icon = "ui/Icons/iconSword"
            },
            new MechUnitDef
            {
                id = "mech_soldier",
                name = "sm_mech_soldier_name",
                desc = "sm_mech_soldier_desc",
                tier = 1,
                baseRank = 2, // D阶
                hpMult = 1.0f,
                dmgMult = 1.0f,
                spdMult = 1.0f,
                traits = new[] { "aggressive" },
                cost = 80,
                icon = "ui/Icons/iconShield"
            },

            // === Tier2：中端机械单位 ===
            new MechUnitDef
            {
                id = "mech_elite",
                name = "sm_mech_elite_name",
                desc = "sm_mech_elite_desc",
                tier = 2,
                baseRank = 4, // C阶
                hpMult = 1.3f,
                dmgMult = 1.2f,
                spdMult = 1.1f,
                traits = new[] { "aggressive", "strong" },
                cost = 200,
                icon = "ui/Icons/iconSword"
            },
            new MechUnitDef
            {
                id = "mech_heavy",
                name = "sm_mech_heavy_name",
                desc = "sm_mech_heavy_desc",
                tier = 2,
                baseRank = 4, // C阶
                hpMult = 1.8f,
                dmgMult = 1.1f,
                spdMult = 0.7f,
                traits = new[] { "aggressive", "tough" },
                cost = 250,
                icon = "ui/Icons/iconShield"
            },

            // === Tier3：高端机械单位 ===
            new MechUnitDef
            {
                id = "mech_immortal",
                name = "sm_mech_immortal_name",
                desc = "sm_mech_immortal_desc",
                tier = 3,
                baseRank = 6, // B阶
                hpMult = 2.0f,
                dmgMult = 1.5f,
                spdMult = 1.0f,
                traits = new[] { "aggressive", "strong", "tough" },
                cost = 500,
                icon = "ui/Icons/iconSword"
            },
            new MechUnitDef
            {
                id = "mech_mecha",
                name = "sm_mech_mecha_name",
                desc = "sm_mech_mecha_desc",
                tier = 3,
                baseRank = 7, // B+阶
                hpMult = 2.5f,
                dmgMult = 1.8f,
                spdMult = 0.9f,
                traits = new[] { "aggressive", "strong", "giant" },
                cost = 800,
                icon = "ui/Icons/iconShield"
            },

            // === Tier4：顶级机械单位 ===
            new MechUnitDef
            {
                id = "mech_warship",
                name = "sm_mech_warship_name",
                desc = "sm_mech_warship_desc",
                tier = 4,
                baseRank = 9, // A+阶
                hpMult = 4.0f,
                dmgMult = 2.5f,
                spdMult = 0.6f,
                traits = new[] { "aggressive", "strong", "tough", "giant" },
                cost = 2000,
                icon = "ui/Icons/iconSword"
            },

            // === Tier5：终极机械单位 ===
            new MechUnitDef
            {
                id = "mech_god",
                name = "sm_mech_god_name",
                desc = "sm_mech_god_desc",
                tier = 5,
                baseRank = 11, // S+阶
                hpMult = 6.0f,
                dmgMult = 3.5f,
                spdMult = 1.0f,
                traits = new[] { "aggressive", "strong", "tough", "giant", "immortal" },
                cost = 5000,
                icon = "ui/Icons/iconShield"
            },
        };

        private static readonly Dictionary<string, MechUnitDef> _defMap = new Dictionary<string, MechUnitDef>();
        private static readonly Dictionary<long, string> _unitType = new Dictionary<long, string>(); // unitId -> mech type id

        public static void Register()
        {
            foreach (var def in UnitTypes)
            {
                _defMap[def.id] = def;
                LocalizedTextManager.add(def.name, GetDefaultName(def.id), pReplace: true);
                LocalizedTextManager.add(def.desc, GetDefaultDesc(def.id), pReplace: true);
            }
            Debug.Log($"[超神机械师] 机械军团系统注册完成：{UnitTypes.Length}种机械单位");
        }

        /// <summary>获取单位类型定义</summary>
        public static MechUnitDef GetDef(string id)
        {
            _defMap.TryGetValue(id, out var def);
            return def;
        }

        /// <summary>获取单位的机械类型</summary>
        public static string GetUnitType(Actor a)
        {
            if (a == null) return null;
            _unitType.TryGetValue(a.id, out var type);
            return type;
        }

        /// <summary>制造机械单位（机械工厂/虚拟创世调用）</summary>
        public static Actor SpawnMechUnit(Actor creator, string mechTypeId, WorldTile tile)
        {
            if (creator == null || !creator.isAlive()) return null;
            if (!_defMap.TryGetValue(mechTypeId, out var def)) return null;

            // 检查成本
            if (SuperMechPotential.GetPotential(creator) < def.cost) return null;
            SuperMechPotential.SpendPotential(creator, def.cost);

            if (tile == null) tile = creator.currentTile;
            if (tile == null) tile = World.world.GetRandomTile();

            Actor mech = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (mech == null) return null;

            // 设置机械系特质
            mech.addTrait(SuperMechTraits.ClassMech);
            mech.addTrait("sm_summoned");
            foreach (var trait in def.traits)
            {
                if (!mech.hasTrait(trait))
                    mech.addTrait(trait);
            }

            // 设置阶位
            int creatorRank = SuperMechAdvancement.GetExactRankIndex(creator);
            int rank = Mathf.Min(def.baseRank + (creatorRank - def.baseRank) / 2, 13);
            SuperMechAdvancement.SetExactRank(mech, rank);

            // 应用属性倍率
            mech.data.health_max = (int)(mech.data.health_max * def.hpMult);
            mech.data.health = mech.data.health_max;
            mech.data.damage = (int)(mech.data.damage * def.dmgMult);
            mech.data.speed = (int)(mech.data.speed * def.spdMult);

            // 记录类型和召唤者
            _unitType[mech.id] = def.id;
            var ctx = SuperMechActorContextRegistry.Get(mech);
            if (ctx != null)
            {
                ctx.summonerId = creator.id;
                ctx.expireAge = mech.age + 1200; // 机械单位寿命更长
            }

            Debug.Log($"[超神机械师] 制造机械单位：{def.id}（阶位{rank}），由{creator.getName()}制造");
            return mech;
        }

        /// <summary>获取可制造的机械单位列表（根据制造者阶位解锁）</summary>
        public static List<MechUnitDef> GetAvailableTypes(Actor creator)
        {
            int rank = SuperMechAdvancement.GetExactRankIndex(creator);
            var list = new List<MechUnitDef>();
            foreach (var def in UnitTypes)
            {
                // 制造者阶位达到单位基础阶位即可制造
                if (rank >= def.baseRank)
                    list.Add(def);
            }
            return list;
        }

        /// <summary>清理死亡单位数据</summary>
        public static void CleanupDead(HashSet<long> aliveIds)
        {
            var toRemove = new List<long>();
            foreach (var id in _unitType.Keys)
            {
                if (!aliveIds.Contains(id))
                    toRemove.Add(id);
            }
            foreach (var id in toRemove)
                _unitType.Remove(id);
        }

        public static void Clear()
        {
            _unitType.Clear();
        }

        // === 默认名称（对照原著） ===
        private static string GetDefaultName(string id)
        {
            return id switch
            {
                "mech_ranger" => "游骑兵",
                "mech_soldier" => "械兵",
                "mech_elite" => "精英械兵",
                "mech_heavy" => "重装械兵",
                "mech_immortal" => "不灭械兵",
                "mech_mecha" => "战斗机甲",
                "mech_warship" => "星际战舰",
                "mech_god" => "机械战神",
                _ => id
            };
        }

        private static string GetDefaultDesc(string id)
        {
            return id switch
            {
                "mech_ranger" => "低端多功能机器人，全面性高，改造潜力大",
                "mech_soldier" => "标准机械士兵，量产型作战单位",
                "mech_elite" => "精英机械单位，经过强化改造",
                "mech_heavy" => "重装机械单位，高防御高生命",
                "mech_immortal" => "不灭械兵，高级机械士兵，难以摧毁",
                "mech_mecha" => "大型战斗机甲，火力强大",
                "mech_warship" => "星际战舰，超大型作战平台",
                "mech_god" => "机械战神，终极机械生命体",
                _ => id
            };
        }
    }
}
