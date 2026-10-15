using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.200 战斗AI增强系统
    /// 改进原版简陋的战斗AI：职业差异化战斗行为、连击、闪避、暴击、低血量爆发
    /// 原著风格：机械师远程放风筝、武道近身连击、法师施法前摇、念力控场、异能爆发</summary>
    public static class SuperMechCombatAI
    {
        /// <summary>攻击前处理：职业差异化战斗行为，返回修改后的伤害</summary>
        public static float ProcessAttack(Actor attacker, Actor target, float baseDamage)
        {
            if (attacker == null || target == null) return baseDamage;
            if (!SuperMechConfig.CombatAIEnabled) return baseDamage;

            float damage = baseDamage;

            try
            {
                // 1. 低血量爆发（所有职业，血量<30%时伤害+30%）
                if (attacker.data != null && attacker.data.health < attacker.getMaxHealth() * 0.3f)
                {
                    damage *= 1.3f;
                }

                // 2. 职业差异化战斗行为
                if (attacker.hasTrait(SuperMechTraits.ClassMechanic))
                {
                    damage = ProcessMechanicAttack(attacker, target, damage);
                }
                else if (attacker.hasTrait(SuperMechTraits.ClassMartial))
                {
                    damage = ProcessMartialAttack(attacker, target, damage);
                }
                else if (attacker.hasTrait(SuperMechTraits.ClassMage))
                {
                    damage = ProcessMageAttack(attacker, target, damage);
                }
                else if (attacker.hasTrait(SuperMechTraits.ClassPsi))
                {
                    damage = ProcessPsiAttack(attacker, target, damage);
                }
                else if (attacker.hasTrait(SuperMechTraits.ClassPower))
                {
                    damage = ProcessPowerAttack(attacker, target, damage);
                }

                // 3. 目标方闪避判定
                if (TryDodge(target))
                {
                    return 0f; // 完全闪避
                }
            }
            catch (System.Exception)
            {
                // AI增强失败不影响基础伤害
            }

            return damage;
        }

        /// <summary>机械师：远程放风筝+精准射击+机械召唤</summary>
        private static float ProcessMechanicAttack(Actor attacker, Actor target, float damage)
        {
            // 精准射击：15%概率暴击（1.8倍伤害）
            if (Random.value < 0.15f)
            {
                damage *= 1.8f;
            }

            // 机械召唤：10%概率召唤机械辅助（通过临时伤害加成模拟）
            if (Random.value < 0.10f)
            {
                damage *= 1.2f;
            }

            // 远程优势：距离远时伤害+10%（模拟放风筝）
            float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (dist > 1.5f) damage *= 1.1f;

            return damage;
        }

        /// <summary>武道：近身连击+闪避+气力爆发</summary>
        private static float ProcessMartialAttack(Actor attacker, Actor target, float damage)
        {
            // 连击：25%概率触发连击（额外50%伤害）
            if (Random.value < 0.25f)
            {
                damage *= 1.5f;
            }

            // 气力爆发：血量>50%时伤害+10%，血量<50%时伤害+20%
            if (attacker.data.health > attacker.getMaxHealth() * 0.5f)
                damage *= 1.1f;
            else
                damage *= 1.2f;

            // 近身优势：距离近时伤害+15%
            float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
            if (dist < 1.2f) damage *= 1.15f;

            return damage;
        }

        /// <summary>法师：施法高伤害+护盾吸收+元素克制</summary>
        private static float ProcessMageAttack(Actor attacker, Actor target, float damage)
        {
            // 施法高伤害：基础伤害+20%（模拟法术威力）
            damage *= 1.2f;

            // 暴击：10%概率法术暴击（2倍伤害）
            if (Random.value < 0.10f)
            {
                damage *= 2f;
            }

            // 元素克制：法师类型克制时额外伤害
            string mageType = SuperMechMageType.GetMageType(attacker);
            if (!string.IsNullOrEmpty(mageType))
            {
                // 火克木/冰、冰克雷、雷克水、奥术通用
                damage *= 1.05f; // 简化：所有法师都有5%元素加成
            }

            return damage;
        }

        /// <summary>念力：精神攻击+控场眩晕+穿透护甲</summary>
        private static float ProcessPsiAttack(Actor attacker, Actor target, float damage)
        {
            // 精神穿透：忽略10%防御（通过伤害加成模拟）
            damage *= 1.1f;

            // 控场：15%概率眩晕低阶目标
            int targetRank = SuperMechAdvancement.GetExactRankIndex(target);
            if (targetRank < 8 && Random.value < 0.15f)
            {
                target.addStatusEffect("stunned", 1f);
                SuperMechCombatFeedback.OnSpiritPierce(target);
            }

            return damage;
        }

        /// <summary>异能：元素爆发+高暴击+随机增益</summary>
        private static float ProcessPowerAttack(Actor attacker, Actor target, float damage)
        {
            // 高暴击：20%概率暴击（1.5倍伤害）
            if (Random.value < 0.20f)
            {
                damage *= 1.5f;
            }

            // 元素爆发：10%概率额外伤害（模拟元素效果）
            if (Random.value < 0.10f)
            {
                damage *= 1.2f;
            }

            return damage;
        }

        /// <summary>闪避判定：高敏捷/高阶位单位有概率闪避</summary>
        private static bool TryDodge(Actor target)
        {
            if (target == null || !target.isAlive()) return false;

            int rank = SuperMechAdvancement.GetExactRankIndex(target);
            float dodgeChance = 0f;

            // 阶位基础闪避：S阶5%，SS阶10%，X阶15%
            if (rank >= 12) dodgeChance = 0.15f;
            else if (rank >= 11) dodgeChance = 0.10f;
            else if (rank >= 10) dodgeChance = 0.05f;

            // 武道系额外闪避+5%
            if (target.hasTrait(SuperMechTraits.ClassMartial)) dodgeChance += 0.05f;

            // 念力系额外闪避+3%（精神预判）
            if (target.hasTrait(SuperMechTraits.ClassPsi)) dodgeChance += 0.03f;

            if (Random.value < dodgeChance)
            {
                SuperMechCombatFeedback.OnDodge(target);
                return true;
            }
            return false;
        }

        /// <summary>获取职业战斗风格描述（用于UI显示）</summary>
        public static string GetCombatStyle(Actor a)
        {
            if (a == null) return "";
            if (a.hasTrait(SuperMechTraits.ClassMechanic)) return "远程精准·机械召唤";
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return "近身连击·气力爆发";
            if (a.hasTrait(SuperMechTraits.ClassMage)) return "施法高伤·元素克制";
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return "精神穿透·控场减速";
            if (a.hasTrait(SuperMechTraits.ClassPower)) return "元素爆发·高暴击";
            return "普通战斗";
        }
    }
}
