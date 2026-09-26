using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 单位面板注入：在原版属性行之后追加超神机械师自定义数据行。
    /// 参考登神长阶 DivineAscension 的 UnitWindowIntegration 方案：
    /// Harmony Postfix 挂 UnitWindow.showStatsRows，反射调 showStatRow。
    /// 只显示已觉醒五系的单位，凡人不显示。
    /// </summary>
    [HarmonyPatch(typeof(UnitWindow), "showStatsRows")]
    public static class SuperMechUnitWindow
    {
        private static MethodInfo _showStatRow;

        [HarmonyPostfix]
        public static void Postfix(UnitWindow __instance)
        {
            try
            {
                if (!SuperMechConfig.ShowRankInPanel) return;
                Actor actor = GetActor(__instance);
                if (actor == null || !actor.isAlive()) return;

                // 原著：踏入超能=获得天赋倾向，职业方向后天选择。无天赋=普通人，有天赋无方向=野生超能者
                bool hasTalent = SuperMechTalent.HasTalent(actor);
                if (!hasTalent)
                {
                    ShowRow(__instance, "状态", "普通人（未踏入超能）");
                    ShowRow(__instance, "提示", "在知识Tab点击「激发潜能」踏入超能");
                    return;
                }

                // 显示天赋倾向（系别+评级，不显示具体异能，具体异能由SuperMechSpecialty负责）
                var talents = SuperMechTalent.GetTalents(actor);
                string talentText = "";
                foreach (var t in talents)
                {
                    talentText += $"{SuperMechTalent.GetTalentName(t.type)}({SuperMechTalent.RatingNames[t.rating]}) ";
                }
                if (SuperMechTalent.IsFiveSystemGenius(actor))
                    talentText = "★五系天才★ " + talentText;
                ShowRow(__instance, "天赋倾向", talentText.Trim());

                // 显示具体异能（仅异能系，原著：异能系天生有具体异能，其他四系无此设定）
                if (actor.hasTrait(SuperMechTraits.ClassPsi))
                {
                    var specs = SuperMechSpecialty.GetSpecialties(actor);
                    if (specs.Count > 0)
                    {
                        string specText = "";
                        foreach (var s in specs)
                        {
                            string specName = LocalizedTextManager.getText("trait_" + s);
                            specText += specName + " ";
                        }
                        ShowRow(__instance, "具体异能", specText.Trim());
                    }
                }

                // 显示专长
                var perks = SuperMechPerks.GetPerks(actor);
                if (perks.Count > 0)
                {
                    string perkText = "";
                    foreach (var p in perks)
                    {
                        string perkName = LocalizedTextManager.getText("trait_" + p);
                        perkText += perkName + " ";
                    }
                    ShowRow(__instance, "专长", perkText.Trim());
                }

                bool hasProfession = SuperMechProfession.HasProfession(actor);
                string cls = hasProfession ? SuperMechProfession.GetClass(actor) : null;

                bool isAwakened = SuperMechAwakened.IsAwakened(actor);

                // 行1：阶位
                string rank = GetRank(actor);
                ShowRow(__instance, "阶位", rank);

                // 行1b：种族（原著：阶位到了自动进化种族）
                string race = SuperMechRace.GetRaceName(actor);
                if (race != "碳基人类（黄）")
                    ShowRow(__instance, "种族", race);

                // 行1c：超A名号（原著ch770：种族名用名号命名，只有S阶以上种族才有）
                if (actor.hasTrait(SuperMechRace.TraitSuperARace))
                {
                    string title = SuperMechRace.GetTitle(actor);
                    if (!string.IsNullOrEmpty(title))
                        ShowRow(__instance, "名号", title);
                }

                // 行2：体系（选定方向才显示，否则显示野生超能者）
                if (hasProfession)
                {
                    string clsAspect = GetClassAspect(cls);
                    ShowRow(__instance, "体系", cls + (string.IsNullOrEmpty(clsAspect) ? "" : $"（{clsAspect}）"));
                }
                else
                {
                    ShowRow(__instance, "体系", "野生超能者（未选定方向）");
                    ShowRow(__instance, "提示", "在知识Tab选定主职业方向");
                }

                // 行3：降临者标识 + 职业相关（只有选定方向才显示）
                if (hasProfession)
                {
                    if (isAwakened)
                    {
                        ShowRow(__instance, "身份", "降临者");
                        // 降临者：显示职业等级+经验+阶段
                        string lvText = SuperMechAwakened.GetLevelText(actor);
                        ShowRow(__instance, "职业等级", lvText);
                        string stage = SuperMechStage.GetStageName(actor);
                        if (stage != "—" && stage != "未入门")
                            ShowRow(__instance, "职业阶段", stage);
                        if (SuperMechAwakened.CanAdvanceStage(actor))
                        {
                            ShowRow(__instance, "转职", "可转职！");
                            string reqText = SuperMechAdvancementTask.GetReqText(actor);
                            if (reqText != null)
                                ShowRow(__instance, "转职条件", reqText);
                        }
                    }
                    else
                    {
                        // 星海人：也有职业阶段
                        string stage = SuperMechStage.GetStageName(actor);
                        if (stage != "—" && stage != "未入门")
                            ShowRow(__instance, "职业阶段", stage);
                    }

                    // 行3b：职业树进度
                    string treeName = GetKnowledgeTreeName(cls);
                    string prefix = SuperMechKnowledge.GetPrefixForClass(cls);
                    int unlocked = SuperMechKnowledge.GetUnlockedCount(actor, prefix);
                    ShowRow(__instance, "职业树", $"{treeName}（{unlocked}节点）");

                    // 行3c：职业技能
                    var skills = SuperMechSkills.GetLearned(actor);
                    if (skills.Count > 0)
                    {
                        string skillNames = string.Join("、", skills.ConvertAll(s => s.name));
                        ShowRow(__instance, "职业技能", skillNames);
                    }
                }

                // 行4：气力（原著五系统一，ch3/ch50。机械系磁环阶段后改称械力，ch237）
                float qi = SuperMechQi.GetQi(actor);
                float qiMax = SuperMechQi.GetQiMax(actor);
                int qiLv = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                string qiLvText = qiLv > 0 ? SuperMechQi.LevelNames[qiLv - 1] : "未入流";
                int dec = SuperMechConfig.QiDisplayDecimals;
                string fmt = dec > 0 ? $"F{dec}" : "N0";
                string qiValue = qi >= 1000 && dec == 0 ? $"{qi:N0}" : qi.ToString(fmt);
                string qiMaxValue = qiMax > 0 ? (qiMax >= 1000 && dec == 0 ? $"{qiMax:N0}" : qiMax.ToString(fmt)) : qiValue;
                // 机械系磁环阶段（阶段4，索引3）后改称械力（原著ch237）
                string qiLabel = "气力";
                if (cls == "机械系" && SuperMechStage.GetStage(actor) >= 4)
                    qiLabel = "械力";
                ShowRow(__instance, qiLabel, $"{qiValue}/{qiMaxValue}【{qiLvText}】");

                // 行4b：械感（机械亲和度，百分比，原著ch626 Lv21+4282%）
                if (cls == "机械系")
                {
                    int qiLvForMech = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                    float mechAffinity = 100f * Mathf.Pow(1.2f, qiLvForMech);
                    ShowRow(__instance, "械感", $"+{mechAffinity:F0}%（机械亲和度）");
                }

                // 行4c：魔感（魔法亲和度，百分比，原著领袖之证"职业特色气力属性：魔法亲和"）
                if (cls == "魔法系")
                {
                    int qiLvForMage = SuperMechQi.GetLevel(qiMax > 0 ? qiMax : qi);
                    float mageAffinity = 100f * Mathf.Pow(1.2f, qiLvForMage);
                    ShowRow(__instance, "魔感", $"+{mageAffinity:F0}%（魔法亲和度）");
                }

                // 行5：欧纳（能级）
                float onar = SuperMechAdvancement.CalcOnar(actor);
                ShowRow(__instance, "欧纳", $"{onar:F0}");

                // 行6：潜能点（神可以看到所有单位的完整信息，降临者和星海人都显示）
                int unlockedK = SuperMechPotential.GetUnlockedCount(actor);
                int pot = SuperMechPotential.GetPotential(actor);
                int awk = SuperMechPotential.GetAwakening(actor);
                string potText = awk > 0 ? $"{pot}（觉醒点{awk}）" : $"{pot}";
                if (isAwakened)
                {
                    ShowRow(__instance, "潜能点", $"{potText} | 知识{unlockedK}个");
                }
                else
                {
                    // 星海人：潜能点+传承度（他们靠感悟自动解锁知识）
                    float heritage = SuperMechHeritage.GetHeritage(actor);
                    int autoUnlocked = SuperMechHeritage.GetAutoUnlockedCount(actor);
                    ShowRow(__instance, "潜能点", $"{potText} | 知识{unlockedK}个（传承度{heritage:F0}，已悟{autoUnlocked}项）");
                }

                // 行7：圣所/神性蜕变（原著 ch1039/ch1362：六圣所=五系+信息态）
                bool divinity = SuperMechDivinity.IsDivineAwakened(actor);
                if (divinity)
                {
                    int pts = SuperMechDivinity.GetPoints(actor);
                    int prof = SuperMechDivinity.GetProfLayers(actor);
                    int spec = SuperMechDivinity.GetSpeciesLayers(actor);
                    string divText = $"点数{pts} | 职业{prof}/10 种族{spec}/10";
                    if (SuperMechAwakened.IsAwakened(actor))
                        divText += "（可加点）";
                    else
                        divText += "（感悟转化中）";
                    ShowRow(__instance, "神性蜕变", divText);
                }
                else
                {
                    float onarForDiv = SuperMechAdvancement.CalcOnar(actor);
                    float qiMaxForDiv = SuperMechQi.GetQiMax(actor);
                    if (qiMaxForDiv <= 0) qiMaxForDiv = SuperMechQi.GetQi(actor);
                    int qiLvForDiv = SuperMechQi.GetLevel(qiMaxForDiv);
                    string divText = $"未触发（需气力Lv21+78000欧纳，当前Lv{qiLvForDiv}/{onarForDiv:F0}）";
                    ShowRow(__instance, "神性蜕变", divText);
                }

                // 行8：圣所解锁进度（跨存档全局数据）
                int sanUnlocked = 0;
                for (int i = 0; i < 6; i++)
                    if ((SuperMechSanctuary.Data.unlocked_sanctuaries & (1 << i)) != 0) sanUnlocked++;
                string sanText = $"{sanUnlocked}/6 已解锁 | 碎片[";
                for (int i = 0; i < 6; i++)
                {
                    sanText += SuperMechSanctuary.Data.sanctuary_fragments[i];
                    if (i < 5) sanText += "/";
                }
                sanText += "]";
                int revCount = SuperMechSanctuary.GetReviveCount(actor);
                if (revCount > 0) sanText += $" | 已复活{revCount}次（信息丢失{Mathf.Clamp(0.1f*revCount,0.1f,0.5f):P0}）";
                ShowRow(__instance, "圣所", sanText);

                // 行8b：个人圣所权限（原著ch1266：碎片=权限，影响进入圣所能带走的知识量）
                int totalAuth = SuperMechSanctuary.GetTotalAuthority(actor);
                if (totalAuth > 0)
                {
                    string authText = $"综合权限{totalAuth} | 各圣所[";
                    for (int i = 0; i < 6; i++)
                    {
                        authText += SuperMechSanctuary.GetAuthority(actor, i);
                        if (i < 5) authText += "/";
                    }
                    authText += "]";
                    ShowRow(__instance, "圣所权限", authText);
                }

                // 行8a：信息态能力（第六圣所解锁后获得，ch1108/ch1141/ch1224）
                string infoText = SuperMechInfoState.GetStatusText(actor);
                if (!string.IsNullOrEmpty(infoText))
                    ShowRow(__instance, "信息态", infoText);

                // 行8b：冥冥感应/使命（原著ch1204：S阶以上感应到蜕变契机）
                var destiny = SuperMechIntuition.GetDestiny(actor);
                if (destiny != null)
                {
                    string destText = destiny.completed ?
                        $"【{destiny.name}】已证道！" :
                        $"【{destiny.name}】{destiny.progress:F0}/{destiny.target:F0}";
                    ShowRow(__instance, "冥冥感应", destText);
                }
                else if (SuperMechAdvancement.GetExactRankIndex(actor) >= 10)
                {
                    ShowRow(__instance, "冥冥感应", "正在感应中...");
                }

                // 行8c：超神级突破（ch1396：X阶需手动突破，星海人无法感知超神遗力）
                if (SuperMechAdvancement.GetExactRankIndex(actor) >= 12 || SuperMechTranscendence.IsTranscended(actor))
                {
                    ShowRow(__instance, "超神突破", SuperMechTranscendence.GetStatusText(actor));
                    int catalyst = SuperMechTranscendence.GetCatalystLayers(actor);
                    if (catalyst > 0)
                        ShowRow(__instance, "神之催化", $"{catalyst}层（成功率+{catalyst * 10}%）");
                }

                // 行9：分支（五系各三分支，机械原著ch50，其他参考同人二创）
                string branch = SuperMechBranch.GetBranchName(actor);
                if (branch != "未选择")
                    ShowRow(__instance, "分支", branch);

                // 行9b：异能潜力评级（原著ch48：EDCBAS，仅异能系）
                if (actor.hasTrait(SuperMechTraits.ClassPsi))
                {
                    string rating = SuperMechPotentialRating.GetRating(actor);
                    if (!string.IsNullOrEmpty(rating))
                    {
                        float growth = SuperMechPotentialRating.GetQiGrowthMult(actor);
                        ShowRow(__instance, "潜力评级", $"{rating}级（气力增长×{growth:F1}）");
                    }
                }

                // 行9b：气力属性（原著ch48/ch49：磁/精神/火/风/铁等）
                string qiAttr = SuperMechQiAttribute.GetAttribute(actor);
                if (qiAttr != SuperMechQiAttribute.AttrNone)
                {
                    string attrDesc = SuperMechQiAttribute.AttrDesc.ContainsKey(qiAttr) ?
                        SuperMechQiAttribute.AttrDesc[qiAttr] : "";
                    ShowRow(__instance, "气力属性", $"{qiAttr} — {attrDesc}");
                }

                // 行9b2：分系核心能量阶段（原著：基因链/魔力池/精神力，独立修炼系统）
                if (cls == "异能系")
                {
                    string stageName = SuperMechCorePower.GetGeneStageName(actor);
                    float prog = SuperMechCorePower.GetGeneProgress(actor);
                    ShowRow(__instance, "基因链", $"{stageName}（修炼{prog:F0}%）");
                }
                else if (cls == "魔法系")
                {
                    string stageName = SuperMechCorePower.GetManaStageName(actor);
                    float prog = SuperMechCorePower.GetManaProgress(actor);
                    ShowRow(__instance, "魔力池", $"{stageName}（修炼{prog:F0}%）");
                }
                else if (cls == "念力系")
                {
                    string stageName = SuperMechCorePower.GetMindStageName(actor);
                    float prog = SuperMechCorePower.GetMindProgress(actor);
                    ShowRow(__instance, "精神力", $"{stageName}（修炼{prog:F0}%）");
                }

                // 行9c：传说度（原著ch1196：传奇事迹影响突破，信息态权重）
                int legend = SuperMechLegend.GetLegend(actor);
                if (legend > 0)
                {
                    string legendTier = SuperMechLegend.GetTierName(actor);
                    string lastDeed = SuperMechLegend.GetLastDeed(actor);
                    // 降临者显示精确数值，星海人只显示等级和"隐约感到突破契机"
                    string legendText = isAwakened ?
                        $"{legendTier}（{legend}点，突破+{SuperMechLegend.GetBreakthroughBonus(actor):P0}）" :
                        $"{legendTier}（隐约感到突破契机）";
                    if (!string.IsNullOrEmpty(lastDeed)) legendText += $" 最近：{lastDeed}";
                    ShowRow(__instance, "传说度", legendText);
                }

                // 行9d：气势震慑状态（原著ch378/ch797：生命层次威压）
                if (SuperMechAura.IsStunned(actor))
                    ShowRow(__instance, "状态", "【震慑眩晕】被高阶气势压制，无法行动");
                else if (SuperMechAura.IsSuppressed(actor))
                    ShowRow(__instance, "状态", "【被气势震慑】速度/攻击降低");

                // 行10：副职业等级（原著：特工lv9/黑夜潜行者lv10）
                string subText = SuperMechSubClass.GetSubLevelText(actor);
                if (!string.IsNullOrEmpty(subText))
                    ShowRow(__instance, "副职业", subText);

                // 行10b：提炼法锻炼进度（ch50原著：0/80次，每次消耗800经验500体力）
                string refineText = SuperMechRefinement.GetStatusText(actor);
                if (!string.IsNullOrEmpty(refineText))
                    ShowRow(__instance, "提炼法", refineText);

                // 行11：装备品质（9级普通装备）
                string relic = SuperMechRelic.GetCurrentEquipName(actor);
                if (relic != "无")
                {
                    float dur = SuperMechEquipBreak.GetDurability(actor);
                    ShowRow(__instance, "装备", $"{relic}（耐久{dur:0}%）");
                }

                // 行11b：械力融合（机械系专属，装备与身体融合）
                if (SuperMechMechFusion.IsFused(actor))
                {
                    int fLevel = SuperMechMechFusion.GetFusionLevel(actor);
                    float fMul = SuperMechMechFusion.GetFusionMultiplier(actor);
                    ShowRow(__instance, "械力融合", $"Lv{fLevel}（属性×{fMul:0.0}，消耗气力维持）");
                }

                // 行11b：宇宙宝物（独立特殊物品，ch1008）
                string cosmic = SuperMechCosmicRelic.GetEquippedName(actor);
                if (!string.IsNullOrEmpty(cosmic))
                    ShowRow(__instance, "宇宙宝物", cosmic);

                // 行11c：法师塔（百度百科：超A级法师都有法师塔，塔内=完全状态）
                if (actor.hasTrait(SuperMechTraits.ClassMage))
                {
                    string tower = SuperMechMageTower.GetTowerName(actor);
                    if (tower != "无")
                        ShowRow(__instance, "法师塔", tower + "（完全状态）");
                }

                // 行12：次级维度状态
                string dim = SuperMechDimension.GetActiveDimension(actor);
                if (!string.IsNullOrEmpty(dim))
                    ShowRow(__instance, "次级维度", dim + " 强化中");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] 单位面板注入异常: " + e.Message);
            }
        }

        private static Actor GetActor(UnitWindow window)
        {
            try
            {
                var prop = typeof(UnitWindow).GetProperty("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.GetValue(window) as Actor;
                var field = typeof(UnitWindow).GetField("actor",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(window) as Actor;
            }
            catch { }
            return null;
        }

        /// <summary>原著：五系对应神灵五方面——武道=神体/念力=神魂/魔法=神权/异能=神通/机械=神器。</summary>
        public static string GetClassAspect(string cls)
        {
            switch (cls)
            {
                case "武道系": return "神体";
                case "念力系": return "神魂";
                case "魔法系": return "神权";
                case "异能系": return "神通";
                case "机械系": return "神器";
                default: return "";
            }
        }

        private static string GetRank(Actor a)
        {
            // 从精确阶位字典读取（含+位，+位不挂特质只在面板显示）
            return SuperMechRanks.GetRankName(a);
        }

        /// <summary>获取知识树名（百度百科：每系职业树名各不相同）。</summary>
        private static string GetKnowledgeTreeName(string cls)
        {
            switch (cls)
            {
                case "武道系": return "御气技巧树";
                case "异能系": return "基因树";
                case "魔法系": return "魔法知识树";
                case "念力系": return "精神修炼树";
                default: return "机械知识树";
            }
        }

        private static void ShowRow(UnitWindow window, string label, object value)
        {
            try
            {
                if (_showStatRow == null)
                {
                    // 不硬编码参数类型，按方法名查找，兼容游戏版本更新后的签名变化
                    var methods = typeof(UnitWindow).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var m in methods)
                    {
                        if (m.Name == "showStatRow" && m.GetParameters().Length >= 2)
                        {
                            _showStatRow = m;
                            break;
                        }
                    }
                    if (_showStatRow == null)
                    {
                        Debug.LogWarning("[超神机械师] 未找到UnitWindow.showStatRow方法，单位面板注入将跳过");
                        return;
                    }
                }

                // 动态构建参数数组：前两个参数是label和value，其余用默认值填充
                ParameterInfo[] parms = _showStatRow.GetParameters();
                object[] args = new object[parms.Length];
                args[0] = label;
                args[1] = value;
                for (int i = 2; i < parms.Length; i++)
                {
                    Type pt = parms[i].ParameterType;
                    if (pt == typeof(string)) args[i] = null;
                    else if (pt == typeof(bool)) args[i] = false;
                    else if (pt == typeof(long)) args[i] = -1L;
                    else if (pt == typeof(MetaType)) args[i] = MetaType.None;
                    else if (pt.IsEnum) args[i] = System.Enum.ToObject(pt, 0);
                    else args[i] = null;
                }
                _showStatRow.Invoke(window, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[超神机械师] showStatRow调用失败: " + e.Message);
            }
        }
    }
}
