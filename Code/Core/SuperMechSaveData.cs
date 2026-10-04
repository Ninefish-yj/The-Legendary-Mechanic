using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechSaveData
    {
        private const string SaveDirName = "Saves";
        private const string FileExt = ".json";
        private const int CurrentSaveVersion = 3;

        /// <summary>已首次突破的阶位（全图只记一次，参考西幻mod首位突破记录）</summary>
        public static readonly HashSet<string> FirstBreakthroughRanks = new HashSet<string>();

        /// <summary>
        /// 存档迁移：将旧版本存档数据迁移到当前版本。
        /// 缺失字段自动使用类定义中的默认值。
        /// 未来字段重命名/格式变化时，在此添加对应版本的迁移逻辑。
        /// </summary>
        private static SaveData MigrateSaveData(SaveData data)
        {
            if (data == null) return null;

            int fromVersion = data.version;

            // v1 → v2：无字段变化，仅版本号升级
            // v2 → v3：新增气力层次/属性/能级峰值/融合历史/宇宙迭代/文明数据预留字段
            // 缺失字段自动使用类定义中的默认值（0/null/空列表）

            data.version = CurrentSaveVersion;
            if (fromVersion < CurrentSaveVersion)
            {
                Debug.Log($"[超神机械师] 存档迁移: v{fromVersion} → v{CurrentSaveVersion}");
            }
            return data;
        }

        [Serializable]
        public class SaveData
        {
            public int version = CurrentSaveVersion;
            public Dictionary<string, ActorSaveData> actors = new Dictionary<string, ActorSaveData>();
            public SanctuarySaveData sanctuary = new SanctuarySaveData();
            public SuperMechFaction.FactionSaveData faction = new SuperMechFaction.FactionSaveData();
            public SuperMechTrade.TradeSaveData trade = new SuperMechTrade.TradeSaveData();
            public SuperMechGenetics.GeneticsSaveData genetics = new SuperMechGenetics.GeneticsSaveData();
            public SuperMechStarGate.StarGateSaveData starGates = new SuperMechStarGate.StarGateSaveData();
            public SuperMechIntel.IntelSaveData intel = new SuperMechIntel.IntelSaveData();
            public SuperMechCosmicBeast.CosmicBeastSaveData cosmicBeasts = new SuperMechCosmicBeast.CosmicBeastSaveData();
            public string worldSeed = "";
            public long savedAt = 0;
            // === v0.31.0 预留字段 ===
            public int cosmicIteration = 0;       // 宇宙迭代次数
            public string civilizationData = "";  // 文明数据（JSON序列化）
            public List<string> unlockedRecipes = new List<string>(); // 已解锁融合配方
            // === v0.37.0 超神遗力还原 ===
            public string worldLegacyPool = "";   // 世界游离遗力池（JSON序列化）
            // === v0.39.7 首位突破记录 ===
            public List<string> firstBreakthroughRanks = new List<string>(); // 已首次突破的阶位（全图只记一次）
        }

        [Serializable]
        public class ActorSaveData
        {
            public string name;
            public int qiLevel;
            public float qiCurrent;
            public float qiMax;
            public int stage;
            public int exactRank;
            public int potential;
            public int awakeningPoints;
            public int divinityPoints;
            public int divinityProfLayers;
            public int divinitySpeciesLayers;
            public bool divinityTriggered;
            public int legacyPower;
            public string legacySources = "";  // v0.37.0 遗力来源记录（JSON序列化）
            public bool transcended;
            public float advancementProgress;
            public bool advancementTaskDone;
            public int infoStateLevel;
            public int awakenedLevel;
            public float awakenedXp;
            public float heritage;
            public int reviveCount;
            public string qiAttribute;
            public int legend;
            public string lastDeed;
            public string destinyName;
            public float destinyProgress;
            public bool destinyCompleted;
            public string branch;
            public string subclass;
            public int subclassLevel;
            public List<string> knowledge;
            public List<string> equipBag;
            public string currentEquip;
            public List<string> fusionRecipes;
            public int mechFusionLevel;
            public string fusedEquip;
            public int[] sanctuaryAuthority;
            public string talents;
            public int profession;
            public int switchCount;
            public bool fiveSystemGenius;
            public string equippedAffixes;
            public string durability;
            public int refineCount;
            public int emRefineCount;
            public float refineQiBonus;
            public float emRefineQiBonus;
            public List<string> activeSynergies;
            public int geneStage;
            public int manaStage;
            public int mindStage;
            public int geneProgress;
            public int manaProgress;
            public int mindProgress;
            public string activeDimension;
            public float insightProgress;
            public string potentialRating;
            public List<string> learnedSkills;
            public int towerLevel;
            public string subXpData;
            public string subLevelData;
            public int craftCount;
            // === v0.31.0 预留字段 ===
            public int qiLayer;              // 气力层次（Lv1~Lv10）
            public float energyPeak;         // 能级峰值记录
            public List<string> fusionHistory; // 知识融合历史
            public string informationState;  // 信息态记录（预留）
        }

        [Serializable]
        public class SanctuarySaveData
        {
            public int unlockedSanctuaries;
            public int keyFragments;
            public int keyMaterials; // v0.46.0
            public int[] sanctuaryFragments;
            public int totalPermission;
            public int totalVisits;
            public bool messageBoardUnlocked;
            public int totalDivinityAscensions;
            public int totalResurrections;
        }

        private static string GetSavePath()
        {
            string modDir = GetModDirectory();
            string dir = Path.Combine(modDir, SaveDirName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string seed = MapBox.current_world_seed_id.ToString();
            return Path.Combine(dir, seed + FileExt);
        }

        private static string GetModDirectory()
        {
            try
            {
                return Main.Instance.GetDeclaration().FolderPath;
            }
            catch
            {
                return Path.Combine(Application.dataPath, "Mods", "sm_savedata_985");
            }
        }

        public static void Save()
        {
            try
            {
                var data = new SaveData
                {
                    worldSeed = MapBox.current_world_seed_id.ToString() ?? "",
                    savedAt = DateTime.Now.Ticks
                };

                var units = World.world?.units?.units_only_alive;
                if (units != null)
                {
                    foreach (Actor a in units)
                    {
                        if (a == null || !SuperMechTalent.HasTalent(a)) continue;
                        var ad = new ActorSaveData
                        {
                            name = a.name ?? "",
                            qiLevel = SuperMechQi.GetLevel(SuperMechQi.GetQiMax(a)),
                            qiCurrent = SuperMechQi.GetQi(a),
                            qiMax = SuperMechQi.GetQiMax(a),
                            stage = SuperMechStage.GetStage(a),
                            exactRank = SuperMechAdvancement.GetExactRankIndex(a),
                            potential = SuperMechPotential.GetPotential(a),
                            divinityPoints = SuperMechDivinity.GetPoints(a),
                            divinityProfLayers = SuperMechDivinity.GetProfLayers(a),
                            divinitySpeciesLayers = SuperMechDivinity.GetSpeciesLayers(a),
                            divinityTriggered = SuperMechDivinity.IsDivineAwakened(a),
                            legacyPower = SuperMechTranscendence.GetLegacyPower(a),
                            legacySources = SerializeLegacySources(SuperMechTranscendence.GetLegacySources(a)),
                            transcended = SuperMechTranscendence.IsTranscended(a),
                            advancementProgress = SuperMechTranscendence.GetAdvancementProgress(a),
                            advancementTaskDone = SuperMechTranscendence.IsAdvancementTaskDone(a),
                            infoStateLevel = SuperMechInfoState.GetLevel(a),
                            awakenedLevel = SuperMechAwakened.GetLevel(a),
                            awakenedXp = SuperMechAwakened.GetXp(a),
                            heritage = SuperMechHeritage.GetHeritage(a),
                            reviveCount = SuperMechSanctuary.GetReviveCount(a),
                            qiAttribute = SuperMechQiAttribute.GetAttribute(a),
                            legend = SuperMechLegend.GetLegend(a),
                            lastDeed = SuperMechLegend.GetLastDeed(a),
                            branch = SuperMechBranch.GetBranchTrait(a),
                            subclass = GetActiveSubClassId(a),
                            subclassLevel = GetActiveSubLevel(a),
                            knowledge = GetUnlockedKnowledgeList(a),
                            equipBag = new List<string>(SuperMechEquipBag.GetBag(a)),
                            currentEquip = SuperMechRelic.GetCurrentEquipId(a),
                            fusionRecipes = GetLearnedFusionRecipes(a),
                            mechFusionLevel = SuperMechMechFusion.GetFusionLevel(a),
                            fusedEquip = SuperMechMechFusion.GetFusedEquipId(a),
                            sanctuaryAuthority = GetSanctuaryAuthority(a),
                            talents = SerializeTalents(a),
                            profession = (int)SuperMechProfession.GetProfession(a),
                            switchCount = SuperMechProfession.GetSwitchCount(a),
                            fiveSystemGenius = SuperMechTalent.IsFiveSystemGenius(a),
                            equippedAffixes = SerializeAffixes(a),
                            durability = SuperMechEquipBreak.GetDurability(a).ToString(),
                            refineCount = SuperMechRefinement.GetRefineCount(a),
                            emRefineCount = SuperMechRefinement.GetEmRefineCount(a),
                            refineQiBonus = SuperMechRefinement.GetRefineQiBonus(a),
                            emRefineQiBonus = GetEmRefineQiBonus(a),
                            activeSynergies = GetActiveSynergyIds(a),
                            geneStage = SuperMechCorePower.GetGeneStage(a),
                            manaStage = SuperMechCorePower.GetManaStage(a),
                            mindStage = SuperMechCorePower.GetMindStage(a),
                            geneProgress = (int)SuperMechCorePower.GetGeneProgress(a),
                            manaProgress = (int)SuperMechCorePower.GetManaProgress(a),
                            mindProgress = (int)SuperMechCorePower.GetMindProgress(a),
                            activeDimension = SuperMechDimension.GetActiveDimension(a),
                            insightProgress = GetInsightProgress(a),
                            potentialRating = SuperMechPotentialRating.GetRating(a),
                            learnedSkills = GetLearnedSkillIds(a),
                            towerLevel = SuperMechMageTower.GetTowerLevel(a),
                            subXpData = SerializeSubXp(a),
                            subLevelData = SerializeSubLevels(a),
                            craftCount = SuperMechAdvancementTask.GetCraftCount(a)
                        };
                        data.actors[a.data.id.ToString()] = ad;
                    }
                }

                data.sanctuary = new SanctuarySaveData
                {
                    unlockedSanctuaries = SuperMechSanctuary.Data.unlocked_sanctuaries,
                    keyFragments = SuperMechSanctuary.Data.key_fragments,
                    keyMaterials = SuperMechSanctuary.Data.key_materials,
                    sanctuaryFragments = SuperMechSanctuary.Data.sanctuary_fragments,
                    totalPermission = SuperMechSanctuary.Data.total_permission,
                    totalVisits = SuperMechSanctuary.Data.total_visits,
                    messageBoardUnlocked = SuperMechSanctuary.Data.message_board_unlocked,
                    totalDivinityAscensions = SuperMechSanctuary.Data.total_divinity_ascensions,
                    totalResurrections = SuperMechSanctuary.Data.total_resurrections
                };

                // 保存宇宙迭代数据
                data.cosmicIteration = SuperMechCosmicIteration.CurrentIteration;
                data.civilizationData = SuperMechCivilizationData.Serialize();
                // 保存世界遗力池（v0.37.0）
                data.worldLegacyPool = SerializeWorldLegacyPool();
                // 保存首位突破记录（v0.39.7）
                data.firstBreakthroughRanks = new List<string>(FirstBreakthroughRanks);
                // 保存势力数据（v0.48.0）
                data.faction = SuperMechFaction.Save();
                // 保存跨文明贸易数据（v0.65.0）
                data.trade = SuperMechTrade.Save();
                // 保存基因科研数据（v0.66.0）
                data.genetics = SuperMechGenetics.Save();
                // 保存星际航道·星门数据（v0.67.0）
                data.starGates = SuperMechStarGate.Save();
                // 保存势力情报数据（v0.68.0）
                data.intel = SuperMechIntel.Save();
                // 保存宇宙异兽数据（v0.69.0）
                data.cosmicBeasts = SuperMechCosmicBeast.Save();

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(GetSavePath(), json);
                Debug.Log($"[超神机械师] 存档保存：{data.actors.Count}个单位数据");
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 存档保存失败：{e.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                string path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[超神机械师] 无存档数据，新档开始");
                    return;
                }

                string json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<SaveData>(json);
                if (data == null) return;

                // 存档迁移：旧版本自动升级到当前版本
                data = MigrateSaveData(data);
                if (data == null) return;

                SuperMechSanctuary.Data.unlocked_sanctuaries = data.sanctuary.unlockedSanctuaries;
                SuperMechSanctuary.Data.key_fragments = data.sanctuary.keyFragments;
                SuperMechSanctuary.Data.key_materials = data.sanctuary.keyMaterials;
                SuperMechSanctuary.Data.sanctuary_fragments = data.sanctuary.sanctuaryFragments;
                SuperMechSanctuary.Data.total_permission = data.sanctuary.totalPermission;
                SuperMechSanctuary.Data.total_visits = data.sanctuary.totalVisits;
                SuperMechSanctuary.Data.message_board_unlocked = data.sanctuary.messageBoardUnlocked;
                SuperMechSanctuary.Data.total_divinity_ascensions = data.sanctuary.totalDivinityAscensions;
                SuperMechSanctuary.Data.total_resurrections = data.sanctuary.totalResurrections;

                // 加载势力数据（v0.48.0）
                SuperMechFaction.Load(data.faction);
                // 加载跨文明贸易数据（v0.65.0）
                if (data.trade != null) SuperMechTrade.Load(data.trade);
                // 加载基因科研数据（v0.66.0）
                if (data.genetics != null) SuperMechGenetics.Load(data.genetics);
                // 加载星际航道·星门数据（v0.67.0）
                if (data.starGates != null) SuperMechStarGate.Load(data.starGates);
                // 加载势力情报数据（v0.68.0）
                if (data.intel != null) SuperMechIntel.Load(data.intel);
                // 加载宇宙异兽数据（v0.69.0）
                if (data.cosmicBeasts != null) SuperMechCosmicBeast.Load(data.cosmicBeasts);

                _pendingLoad = data;
                _loadPending = true;

                // 初始化宇宙迭代系统
                SuperMechCosmicIteration.Initialize(data.cosmicIteration, data.civilizationData);
                // 加载世界遗力池（v0.37.0）
                SuperMechTranscendence.SetWorldLegacyPool(DeserializeWorldLegacyPool(data.worldLegacyPool));
                // 加载首位突破记录（v0.39.7）
                FirstBreakthroughRanks.Clear();
                if (data.firstBreakthroughRanks != null)
                    foreach (var rank in data.firstBreakthroughRanks)
                        FirstBreakthroughRanks.Add(rank);

                Debug.Log($"[超神机械师] 存档加载：{data.actors.Count}个单位数据待恢复");
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 存档加载失败：{e.Message}");
            }
        }

        private static SaveData _pendingLoad;
        private static bool _loadPending = false;

        public static bool IsRestoring => _loadPending;

        private static string GetActiveSubClassId(Actor a)
        {
            if (a == null) return null;
            foreach (string subId in SuperMechSubClass.AllSubClasses)
            {
                if (a.hasTrait(subId)) return subId;
            }
            return null;
        }

        private static int GetActiveSubLevel(Actor a)
        {
            string subId = GetActiveSubClassId(a);
            if (string.IsNullOrEmpty(subId)) return 0;
            return SuperMechSubClass.GetSubLevel(a, subId);
        }

        public static void TryRestoreActors()
        {
            if (!_loadPending || _pendingLoad == null) return;

            try
            {
                var units = World.world?.units?.units_only_alive;
                if (units == null) return;

                int restored = 0;
                foreach (Actor a in units)
                {
                    if (a == null) continue;
                    string id = a.data.id.ToString();
                    if (!_pendingLoad.actors.TryGetValue(id, out var ad)) continue;

                    if (ad.qiMax > 0) SuperMechQi.SetQiMax(a, ad.qiMax);
                    SuperMechQi.SetQi(a, ad.qiCurrent);
                    if (ad.stage > 0) SuperMechStage.SetStage(a, ad.stage);
                    if (ad.exactRank >= 0) SuperMechAdvancement.SetExactRank(a, ad.exactRank);
                    SuperMechPotential.SetPotential(a, ad.potential);
                    if (ad.divinityTriggered) SuperMechDivinity.TriggerDivinity(a);
                    SuperMechDivinity.SetPoints(a, ad.divinityPoints);
                    SuperMechDivinity.SetLayers(a, ad.divinityProfLayers, ad.divinitySpeciesLayers);
                    SuperMechTranscendence.SetLegacyPower(a, ad.legacyPower);
                    SuperMechTranscendence.SetLegacySources(a, DeserializeLegacySources(ad.legacySources));
                    if (ad.transcended) SuperMechTranscendence.SetTranscended(a);
                    SuperMechTranscendence.SetAdvancementProgress(a, ad.advancementProgress);
                    if (ad.advancementTaskDone) SuperMechTranscendence.SetAdvancementTaskDone(a);
                    SuperMechInfoState.SetLevel(a, ad.infoStateLevel);
                    if (ad.awakenedLevel > 0) SuperMechAwakened.SetLevel(a, ad.awakenedLevel);
                    SuperMechAwakened.SetXp(a, ad.awakenedXp);
                    SuperMechHeritage.SetHeritage(a, ad.heritage);
                    SuperMechSanctuary.SetReviveCount(a, ad.reviveCount);
                    if (!string.IsNullOrEmpty(ad.qiAttribute) && ad.qiAttribute != SuperMechQiAttribute.AttrNone)
                        SuperMechQiAttribute.SetAttribute(a, ad.qiAttribute);
                    if (ad.legend > 0) SuperMechLegend.AddLegend(a, ad.legend, ad.lastDeed);
                    if (!string.IsNullOrEmpty(ad.branch) && !a.hasTrait(ad.branch))
                        a.addTrait(ad.branch);
                    if (!string.IsNullOrEmpty(ad.subclass) && !a.hasTrait(ad.subclass))
                    {
                        a.addTrait(ad.subclass);
                        SuperMechSubClass.AddSubXp(a, ad.subclass, 0);
                    }
                    if (ad.knowledge != null)
                    {
                        foreach (string kid in ad.knowledge)
                        {
                            SuperMechKnowledge.Unlock(a, kid);
                        }
                    }
                    if (ad.equipBag != null)
                    {
                        foreach (string eid in ad.equipBag)
                        {
                            SuperMechEquipBag.AddToBag(a, eid);
                        }
                    }
                    if (!string.IsNullOrEmpty(ad.currentEquip))
                    {
                        SuperMechEquipBag.EquipFromBag(a, ad.currentEquip);
                    }
                    if (ad.fusionRecipes != null)
                    {
                        foreach (string rid in ad.fusionRecipes)
                        {
                            SuperMechKnowledgeFusion.RestoreLearnedRecipe(a, rid);
                        }
                    }
                    if (ad.mechFusionLevel > 0)
                    {
                        SuperMechMechFusion.RestoreFusion(a, ad.mechFusionLevel, ad.fusedEquip);
                    }
                    if (ad.sanctuaryAuthority != null && ad.sanctuaryAuthority.Length >= 6)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (ad.sanctuaryAuthority[i] > 0)
                                SuperMechSanctuary.AddAuthority(a, i, ad.sanctuaryAuthority[i]);
                        }
                    }
                    if (!string.IsNullOrEmpty(ad.talents))
                    {
                        DeserializeTalents(a, ad.talents);
                    }
                    if (ad.profession > 0)
                    {
                        SuperMechProfession.SetProfession(a, (SuperMechProfession.ProfessionType)ad.profession);
                    }
                    if (ad.fiveSystemGenius)
                    {
                        SuperMechTalent._fiveSystemGenius.Clear();
                        SuperMechTalent._fiveSystemGenius.Add(a.id);
                    }

                    RestoreNewFields(a, ad);

                    _pendingLoad.actors.Remove(id);
                    restored++;
                }

                if (_pendingLoad.actors.Count == 0)
                {
                    _loadPending = false;
                    _pendingLoad = null;
                    Debug.Log($"[超神机械师] 存档恢复完成：共恢复{restored}个单位");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 存档恢复异常：{e.Message}");
            }
        }

        private static List<string> GetUnlockedKnowledgeList(Actor a)
        {
            var list = new List<string>();
            string[] prefixes = { "mech", "martial", "psi", "mage", "mind" };
            foreach (string prefix in prefixes)
            {
                var unlocked = SuperMechKnowledge.GetUnlockedList(a, prefix);
                foreach (var def in unlocked) list.Add(def.id);
            }
            return list;
        }

        private static List<string> GetLearnedFusionRecipes(Actor a)
        {
            var list = new List<string>();
            var learned = SuperMechKnowledgeFusion.GetLearnedRecipes(a);
            foreach (var recipe in learned) list.Add(recipe.id);
            return list;
        }

        private static int[] GetSanctuaryAuthority(Actor a)
        {
            var arr = new int[6];
            for (int i = 0; i < 6; i++)
                arr[i] = SuperMechSanctuary.GetAuthority(a, i);
            return arr;
        }

        private static string SerializeTalents(Actor a)
        {
            var talents = SuperMechTalent.GetTalents(a);
            if (talents == null || talents.Count == 0) return null;
            var list = new List<object>();
            foreach (var t in talents)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "type", (int)t.type },
                    { "rating", t.rating },
                    { "specificPower", t.specificPower ?? "" }
                });
            }
            return JsonConvert.SerializeObject(list);
        }

        private static void DeserializeTalents(Actor a, string json)
        {
            try
            {
                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (list == null) return;
                var talents = new List<SuperMechTalent.TalentInfo>();
                foreach (var d in list)
                {
                    talents.Add(new SuperMechTalent.TalentInfo
                    {
                        type = (SuperMechTalent.TalentType)System.Convert.ToInt32(d["type"]),
                        rating = System.Convert.ToInt32(d["rating"]),
                        specificPower = d.ContainsKey("specificPower") ? d["specificPower"].ToString() : ""
                    });
                }
                SuperMechTalent._talents[a.id] = talents;
            }
            catch { Debug.LogWarning("[超神机械师] 存档恢复失败"); }
        }

        // === v0.37.0 超神遗力序列化 ===
        private static string SerializeLegacySources(List<LegacyPowerSource> sources)
        {
            if (sources == null || sources.Count == 0) return "";
            var list = new List<Dictionary<string, object>>();
            foreach (var s in sources)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "name", s.sourceName ?? "" },
                    { "rank", s.sourceRank },
                    { "energy", s.sourceEnergy },
                    { "damage", s.deathDamage },
                    { "deathType", s.deathType ?? "" },
                    { "loadType", s.loadType ?? "" },
                    { "consciousness", s.consciousnessName ?? "" },
                    { "wish", s.wishText ?? "" },
                    { "gone", s.consciousnessGone }
                });
            }
            return JsonConvert.SerializeObject(list);
        }

        private static List<LegacyPowerSource> DeserializeLegacySources(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (list == null) return null;
                var result = new List<LegacyPowerSource>();
                foreach (var d in list)
                {
                    result.Add(new LegacyPowerSource
                    {
                        sourceName = d.ContainsKey("name") ? d["name"].ToString() : "",
                        sourceRank = d.ContainsKey("rank") ? System.Convert.ToInt32(d["rank"]) : 0,
                        sourceEnergy = d.ContainsKey("energy") ? System.Convert.ToSingle(d["energy"]) : 0f,
                        deathDamage = d.ContainsKey("damage") ? System.Convert.ToSingle(d["damage"]) : 0f,
                        deathType = d.ContainsKey("deathType") ? d["deathType"].ToString() : "other",
                        loadType = d.ContainsKey("loadType") ? d["loadType"].ToString() : "physical",
                        consciousnessName = d.ContainsKey("consciousness") ? d["consciousness"].ToString() : "",
                        wishText = d.ContainsKey("wish") ? d["wish"].ToString() : "",
                        consciousnessGone = d.ContainsKey("gone") && System.Convert.ToBoolean(d["gone"])
                    });
                }
                return result;
            }
            catch { return null; }
        }

        private static string SerializeWorldLegacyPool()
        {
            var pool = SuperMechTranscendence.GetWorldLegacyPool();
            if (pool == null || pool.Count == 0) return "";
            return SerializeLegacySources(pool);
        }

        private static List<LegacyPowerSource> DeserializeWorldLegacyPool(string json)
        {
            return DeserializeLegacySources(json);
        }

        private static float GetEmRefineQiBonus(Actor a)
        {            var dict = SuperMechRefinement._emRefineQiBonus;
            return dict != null && dict.TryGetValue(a.id, out float v) ? v : 0f;
        }

        private static float GetInsightProgress(Actor a)
        {
            var dict = SuperMechDivinity._insightProgress;
            return dict != null && dict.TryGetValue(a.id, out float v) ? v : 0f;
        }

        private static List<string> GetActiveSynergyIds(Actor a)
        {
            var active = SuperMechKnowledgeSynergy.GetActiveSynergies(a);
            if (active == null || active.Count == 0) return null;
            var list = new List<string>();
            foreach (var s in active) list.Add(s.id);
            return list;
        }

        private static List<string> GetLearnedSkillIds(Actor a)
        {
            var learned = SuperMechSkills.GetLearned(a);
            if (learned == null || learned.Count == 0) return null;
            var list = new List<string>();
            foreach (var s in learned) list.Add(s.id);
            return list;
        }

        private static string SerializeSubXp(Actor a)
        {
            var dict = SuperMechSubClass._subXp;
            if (dict == null || !dict.TryGetValue(a.id, out var subDict) || subDict.Count == 0) return null;
            return JsonConvert.SerializeObject(subDict);
        }

        private static string SerializeSubLevels(Actor a)
        {
            var dict = SuperMechSubClass._subLevel;
            if (dict == null || !dict.TryGetValue(a.id, out var subDict) || subDict.Count == 0) return null;
            return JsonConvert.SerializeObject(subDict);
        }

        private static string SerializeAffixes(Actor a)
        {
            var affixes = SuperMechEquipAffix.GetAffixes(a);
            if (affixes == null || affixes.Count == 0) return null;
            var list = new List<Dictionary<string, object>>();
            foreach (var aff in affixes)
            {
                list.Add(new Dictionary<string, object>
                {
                    { "affixId", aff.affixId ?? "" },
                    { "value", aff.value }
                });
            }
            return JsonConvert.SerializeObject(list);
        }

        private static void RestoreNewFields(Actor a, ActorSaveData ad)
        {
            try
            {
                if (!string.IsNullOrEmpty(ad.equippedAffixes))
                {
                    var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(ad.equippedAffixes);
                    if (list != null)
                    {
                        var affixes = new List<SuperMechEquipAffix.EquipAffixInstance>();
                        foreach (var d in list)
                        {
                            affixes.Add(new SuperMechEquipAffix.EquipAffixInstance
                            {
                                affixId = d.ContainsKey("affixId") ? d["affixId"].ToString() : "",
                                value = System.Convert.ToSingle(d["value"])
                            });
                        }
                        var dict = SuperMechEquipAffix._equippedAffixes;
                        if (dict != null) dict[a.id] = affixes;
                    }
                }

                if (!string.IsNullOrEmpty(ad.durability) && float.TryParse(ad.durability, out float dur))
                {
                    var dict = SuperMechEquipBreak._durability;
                    if (dict != null) dict[a.id] = dur;
                }

                if (ad.refineCount > 0)
                {
                    var dict = SuperMechRefinement._refineCount;
                    if (dict != null) dict[a.id] = ad.refineCount;
                }
                if (ad.emRefineCount > 0)
                {
                    var dict = SuperMechRefinement._emRefineCount;
                    if (dict != null) dict[a.id] = ad.emRefineCount;
                }
                if (ad.refineQiBonus > 0)
                {
                    var dict = SuperMechRefinement._refineQiBonus;
                    if (dict != null) dict[a.id] = ad.refineQiBonus;
                }
                if (ad.emRefineQiBonus > 0)
                {
                    var dict = SuperMechRefinement._emRefineQiBonus;
                    if (dict != null) dict[a.id] = ad.emRefineQiBonus;
                }

                if (ad.activeSynergies != null && ad.activeSynergies.Count > 0)
                {
                    var dict = SuperMechKnowledgeSynergy._active;
                    if (dict != null) dict[a.id] = new HashSet<string>(ad.activeSynergies);
                }

                if (ad.geneStage > 0)
                {
                    var dict = SuperMechCorePower._geneStage;
                    if (dict != null) dict[a.id] = ad.geneStage;
                }
                if (ad.manaStage > 0)
                {
                    var dict = SuperMechCorePower._manaStage;
                    if (dict != null) dict[a.id] = ad.manaStage;
                }
                if (ad.mindStage > 0)
                {
                    var dict = SuperMechCorePower._mindStage;
                    if (dict != null) dict[a.id] = ad.mindStage;
                }
                if (ad.geneProgress > 0)
                {
                    var dict = SuperMechCorePower._geneProgress;
                    if (dict != null) dict[a.id] = ad.geneProgress;
                }
                if (ad.manaProgress > 0)
                {
                    var dict = SuperMechCorePower._manaProgress;
                    if (dict != null) dict[a.id] = ad.manaProgress;
                }
                if (ad.mindProgress > 0)
                {
                    var dict = SuperMechCorePower._mindProgress;
                    if (dict != null) dict[a.id] = ad.mindProgress;
                }

                if (!string.IsNullOrEmpty(ad.activeDimension))
                {
                    var dict = SuperMechDimension._activeDimension;
                    if (dict != null) dict[a.id] = ad.activeDimension;
                }

                if (ad.insightProgress > 0)
                {
                    var dict = SuperMechDivinity._insightProgress;
                    if (dict != null) dict[a.id] = ad.insightProgress;
                }

                if (!string.IsNullOrEmpty(ad.potentialRating))
                {
                    var dict = SuperMechPotentialRating._ratings;
                    if (dict != null) dict[a.id] = ad.potentialRating;
                }

                if (ad.learnedSkills != null && ad.learnedSkills.Count > 0)
                {
                    var dict = SuperMechSkills._learned;
                    if (dict != null) dict[a.id] = new HashSet<string>(ad.learnedSkills);
                }

                if (ad.towerLevel > 0)
                {
                    SuperMechMageTower.SetTowerLevel(a, ad.towerLevel);
                }

                if (!string.IsNullOrEmpty(ad.subXpData))
                {
                    var subDict = JsonConvert.DeserializeObject<Dictionary<string, float>>(ad.subXpData);
                    if (subDict != null)
                    {
                        var dict = SuperMechSubClass._subXp;
                        if (dict != null) dict[a.id] = subDict;
                    }
                }
                if (!string.IsNullOrEmpty(ad.subLevelData))
                {
                    var subDict = JsonConvert.DeserializeObject<Dictionary<string, int>>(ad.subLevelData);
                    if (subDict != null)
                    {
                        var dict = SuperMechSubClass._subLevel;
                        if (dict != null) dict[a.id] = subDict;
                    }
                }

                if (ad.craftCount > 0)
                {
                    var dict = SuperMechAdvancementTask._craftCount;
                    if (dict != null) dict[a.id] = ad.craftCount;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 新字段存档恢复异常: {e.Message}");
            }
        }
    }
}
