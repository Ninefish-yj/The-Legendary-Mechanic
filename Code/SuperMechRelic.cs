using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备系统（原著+原版EquipmentAsset）：
    /// 原著ch1040："金色品质，便代表着宇宙宝物级的装备"。
    /// 用原版EquipmentAsset注册装备物品，设置品质和base_stats，
    /// 单位装备后原版自动merge属性（Actor.cs:1829 stats.mergeStats(equipmentAsset.base_stats)）。
    /// 不直接加单位身上——单位不是装备。
    /// 品质7级（在原版4级Rarity基础上扩展）：
    /// 普通(R0)→精良(R1)→史诗(R2)→传说(R3)→金色·宇宙宝物级→宇宙奇观级→超神级
    /// 原版Rarity枚举只有4级，金色及以上用int rarity字段扩展，quality取R3_Legendary。
    /// </summary>
    public static class SuperMechRelic
    {
        // 装备品质定义（id, 名称, 品质等级int, 原版Rarity, 图标, 伤害倍率, 生命倍率）
        public class EquipDef
        {
            public string id;
            public string name;
            public int qualityLevel;  // 扩展品质等级 0-6
            public Rarity rarity;     // 原版Rarity（超过3都取R3）
            public string icon;
            public float dmgMul;
            public float hpMul;
        }

        public static readonly List<EquipDef> Equipments = new List<EquipDef>
        {
            // 原版4级
            new EquipDef { id="sm_eq_normal",  name="普通装备",       qualityLevel=0, rarity=Rarity.R0_Normal,    icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.0f, hpMul=1.0f },
            new EquipDef { id="sm_eq_fine",    name="精良装备",       qualityLevel=1, rarity=Rarity.R1_Rare,      icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.3f, hpMul=1.2f },
            new EquipDef { id="sm_eq_epic",    name="史诗装备",       qualityLevel=2, rarity=Rarity.R2_Epic,      icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.8f, hpMul=1.6f },
            new EquipDef { id="sm_eq_legend",  name="传说装备",       qualityLevel=3, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconBlessing", dmgMul=2.5f, hpMul=2.2f },
            // 扩展：原著金色=宇宙宝物级（在传说之上）
            new EquipDef { id="sm_eq_gold",    name="金色·宇宙宝物级", qualityLevel=4, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=4.0f, hpMul=3.5f },
            // 扩展：宇宙奇观级（时空琥珀级别，具有"绝对性"）
            new EquipDef { id="sm_eq_wonder",  name="宇宙奇观级",     qualityLevel=5, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=8.0f, hpMul=7.0f },
            // 扩展：超神级（仅超神机械师可造，原著最高）
            new EquipDef { id="sm_eq_super",   name="超神级装备",     qualityLevel=6, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=15.0f, hpMul=12.0f },
        };

        // 上次生命值（检测战斗结束）
        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        // 战斗中累计时间（用于掉落判定）
        private static readonly Dictionary<long, float> _combatTime = new Dictionary<long, float>();

        public static void Register()
        {
            // 注册装备物品到原版物品库（clone自amulet模板）
            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            if (library == null)
            {
                Debug.LogError("[超神机械师] AssetManager.items 为空，无法注册装备");
                return;
            }

            EquipmentAsset template = library.get("$amulet");
            if (template == null) template = library.get("$ring");
            if (template == null)
            {
                Debug.LogError("[超神机械师] 找不到amulet/ring装备模板");
                return;
            }

            int registered = 0;
            foreach (var def in Equipments)
            {
                if (library.get(def.id) != null) continue;
                EquipmentAsset asset = library.clone(def.id, template.id);
                if (asset == null) continue;

                ((Asset)asset).id = def.id;
                ((ItemAsset)asset).equipment_type = EquipmentType.Amulet;
                ((ItemAsset)asset).material = string.Empty;
                ((ItemAsset)asset).animated = false;
                ((ItemAsset)asset).is_pool_weapon = false;
                ((ItemAsset)asset).quality = def.rarity;
                ((ItemAsset)asset).rarity = def.qualityLevel; // 扩展品质等级（int，可超过原版4级）
                ((BaseUnlockableAsset)asset).base_stats = new BaseStats();
                ((BaseUnlockableAsset)asset).base_stats["multiplier_damage"] = def.dmgMul;
                ((BaseUnlockableAsset)asset).base_stats["multiplier_health"] = def.hpMul;
                ((BaseUnlockableAsset)asset).path_icon = def.icon;
                ((BaseUnlockableAsset)asset).unlock(true);

                LocalizedTextManager.add("item_" + def.id, def.name, pReplace: true);
                LocalizedTextManager.add("item_" + def.id + "_desc",
                    $"{def.name}。伤害×{def.dmgMul}，生命×{def.hpMul}。", pReplace: true);
                registered++;
            }

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
                tile.doUnits(delegate (Actor a) { EquipItem(a, 4); }); // index 4 = 金色
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_relic_gold", "赐予金色装备", pReplace: true);

            Debug.Log($"[超神机械师] 装备系统注册完成：{registered}件装备（普通→金色，原版EquipmentAsset）");
        }

        /// <summary>每tick：战斗中概率掉落装备。</summary>
        public static void TickRelicDrops()
        {
            if (!SuperMechConfig.RelicDropEnabled) return;
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
                        TryDropEquip(a);
                    }
                    else if (ct > 0)
                    {
                        _combatTime[a.id] = 0f;
                    }
                }
            }
        }

        /// <summary>尝试掉落装备。品质随阶位提升。</summary>
        private static void TryDropEquip(Actor a)
        {
            int rank = SuperMechAdvancement.GetRankIndex(a);
            float dropChance = SuperMechConfig.RelicDropRate + rank * 0.01f;
            if (Random.value > dropChance) return;

            // 品质roll：阶位越高roll上限越高
            // F-D(0-3):最高精良(1), C-B(4-7):最高史诗(2), A-S(8-10):最高传说(3)
            // S+-SS(11-12):最高金色(4), X(13):最高宇宙奇观(5), 超神级(6)不掉落只能造
            int maxQuality = rank <= 3 ? 1 : rank <= 7 ? 2 : rank <= 10 ? 3 : rank <= 12 ? 4 : 5;
            int quality = Random.Range(0, maxQuality + 1);

            EquipItem(a, quality);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 战斗掉落装备：{Equipments[quality].name}（掉落率{dropChance:F0%}）");
        }

        /// <summary>装备物品到amulet槽（高级替换低级）。用原版装备系统，不直接加单位属性。</summary>
        public static void EquipItem(Actor a, int qualityIndex)
        {
            if (a == null || qualityIndex < 0 || qualityIndex >= Equipments.Count) return;
            if (a.equipment == null) return;

            var def = Equipments[qualityIndex];

            // 检查当前amulet槽是否已有更高级装备
            ActorEquipmentSlot slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot != null && !slot.isEmpty())
            {
                Item current = slot.getItem();
                if (current != null)
                {
                    int currentIdx = GetEquipIndex(current);
                    if (currentIdx >= qualityIndex) return; // 已有同级或更高级，不替换
                    slot.takeAwayItem(); // 移除旧装备
                }
            }

            // 创建物品并装备
            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            EquipmentAsset asset = library.get(def.id);
            if (asset == null) return;

            try
            {
                Item item = World.world.items.generateItem(asset, a.kingdom, a.getName(), 0, a, 1, false);
                if (item == null) return;
                item.calculateValues();
                slot.setItem(item, a);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 装备物品失败: {e.Message}");
            }
        }

        /// <summary>获取当前装备的品质索引。</summary>
        public static int GetCurrentEquipIndex(Actor a)
        {
            if (a == null || a.equipment == null) return -1;
            ActorEquipmentSlot slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null || slot.isEmpty()) return -1;
            Item item = slot.getItem();
            if (item == null) return -1;
            return GetEquipIndex(item);
        }

        /// <summary>从物品获取品质索引。</summary>
        private static int GetEquipIndex(Item item)
        {
            if (item == null || item.asset == null) return -1;
            string id = ((Asset)item.asset).id;
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (Equipments[i].id == id) return i;
            }
            return -1;
        }

        /// <summary>获取当前装备名。</summary>
        public static string GetCurrentEquipName(Actor a)
        {
            int idx = GetCurrentEquipIndex(a);
            return idx >= 0 ? Equipments[idx].name : "无";
        }

        /// <summary>清空装备数据（世界切换用）。</summary>
        public static void Clear()
        {
            _lastHealth.Clear();
            _combatTime.Clear();
        }
    }

    /// <summary>
    /// 宇宙宝物系统（原著ch1008/ch1040）：
    /// 宇宙宝物是独立于普通装备的特殊物品类别，不是装备品质链的一级。
    /// 分两类：人造宇宙宝物、天然宇宙奇观。
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
            new CosmicRelicDef { id="sm_cr_amber", name="时空琥珀", desc="ch1008最经典的无解宇宙奇观，可封印时空。十个巅峰超A合力也损伤不了分毫。", isWonder=true, wonderType="天然", dmgMul=50f, hpMul=50f },
            new CosmicRelicDef { id="sm_cr_loop_spacetime", name="循环时空", desc="ch1217独一份的次级维度宇宙奇观，源能碎片培育出的循环时空，无法复制。", isWonder=true, wonderType="天然", dmgMul=30f, hpMul=40f },
            new CosmicRelicDef { id="sm_cr_soul_transfer", name="转魂仪", desc="ch1181摩多文明的宇宙奇观，可随意转移灵魂并无视排异。原为人工产物，变异后与次级维度产生联系。", isWonder=true, wonderType="变异", dmgMul=35f, hpMul=35f },
            new CosmicRelicDef { id="sm_cr_world_tree", name="世界树", desc="ch1333性质独特的宇宙奇观，拥有意志同时具备工具属性，有信息态能力。", isWonder=true, wonderType="系统级", dmgMul=60f, hpMul=60f },
            new CosmicRelicDef { id="sm_cr_sanctuary", name="圣所", desc="ch1225推测为信息态方面的宇宙奇观，具有记录超A级信息的功能，可复活超A级。", isWonder=true, wonderType="系统级", dmgMul=40f, hpMul=45f },
            new CosmicRelicDef { id="sm_cr_underworld", name="冥土", desc="ch1258经两姐妹完善后近似宇宙奇观级的宝物，灵魂维度。", isWonder=true, wonderType="系统级", dmgMul=35f, hpMul=45f },
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
                    id = r.id, path_icon = "ui/Icons/actor_traits/iconChosenOne", group_id = "sm_cosmic_relic",
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
                    var manMade = Relics.FindAll(r => !r.isWonder);
                    var pick = manMade[Random.Range(0, manMade.Count)];
                    EquipCosmicRelic(a, pick.id);
                });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_cosmic_relic", "赐予宇宙宝物", pReplace: true);

            int wonderCount = Relics.FindAll(r => r.isWonder).Count;
            Debug.Log($"[超神机械师] 宇宙宝物系统注册完成：{Relics.Count}件（人造{Relics.Count - wonderCount}件 + 宇宙奇观{wonderCount}件）");
        }

        /// <summary>装备宇宙宝物（同时只能有一件，高级替换低级）。</summary>
        public static void EquipCosmicRelic(Actor a, string relicId)
        {
            if (a == null) return;
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

        /// <summary>清空宝物数据（世界切换用）。</summary>
        public static void Clear()
        {
            _equipped.Clear();
        }
    }
}
