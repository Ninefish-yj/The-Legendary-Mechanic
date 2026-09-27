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

        [Serializable]
        public class SaveData
        {
            public Dictionary<string, ActorSaveData> actors = new Dictionary<string, ActorSaveData>();
            public SanctuarySaveData sanctuary = new SanctuarySaveData();
            public string worldSeed = "";
            public long savedAt = 0;
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
        }

        [Serializable]
        public class SanctuarySaveData
        {
            public int unlockedSanctuaries;
            public int keyFragments;
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
                            fiveSystemGenius = SuperMechTalent.IsFiveSystemGenius(a)
                        };
                        data.actors[a.data.id.ToString()] = ad;
                    }
                }

                data.sanctuary = new SanctuarySaveData
                {
                    unlockedSanctuaries = SuperMechSanctuary.Data.unlocked_sanctuaries,
                    keyFragments = SuperMechSanctuary.Data.key_fragments,
                    sanctuaryFragments = SuperMechSanctuary.Data.sanctuary_fragments,
                    totalPermission = SuperMechSanctuary.Data.total_permission,
                    totalVisits = SuperMechSanctuary.Data.total_visits,
                    messageBoardUnlocked = SuperMechSanctuary.Data.message_board_unlocked,
                    totalDivinityAscensions = SuperMechSanctuary.Data.total_divinity_ascensions,
                    totalResurrections = SuperMechSanctuary.Data.total_resurrections
                };

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

                SuperMechSanctuary.Data.unlocked_sanctuaries = data.sanctuary.unlockedSanctuaries;
                SuperMechSanctuary.Data.key_fragments = data.sanctuary.keyFragments;
                SuperMechSanctuary.Data.sanctuary_fragments = data.sanctuary.sanctuaryFragments;
                SuperMechSanctuary.Data.total_permission = data.sanctuary.totalPermission;
                SuperMechSanctuary.Data.total_visits = data.sanctuary.totalVisits;
                SuperMechSanctuary.Data.message_board_unlocked = data.sanctuary.messageBoardUnlocked;
                SuperMechSanctuary.Data.total_divinity_ascensions = data.sanctuary.totalDivinityAscensions;
                SuperMechSanctuary.Data.total_resurrections = data.sanctuary.totalResurrections;

                _pendingLoad = data;
                _loadPending = true;
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
                        typeof(SuperMechTalent).GetField("_fiveSystemGenius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            ?.SetValue(null, new HashSet<long> { a.id });
                    }

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
                typeof(SuperMechTalent).GetField("_talents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, new Dictionary<long, List<SuperMechTalent.TalentInfo>> { { a.id, talents } });
            }
            catch { }
        }
    }
}
