using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 模组存档系统（JSON 持久化）。
    ///
    /// 之前的问题：所有系统数据存在静态字典里，重启游戏全部丢失。
    /// 现在：每个世界存档对应一个JSON文件，存mod目录下的Saves/文件夹。
    ///
    /// 存储内容：
    /// - 气力等级/当前值/上限
    /// - 职业阶段
    /// - 阶位（精确阶位含+位）
    /// - 潜能点/觉醒点
    /// - 神性蜕变（点数/职业层数/种族层数）
    /// - 超神遗力/突破状态/进阶任务进度
    /// - 信息态等级
    /// - 降临者等级/经验
    /// - 传承度
    /// - 冥冥感应进度
    /// - 复活次数
    /// - 副职业等级
    /// </summary>
    public static class SuperMechSaveData
    {
        private const string SaveDirName = "Saves";
        private const string FileExt = ".json";

        /// <summary>存档数据容器。</summary>
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
            public string branch; // 职业分支（枪炮师/机械师/械武者等）
            public string subclass; // 副职业ID
            public int subclassLevel; // 副职业等级
            public List<string> knowledge; // 已解锁知识节点ID列表
            public List<string> equipBag; // 装备背包装备ID列表
            public string currentEquip; // 当前装备ID
            public List<string> fusionRecipes; // 已学会的知识融合配方ID列表
            public int mechFusionLevel; // 械力融合等级（0=未融合）
            public string fusedEquip; // 械力融合的装备ID
            public int[] sanctuaryAuthority; // 圣所权限（6个圣所的碎片数，原著ch1266：碎片=权限）
            public string talents; // 天赋倾向（JSON序列化）
            public int profession; // 主职业方向（0=无,1=机械,2=武道,3=异能,4=魔法,5=念力）
            public int switchCount; // 更换职业次数
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
            // 用世界种子作为文件名，每个世界独立存档
            string seed = MapBox.current_world_seed_id.ToString() ?? "default";
            return Path.Combine(dir, seed + FileExt);
        }

        private static string GetModDirectory()
        {
            // 尝试从mod声明获取目录，失败则用相对路径
            try
            {
                return Main.Instance.GetDeclaration().FolderPath;
            }
            catch
            {
                return Path.Combine(Application.dataPath, "Mods", "超神机械师");
            }
        }

        /// <summary>保存所有模组数据到JSON。</summary>
        public static void Save()
        {
            try
            {
                var data = new SaveData
                {
                    worldSeed = MapBox.current_world_seed_id.ToString() ?? "",
                    savedAt = DateTime.Now.Ticks
                };

                // 收集所有存活的超能者数据
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
                            switchCount = SuperMechProfession.GetSwitchCount(a)
                        };
                        data.actors[a.data.id.ToString()] = ad;
                    }
                }

                // 圣所全局数据
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

        /// <summary>从JSON加载所有模组数据。</summary>
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

                // 恢复圣所全局数据
                SuperMechSanctuary.Data.unlocked_sanctuaries = data.sanctuary.unlockedSanctuaries;
                SuperMechSanctuary.Data.key_fragments = data.sanctuary.keyFragments;
                SuperMechSanctuary.Data.sanctuary_fragments = data.sanctuary.sanctuaryFragments;
                SuperMechSanctuary.Data.total_permission = data.sanctuary.totalPermission;
                SuperMechSanctuary.Data.total_visits = data.sanctuary.totalVisits;
                SuperMechSanctuary.Data.message_board_unlocked = data.sanctuary.messageBoardUnlocked;
                SuperMechSanctuary.Data.total_divinity_ascensions = data.sanctuary.totalDivinityAscensions;
                SuperMechSanctuary.Data.total_resurrections = data.sanctuary.totalResurrections;

                // 单位数据在单位生成后通过id匹配恢复（这里先存起来，等单位加载）
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

        /// <summary>是否正在等待恢复存档数据。</summary>
        public static bool IsRestoring => _loadPending;

        /// <summary>获取单位当前激活的副职业ID。</summary>
        private static string GetActiveSubClassId(Actor a)
        {
            if (a == null) return null;
            foreach (string subId in SuperMechSubClass.AllSubClasses)
            {
                if (a.hasTrait(subId)) return subId;
            }
            return null;
        }

        /// <summary>获取单位当前激活的副职业等级。</summary>
        private static int GetActiveSubLevel(Actor a)
        {
            string subId = GetActiveSubClassId(a);
            if (string.IsNullOrEmpty(subId)) return 0;
            return SuperMechSubClass.GetSubLevel(a, subId);
        }

        /// <summary>
        /// 尝试恢复单位数据（在单位加载后调用，通过id匹配）。
        /// 世界加载时单位是逐步生成的，所以每次tick检查一次。
        /// </summary>
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

                    // 恢复单位数据
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
                    // 恢复分支选择（通过特质）
                    if (!string.IsNullOrEmpty(ad.branch) && !a.hasTrait(ad.branch))
                        a.addTrait(ad.branch);
                    // 恢复副职业（通过特质+等级）
                    if (!string.IsNullOrEmpty(ad.subclass) && !a.hasTrait(ad.subclass))
                    {
                        a.addTrait(ad.subclass);
                        SuperMechSubClass.AddSubXp(a, ad.subclass, 0); // 初始化字典
                    }
                    // 恢复知识解锁
                    if (ad.knowledge != null)
                    {
                        foreach (string kid in ad.knowledge)
                        {
                            SuperMechKnowledge.Unlock(a, kid);
                        }
                    }
                    // 恢复装备背包
                    if (ad.equipBag != null)
                    {
                        foreach (string eid in ad.equipBag)
                        {
                            SuperMechEquipBag.AddToBag(a, eid);
                        }
                    }
                    // 恢复当前装备
                    if (!string.IsNullOrEmpty(ad.currentEquip))
                    {
                        SuperMechEquipBag.EquipFromBag(a, ad.currentEquip);
                    }
                    // 恢复知识融合（已学会的配方）
                    if (ad.fusionRecipes != null)
                    {
                        foreach (string rid in ad.fusionRecipes)
                        {
                            SuperMechKnowledgeFusion.RestoreLearnedRecipe(a, rid);
                        }
                    }
                    // 恢复械力融合
                    if (ad.mechFusionLevel > 0)
                    {
                        SuperMechMechFusion.RestoreFusion(a, ad.mechFusionLevel, ad.fusedEquip);
                    }
                    // 恢复圣所权限（原著ch1266：碎片=权限）
                    if (ad.sanctuaryAuthority != null && ad.sanctuaryAuthority.Length >= 6)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (ad.sanctuaryAuthority[i] > 0)
                                SuperMechSanctuary.AddAuthority(a, i, ad.sanctuaryAuthority[i]);
                        }
                    }
                    // 恢复天赋倾向
                    if (!string.IsNullOrEmpty(ad.talents))
                    {
                        DeserializeTalents(a, ad.talents);
                    }
                    // 恢复主职业方向
                    if (ad.profession > 0)
                    {
                        SuperMechProfession.SetProfession(a, (SuperMechProfession.ProfessionType)ad.profession);
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

        /// <summary>获取单位已解锁的所有知识ID列表。</summary>
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

        /// <summary>获取单位已学会的知识融合配方ID列表。</summary>
        private static List<string> GetLearnedFusionRecipes(Actor a)
        {
            var list = new List<string>();
            var learned = SuperMechKnowledgeFusion.GetLearnedRecipes(a);
            foreach (var recipe in learned) list.Add(recipe.id);
            return list;
        }

        /// <summary>获取单位圣所权限数组（6个圣所）。</summary>
        private static int[] GetSanctuaryAuthority(Actor a)
        {
            var arr = new int[6];
            for (int i = 0; i < 6; i++)
                arr[i] = SuperMechSanctuary.GetAuthority(a, i);
            return arr;
        }

        /// <summary>序列化天赋倾向为JSON字符串。</summary>
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

        /// <summary>从JSON字符串反序列化天赋倾向。</summary>
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
                // 直接设置天赋（绕过GrantTalents的已有检查）
                typeof(SuperMechTalent).GetField("_talents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, new Dictionary<long, List<SuperMechTalent.TalentInfo>> { { a.id, talents } });
            }
            catch { }
        }
    }
}
