using NeoModLoader.api;
using NeoModLoader.services;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPowers
    {
        public const string SummonDescendant = "sm_summon_awakened"; // 降临者（玩家化身）；id保留旧值不影响运行时
        public const string DisasterAlien = "sm_disaster_alien";
        public const string OpenSanctuary = "sm_open_sanctuary";
        public const string OpenRank = "sm_open_rank";
        public const string OpenFaction = "sm_open_faction";

        public const string TabName = "SuperMech";
        private static bool _buttonsCreated;
        private static PowersTab _modTab;

        public static void Register()
        {
            // 只有需要点击地图的神力才注册GodPower
            AddAwakenedPower(SummonDescendant, "sm_powers_922", "actor_traits/iconChosenOne");
            AddDisaster(DisasterAlien, "sm_powers_924");
            RegisterDisasterTrait();
        }

        /// <summary>异化怪物trait（原著第513章：异化原体影响微生物，生物异化暴走）</summary>
        private static bool _disasterTraitRegistered;
        private static void RegisterDisasterTrait()
        {
            if (_disasterTraitRegistered) return;
            _disasterTraitRegistered = true;
            try
            {
                LocalizedTextManager.add("trait_sm_disaster_infected", LocalizedTextManager.getText("sm_disaster_infected_080"), pReplace: true);
                LocalizedTextManager.add("trait_sm_disaster_infected_info", LocalizedTextManager.getText("sm_disaster_infected_081"), pReplace: true);
                var t = new ActorTrait
                {
                    id = "sm_disaster_infected",
                    path_icon = "actor_traits/iconPoison",
                    group_id = "sm_disaster",
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                // 微生物异化强化：狂暴化 + 血肉畸变
                t.base_stats["multiplier_damage"] = 1.8f;
                t.base_stats["multiplier_health"] = 1.4f;
                AssetManager.traits.add(t);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[超神机械师] 异化怪物trait注册失败: " + e.Message);
            }
        }

        public static void TryCreateButtons()
        {
            if (_buttonsCreated) return;
            if (PowerButtonSelector.instance == null) return;

            // 创建独立的"超神机械师"Tab
            if (_modTab == null)
            {
                Sprite tabIcon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight");
                _modTab = TabManager.CreateTab(TabName, "sm_tab_title", "sm_tab_desc", tabIcon);
                if (_modTab == null)
                {
                    Debug.LogWarning("[超神机械师] 创建Tab失败，稍后重试");
                    return;
                }
                _modTab.SetLayout(new System.Collections.Generic.List<string> { "tools" });
                _modTab.UpdateLayout();
                Debug.Log("[超神机械师] 独立Tab创建成功");
            }

            int created = 0;

            // 1. 召唤降临者（玩家化身，需要点击地图，用GodPower按钮）
            if (PowerButton.get(SummonDescendant) == null && AssetManager.powers.get(SummonDescendant) != null)
            {
                try
                {
                    Sprite icon = AssetManager.powers.get(SummonDescendant).getIconSprite()
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateGodPowerButton(SummonDescendant, icon);
                    if (pb != null) { _modTab.AddPowerButton("tools", pb); created++; }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 召唤按钮失败: {e.Message}"); }
            }

            // 2. 异化之灾（需要点击地图，用GodPower按钮）
            if (PowerButton.get(DisasterAlien) == null && AssetManager.powers.get(DisasterAlien) != null)
            {
                try
                {
                    Sprite icon = AssetManager.powers.get(DisasterAlien).getIconSprite()
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateGodPowerButton(DisasterAlien, icon);
                    if (pb != null) { _modTab.AddPowerButton("tools", pb); created++; }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 异化按钮失败: {e.Message}"); }
            }

            // 3. 圣所（窗口按钮，用SimpleButton直接绑定Action）
            // v0.75.27: 圣所恢复为神权栏单独按钮；窗口入口聚合在「星海总览」大面板（下方按钮4）

            // 6. 宇宙迭代（大重启）
            if (PowerButton.get("sm_great_restart") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconPlanet")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_great_restart", () =>
                    {
                        SuperMechCosmicIteration.TriggerGreatRestart();
                    }, icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_great_restart_title", "sm_great_restart_desc");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 宇宙迭代按钮失败: {e.Message}"); }
            }

            // 4. 星海总览（窗口按钮，点击打开17个系统窗口入口总览）
            if (PowerButton.get("sm_overview_open") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconPlanet")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_overview_open", () =>
                    {
                        SuperMechWindowManager.OpenOverview();
                    }, icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_overview_open", "sm_overview_open_desc");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 星海总览按钮失败: {e.Message}"); }
            }

            // 5. 圣所（窗口按钮，点击打开圣所：钥匙/留言板/文明遗产）
            if (PowerButton.get("sm_sanctuary_open") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_sanctuary_open", () =>
                    {
                        SuperMechWindowManager.OpenSanctuary();
                    }, icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_sanctuary_open", "sm_sanctuary_open_desc");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 圣所按钮失败: {e.Message}"); }
            }

            if (created > 0)
            {
                _modTab.UpdateLayout();
                Debug.Log($"[超神机械师] 神权栏按钮创建完成: {created}个");
            }

            _buttonsCreated = true;
        }

        private static void SetupTooltip(PowerButton btn, string nameKey, string descKey)
        {
            if (btn == null) return;
            LocalizedTextManager.add("power_" + btn.gameObject.name,
                LocalizedTextManager.getText(nameKey), pReplace: true);
            LocalizedTextManager.add("power_" + btn.gameObject.name + "_desc",
                LocalizedTextManager.getText(descKey), pReplace: true);
        }

        private static void AddAwakenedPower(string id, string name, string icon)
        {
            var p = new GodPower
            {
                id = id, name = name, path_icon = icon,
                rank = PowerRank.Rank1_common, force_map_mode = MetaType.None,
                ignore_fast_spawn = true, hold_action = false,
                unselect_when_window = true, requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    SuperMechTalent.GrantTalents(a);
                    // 随机分配五系职业（原著：降临者=玩家，可选任意职业）
                    var classes = new[] {
                        SuperMechTraits.ClassMech, SuperMechTraits.ClassMind,
                        SuperMechTraits.ClassPsi, SuperMechTraits.ClassMartial,
                        SuperMechTraits.ClassMage
                    };
                    string chosenClass = classes[UnityEngine.Random.Range(0, classes.Length)];
                    if (!a.hasTrait(chosenClass)) a.addTrait(chosenClass);
                    SuperMechProfession.ProfessionType pType = chosenClass switch
                    {
                        SuperMechTraits.ClassMech => SuperMechProfession.ProfessionType.Mechanical,
                        SuperMechTraits.ClassMartial => SuperMechProfession.ProfessionType.Martial,
                        SuperMechTraits.ClassPsi => SuperMechProfession.ProfessionType.Psi,
                        SuperMechTraits.ClassMage => SuperMechProfession.ProfessionType.Mage,
                        _ => SuperMechProfession.ProfessionType.Mind
                    };
                    SuperMechProfession.SetProfession(a, pType);
                    // 魔法系分配法师类型
                    if (chosenClass == SuperMechTraits.ClassMage)
                        SuperMechMageType.AssignMageType(a);
                    a.addTrait(SuperMechAwakened.AwakenedTrait);
                    a.addTrait(SuperMechTraits.Descendant); // 降临者特质
                    if (!a.hasTrait("infertile")) a.addTrait("infertile"); // 玩家化身：不育（原著：玩家不属于这个世界）
                    if (!a.data.favorite) a.switchFavorite(); // 玩家标记：自动收藏（金色名字）
                    if (!a.hasTrait("sm_rank_00_f"))
                        a.addTrait("sm_rank_00_f");
                    SuperMechAdvancement.SetExactRank(a, 0);
                    SuperMechSpecialty.AssignRandomSpecialty(a);
                    SuperMechQi.SetQi(a, 100f);
                    SuperMechQi.SetQiMax(a, 100f);
                    Debug.Log($"[超神机械师] 召唤降临者：{a.name}（{pType}）");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }

        private static void AddDisaster(string id, string name)
        {
            var p = new GodPower
            {
                id = id,
                name = name,
                path_icon = "iconDiscord",
                rank = PowerRank.Rank1_common,
                force_map_mode = MetaType.None,
                ignore_fast_spawn = true,
                hold_action = false,
                unselect_when_window = true,
                requires_premium = true
            };
            p.click_action += (tile, powerId) =>
            {
                if (tile == null) return false;
                // v0.75.24: 异化原体投放（原著第513章：生化武器，影响微生物）——感染周围生物，引发异化怪物潮
                int infected = 0;
                var units = World.world.units.units_only_alive;
                if (units != null)
                {
                    foreach (Actor u in units)
                    {
                        if (u == null || !u.isAlive()) continue;
                        // v0.75.24: 全球异化蔓延（原著第509章"全面爆发的异化之灾"=全球性浩劫，不按投放点限域）
                        if (u.hasTrait("sm_disaster_infected")) continue;
                        if (u.hasTrait("sm_rank_00_f") || u.hasTrait("sm_rank_01_e") || u.hasTrait("sm_rank_02_d")) continue; // 超能者免疫（原著：异人是抵抗中坚）
                        if (Random.value < 0.25f)
                        {
                            u.addTrait("sm_disaster_infected");
                            u.addTrait("aggressive");
                            infected++;
                        }
                    }
                }
                // 异化怪物潮：生成3~5只D级异化野兽
                int swarm = 3 + Random.Range(0, 3);
                for (int i = 0; i < swarm; i++)
                {
                    Actor a = World.world.units.createNewUnit("beast", tile, pMiracleSpawn: false, pAdultAge: true);
                    if (a != null)
                    {
                        a.addTrait("sm_disaster_infected");
                        a.addTrait("aggressive");
                        a.addTrait("sm_rank_02_d");
                    }
                }
                Debug.Log($"[超神机械师]【异化之灾】异化原体投放：感染{infected}个生物，生成{swarm}只异化怪物");
                return true;
            };
            AssetManager.powers.add(p);
        }
    }
}
