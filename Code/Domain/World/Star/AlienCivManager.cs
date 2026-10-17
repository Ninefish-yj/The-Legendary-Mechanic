using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.229 外星文明外交管理器：贸易、联盟、科技交换、宣战、和平
    /// 参考Stellaris/群星的外交系统
    /// </summary>
    public static class AlienCivManager
    {
        // 玩家文明ID（固定）
        public const string PLAYER_CIV_ID = "player";

        /// <summary>确保玩家文明存在</summary>
        public static void EnsurePlayerCiv()
        {
            var galaxy = GalaxyManager.Galaxy;
            if (galaxy == null) return;

            var player = galaxy.civilizations.Find(c => c.id == PLAYER_CIV_ID);
            if (player == null)
            {
                player = new AlienCivilization
                {
                    id = PLAYER_CIV_ID,
                    name = LocalizedTextManager.getText("sm_player_civ_name"),
                    speciesName = LocalizedTextManager.getText("sm_player_species"),
                    techLevel = CivTechLevel.Interplanetary,
                    skinColor = new Color(0.35f, 0.75f, 0.85f, 1f),
                    governmentType = LocalizedTextManager.getText("sm_player_government"),
                    ideology = LocalizedTextManager.getText("sm_player_ideology"),
                    isPlayer = true,
                    economy = 100,
                    military = 50,
                    research = 80,
                    population = 1000000
                };
                player.totalPower = player.economy + player.military + player.research;
                galaxy.civilizations.Insert(0, player);

                // 玩家控制母星系
                var home = GalaxyManager.GetHomeSystem();
                if (home != null)
                {
                    home.ownerCiv = PLAYER_CIV_ID;
                    player.controlledSystems.Add(home.id);
                }

                // 初始化与其他文明的关系
                foreach (var civ in galaxy.civilizations)
                {
                    if (civ.id == PLAYER_CIV_ID) continue;
                    if (GalaxyManager.GetRelation(PLAYER_CIV_ID, civ.id) == null)
                    {
                        galaxy.relations.Add(new DiplomacyRelation
                        {
                            civA = PLAYER_CIV_ID,
                            civB = civ.id,
                            state = DiplomacyState.Unknown,
                            opinion = 0
                        });
                    }
                }
            }
        }

        /// <summary>建立贸易协定</summary>
        public static bool ProposeTrade(string targetCivId)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || rel.state == DiplomacyState.Unknown) return false;
            if (rel.state == DiplomacyState.War || rel.state == DiplomacyState.Hostile) return false;
            if (rel.hasTradeAgreement) return false;

            // AI接受概率：基于好感度和经济互补
            var target = GalaxyManager.GetCivilization(targetCivId);
            float acceptChance = 0.3f + rel.opinion * 0.005f;
            if (target != null && target.economy > 100) acceptChance += 0.2f;

            if (UnityEngine.Random.value < acceptChance)
            {
                rel.hasTradeAgreement = true;
                rel.state = DiplomacyState.Trade;
                rel.opinion = Mathf.Clamp(rel.opinion + 10, -100, 100);
                Debug.Log($"[超神机械师] 与{target?.name}建立贸易协定");
                return true;
            }
            Debug.Log($"[超神机械师] {target?.name}拒绝了贸易协定");
            return false;
        }

        /// <summary>提议联盟</summary>
        public static bool ProposeAlliance(string targetCivId)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || rel.state == DiplomacyState.Unknown) return false;
            if (rel.state == DiplomacyState.War) return false;
            if (rel.hasAlliance) return false;
            if (rel.opinion < 50) return false; // 需要高好感

            var target = GalaxyManager.GetCivilization(targetCivId);
            float acceptChance = 0.2f + rel.opinion * 0.005f;
            if (target != null)
            {
                var player = GalaxyManager.GetCivilization(PLAYER_CIV_ID);
                if (player != null && player.totalPower > target.totalPower * 0.5f)
                    acceptChance += 0.2f; // 玩家够强才愿意联盟
            }

            if (UnityEngine.Random.value < acceptChance)
            {
                rel.hasAlliance = true;
                rel.state = DiplomacyState.Allied;
                rel.opinion = Mathf.Clamp(rel.opinion + 20, -100, 100);
                Debug.Log($"[超神机械师] 与{target?.name}结成同盟");
                return true;
            }
            Debug.Log($"[超神机械师] {target?.name}拒绝了联盟提议");
            return false;
        }

        /// <summary>宣战</summary>
        public static bool DeclareWar(string targetCivId)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || rel.state == DiplomacyState.Unknown) return false;
            if (rel.state == DiplomacyState.War) return false;
            if (rel.hasAlliance) return false; // 不能对盟友宣战

            rel.state = DiplomacyState.War;
            rel.opinion = -100;
            rel.warScore = 0;
            rel.hasTradeAgreement = false;
            rel.hasNonAggression = false;

            var target = GalaxyManager.GetCivilization(targetCivId);
            Debug.Log($"[超神机械师] 向{target?.name}宣战！");
            return true;
        }

        /// <summary>提议和平</summary>
        public static bool ProposePeace(string targetCivId, int warScoreOffer = 0)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || rel.state != DiplomacyState.War) return false;

            var target = GalaxyManager.GetCivilization(targetCivId);
            float acceptChance = 0.3f;
            if (rel.warScore > 50) acceptChance += 0.3f; // 玩家占优
            if (rel.warScore < -50) acceptChance -= 0.2f; // 玩家劣势

            if (UnityEngine.Random.value < acceptChance)
            {
                rel.state = DiplomacyState.Cold;
                rel.opinion = Mathf.Clamp(rel.opinion + 20, -100, 100);
                rel.warScore = 0;
                Debug.Log($"[超神机械师] 与{target?.name}达成和平");
                return true;
            }
            Debug.Log($"[超神机械师] {target?.name}拒绝和平");
            return false;
        }

        /// <summary>科技交换（花费资源获取对方科技）</summary>
        public static bool TradeTechnology(string targetCivId, string techId)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || !rel.hasTradeAgreement) return false;
            if (rel.opinion < 20) return false;

            var target = GalaxyManager.GetCivilization(targetCivId);
            if (target == null) return false;

            // 科技交换成本
            float cost = 200f * (int)target.techLevel * 0.1f;
            var mainPlanet = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (mainPlanet == null || mainPlanet.resource < cost) return false;

            mainPlanet.resource -= cost;
            rel.opinion = Mathf.Clamp(rel.opinion + 5, -100, 100);
            Debug.Log($"[超神机械师] 从{target.name}获取科技：{techId}（花费{cost}）");
            return true;
        }

        /// <summary>派遣外交使团（提升好感）</summary>
        public static bool SendDiplomat(string targetCivId)
        {
            var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, targetCivId);
            if (rel == null || rel.state == DiplomacyState.Unknown) return false;
            if (rel.state == DiplomacyState.War) return false;

            var mainPlanet = SuperMechStarSystem.GetPlanet(SuperMechStarSystem.MAIN_PLANET);
            if (mainPlanet == null || mainPlanet.resource < 50) return false;

            mainPlanet.resource -= 50;
            int opinionGain = UnityEngine.Random.Range(5, 15);
            rel.opinion = Mathf.Clamp(rel.opinion + opinionGain, -100, 100);

            // 好感度提升可能改善关系
            if (rel.opinion > 30 && rel.state == DiplomacyState.Neutral)
                rel.state = DiplomacyState.Friendly;
            if (rel.opinion < -30 && rel.state == DiplomacyState.Neutral)
                rel.state = DiplomacyState.Cold;

            var target = GalaxyManager.GetCivilization(targetCivId);
            Debug.Log($"[超神机械师] 派遣外交使团到{target?.name}，好感+{opinionGain}");
            return true;
        }

        /// <summary>获取外交状态文本</summary>
        public static string GetDiplomacyStateText(DiplomacyState state)
        {
            return state switch
            {
                DiplomacyState.Unknown => LocalizedTextManager.getText("sm_diplo_unknown"),
                DiplomacyState.Neutral => LocalizedTextManager.getText("sm_diplo_neutral"),
                DiplomacyState.Friendly => LocalizedTextManager.getText("sm_diplo_friendly"),
                DiplomacyState.Allied => LocalizedTextManager.getText("sm_diplo_allied"),
                DiplomacyState.Trade => LocalizedTextManager.getText("sm_diplo_trade"),
                DiplomacyState.Cold => LocalizedTextManager.getText("sm_diplo_cold"),
                DiplomacyState.Hostile => LocalizedTextManager.getText("sm_diplo_hostile"),
                DiplomacyState.War => LocalizedTextManager.getText("sm_diplo_war"),
                _ => "?"
            };
        }

        /// <summary>获取科技等级文本</summary>
        public static string GetTechLevelText(CivTechLevel level)
        {
            return level switch
            {
                CivTechLevel.PreIndustrial => LocalizedTextManager.getText("sm_tech_preindustrial"),
                CivTechLevel.Industrial => LocalizedTextManager.getText("sm_tech_industrial"),
                CivTechLevel.Atomic => LocalizedTextManager.getText("sm_tech_atomic"),
                CivTechLevel.Space => LocalizedTextManager.getText("sm_tech_space"),
                CivTechLevel.Interplanetary => LocalizedTextManager.getText("sm_tech_interplanetary"),
                CivTechLevel.Interstellar => LocalizedTextManager.getText("sm_tech_interstellar"),
                CivTechLevel.Galactic => LocalizedTextManager.getText("sm_tech_galactic"),
                _ => "?"
            };
        }

        /// <summary>AI外交决策tick</summary>
        public static void AIDiplomacyTick(float deltaTime)
        {
            var galaxy = GalaxyManager.Galaxy;
            if (galaxy == null) return;

            foreach (var civ in galaxy.civilizations)
            {
                if (civ.isPlayer) continue;

                // AI对玩家的外交决策
                var rel = GalaxyManager.GetRelation(PLAYER_CIV_ID, civ.id);
                if (rel == null || rel.state == DiplomacyState.Unknown) continue;

                // 战争中：AI可能主动求和
                if (rel.state == DiplomacyState.War && rel.warScore < -30)
                {
                    if (UnityEngine.Random.value < 0.01f * deltaTime)
                    {
                        rel.state = DiplomacyState.Cold;
                        rel.opinion = Mathf.Clamp(rel.opinion + 10, -100, 100);
                        Debug.Log($"[超神机械师] {civ.name}主动求和");
                    }
                }

                // 和平中：AI可能宣战（如果好感极低且军力占优）
                if (rel.state == DiplomacyState.Hostile && rel.opinion < -70)
                {
                    var player = GalaxyManager.GetCivilization(PLAYER_CIV_ID);
                    if (player != null && civ.military > player.military * 1.5f)
                    {
                        if (UnityEngine.Random.value < 0.005f * deltaTime)
                        {
                            rel.state = DiplomacyState.War;
                            Debug.Log($"[超神机械师] {civ.name}向玩家宣战！");
                        }
                    }
                }
            }
        }
    }
}
