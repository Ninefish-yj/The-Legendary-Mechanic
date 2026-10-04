using NeoModLoader.api;
using NeoModLoader.services;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechPowers
    {
        public const string SummonAwakened = "sm_summon_awakened";
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
            AddAwakenedPower(SummonAwakened, "sm_powers_922", "actor_traits/iconChosenOne");
            AddDisaster(DisasterAlien, "sm_powers_924");
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

            // 1. 召唤降临者（需要点击地图，用GodPower按钮）
            if (PowerButton.get(SummonAwakened) == null && AssetManager.powers.get(SummonAwakened) != null)
            {
                try
                {
                    Sprite icon = AssetManager.powers.get(SummonAwakened).getIconSprite()
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateGodPowerButton(SummonAwakened, icon);
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
            if (PowerButton.get(OpenSanctuary) == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("actor_traits/iconBlessing")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton(OpenSanctuary, () => SuperMechWindowManager.OpenSanctuary(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_sanctuary_974", "sm_sanctuary_975");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 圣所按钮失败: {e.Message}"); }
            }

            // 4. 阶位排行榜（窗口按钮，用SimpleButton直接绑定Action）
            if (PowerButton.get(OpenRank) == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconDivineLight")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton(OpenRank, () => SuperMechWindowManager.OpenRank(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_rank_title", "sm_rank_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 阶位按钮失败: {e.Message}"); }
            }

            // 5. 势力列表（窗口按钮）
            if (PowerButton.get(OpenFaction) == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconKingdomList")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton(OpenFaction, () => SuperMechWindowManager.OpenFaction(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_faction_title", "sm_faction_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 势力按钮失败: {e.Message}"); }
            }

            // 5b. v0.65.0 跨文明贸易中心（窗口按钮）
            if (PowerButton.get("sm_trade_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconTrade")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_trade_center", () => SuperMechWindowManager.OpenTrade(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_trade_title", "sm_ui_trade_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 贸易按钮失败: {e.Message}"); }
            }

            // 5c. v0.66.0 基因科研中心（窗口按钮）
            if (PowerButton.get("sm_genetics_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconBio")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_genetics_center", () => SuperMechWindowManager.OpenGenetics(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_genetics_title", "sm_ui_genetics_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 基因按钮失败: {e.Message}"); }
            }

            // 5d. v0.67.0 星际航道·星门控制台（窗口按钮）
            if (PowerButton.get("sm_stargate_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconStarGate")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_stargate_center", () => SuperMechWindowManager.OpenStarGate(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_stargate_title", "sm_ui_stargate_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 星门按钮失败: {e.Message}"); }
            }

            // 5e. v0.68.0 情报中心（窗口按钮）
            if (PowerButton.get("sm_intel_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconIntel")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_intel_center", () => SuperMechWindowManager.OpenIntel(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_intel_title", "sm_ui_intel_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 情报按钮失败: {e.Message}"); }
            }

            // 5g. v0.70.x 战斗核心（窗口按钮）
            if (PowerButton.get("sm_combat_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconCombat")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_combat_center", () => SuperMechWindowManager.OpenCombatEnhance(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_combat_title", "sm_ui_combat_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 战斗按钮失败: {e.Message}"); }
            }

            // 5f. v0.69.0 宇宙异兽观测站（窗口按钮）
            if (PowerButton.get("sm_beast_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconBeast")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_beast_center", () => SuperMechWindowManager.OpenCosmicBeast(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_beast_title", "sm_ui_beast_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 异兽按钮失败: {e.Message}"); }
            }

            // 5g. v0.71.0 玩家降临·第四天灾（窗口按钮）
            if (PowerButton.get("sm_player_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconPlayer")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_player_center", () => SuperMechWindowManager.OpenPlayer(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_player_title", "sm_ui_player_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 玩家降临按钮失败: {e.Message}"); }
            }

            // 5h. v0.72.0 黑星军团·军团命令（窗口按钮）
            if (PowerButton.get("sm_legion_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconMilitary")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_legion_center", () => SuperMechWindowManager.OpenLegion(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_legion_title", "sm_ui_legion_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 黑星军团按钮失败: {e.Message}"); }
            }

            // 5i. v0.73.0 世界树入侵监测（窗口按钮）
            if (PowerButton.get("sm_world_tree_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconTree")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_world_tree_center", () => SuperMechWindowManager.OpenWorldTree(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_tree_title", "sm_ui_tree_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 世界树按钮失败: {e.Message}"); }
            }

            // 5j. v0.74.0 异神监测（窗口按钮）
            if (PowerButton.get("sm_esgod_center") == null)
            {
                try
                {
                    Sprite icon = SpriteTextureLoader.getSprite("ui/Icons/iconSkull")
                        ?? SpriteTextureLoader.getSprite("ui/Icons/iconQuestion");
                    var pb = PowerButtonCreator.CreateSimpleButton("sm_esgod_center", () => SuperMechWindowManager.OpenEsGod(), icon);
                    if (pb != null)
                    {
                        SetupTooltip(pb, "sm_ui_esgod_title", "sm_ui_esgod_subtitle");
                        _modTab.AddPowerButton("tools", pb); created++;
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[超神机械师] 异神按钮失败: {e.Message}"); }
            }

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
                Actor a = World.world.units.createNewUnit("beast", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a != null)
                {
                    a.addTrait("sm_rank_06_b");
                    a.addTrait("aggressive");
                }
                return true;
            };
            AssetManager.powers.add(p);
        }
    }
}
