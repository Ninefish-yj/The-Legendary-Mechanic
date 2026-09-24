using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备品质系统（原著）：
    /// 普通装备品质从低到高：劣质灰→普通白→良好绿→优质蓝→极佳紫→珍稀粉→传说橙→神器红→金色
    /// 原著ch1040："金色品质，便代表着宇宙宝物级的装备"——金色是普通装备的顶级。
    /// 宇宙宝物是独立的特殊物品类别（秘法之殿/时空琥珀/万神权杖等），不是普通装备品质链的一级——见 SuperMechCosmicRelic。
    /// 使徒兵器是机械系高级造兵单位（ch1010），伴生武器约橙~红品质——留待造兵系统实现。
    /// 战斗中概率掉落装备，品质随击杀者阶位提升。同时只能装备一个品质（高级替换低级）。
    /// </summary>
    public static class SuperMechRelic
    {
        // 普通装备品质从低到高（9级，原著品质色）
        public const string QGray    = "sm_relic_gray";     // 劣质灰
        public const string QWhite   = "sm_relic_white";    // 普通白
        public const string QGreen   = "sm_relic_green";    // 良好绿
        public const string QBlue    = "sm_relic_blue";     // 优质蓝
        public const string QPurple  = "sm_relic_purple";   // 极佳紫
        public const string QPink    = "sm_relic_pink";     // 珍稀粉
        public const string QOrange  = "sm_relic_orange";   // 传说橙
        public const string QRed     = "sm_relic_red";      // 神器红
        public const string QGold    = "sm_relic_gold";     // 金色（顶级普通装备=宇宙宝物级门槛）

        public static readonly string[] QualityOrder = {
            QGray, QWhite, QGreen, QBlue, QPurple, QPink, QOrange, QRed, QGold
        };
        public static readonly string[] QualityNames = {
            "劣质灰", "普通白", "良好绿", "优质蓝", "极佳紫", "珍稀粉", "传说橙", "神器红", "金色"
        };

        // 上次生命值（检测战斗结束）
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        // 战斗中累计时间（用于掉落判定）
        private static readonly Dictionary<long, float> _combatTime = new Dictionary<long, float>();

        public static void Register()
        {
            AddRelic(QGray,   "劣质灰装", 0.8f, 0.8f);
            AddRelic(QWhite,  "普通白装", 1.0f, 1.0f);
            AddRelic(QGreen,  "良好绿装", 1.3f, 1.2f);
            AddRelic(QBlue,   "优质蓝装", 1.7f, 1.5f);
            AddRelic(QPurple, "极佳紫装", 2.2f, 2.0f);
            AddRelic(QPink,   "珍稀粉装", 3.0f, 2.8f);
            AddRelic(QOrange, "传说橙装", 4.5f, 4.0f);
            AddRelic(QRed,    "神器红装", 7.0f, 6.0f);
            AddRelic(QGold,   "金色装备", 12.0f, 10.0f);  // ch1040：金色=宇宙宝物级门槛

            // 注册"赐予金装"神权
            var givePower = new GodPower
            {
                id = "sm_give_relic_gold",
                name = "赐予金色装备",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            givePower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a) { EquipRelic(a, 8); }); // index 8 = 金色
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_relic_gold", "赐予金色装备", pReplace: true);

            Debug.Log("[超神机械师] 装备品质系统注册完成：9级品质（灰→金）");
        }

        /// <summary>每tick：战斗中概率掉落装备。</summary>
        public static void TickRelicDrops()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                float curHealth = a.data.health;
                float lastH;
                _lastHealth.TryGetValue(a.id, out lastH);
                bool inCombat = curHealth < lastH - 0.5f || SuperMechQi.IsInCombat(a);
                _lastHealth[a.id] = curHealth;

                if (inCombat)
                {
                    float ct;
                    _combatTime.TryGetValue(a.id, out ct);
                    _combatTime[a.id] = ct + tickInterval;
                }
                else
                {
                    float ct;
                    if (_combatTime.TryGetValue(a.id, out ct) && ct >= 10f)
                    {
                        _combatTime[a.id] = 0f;
                        TryDropRelic(a);
                    }
                    else if (ct > 0)
                    {
                        _combatTime[a.id] = 0f;
                    }
                }
            }
        }

        /// <summary>尝试掉落装备。品质随阶位提升。</summary>
        private static void TryDropRelic(Actor a)
        {
            int rank = SuperMechAdvancement.GetRankIndex(a);
            float dropChance = 0.1f + rank * 0.02f;
            if (Random.value > dropChance) return;

            // 品质roll：基础0-3，阶位越高roll上限越高（最高金色index8）
            int maxQuality = Mathf.Min(3 + rank / 2, 8);
            int quality = Random.Range(0, maxQuality + 1);

            EquipRelic(a, quality);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.Name} 战斗掉落装备：{QualityNames[quality]}（掉落率{dropChance:F0%}）");
        }

        /// <summary>装备（高级替换低级）。</summary>
        public static void EquipRelic(Actor a, int qualityIndex)
        {
            if (a == null || qualityIndex < 0 || qualityIndex >= QualityOrder.Length) return;

            int current = GetCurrentRelicIndex(a);
            if (current >= qualityIndex) return;

            for (int i = 0; i <= current; i++)
            {
                if (a.hasTrait(QualityOrder[i])) a.removeTrait(QualityOrder[i]);
            }

            a.addTrait(QualityOrder[qualityIndex]);
        }

        /// <summary>获取当前装备等级。</summary>
        public static int GetCurrentRelicIndex(Actor a)
        {
            for (int i = QualityOrder.Length - 1; i >= 0; i--)
            {
                if (a.hasTrait(QualityOrder[i])) return i;
            }
            return -1;
        }

        /// <summary>获取当前装备名。</summary>
        public static string GetCurrentRelicName(Actor a)
        {
            int idx = GetCurrentRelicIndex(a);
            return idx >= 0 ? QualityNames[idx] : "无";
        }

        private static void AddRelic(string id, string name, float dmgMul, float hpMul)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info",
                $"装备品质：{name}。伤害×{dmgMul}，生命×{hpMul}。", pReplace: true);
            var t = new ActorTrait
            {
                id = id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_relic",
                needs_to_be_explored = false, base_stats = new BaseStats()
            };
            t.base_stats["multiplier_damage"] = dmgMul;
            t.base_stats["multiplier_health"] = hpMul;
            AssetManager.traits.add(t);
        }
    }

    /// <summary>
    /// 宇宙宝物系统（原著ch1008/ch1040）：
    /// 宇宙宝物是独立于普通装备的特殊物品类别，不是装备品质链的一级。
    /// 分两类：
    /// 1. 人造宇宙宝物：奥斯汀秘法之殿、贝奥尼火核之地、光辉高维天启传送器、虚灵万神权杖、暗影提灯等
    /// 2. 天然宇宙奇观：时空琥珀等（最顶级，无解的存在）
    /// 一般超A手里有一个宇宙宝物就很好了（ch1001）。
    /// 宇宙级文明具备制造宇宙宝物的技术（ch1008）。
    /// 使徒兵器（ch1010）是机械系高级造兵单位，有火种能量源，伴生武器约橙~红品质，不属于宇宙宝物。
    /// </summary>
    public static class SuperMechCosmicRelic
    {
        public class CosmicRelicDef
        {
            public string id;
            public string name;
            public string desc;
            public bool isWonder;   // true=宇宙奇观，false=人造宇宙宝物
            public string wonderType; // 奇观类型：天然/变异/系统级
            public float dmgMul;
            public float hpMul;
        }

        // 原著出现的宇宙宝物与宇宙奇观
        // ch1008：宇宙宝物分人造（秘法之殿等）和天然宇宙奇观（时空琥珀等）
        // ch1172：宇宙奇观具有"绝对性"，巅峰超A也损伤不了
        // ch1082/ch1403：制造宇宙宝物有微小几率变异成宇宙奇观
        public static readonly List<CosmicRelicDef> Relics = new List<CosmicRelicDef>
        {
            // ===== 人造宇宙宝物 =====
            new CosmicRelicDef { id="sm_cr_secret_hall", name="秘法之殿", desc="ch1008奥斯汀的人造宇宙宝物，法师圣地。", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_fire_core", name="火核之地", desc="ch1008贝奥尼的人造宇宙宝物。", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_teleporter", name="高维天启传送器", desc="ch1008光辉联邦的战略级人造宇宙宝物。", isWonder=false, wonderType="", dmgMul=15f, hpMul=20f },
            new CosmicRelicDef { id="sm_cr_wand", name="万神权杖", desc="ch1008虚灵教派的人造宇宙宝物。", isWonder=false, wonderType="", dmgMul=22f, hpMul=12f },
            new CosmicRelicDef { id="sm_cr_shadow_lamp", name="暗影提灯", desc="ch1008灯芯是暗影维度源能碎片，可打开维度门户。", isWonder=false, wonderType="", dmgMul=18f, hpMul=18f },
            new CosmicRelicDef { id="sm_cr_evolution_cube", name="进化方块", desc="ch740激发物种潜力，西斯科用它完成超A物种蜕变。", isWonder=false, wonderType="", dmgMul=15f, hpMul=25f },

            // ===== 宇宙奇观（具有"绝对性"，ch1172）=====
            // 天然宇宙奇观
            new CosmicRelicDef { id="sm_cr_amber", name="时空琥珀", desc="ch1008最经典的无解宇宙奇观，可封印时空。十个巅峰超A合力也损伤不了分毫。", isWonder=true, wonderType="天然", dmgMul=50f, hpMul=50f },
            new CosmicRelicDef { id="sm_cr_loop_spacetime", name="循环时空", desc="ch1217独一份的次级维度宇宙奇观，源能碎片培育出的循环时空，无法复制。", isWonder=true, wonderType="天然", dmgMul=30f, hpMul=40f },
            // 变异宇宙奇观（人造物变异而成）
            new CosmicRelicDef { id="sm_cr_soul_transfer", name="转魂仪", desc="ch1181摩多文明的宇宙奇观，可随意转移灵魂并无视排异。原为人工产物，变异后与次级维度产生联系。只有转魂双子能发挥其能力。", isWonder=true, wonderType="变异", dmgMul=35f, hpMul=35f },
            // 系统级宇宙奇观（非物品，是维度/系统）
            new CosmicRelicDef { id="sm_cr_world_tree", name="世界树", desc="ch1333性质独特的宇宙奇观，拥有意志同时具备工具属性，有信息态能力，统计无数物体合成表。被命运之子称为'天敌'。", isWonder=true, wonderType="系统级", dmgMul=60f, hpMul=60f },
            new CosmicRelicDef { id="sm_cr_sanctuary", name="圣所", desc="ch1225推测为信息态方面的宇宙奇观，具有记录超A级信息的功能，可复活超A级。", isWonder=true, wonderType="系统级", dmgMul=40f, hpMul=45f },
            new CosmicRelicDef { id="sm_cr_underworld", name="冥土", desc="ch1258经两姐妹完善后近似宇宙奇观级的宝物，灵魂维度，运行机制完善，全盛状态可压制巅峰超A。", isWonder=true, wonderType="系统级", dmgMul=35f, hpMul=45f },
        };

        private static readonly Dictionary<long, string> _equipped = new Dictionary<long, string>();

        public static void Register()
        {
            foreach (var r in Relics)
            {
                string typeName = r.isWonder ? $"宇宙奇观（{r.wonderType}）" : "宇宙宝物";
                LocalizedTextManager.add("trait_" + r.id, r.name, pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info",
                    $"{typeName}（原著）。{r.desc}", pReplace: true);
                var t = new ActorTrait
                {
                    id = r.id, path_icon = "ui/Icons/actor_traits/iconHardSkin", group_id = "sm_cosmic_relic",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = r.dmgMul;
                t.base_stats["multiplier_health"] = r.hpMul;
                AssetManager.traits.add(t);
            }

            // 注册"赐予随机宇宙宝物"神权
            var givePower = new GodPower
            {
                id = "sm_give_cosmic_relic",
                name = "赐予宇宙宝物",
                path_icon = "ui/powers/power_bless",
                rank = PowerRank.Rank0_free,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = false
            };
            givePower.click_action += (WorldTile tile, string powerId) =>
            {
                if (tile == null) return true;
                tile.doUnits(delegate (Actor a)
                {
                    // 随机赐予一个人造宇宙宝物（宇宙奇观级太稀有，不随机给）
                    var manMade = Relics.FindAll(r => !r.isWonder);
                    var pick = manMade[Random.Range(0, manMade.Count)];
                    EquipCosmicRelic(a, pick.id);
                });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_cosmic_relic", "赐予宇宙宝物", pReplace: true);

            int wonderCount = Relics.FindAll(r => r.isWonder).Count;
            Debug.Log($"[超神机械师] 宇宙宝物系统注册完成：{Relics.Count}件（人造宇宙宝物{Relics.Count - wonderCount}件 + 宇宙奇观{wonderCount}件）");
        }

        /// <summary>装备宇宙宝物（同时只能有一件，高级替换低级）。</summary>
        public static void EquipCosmicRelic(Actor a, string relicId)
        {
            if (a == null) return;
            // 移除已有宇宙宝物
            if (_equipped.TryGetValue(a.data.id, out string oldId) && !string.IsNullOrEmpty(oldId))
            {
                if (a.hasTrait(oldId)) a.removeTrait(oldId);
            }
            a.addTrait(relicId);
            _equipped[a.data.id] = relicId;
        }

        /// <summary>获取当前装备的宇宙宝物名。</summary>
        public static string GetEquippedName(Actor a)
        {
            if (a == null) return "";
            if (_equipped.TryGetValue(a.data.id, out string id) && !string.IsNullOrEmpty(id))
            {
                var r = Relics.Find(x => x.id == id);
                return r != null ? r.name : "";
            }
            return "";
        }
    }
}
