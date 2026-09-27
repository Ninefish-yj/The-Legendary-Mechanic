using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 信息态技术系统（原著 ch1108/ch1141/ch1211/ch1214/ch1224/ch1267）。
    ///
    /// 【原著设定】
    /// - 信息态是高维存在形式，可在实体与高维信息态之间切换（ch1141）
    /// - 圣所本身是信息态宇宙奇观，复苏=信息态投影（ch1214/ch1225）
    /// - 信息态扰动：命运之子苏醒时全宇宙随机觉醒子体（ch1108）
    /// - 信息态转化技术：诸星联世界重启计划，智能瘟疫（ch1211/ch1267）
    /// - 信息态武器：圣约组织的信息态扰动装置（ch1224）
    /// - 第六圣所=信息态技术，最难获取（ch1309：进度只有7%）
    /// - 超神遗力=突破失败者的信息态残留（ch1396/ch1399）
    /// - X阶超神级信息态重生（ch1450）
    ///
    /// 【模组实现】
    /// 第六圣所解锁后，进入过第六圣所的单位获得信息态能力：
    /// 1. 信息态感知：发现隐藏单位，预知危险（灾害预警）
    /// 2. 信息态攻击：部分伤害无视护甲
    /// 3. 信息态护盾：概率免疫物理攻击
    /// 4. 信息态扰动：削弱周围敌方单位
    /// 5. 信息态转化：击杀后概率转化为信息态仆从（智能瘟疫）
    /// </summary>
    public static class SuperMechInfoState
    {
        // 信息态等级（unit.id -> level 0-5）
        private static readonly Dictionary<long, int> _infoLevel = new Dictionary<long, int>();
        // 信息态护盾冷却（unit.id -> cooldown time）
        private static readonly Dictionary<long, float> _shieldCooldown = new Dictionary<long, float>();

        public const int MaxLevel = 5;

        /// <summary>是否拥有信息态能力（第六圣所解锁且进入过）。</summary>
        public static bool HasInfoState(Actor a)
        {
            if (a == null) return false;
            return GetLevel(a) > 0;
        }

        /// <summary>获取信息态等级。</summary>
        public static int GetLevel(Actor a)
        {
            if (a == null) return 0;
            int v; _infoLevel.TryGetValue(a.id, out v); return v;
        }

        /// <summary>设置信息态等级（进入第六圣所时提升）。</summary>
        public static void SetLevel(Actor a, int level)
        {
            if (a == null) return;
            _infoLevel[a.id] = Mathf.Clamp(level, 0, MaxLevel);
        }

        /// <summary>提升信息态等级（进入第六圣所时调用）。</summary>
        public static void Upgrade(Actor a)
        {
            if (a == null) return;
            int lv = GetLevel(a);
            if (lv < MaxLevel)
            {
                SetLevel(a, lv + 1);
                // 信息态攻击：永久伤害加成（ch1224：信息态武器）
                var s = SuperMechStats.Of(a);
                if (s != null)
                {
                    s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * 1.05f;
                    s["intelligence"] = (s["intelligence"]) + 10f;
                }
                Debug.Log($"[超神机械师] {a.name} 信息态等级提升至Lv{lv + 1}");
            }
        }

        /// <summary>
        /// 信息态攻击：部分伤害无视护甲（ch1224：信息态武器）。
        /// 返回实际伤害倍率（1.0=正常，>1.0=额外伤害）。
        /// </summary>
        public static float GetAttackMultiplier(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return 1f;
            // 每级+5%无视护甲伤害，最高+25%
            return 1f + lv * 0.05f;
        }

        /// <summary>
        /// 信息态克制世界树（ch1309：第六圣所专门克制世界树）。
        /// 对植物/树灵类单位额外伤害。
        /// </summary>
        public static float GetWorldTreeBonus(Actor a, Actor target)
        {
            int lv = GetLevel(a);
            if (lv <= 0 || target == null) return 1f;
            // 检测目标是否为植物/树灵类（WorldBox中植物单位）
            string asset = target.asset?.id ?? "";
            if (asset.Contains("plant") || asset.Contains("tree") || asset.Contains("ent") ||
                target.hasTrait("plant") || target.hasTrait("tree_ent"))
            {
                return 1f + lv * 0.10f; // 每级+10%对植物伤害
            }
            return 1f;
        }

        /// <summary>
        /// 高维信息态形态（ch1141：Lv5时可短暂进入高维信息态，免疫物理攻击）。
        /// </summary>
        public static bool IsHighDimensional(Actor a)
        {
            return GetLevel(a) >= MaxLevel;
        }

        /// <summary>
        /// 信息态护盾：概率免疫物理攻击（ch1141：实体↔信息态切换）。
        /// 每级10%概率，最高50%，有冷却。
        /// </summary>
        public static bool TryShield(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return false;
            float cd;
            if (_shieldCooldown.TryGetValue(a.id, out cd) && Time.time < cd) return false;
            float chance = lv * 0.10f; // 每级10%
            if (Random.value < chance)
            {
                _shieldCooldown[a.id] = Time.time + 3f; // 3秒冷却
                return true;
            }
            return false;
        }

        /// <summary>
        /// 信息态扰动：削弱周围敌方单位（ch1108：信息态扰动）。
        /// 返回敌方减益倍率（<1.0=削弱）。
        /// </summary>
        public static float GetDisturbDebuff(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return 1f;
            // 每级周围敌方-3%伤害，最高-15%
            return 1f - lv * 0.03f;
        }

        /// <summary>
        /// 信息态感知：发现隐藏单位/预知危险（ch1141/ch1108）。
        /// 高等级信息态单位能感知到超神遗力和隐藏单位。
        /// </summary>
        public static bool CanSenseHidden(Actor a)
        {
            return GetLevel(a) >= 2;
        }

        /// <summary>
        /// 信息态转化：击杀后概率转化为信息态仆从（ch1267：智能瘟疫=信息态转化）。
        /// 每级3%概率，最高15%。
        /// </summary>
        public static bool TryConvert(Actor killer, Actor victim)
        {
            int lv = GetLevel(killer);
            if (lv <= 0) return false;
            if (victim == null) return false;
            // 只能转化比自己阶位低的单位
            int killerRank = SuperMechAdvancement.GetExactRankIndex(killer);
            int victimRank = SuperMechAdvancement.GetExactRankIndex(victim);
            if (victimRank >= killerRank) return false;
            float chance = lv * 0.03f;
            return Random.value < chance;
        }

        /// <summary>
        /// Tick：信息态被动效果。
        /// 1. 信息态扰动：持续削弱周围敌人
        /// 2. 信息态感知：高等级单位自动感知超神遗力
        /// </summary>
        public static void TickInfoState()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                int lv = GetLevel(a);
                if (lv <= 0) continue;

                // 信息态感知：Lv3以上自动感知超神遗力（辅助突破）
                if (lv >= 3 && SuperMechAdvancement.GetExactRankIndex(a) >= 12)
                {
                    if (SuperMechDivinity.IsDivineAwakened(a) &&
                        SuperMechTranscendence.IsAdvancementTaskDone(a) &&
                        !SuperMechTranscendence.IsTranscended(a))
                    {
                        // 信息态感知加速超神遗力收集
                        if (Random.value < 0.01f) // 额外1%概率
                        {
                            SuperMechTranscendence.AddLegacyPower(a, 1);
                        }
                    }
                }
            }
        }

        /// <summary>获取信息态能力描述（面板显示）。</summary>
        public static string GetStatusText(Actor a)
        {
            int lv = GetLevel(a);
            if (lv <= 0) return "";
            string text = $"sm_infostate_435";
            text += $"sm_infostate_436";
            text += $"sm_infostate_437";
            text += $"sm_infostate_438";
            text += $"sm_infostate_439";
            if (lv >= 2) text += "sm_infostate_440";
            if (lv >= 3) text += "sm_infostate_441";
            if (lv >= 4) text += "sm_infostate_442";
            if (lv >= 5) text += "sm_infostate_443";
            return text.Trim();
        }

        /// <summary>清除数据（单位死亡时）。</summary>
        public static void Clear() { _infoLevel.Clear(); _shieldCooldown.Clear(); }

        /// <summary>清理已死亡单位的字典数据。</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += SuperMechCleanup.CleanDict(_infoLevel, alive);
            removed += SuperMechCleanup.CleanDict(_shieldCooldown, alive);
            return removed;
        }
        public static void Clear(Actor a)
        {
            if (a == null) return;
            _infoLevel.Remove(a.id);
            _shieldCooldown.Remove(a.id);
        }
    }
}
