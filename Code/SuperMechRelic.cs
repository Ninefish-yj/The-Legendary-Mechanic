using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechRelic
    {
        public class EquipDef
        {
            public string id;
            public string name;
            public int qualityLevel;
            public Rarity rarity;
            public string icon;
            public float dmgMul;
            public float hpMul;
        }

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

        private static readonly Dictionary<long, float> _lastHealth = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _combatTime = new Dictionary<long, float>();

        public static void Register()
        {
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
                ((ItemAsset)asset).rarity = def.qualityLevel;
                ((BaseUnlockableAsset)asset).base_stats = new BaseStats();
                ((BaseUnlockableAsset)asset).base_stats["multiplier_damage"] = def.dmgMul;
                ((BaseUnlockableAsset)asset).base_stats["multiplier_health"] = def.hpMul;
                ((BaseUnlockableAsset)asset).path_icon = def.icon;
                ((BaseUnlockableAsset)asset).unlock(true);

                LocalizedTextManager.add("item_" + def.id, LocalizedTextManager.getText(def.name), pReplace: true);
                LocalizedTextManager.add("item_" + def.id + "_desc",
                    LocalizedTextManager.getText("sm_relic_103"), pReplace: true);
                registered++;
            }

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
                    if (Random.value < 0.5f)
                    {
                        EquipItem(a, 8);
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

        public static void TickRelicDrops()
        {
            if (!SuperMechConfig.RelicDropEnabled) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            float tickInterval = SuperMechConfig.TickInterval * 4f;

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

        private static void TryDropEquip(Actor a)
        {
            int rank = SuperMechAdvancement.GetRankIndex(a);
            float dropChance = SuperMechConfig.RelicDropRate + rank * 0.01f;
            if (Random.value > dropChance) return;

            int maxQuality = rank <= 3 ? 1 : rank <= 7 ? 2 : rank <= 10 ? 4 : rank <= 12 ? 6 : 7;
            int quality = Random.Range(0, maxQuality + 1);

            EquipItem(a, quality);
            if (SuperMechConfig.LogVerbose)
                Debug.Log($"[超神机械师] {a.name} 战斗掉落装备：{Equipments[quality].name}（掉落率{dropChance:F0%}）");
        }

        public static void EquipItem(Actor a, int qualityIndex)
        {
            if (a == null || qualityIndex < 0 || qualityIndex >= Equipments.Count) return;
            if (a.equipment == null) return;

            var def = Equipments[qualityIndex];

            ActorEquipmentSlot slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot != null && !slot.isEmpty())
            {
                Item current = slot.getItem();
                if (current != null)
                {
                    int currentIdx = GetEquipIndex(current);
                    if (currentIdx >= qualityIndex) return;
                    if (currentIdx >= 0)
                    {
                        SuperMechEquipBag.AddToBag(a, Equipments[currentIdx].id);
                    }
                    slot.takeAwayItem();
                }
            }

            var library = (AssetLibrary<EquipmentAsset>)(object)AssetManager.items;
            EquipmentAsset asset = library.get(def.id);
            if (asset == null) return;

            try
            {
                Item item = World.world.items.generateItem(asset, a.kingdom, a.getName(), 0, a, 1, false);
                if (item == null) return;
                item.calculateValues();
                slot.setItem(item, a);
                SuperMechEquipAffix.OnEquip(a, qualityIndex);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[超神机械师] 装备物品失败: {e.Message}");
            }
        }

        public static int GetCurrentEquipIndex(Actor a)
        {
            if (a == null || a.equipment == null) return -1;
            ActorEquipmentSlot slot = a.equipment.getSlot(EquipmentType.Amulet);
            if (slot == null || slot.isEmpty()) return -1;
            Item item = slot.getItem();
            if (item == null) return -1;
            return GetEquipIndex(item);
        }

        public static int GetEquipIndex(Item item)
        {
            if (item == null || item.asset == null) return -1;
            string id = ((Asset)item.asset).id;
            return GetEquipIndex(id);
        }

        public static int GetEquipIndex(string equipId)
        {
            if (string.IsNullOrEmpty(equipId)) return -1;
            for (int i = 0; i < Equipments.Count; i++)
            {
                if (Equipments[i].id == equipId) return i;
            }
            return -1;
        }

        public static string GetCurrentEquipName(Actor a)
        {
            int idx = GetCurrentEquipIndex(a);
            return idx >= 0 ? Equipments[idx].name : "sm_relic_105";
        }

        public static string GetCurrentEquipId(Actor a)
        {
            int idx = GetCurrentEquipIndex(a);
            return idx >= 0 ? Equipments[idx].id : null;
        }

        public static void Clear()
        {
            _lastHealth.Clear();
            _combatTime.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_lastHealth, alive);
            removed += SuperMechCleanup.CleanDict(_combatTime, alive);
            return removed;
        }
    }

    public static class SuperMechCosmicRelic
    {
        public class CosmicRelicDef
        {
            public string id;
            public string name;
            public string desc;
            public bool isWonder;
            public string wonderType;
            public float dmgMul;
            public float hpMul;
        }

        public static readonly List<CosmicRelicDef> Relics = new List<CosmicRelicDef>
        {
            new CosmicRelicDef { id="sm_cr_secret_hall", name="sm_relic_106", desc="sm_relic_107", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_fire_core", name="sm_relic_108", desc="sm_relic_109", isWonder=false, wonderType="", dmgMul=20f, hpMul=15f },
            new CosmicRelicDef { id="sm_cr_teleporter", name="sm_relic_110", desc="sm_relic_111", isWonder=false, wonderType="", dmgMul=15f, hpMul=20f },
            new CosmicRelicDef { id="sm_cr_wand", name="sm_relic_112", desc="sm_relic_113", isWonder=false, wonderType="", dmgMul=22f, hpMul=12f },
            new CosmicRelicDef { id="sm_cr_shadow_lamp", name="sm_relic_114", desc="sm_relic_115", isWonder=false, wonderType="", dmgMul=18f, hpMul=18f },
            new CosmicRelicDef { id="sm_cr_evolution_cube", name="sm_relic_116", desc="sm_relic_117", isWonder=false, wonderType="", dmgMul=15f, hpMul=25f },

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
                string typeName = r.isWonder ? LocalizedTextManager.getText("sm_relic_133") : "sm_relic_134";
                LocalizedTextManager.add("trait_" + r.id, LocalizedTextManager.getText(r.name), pReplace: true);
                LocalizedTextManager.add("trait_" + r.id + "_info",
                    LocalizedTextManager.getText("sm_relic_135"), pReplace: true);
                var t = new ActorTrait
                {
                    id = r.id, path_icon = "ui/Icons/actor_traits/iconChosenOne", group_id = "sm_cosmic_relic",
                    needs_to_be_explored = false, base_stats = new BaseStats()
                };
                t.base_stats["multiplier_damage"] = r.dmgMul;
                t.base_stats["multiplier_health"] = r.hpMul;
                AssetManager.traits.add(t);
            }


            int wonderCount = Relics.FindAll(r => r.isWonder).Count;
            Debug.Log($"[超神机械师] 宇宙宝物系统注册完成：{Relics.Count}件（人造{Relics.Count - wonderCount}件 + 宇宙奇观{wonderCount}件）");
        }

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

        public static void Clear()
        {
            _equipped.Clear();
        }

        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_equipped, alive);
        }
    }
}
