using System;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechConfig
    {
        public static bool ModEnabled = true;
        public static int EraCalendar = 0; // 历法风格（0原著历法/1文明历法/2时代历法；玩家可选/修改，v0.76.13）

        public static bool AutoAwakening = true;
        public static float AwakeningChance = 0.05f;
        public static int AwakeningMinAge = 16;

        public static float QiGrowthRate = 1.0f;
        public static bool QiUnlimited = true;
        public static int QiDisplayDecimals = 0;
        /// <summary>气力属性克制环开关（游戏化扩展，原著无此设定，默认关闭）</summary>
        public static bool QiAttributeCounterEnabled = false;

        public static float PromotionSpeed = 1.0f;
        public static float OnaMultiplier = 1.0f;
        public static bool AutoPromotion = true;
        public static int AutoPromotionMaxRank = 12;
        public static bool ShowRankInPanel = true;

        public static bool AutoFavoriteEnabled = true;
        /// <summary>自动收藏最低阶位（0=F,1=E,3=D,5=C,7=B,9=A,11=S,12=SS,13=X），默认9=A阶</summary>
        public static int AutoFavoriteMinRank = 9;

        public static bool SanctuaryEnabled = true;
        public static bool SanctuaryAutoSave = true;
        /// <summary>单位自动访问圣所（v0.31.0）：A级及以上超能者自动访问匹配体系的圣所</summary>
        public static bool AutoVisitSanctuary = true;
        /// <summary>单位自动进入圣所（v0.31.0）：S级及以上有足够钥匙时自动进入圣所修炼</summary>
        public static bool AutoEnterSanctuary = true;
        // === 圣所钥匙机制（v0.46.0，原著向）===
        /// <summary>高阶单位死亡掉落圣所钥匙材料开关</summary>
        public static bool SanctuaryKeyDropEnabled = true;
        /// <summary>掉落钥匙材料的最低阶位（0=F~13=X），默认10=S阶</summary>
        public static int KeyDropMinRank = 10;
        /// <summary>多少块钥匙材料合成1把圣所钥匙（默认3）</summary>
        public static int KeyMaterialsPerKey = 3;
        /// <summary>异能系/念力系单位死亡额外掉落钥匙材料数（原始异能体碎片，默认2）</summary>
        public static int PsionicKeyMaterialBonus = 2;
        /// <summary>每次进入圣所消耗钥匙数</summary>
        public static int SanctuaryKeyCostEnter = 3;
        /// <summary>圣所能量恢复速率（每秒点数，0=关闭恢复；原著：圣所不灭自行运转，能量不枯竭）</summary>
        public static float SanctuaryEnergyRegen = 1f;
        /// <summary>跨文明贸易系统总开关（v0.65.0，原著：星海文明开展跨星域贸易）</summary>
        public static bool TradeEnabled = true;
        /// <summary>基因科研系统总开关（v0.66.0，原著：基因链是力量基础，基因优化/升华）</summary>
        public static bool GeneticsEnabled = true;
        /// <summary>星际航道·星门总开关（v0.67.0，原著：跨星域星门是唯一稳定快速跨星域方法）</summary>
        public static bool StarGateEnabled = true;
        /// <summary>势力情报系统总开关（v0.68.0，原著：十三局/风眼/暗网情报博弈）</summary>
        public static bool IntelEnabled = true;
        /// <summary>宇宙异兽体系总开关（v0.69.0，原著：评级标准之外的宇宙威胁）</summary>
        public static bool CosmicBeastEnabled = true;
        public static float CosmicBeastInterval = 500f; // 异兽入侵间隔（tick）
        /// <summary>战斗核心总开关（v0.70.x，原著：伤害分区/抗性/控制/Buff）</summary>
        public static bool CombatEnhanceEnabled = true;
        /// <summary>玩家降临·第四天灾总开关（v0.71.0）</summary>
        public static bool PlayerEnabled = true;
        /// <summary>降临者生成间隔（tick，默认60）</summary>
        public static int PlayerSpawnIntervalTicks = 60;
        /// <summary>黑星军团·军团命令总开关（v0.72.0，原著：黑星军团信用积分/军团通缉/一人即军团）</summary>
        public static bool LegionEnabled = true;
        /// <summary>世界树文明入侵总开关（v0.73.0，原著：5.0版本世界树入侵/星际联合军）</summary>
        public static bool WorldTreeEnabled = true;
        /// <summary>世界树入侵间隔（tick，默认400）</summary>
        public static float WorldTreeInterval = 400f;
        /// <summary>异神终局总开关（v0.74.0：封印时空琥珀→卷土重来→最终决战）</summary>
        public static bool EsGodEnabled = true;
        public static bool SuperAEnabled = true;   // 个体伟力·集体伟力体系（超A级/清算/协会）

        // === 超神内空间（v0.47.0，原著第1430章）===
        /// <summary>超神内空间总开关（X阶放出内空间，原著第1430章）</summary>
        public static bool InnerSpaceEnabled = true;
        /// <summary>势力/组织系统总开关（v0.48.0）</summary>
        public static bool FactionEnabled = true;
        /// <summary>放出内空间最低阶位（默认13=X阶/超神级）</summary>
        public static int InnerSpaceMinRank = 13;
        /// <summary>内空间持续时间（秒）</summary>
        public static float InnerSpaceDuration = 8f;
        /// <summary>内空间内主人伤害加成（默认0.15=15%）</summary>
        public static float InnerSpaceDamageBonus = 0.15f;
        /// <summary>内空间内敌人伤害惩罚（默认0.10=10%）</summary>
        public static float InnerSpaceEnemyPenalty = 0.10f;
        /// <summary>内空间内主人减伤（默认0.10=10%）</summary>
        public static float InnerSpaceDamageReduction = 0.10f;

        public static bool MechSummonEnabled = true;
        public static int MaxSummonedUnits = 50;

        // === 超能者AI配置（v0.45.0）===
        /// <summary>自动竞争总开关：相近阶位超能者定期切磋</summary>
        public static bool AutoCompetition = true;
        /// <summary>竞争最低阶位（0=F~13=X），默认4=C阶</summary>
        public static int CompetitionMinRank = 4;
        /// <summary>竞争间隔（岁），同一超能者两次竞争的年龄间隔</summary>
        public static int CompetitionInterval = 5;
        /// <summary>NPC自动修炼开关：非降临者超能者自动解锁本系知识</summary>
        public static bool NpcAutoKnowledge = true;
        /// <summary>NPC自动解锁知识的年龄间隔（岁）</summary>
        public static int NpcKnowledgeInterval = 5;

        public static float TickInterval = 1.25f;
        public static int MaxTrackedActors = 500;
        public static bool LogVerbose = false;

        public static bool RelicDropEnabled = true;
        public static float RelicDropRate = 0.01f;

        public static float RefinementBonus = 2.0f;

        public static bool CrossModEnergySync = true;
        public static float CrossModEnergyRatio = 1.0f;

        // === 战斗压制配置（精简版：只暴露总控，内部参数锁死原著默认值）===
        /// <summary>能级压制总开关</summary>
        public static bool CombatSuppressionEnabled = true;
        /// <summary>压制强度倍率（0.5~2.0），乘在每档伤害/命中上</summary>
        public static float SuppressIntensity = 1.0f;
        /// <summary>精神穿甲总开关</summary>
        public static bool SpiritPierceEnabled = true;
        /// <summary>精神攻击穿甲比例（念力/异能对机械系）</summary>
        public static float SpiritPierceRatio = 0.30f;

        // === 内部参数（不暴露配置，锁死原著默认值）===
        public static float SuppressThresholdMinor = 1.1f;
        public static float SuppressThresholdModerate = 1.5f;
        public static float SuppressThresholdStrong = 2.0f;
        public static float SuppressThresholdOverwhelm = 5.0f;
        public static float SuppressThresholdAnnihilate = 10.0f;
        public static float SuppressDmgMinor = 0.10f;
        public static float SuppressDmgModerate = 0.25f;
        public static float SuppressDmgStrong = 0.50f;
        public static float SuppressDmgOverwhelm = 1.00f;
        public static float SuppressDmgAnnihilate = 2.00f;
        public static float SuppressHitModerate = 0.10f;
        public static float SuppressHitStrong = 0.20f;
        public static float SuppressHitOverwhelm = 0.40f;
        public static float SuppressHitAnnihilate = 0.60f;
        public static float SuppressCritModerate = 0.08f;
        public static float SuppressCritStrong = 0.15f;
        public static float SuppressCritOverwhelm = 0.25f;
        public static float SuppressDefModerate = 0.10f;
        public static float SuppressDefStrong = 0.20f;
        public static float SuppressDefOverwhelm = 0.35f;
        public static float SuppressDefAnnihilate = 0.50f;
        public static float SuppressDodgeModerate = 0.05f;
        public static float SuppressDodgeStrong = 0.12f;
        public static float SuppressDodgeOverwhelm = 0.25f;
        public static float SuppressDodgeAnnihilate = 0.40f;
        /// <summary>知识融合属性加成上限（单配方和总上限）</summary>
        public static float FusionSingleMulCap = 2.0f;
        public static float FusionTotalMulCap = 3.0f;

        /// <summary>
        /// v0.75.22: 启动时把 NML 已加载的配置值全量同步到静态字段（根治"改了配置不生效"）。
        /// NML 机制：LoadConfig 只把 json 值存入 ModConfigItem，只有面板修改才触发回调；
        /// 手改 json / 启动加载的非默认值不会自动流入代码——此处对每项调 SetValue(当前值, skipCallback:false)
        /// 主动触发回调（SetXXX 更新静态字段）。
        /// </summary>
        public static void ApplyFromNML(NeoModLoader.api.ModConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                string path = System.IO.Path.Combine(Main.ModPath, "default_config.json");
                if (!System.IO.File.Exists(path)) return;
                string jsonText = System.IO.File.ReadAllText(path);
                var raw = Newtonsoft.Json.JsonConvert.DeserializeObject<
                    System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>>>(jsonText);
                if (raw == null) return;

                int synced = 0;
                foreach (var groupKv in raw)
                {
                    foreach (var item in groupKv.Value)
                    {
                        if (item == null || !item.ContainsKey("Id")) continue;
                        string id = item["Id"] as string;
                        if (string.IsNullOrEmpty(id)) continue;
                        try
                        {
                            var mi = cfg[groupKv.Key][id];
                            if (mi == null) continue;
                            object val = null;
                            switch (mi.Type)
                            {
                                case NeoModLoader.api.ConfigItemType.SWITCH: val = mi.BoolVal; break;
                                case NeoModLoader.api.ConfigItemType.SLIDER: val = mi.FloatVal; break;
                                case NeoModLoader.api.ConfigItemType.INT_SLIDER: val = mi.IntVal; break;
                                case NeoModLoader.api.ConfigItemType.SELECT: val = mi.IntVal; break;
                            }
                            // 历法风格 v0.76.14 起由星海总览·历法页签就地切换（内存态），不再走配置面板
                            if (val != null) { mi.SetValue(val, false); synced++; }
                        }
                        catch (System.Exception) { /* 组/项不存在则跳过 */ }
                    }
                }
                Debug.Log($"[超神机械师] 配置同步完成：{synced} 项配置值已应用");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 配置同步失败: " + e.Message);
            }
        }

        public static void Init()
        {
            try
            {
                RegisterLocalization();
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 配置初始化异常: " + e.Message);
            }
        }

        private struct CatInfo { public string key, name; public CatInfo(string a, string b) { key=a; name=b; } }
        private struct ItemInfo { public string id, name, desc; public ItemInfo(string a, string b, string c) { id=a; name=b; desc=c; } }
        private static void RegisterLocalization()
        {
            var categories = new CatInfo[]
            {
                new CatInfo("General", "sm_config_549"),
                new CatInfo("Awakening", "sm_config_550"),
                new CatInfo("Qi", "sm_config_551"),
                new CatInfo("Promotion", "sm_config_552"),
                new CatInfo("AutoFavorite", "sm_config_553"),
                new CatInfo("Sanctuary", "sm_config_554"),
                new CatInfo("Mech", "sm_config_555"),
                new CatInfo("Refinement", "sm_config_556"),
                new CatInfo("Relic", "sm_config_557"),
                new CatInfo("Performance", "sm_config_558"),
                new CatInfo("Combat", "sm_config_621"),
                new CatInfo("EventLog", "sm_config_710"),
                new CatInfo("AI", "sm_config_729"),
                new CatInfo("Advanced", "sm_config_adv"),
            };
            foreach (var c in categories)
                LocalizedTextManager.add(c.key, LocalizedTextManager.getText(c.name), pReplace: true);

            var items = new ItemInfo[]
            {
                new ItemInfo("mod_enabled", "sm_config_559", "sm_config_560"),
                new ItemInfo("auto_awakening", "sm_config_561", "sm_config_562"),
                new ItemInfo("qi_growth_rate", "sm_config_567", "sm_config_568"),
                new ItemInfo("qi_unlimited", "sm_config_569", "sm_config_570"),
                new ItemInfo("auto_promotion", "sm_config_571", "sm_config_572"),
                new ItemInfo("auto_favorite_enabled", "sm_config_581", "sm_config_582"),
                new ItemInfo("sanctuary_enabled", "sm_config_601", "sm_config_602"),
                new ItemInfo("sanctuary_autosave", "sm_config_603", "sm_config_604"),
                new ItemInfo("auto_visit_sanctuary", "sm_config_653", "sm_config_654"),
                new ItemInfo("auto_enter_sanctuary", "sm_config_655", "sm_config_656"),
                new ItemInfo("sanctuary_key_drop_enabled", "sm_config_740", "sm_config_741"),
                new ItemInfo("sanctuary_energy_regen", "sm_config_772", "sm_config_773"),
                new ItemInfo("trade_enabled", "sm_config_774", "sm_config_775"),
                new ItemInfo("genetics_enabled", "sm_config_776", "sm_config_777"),
                new ItemInfo("star_gate_enabled", "sm_config_778", "sm_config_779"),
                new ItemInfo("intel_enabled", "sm_config_780", "sm_config_781"),
                new ItemInfo("cosmic_beast_enabled", "sm_config_782", "sm_config_783"),
                new ItemInfo("cosmic_beast_interval", "sm_config_784", "sm_config_785"),
                new ItemInfo("combat_enhance_enabled", "sm_config_786", "sm_config_787"),
                new ItemInfo("inner_space_enabled", "sm_config_752", "sm_config_753"),
                new ItemInfo("inner_space_min_rank", "sm_config_754", "sm_config_755"),
                new ItemInfo("inner_space_duration", "sm_config_756", "sm_config_757"),
                new ItemInfo("inner_space_damage_bonus", "sm_config_758", "sm_config_759"),
                new ItemInfo("inner_space_enemy_penalty", "sm_config_760", "sm_config_761"),
                new ItemInfo("inner_space_damage_reduction", "sm_config_762", "sm_config_763"),
                new ItemInfo("faction_enabled", "sm_config_770", "sm_config_771"),
                new ItemInfo("mech_summon_enabled", "sm_config_605", "sm_config_606"),
                new ItemInfo("max_summoned_units", "sm_config_607", "sm_config_608"),
                new ItemInfo("relic_drop_enabled", "sm_config_611", "sm_config_612"),
                new ItemInfo("combat_suppression_enabled", "sm_config_622", "sm_config_623"),
                new ItemInfo("spirit_pierce_enabled", "sm_config_702", "sm_config_703"),
                new ItemInfo("event_log_enabled", "sm_config_711", "sm_config_712"),
                new ItemInfo("auto_competition", "sm_config_730", "sm_config_731"),
                new ItemInfo("npc_auto_knowledge", "sm_config_736", "sm_config_737"),
            };
            foreach (var it in items)
            {
                LocalizedTextManager.add(it.id, LocalizedTextManager.getText(it.name), pReplace: true);
                LocalizedTextManager.add(it.id + " Description", LocalizedTextManager.getText(it.desc), pReplace: true);
            }

        }


        public static void SetModEnabled(bool val) { ModEnabled = val; }

        public static void SetAutoAwakening(bool val) { AutoAwakening = val; }
        public static void SetAwakeningChance(float val) { AwakeningChance = Mathf.Clamp01(val); }
        public static void SetAwakeningMinAge(int val) { AwakeningMinAge = Mathf.Max(0, val); }

        public static void SetQiGrowthRate(float val) { QiGrowthRate = Mathf.Max(0.1f, val); }
        public static void SetQiUnlimited(bool val) { QiUnlimited = val; }
        public static void SetQiAttributeCounterEnabled(bool val) { QiAttributeCounterEnabled = val; }

        public static void SetPromotionSpeed(float val) { PromotionSpeed = Mathf.Max(0.1f, val); }
        public static void SetOnaMultiplier(float val) { OnaMultiplier = Mathf.Max(0.1f, val); }
        public static void SetAutoPromotion(bool val) { AutoPromotion = val; }
        public static void SetAutoPromotionMaxRank(int val) { AutoPromotionMaxRank = Mathf.Clamp(val, 0, 12); }
        public static void SetShowRankInPanel(bool val) { ShowRankInPanel = val; }

        public static void SetAutoFavoriteEnabled(bool val) { AutoFavoriteEnabled = val; }
        public static void SetAutoFavoriteMinRank(int val) { AutoFavoriteMinRank = Mathf.Clamp(val, 0, 13); }

        public static bool ShouldFavoriteRank(int rankIdx)
        {
            return rankIdx >= AutoFavoriteMinRank;
        }

        public static void SetSanctuaryEnabled(bool val) { SanctuaryEnabled = val; }
        public static void SetSanctuaryAutoSave(bool val) { SanctuaryAutoSave = val; }
        public static void SetAutoVisitSanctuary(bool val) { AutoVisitSanctuary = val; }
        public static void SetAutoEnterSanctuary(bool val) { AutoEnterSanctuary = val; }
        public static void SetSanctuaryKeyDropEnabled(bool val) { SanctuaryKeyDropEnabled = val; }
        public static void SetKeyDropMinRank(int val) { KeyDropMinRank = Mathf.Clamp(val, 0, 13); }
        public static void SetKeyMaterialsPerKey(int val) { KeyMaterialsPerKey = Mathf.Clamp(val, 1, 10); }
        public static void SetPsionicKeyMaterialBonus(int val) { PsionicKeyMaterialBonus = Mathf.Clamp(val, 0, 10); }
        public static void SetSanctuaryKeyCostEnter(int val) { SanctuaryKeyCostEnter = Mathf.Clamp(val, 1, 10); }
        public static void SetSanctuaryEnergyRegen(float val) { SanctuaryEnergyRegen = Mathf.Clamp(val, 0f, 10f); }
        public static void SetTradeEnabled(bool val) { TradeEnabled = val; }
        public static void SetGeneticsEnabled(bool val) { GeneticsEnabled = val; }
        public static void SetStarGateEnabled(bool val) { StarGateEnabled = val; }
        public static void SetIntelEnabled(bool val) { IntelEnabled = val; }
        public static void SetCosmicBeastEnabled(bool val) { CosmicBeastEnabled = val; }
        public static void SetCosmicBeastInterval(float val) { CosmicBeastInterval = Mathf.Clamp(val, 150f, 800f); }
        public static void SetCombatEnhanceEnabled(bool val) { CombatEnhanceEnabled = val; }
        public static void SetPlayerEnabled(bool val) { PlayerEnabled = val; }
        public static void SetPlayerSpawnIntervalTicks(int val) { PlayerSpawnIntervalTicks = Mathf.Clamp(val, 20, 600); }
        public static void SetLegionEnabled(bool val) { LegionEnabled = val; }
        public static void SetWorldTreeEnabled(bool val) { WorldTreeEnabled = val; }
        public static void SetWorldTreeInterval(float val) { WorldTreeInterval = Mathf.Clamp(val, 200f, 2000f); }
        public static void SetEsGodEnabled(bool val) { EsGodEnabled = val; }
        public static void SetSuperAEnabled(bool val) { SuperAEnabled = val; }
        public static void SetInnerSpaceEnabled(bool val) { InnerSpaceEnabled = val; }
        public static void SetInnerSpaceMinRank(int val) { InnerSpaceMinRank = Mathf.Clamp(val, 0, 13); }
        public static void SetInnerSpaceDuration(float val) { InnerSpaceDuration = Mathf.Clamp(val, 1f, 30f); }
        public static void SetInnerSpaceDamageBonus(float val) { InnerSpaceDamageBonus = Mathf.Clamp01(val); }
        public static void SetInnerSpaceEnemyPenalty(float val) { InnerSpaceEnemyPenalty = Mathf.Clamp01(val); }
        public static void SetInnerSpaceDamageReduction(float val) { InnerSpaceDamageReduction = Mathf.Clamp01(val); }
        public static void SetFactionEnabled(bool val) { FactionEnabled = val; }

        public static void SetMechSummonEnabled(bool val) { MechSummonEnabled = val; }
        public static void SetMaxSummonedUnits(int val) { MaxSummonedUnits = Mathf.Clamp(val, 0, 500); }

        public static void SetAutoCompetition(bool val) { AutoCompetition = val; }
        public static void SetCompetitionMinRank(int val) { CompetitionMinRank = Mathf.Clamp(val, 0, 13); }
        public static void SetCompetitionInterval(int val) { CompetitionInterval = Mathf.Clamp(val, 1, 50); }
        public static void SetNpcAutoKnowledge(bool val) { NpcAutoKnowledge = val; }
        public static void SetNpcKnowledgeInterval(int val) { NpcKnowledgeInterval = Mathf.Clamp(val, 1, 50); }

        public static void SetTickInterval(float val) { TickInterval = Mathf.Clamp(val, 1f, 60f); }
        public static void SetMaxTrackedActors(int val) { MaxTrackedActors = Mathf.Clamp(val, 50, 5000); }
        public static void SetLogVerbose(bool val) { LogVerbose = val; }

        public static void SetRelicDropEnabled(bool val) { RelicDropEnabled = val; }
        public static void SetRelicDropRate(float val) { RelicDropRate = Mathf.Clamp01(val); }

        public static void SetRefinementBonus(float val) { RefinementBonus = Mathf.Max(0f, val); }

        public static void SetCombatSuppressionEnabled(bool val) { CombatSuppressionEnabled = val; }
        public static void SetSuppressIntensity(float val) { SuppressIntensity = Mathf.Clamp(val, 0.1f, 3.0f); }
        public static void SetSpiritPierceEnabled(bool val) { SpiritPierceEnabled = val; }
        public static void SetSpiritPierceRatio(float val) { SpiritPierceRatio = Mathf.Clamp01(val); }
        public static void SetFusionSingleMulCap(float val) { FusionSingleMulCap = Mathf.Max(1.0f, val); }
        public static void SetFusionTotalMulCap(float val) { FusionTotalMulCap = Mathf.Max(1.0f, val); }

        public static void SetQiDisplayDecimals(int val) { QiDisplayDecimals = Mathf.Clamp(val, 0, 3); }
        public static void SetCrossModEnergySync(bool val) { CrossModEnergySync = val; }
        public static void SetCrossModEnergyRatio(float val) { CrossModEnergyRatio = Mathf.Clamp(val, 0.1f, 10f); }

        // === 事件日志配置（v0.39.6：阈值过滤，避免后期刷屏）===
        /// <summary>事件日志总开关</summary>
        public static bool EventLogEnabled = true;
        /// <summary>觉醒日志最低天赋评级（0=F,1=E,2=D,3=C,4=B,5=A,6=S），默认3=C级以上才记录</summary>
        public static int EventLogAwakenMinTalent = 3;
        /// <summary>晋升日志最低阶位（0=F~14=X），默认7=B阶以上才记录</summary>
        public static int EventLogPromotionMinRank = 7;
        /// <summary>知识融合日志开关（默认关，基础融合太频繁）</summary>
        public static bool EventLogFusionEnabled = false;
        /// <summary>法术习得日志开关（默认关）</summary>
        public static bool EventLogSpellEnabled = false;
        /// <summary>圣所访问日志开关（默认开，稀有事件）</summary>
        public static bool EventLogSanctuaryEnabled = true;
        /// <summary>宇宙迭代日志开关（默认开，全局大事件）</summary>
        public static bool EventLogIterationEnabled = true;
        /// <summary>超A复活日志开关（默认开，稀有事件）</summary>
        public static bool EventLogResurrectionEnabled = true;
        /// <summary>概念重塑日志开关（默认开，极稀有）</summary>
        public static bool EventLogConceptReshapeEnabled = true;

        public static void SetEventLogEnabled(bool val) { EventLogEnabled = val; }
        public static void SetEventLogAwakenMinTalent(int val) { EventLogAwakenMinTalent = Mathf.Clamp(val, 0, 6); }
        public static void SetEventLogPromotionMinRank(int val) { EventLogPromotionMinRank = Mathf.Clamp(val, 0, 13); }
        public static void SetEventLogFusionEnabled(bool val) { EventLogFusionEnabled = val; }
        public static void SetEventLogSpellEnabled(bool val) { EventLogSpellEnabled = val; }
        public static void SetEventLogSanctuaryEnabled(bool val) { EventLogSanctuaryEnabled = val; }
        public static void SetEventLogIterationEnabled(bool val) { EventLogIterationEnabled = val; }
        public static void SetEventLogResurrectionEnabled(bool val) { EventLogResurrectionEnabled = val; }
        public static void SetEventLogConceptReshapeEnabled(bool val) { EventLogConceptReshapeEnabled = val; }
    }
}
