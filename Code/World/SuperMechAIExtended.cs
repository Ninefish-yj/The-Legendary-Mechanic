using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>AI系统扩展（v0.137.0）
    /// 补充缺失的AI行为：装备自动穿戴、建筑自动使用、经济自动消费、技能战斗优化
    /// 由 SuperMechUnifiedTick 调度，与现有 SuperMechAI 并行运行</summary>
    public static class SuperMechAIExtended
    {
        // === 装备AI ===
        private static readonly Dictionary<long, int> _lastEquipCheck = new Dictionary<long, int>();
        private const int EquipCheckInterval = 64;  // 每64tick检查一次装备

        // === 建筑AI ===
        private static readonly Dictionary<long, int> _lastBuildingUse = new Dictionary<long, int>();
        private const int BuildingUseInterval = 48;  // 每48tick使用一次建筑

        // === 经济AI ===
        private static readonly Dictionary<long, int> _lastEconomyAction = new Dictionary<long, int>();
        private const int EconomyActionInterval = 128;  // 每128tick进行一次经济行为

        /// <summary>装备AI：超能者自动穿戴更好的装备</summary>
        public static void TickAutoEquip()
        {
            if (!SuperMechConfig.AutoCompetition) return; // 复用AI总开关
            if (World.world == null || World.world.units == null) return;

            int processed = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue; // 降临者由玩家控制
                if (a.age - _lastEquipCheck.GetValueOrDefault(a.id, 0) < EquipCheckInterval) continue;
                if (processed >= 10) break; // 每tick最多处理10个单位

                _lastEquipCheck[a.id] = a.age;
                TryAutoEquip(a);
                processed++;
            }
        }

        /// <summary>尝试自动穿戴更好的装备</summary>
        private static void TryAutoEquip(Actor a)
        {
            if (a == null || a.equipment == null) return;
            // 检查背包中是否有更好的装备
            // 简单策略：如果有未装备的高品质装备，自动穿戴
            // 装备系统通过装备词缀提供加成，超A优先穿戴
            if (SuperMechSupermA.IsSuperA(a))
            {
                // 超A：每周期检查并穿戴最好的装备
                TryEquipBest(a);
            }
            else if (SuperMechAdvancement.GetExactRankIndex(a) >= 6) // B阶以上
            {
                // 高阶超能者：有概率穿戴装备
                if (Random.value < 0.3f) TryEquipBest(a);
            }
        }

        /// <summary>穿戴当前最好的可用装备（简化实现：检查装备词缀）</summary>
        private static void TryEquipBest(Actor a)
        {
            // 装备系统通过 SuperMechEquipAffix 管理词缀
            // 如果单位没有已装备的词缀，尝试从背包装备中获取
            var equipped = SuperMechEquipAffix.GetAffixes(a);
            if (equipped != null && equipped.Count > 0) return; // 已有装备，不频繁更换

            // 检查是否有可制造的装备
            var recipes = SuperMechCrafting.GetAvailableRecipes(a);
            if (recipes != null && recipes.Count > 0 && Random.value < 0.2f)
            {
                // 有概率自动制造装备（需要材料）
                foreach (var recipe in recipes)
                {
                    if (recipe == null) continue;
                    if (SuperMechCrafting.HasMaterials(a, recipe.cost))
                    {
                        // 在当前位置制造
                        if (a.currentTile != null)
                        {
                            SuperMechCrafting.TryCraft(a, recipe.id, a.currentTile);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>建筑AI：超能者自动使用建筑功能</summary>
        public static void TickAutoUseBuilding()
        {
            if (!SuperMechConfig.AutoCompetition) return;
            if (World.world == null || World.world.units == null) return;

            int processed = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                if (SuperMechAwakened.IsAwakened(a)) continue;
                if (a.kingdom == null) continue; // 需要有文明才能使用建筑
                if (a.age - _lastBuildingUse.GetValueOrDefault(a.id, 0) < BuildingUseInterval) continue;
                if (processed >= 8) break;

                _lastBuildingUse[a.id] = a.age;
                UseAppropriateBuilding(a);
                processed++;
            }
        }

        /// <summary>根据职业使用对应建筑</summary>
        private static void UseAppropriateBuilding(Actor a)
        {
            if (a == null || a.kingdom == null) return;
            string cls = SuperMechBranch.GetClass(a);

            // 根据职业选择建筑行为
            if (cls == "sm_class_mech" || cls.Contains("mech"))
            {
                // 机械师：使用机械工厂制造/维修
                TryUseBuilding(a, SuperMechBuilding.MechFactory);
            }
            else if (cls == "sm_class_martial" || cls.Contains("martial"))
            {
                // 武道：使用佣兵公会训练
                TryUseBuilding(a, SuperMechBuilding.MercGuild);
            }
            else if (cls == "sm_class_magic" || cls.Contains("magic"))
            {
                // 法师：使用黑星宫殿研究
                TryUseBuilding(a, SuperMechBuilding.BlackStarPalace);
            }
            else
            {
                // 其他：随机使用建筑
                string[] buildings = { SuperMechBuilding.MechFactory, SuperMechBuilding.MercGuild, SuperMechBuilding.StarBank };
                TryUseBuilding(a, buildings[Random.Range(0, buildings.Length)]);
            }
        }

        /// <summary>尝试使用指定建筑（检查文明是否已解锁）</summary>
        private static void TryUseBuilding(Actor a, string buildingId)
        {
            if (a == null || a.kingdom == null) return;
            if (!SuperMechBuilding.IsUnlocked(a.kingdom.id, buildingId)) return;

            // 建筑效果：根据类型给予临时加成
            switch (buildingId)
            {
                case "sm_mech_factory":
                    // 机械工厂：提升制造效率（简化为经验加成）
                    SuperMechAwakened.SetXp(a, SuperMechAwakened.GetXp(a) + 5);
                    break;
                case "sm_merc_guild":
                    // 佣兵公会：训练提升战斗力（简化为气力恢复）
                    SuperMechQi.AddQi(a, 20);
                    break;
                case "sm_star_bank":
                    // 星际银行：经济相关（超A获得少量伊纳尔）
                    if (SuperMechSupermA.IsSuperA(a))
                        SuperMechInal.AddInal(a, 10);
                    break;
                case "sm_blackstar_palace":
                    // 黑星宫殿：研究加成
                    SuperMechAwakened.SetXp(a, SuperMechAwakened.GetXp(a) + 8);
                    break;
            }
        }

        /// <summary>经济AI：超A自动消费和投资伊纳尔</summary>
        public static void TickEconomy()
        {
            if (!SuperMechConfig.AutoCompetition) return;
            if (World.world == null || World.world.units == null) return;

            int processed = 0;
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;
                if (!SuperMechSupermA.IsSuperA(a)) continue; // 只有超A有伊纳尔经济
                if (SuperMechAwakened.IsAwakened(a)) continue;
                if (a.age - _lastEconomyAction.GetValueOrDefault(a.id, 0) < EconomyActionInterval) continue;
                if (processed >= 5) break;

                _lastEconomyAction[a.id] = a.age;
                DoEconomyAction(a);
                processed++;
            }
        }

        /// <summary>超A经济行为：消费/投资/储蓄</summary>
        private static void DoEconomyAction(Actor a)
        {
            if (a == null) return;
            int balance = SuperMechInal.GetInal(a);
            if (balance <= 0) return;

            // 根据余额决定行为
            if (balance > 1000)
            {
                // 富裕：投资公司（增加公司金库）
                var company = SuperMechCompany.GetCompanyByActor(a);
                if (company != null)
                {
                    int invest = Mathf.Min(balance / 10, 100);
                    if (SuperMechInal.SpendInal(a, invest))
                    {
                        company.treasury += invest;
                        company.reputation += 1;
                        Debug.Log($"[超神机械师] {a.name} 向 {LocalizedTextManager.getText(company.nameKey)} 投资 {invest} 伊纳尔");
                    }
                }
            }
            else if (balance > 100)
            {
                // 中等：消费（购买服务，简化为经验/气力加成）
                int spend = Mathf.Min(balance / 5, 50);
                if (SuperMechInal.SpendInal(a, spend))
                {
                    SuperMechAwakened.SetXp(a, SuperMechAwakened.GetXp(a) + spend);
                    SuperMechQi.AddQi(a, spend / 2);
                }
            }
            // 余额少：储蓄不消费
        }

        /// <summary>技能AI优化：战斗中优先选择合适的技能</summary>
        public static bool TryCastBestSkill(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return false;
            var learned = SuperMechSkills.GetLearned(attacker);
            if (learned == null || learned.Count == 0) return false;

            // 按优先级选择技能：治疗 > 控制 > 伤害 > buff
            SuperMechSkills.SkillDef bestHeal = null;
            SuperMechSkills.SkillDef bestDebuff = null;
            SuperMechSkills.SkillDef bestDamage = null;
            SuperMechSkills.SkillDef bestBuff = null;

            foreach (var def in learned)
            {
                if (def == null || def.type != SuperMechSkills.SkillType.Active) continue;
                if (SuperMechSkillRuntime.IsOnCooldown(attacker.id, def.id)) continue;
                if (def.qiCost > 0 && SuperMechQi.GetQi(attacker) < def.qiCost) continue;

                switch (def.effectType)
                {
                    case SuperMechSkills.SkillEffectType.Heal:
                        if (bestHeal == null || def.effectValue > bestHeal.effectValue)
                            bestHeal = def;
                        break;
                    case SuperMechSkills.SkillEffectType.Debuff:
                        if (bestDebuff == null || def.effectValue < bestDebuff.effectValue)
                            bestDebuff = def;
                        break;
                    case SuperMechSkills.SkillEffectType.Damage:
                        if (bestDamage == null || def.effectValue > bestDamage.effectValue)
                            bestDamage = def;
                        break;
                    case SuperMechSkills.SkillEffectType.Buff:
                        if (bestBuff == null || def.effectValue > bestBuff.effectValue)
                            bestBuff = def;
                        break;
                }
            }

            // 低血量优先治疗
            if (bestHeal != null && attacker.getHealthRatio() < 0.4f)
                return CastSkill(attacker, target, bestHeal);
            // 强敌优先减益
            if (bestDebuff != null && SuperMechAdvancement.GetExactRankIndex(target) >= SuperMechAdvancement.GetExactRankIndex(attacker))
                return CastSkill(attacker, target, bestDebuff);
            // 否则伤害
            if (bestDamage != null)
                return CastSkill(attacker, target, bestDamage);
            // 最后buff
            if (bestBuff != null)
                return CastSkill(attacker, target, bestBuff);

            return false;
        }

        /// <summary>释放指定技能</summary>
        private static bool CastSkill(Actor caster, Actor target, SuperMechSkills.SkillDef def)
        {
            if (def.qiCost > 0)
                SuperMechQi.SetQi(caster, SuperMechQi.GetQi(caster) - def.qiCost);
            // 通过反射调用私有方法ExecuteEffect
            var method = typeof(SuperMechSkillRuntime).GetMethod("ExecuteEffect",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { caster, target, def });
                // 设置冷却
                var setCd = typeof(SuperMechSkillRuntime).GetMethod("SetCooldown",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                setCd?.Invoke(null, new object[] { caster.id, def.id, def.cooldown });
                return true;
            }
            return false;
        }

        /// <summary>清理死亡单位数据</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += CleanupDict(_lastEquipCheck, alive);
            removed += CleanupDict(_lastBuildingUse, alive);
            removed += CleanupDict(_lastEconomyAction, alive);
            return removed;
        }

        private static int CleanupDict(Dictionary<long, int> dict, System.Collections.Generic.HashSet<long> alive)
        {
            var toRemove = new List<long>();
            foreach (var kv in dict)
                if (!alive.Contains(kv.Key)) toRemove.Add(kv.Key);
            foreach (var id in toRemove) dict.Remove(id);
            return toRemove.Count;
        }

        public static void Clear()
        {
            _lastEquipCheck.Clear();
            _lastBuildingUse.Clear();
            _lastEconomyAction.Clear();
        }
    }
}
