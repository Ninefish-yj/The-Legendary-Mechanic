using System;
using System.Reflection;
using NeoModLoader.api;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 种族进化系统（原著 ch770/ch1402）。
    /// 参考 DivineAscension 登神长阶的 TranscendentSpeciesSystem 实现：
    /// 达到S阶（超A）时脱离原亚种，创建独立亚种"黑星族"；
    /// 达到X阶（超神级）时升级为"黑星神系·王族血脉"。
    /// 低阶位不做种族变化。
    /// </summary>
    public static class SuperMechRace
    {
        public const string TraitBlackStarRace = "sm_race_blackstar";
        public const string TraitRoyalBlood = "sm_race_blackstar_royal";

        // 亚种特质（subspecies trait，挂在亚种上而非单位上）——原著真实种族天赋
        public const string SubspeciesMechGenius = "sm_subspecies_mech_genius";   // 【机械天才】黑星族
        public const string SubspeciesDivineGene = "sm_subspecies_divine_gene";   // 【神力基因】王族血脉
        public const string SubspeciesBornElite  = "sm_subspecies_born_elite";    // 【天生精英】王族血脉

        private static bool _registered = false;

        public static void Register()
        {
            if (_registered) return;
            _registered = true;

            // 单位标记特质
            AddRaceTrait(TraitBlackStarRace, "黑星族", "ch770：超A级物种蜕变，以黑星为名的新种族。全属性+40%。",
                dmg: 0.40f, hp: 0.40f, intel: 15);
            AddRaceTrait(TraitRoyalBlood, "黑星神系·王族血脉", "ch1402：X阶物种神化，神系王族血脉。全属性+100%。",
                dmg: 1.00f, hp: 1.00f, intel: 30);

            // 原著种族天赋（亚种特质）
            // 【机械天才】ch770/ch1039/ch1202/ch1401：机械总亲和+机械造物性能+机械系技能等级
            AddSubspeciesTrait(SubspeciesMechGenius, "机械天才",
                "ch770黑星族专属种族天赋：机械总亲和1.25x，机械造物性能+40%，机械系技能等级+1。随阶位成长。",
                intel: 10, dmg: 0.25f, speed: 0.10f);
            // 【神力基因】ch1402：能力强度+10%，每次进阶+2%（X阶累计46%）
            AddSubspeciesTrait(SubspeciesDivineGene, "神力基因",
                "ch1402王族血脉专属天赋：能力强度+10%，每次进阶+2%。遗传性天赋。",
                dmg: 0.46f);
            // 【天生精英】ch1402：全属性+10%，升级额外获得自由属性点（累计3360点）
            AddSubspeciesTrait(SubspeciesBornElite, "天生精英",
                "ch1402王族血脉专属天赋：全属性+10%，升级额外获得自由属性点。遗传性天赋。",
                hp: 0.10f, dmg: 0.10f, intel: 5);

            Debug.Log("[超神机械师] 种族系统注册完成：2单位特质 + 3原著种族天赋（机械天才/神力基因/天生精英）");
        }

        private static void AddSubspeciesTrait(string id, string name, string desc,
            int intel = 0, float dmg = 0f, float hp = 0f, float speed = 0f)
        {
            LocalizedTextManager.add("subspecies_trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("subspecies_trait_" + id + "_info", desc, pReplace: true);
            var st = new SubspeciesTrait
            {
                id = id,
                can_be_given = false,
                can_be_removed = false,
                needs_to_be_explored = false,
                base_stats_meta = new BaseStats()
            };
            if (intel > 0) st.base_stats_meta["intelligence"] = intel;
            if (dmg > 0) st.base_stats_meta["multiplier_damage"] = 1f + dmg;
            if (hp > 0) st.base_stats_meta["multiplier_health"] = 1f + hp;
            if (speed > 0) st.base_stats_meta["multiplier_speed"] = 1f + speed;
            AssetManager.subspecies_traits.add(st);
        }

        private static void AddRaceTrait(string id, string name, string desc,
            float dmg = 0f, float hp = 0f, int intel = 0)
        {
            LocalizedTextManager.add("trait_" + id, name, pReplace: true);
            LocalizedTextManager.add("trait_" + id + "_info", desc, pReplace: true);
            var t = new ActorTrait
            {
                id = id,
                path_icon = "ui/Icons/actor_traits/iconHardSkin",
                group_id = "sm_race",
                can_be_removed = false,
                can_be_given = false,
                needs_to_be_explored = false,
                base_stats = new BaseStats()
            };
            if (intel > 0) t.base_stats["intelligence"] = intel;
            if (dmg > 0) t.base_stats["multiplier_damage"] = 1f + dmg;
            if (hp > 0) t.base_stats["multiplier_health"] = 1f + hp;
            AssetManager.traits.add(t);
        }

        /// <summary>根据阶位自动进化种族：S阶→黑星族，X阶→王族血脉。参考登神长阶的独立亚种做法。</summary>
        public static void AutoEvolve(Actor a, int rankIndex)
        {
            if (a == null) return;

            // X阶：王族血脉
            if (rankIndex >= 13)
            {
                if (!a.hasTrait(TraitRoyalBlood))
                {
                    a.addTrait(TraitRoyalBlood);
                    DetachSubspecies(a, "黑星神系·王族血脉",
                        new[] { SubspeciesMechGenius, SubspeciesDivineGene, SubspeciesBornElite });
                    Debug.Log($"[超神机械师] {a.Name} 物种神化 → 黑星神系·王族血脉（独立亚种）");
                }
                return;
            }

            // S阶：黑星族
            if (rankIndex >= 10)
            {
                if (!a.hasTrait(TraitBlackStarRace))
                {
                    a.addTrait(TraitBlackStarRace);
                    DetachSubspecies(a, "黑星族", new[] { SubspeciesMechGenius });
                    Debug.Log($"[超神机械师] {a.Name} 物种蜕变 → 黑星族（独立亚种）");
                }
            }
        }

        /// <summary>
        /// 脱离原亚种，创建独立亚种并改名。参考 DivineAscension TranscendentSpeciesSystem.DetachSubspecies。
        /// 复制原亚种基因与特质，从原亚种单位列表移除，单位切换到新亚种。
        /// </summary>
        private static void DetachSubspecies(Actor actor, string subspeciesName, string[] subspeciesTraitIds)
        {
            try
            {
                Subspecies oldSpecies = actor.subspecies;
                Subspecies newSpecies = World.world.subspecies.newSpecies(actor.asset, actor.current_tile);
                if (newSpecies == null)
                {
                    Debug.LogWarning($"[超神机械师] {actor.Name} 创建独立亚种失败（newSpecies返回null）");
                    return;
                }

                if (oldSpecies != null)
                {
                    // 复制原亚种特质
                    foreach (SubspeciesTrait trait in oldSpecies.getTraits())
                        newSpecies.addTrait(trait);
                    newSpecies.nucleus.cloneFrom(oldSpecies.nucleus);
                    // 复制出生特质
                    var newBirth = newSpecies.getActorBirthTraits();
                    var oldBirth = oldSpecies.getActorBirthTraits();
                    newBirth.reset();
                    foreach (ActorTrait bTrait in oldBirth.getTraits())
                        newBirth.addTrait(bTrait);
                    // 从原亚种单位列表移除
                    oldSpecies.units.Remove(actor);
                }

                // 添加新种族专属亚种特质
                foreach (var tid in subspeciesTraitIds)
                {
                    SubspeciesTrait raceTrait = AssetManager.subspecies_traits.get(tid);
                    if (raceTrait != null)
                    {
                        newSpecies.addTrait(raceTrait);
                        Debug.Log($"[超神机械师] 新亚种已添加种族天赋：{tid}");
                    }
                }

                // 设置新亚种名（反射）
                SetSubspeciesName(newSpecies, subspeciesName);
                // 切换单位亚种
                actor.setSubspecies(newSpecies);

                Debug.Log($"[超神机械师] {actor.Name} 亚种已改为：{subspeciesName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[超神机械师] 创建独立亚种失败: {e.Message}");
            }
        }

        private static void SetSubspeciesName(Subspecies species, string name)
        {
            try
            {
                var nameField = species.GetType().GetField("name",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    nameField.SetValue(species, name);
                }
                else
                {
                    var setName = species.GetType().GetMethod("set_name",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    setName?.Invoke(species, new object[] { name });
                }
                // 同步本地化名称
                var nameLocField = species.GetType().GetField("name_localized",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameLocField != null)
                {
                    var locText = nameLocField.GetValue(species);
                    if (locText != null)
                    {
                        var textField = locText.GetType().GetField("text",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        textField?.SetValue(locText, name);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 设置亚种名失败: {e.Message}");
            }
        }

        /// <summary>获取单位当前种族名。</summary>
        public static string GetRaceName(Actor a)
        {
            if (a == null) return "碳基人类（黄）";
            if (a.hasTrait(TraitRoyalBlood)) return "黑星神系·王族血脉";
            if (a.hasTrait(TraitBlackStarRace)) return "黑星族";
            return "碳基人类（黄）";
        }
    }
}
