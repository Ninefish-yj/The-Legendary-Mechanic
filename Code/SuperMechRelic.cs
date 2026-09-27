using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 装备系统（原著9级品质 + 原版EquipmentAsset兼容）：
    /// 原著品质（从原文提取）：灰→绿→蓝→淡紫→紫→粉(珍稀)→橙(传说)→银橙(使徒兵器)→金(宇宙宝物级)
    /// 用原版EquipmentAsset注册装备物品，走原版装备系统（装备槽/属性merge）。
    /// 兼容方案：ItemAsset.rarity(int)存9级品质0-8，quality(Rarity枚举)映射到原版4级（0→R0,1→R1,2→R2,3+→R3）。
    /// 单位装备后原版自动merge属性（Actor.cs:1829 stats.mergeStats(equipmentAsset.base_stats)）。
    /// 不直接加单位身上——单位不是装备。
    /// </summary>
    public static class SuperMechRelic
    {
        // 装备品质定义（原著9级）
        public class EquipDef
        {
            public string id;
            public string name;        // 原著品质名
            public int qualityLevel;   // 9级品质 0-8
            public Rarity rarity;      // 映射到原版Rarity（兼容原版UI）
            public string icon;
            public float dmgMul;
            public float hpMul;
        }

        // 原著9级品质（从原文提取）
        public static readonly List<EquipDef> Equipments = new List<EquipDef>
        {
            new EquipDef { id="sm_eq_gray",    name="sm_relic_094",       qualityLevel=0, rarity=Rarity.R0_Normal,    icon="ui/Icons/actor_traits/iconBlessing", dmgMul=0.8f, hpMul=0.8f },
            new EquipDef { id="sm_eq_green",   name="sm_relic_095",       qualityLevel=1, rarity=Rarity.R1_Rare,      icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.0f, hpMul=1.0f },
            new EquipDef { id="sm_eq_blue",    name="sm_relic_096",       qualityLevel=2, rarity=Rarity.R2_Epic,      icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.3f, hpMul=1.2f },
            new EquipDef { id="sm_eq_lightpurple", name="sm_relic_097", qualityLevel=3, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconBlessing", dmgMul=1.7f, hpMul=1.5f },
            new EquipDef { id="sm_eq_purple",  name="sm_relic_098",       qualityLevel=4, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconBlessing", dmgMul=2.2f, hpMul=1.8f },
            new EquipDef { id="sm_eq_pink",    name="sm_relic_099",  qualityLevel=5, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=3.0f, hpMul=2.5f },
            new EquipDef { id="sm_eq_orange",  name="sm_relic_100",  qualityLevel=6, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=4.5f, hpMul=3.5f },
            new EquipDef { id="sm_eq_silverorange", name="sm_relic_101", qualityLevel=7, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=7.0f, hpMul=5.0f },
            new EquipDef { id="sm_eq_gold",    name="sm_relic_102", qualityLevel=8, rarity=Rarity.R3_Legendary, icon="ui/Icons/actor_traits/iconChosenOne", dmgMul=12.0f, hpMul=8.0f },
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
                    $"sm_relic_103", pReplace: true);
                registered++;
            }

            // 注册"sm_relic_104"神权（随机赐予金色装备或宇宙宝物）
            var givePower = new GodPower
            {
                id = "sm_give_equip",
                name = "sm_relic_104",
                path_icon = "iconDivineLight",
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
                    // 50%概率赐予金色装备，50%概率赐予宇宙宝物
                    if (Random.value < 0.5f)
                    {
                        EquipItem(a, 8); // index 8 = 金色·宇宙宝物级
                    }
                    else
                    {
                        var manMade = SuperMechCosmicRelic.Relics.FindAll(r => !r.isWonder);
                        if (manMade.Count > 0)
                        {
                            var pick = manMade[Random.Range(0, manMade.Count)];
                            SuperMechCosmicRelic.EquipCosmicRelic(a, pick.id);
                        }
                        else
                        {
                            EquipItem(a, 8);
                        }
                    }
                });
                return true;
            };
            AssetManager.powers.add(givePower);
            LocalizedTextManager.add("power_sm_give_equip", LocalizedTextManager.getText("sm_relic_104"), pReplace: true);

            Debug.Log($"[超神机械师] 装备系统注册完成：{registered}件装备（普通→金色，原版EquipmentAsset）");
        }

        /// <summary>每tick：战斗中概率掉落装备。</summary>
        public static void TickRelicDrops()
        {
            if (!SuperMechConfig.RelicDropEnabled) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f; // 分组4：每4次UnifiedTick调用才跑一次本系统

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

            // 品质roll：阶位越高roll上限越高（原著9级）
            // F-D(0-3):最高绿色(1), C-B(4-7):最高蓝色(2), A-S(8-10):最高紫色(4)
            // S+-SS(11-12):最高橙色(6), X(13):最高银橙色(7), 金色(8)不掉落只能造
            int maxQuality = rank <= 3 ? 1 : rank <= 7 ? 2 : rank <= 10 ? 4 : rank <= 12 ? 6 : 7;
            int quality = Random.Range(0, maxQuality + 1);

            EquipItem(a, quality);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 战斗掉落装备：{Equipments[quality].name}（掉落率{dropChance:F0%}）");
        }

        /// <summary>装备物品到amulet槽（高级替换低级，旧装备自动回背包）。用原版装备系统，不直接加单位属性。</summary>
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
                    // 旧装备放回背包
                    if (currentIdx >= 0)
                    {
                        SuperMechEquipBag.AddToBag(a, Equipments[currentIdx].id);
                    }
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
                // 装备词条：根据品质roll随机属性
                SuperMechEquipAffix.OnEquip(a, qualityIndex);
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
        public static int GetEquipIndex(Item item)
        {
            if (item == null || item.asset == null) return -1;
            string id = ((Asset)item.asset).id;
            return GetEquipIndex(id);
        }

        /// <summary>从装备ID获取品质索引（public，供装备背包调用）。</summary>
        public static int GetEquipIndex(string equipId)
        {
            if (string.IsNullOrEmpty(equipId)) return -1;
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (Equipments[i].id == equipId) return i;
            }
            return -1;
        }

        /// <summary>获取当前装备名。</summary>
        public static string GetCurrentEquipName(Actor a)
        {
            int idx = GetCurrentEquipIndex(a);
            return idx >= 0 ? Equipments[idx].name : "sm_relic_105";
        }

        /// <summary>获取当前装备ID。</summary>
        public static string GetCurrentEquipId(Actor a)
        {
            int idx = GetCurrentEquipIndex(a);
            return idx >= 0 ? Equipments[idx].id : null;
        }

        /// <summary>清空装备数据（世界切换用）。</summary>
        public static void Clear()
        {
            _lastHealth.Clear();
            _combatTime.Clear();
        }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_lastHealth, alive);
            removed += SuperMechCleanup.CleanDict(_combatTime, alive);
            return removed;
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
            new CosmicRelicDef { id="sm_cr_secret_hall", name="sm_relic_106", desc="sm_relic_107", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_fire_core", name="sm_relic_108", desc="sm_relic_109", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_teleporter", name="sm_relic_110", desc="sm_relic_111", isWonder=false, wonderType="", dmgMul=15f, hpMul=20f },
            new CosmicRelicDef { id="sm_cr_wand", name="sm_relic_112", desc="sm_relic_113", isWonder=false, wonderType="", dmgMul=22f, hpMul=12f },
            new CosmicRelicDef { id="sm_cr_shadow_lamp", name="sm_relic_114", desc="sm_relic_115", isWonder=false, wonderType="", dmgMul=18f, hpMul=18f },
            new CosmicRelicDef { id="sm_cr_evolution_cube", name="sm_relic_116", desc="sm_relic_117", isWonder=false, wonderType="", dmgMul=15f, hpMul=25f },

            // ===== 宇宙奇观（具有"绝对性"，ch1172）=====
            new CosmicRelicDef { id="sm_cr_amber", name="sm_relic_118", desc="sm_relic_119", isWonder=true, wonderType="sm_relic_120", dmgMul=50f, hpMul=50f },
            new CosmicRelicDef { id="sm_cr_loop_spacetime", name="sm_relic_121", desc="sm_relic_122", isWonder=true, wonderType="sm_relic_120", dmgMul=30f, hpMul=40f },
            new CosmicRelicDef { id="sm_cr_soul_transfer", name="sm_relic_123", desc="sm_relic_124", isWonder=true, wonderType="sm_relic_125", dmgMul=35f, hpMul=35f },
            new CosmicRelicDef { id="sm_cr_world_tree", name="sm_relic_126", desc="sm_relic_127", isWonder=true, wonderType="sm_relic_128", dmgMul=60f, hpMul=60f },
            new CosmicRelicDef { id="sm_cr_sanctuary", name="sm_relic_129", desc="sm_relic_130", isWonder=true, wonderType="sm_relic_128", dmgMul=40f, hpMul=45f },
            new CosmicRelicDef { id="sm_cr_underworld", name="sm_relic_131", desc="sm_relic_132", isWonder=true, wonderType="sm_relic_128", dmgMul=35f, hpMul=45f },
        };

        private static readonly Dictionary<long, string> _equipped = new Dictionary<long, string>();

        public static void Register()
        {
            foreach (var r in Relics)
            {
                string typeName = r.isWonder ? $"sm_relic_133" : "sm_relic_134";
                LocalizedTextManager.add("trait_" + r.id, r.name, pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info",
                    $"sm_relic_135", pReplace: true);
                var t = new ActorTrait
                {
                    id = r.id, path_icon = "ui/Icons/actor_traits/iconChosenOne", group_id = "sm_cosmic_relic",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = r.dmgMul;
                t.base_stats["multiplier_health"] = r.hpMul;
                AssetManager.traits.add(t);
            }

            // "赐予宇宙宝物"神权已合并到"sm_relic_104"神权（上方），不再单独注册

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

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_equipped, alive);
        }
    }
}
