using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.70.x 伤害分区+抗性穿透+控制+Buff（原著向）
    /// 原著依据：
    ///  1. 伤害分区与抗性：物理/能量伤害被耐力削减、精神伤害不享受耐力减免；
    ///     物理抗性/精神抗性/异常抗性为百分比减伤，上限90%（原著力量体系）；
    ///  2. 抗性穿透：高阶攻击与特殊能力可无视目标部分抗性（原著第1061章：精神抗性穿透/格挡穿透）；
    ///  3. 控制：晕眩是精神攻击者的擅用手段；爆鸣手雷从生理层面形成晕眩、不看精神抗性而判定耐力
    ///     （原著第143章）；力量判定成功附加晕眩/断骨（原著第7章）；
    ///  4. Buff：战斗增益/减益（攻击强化/防御强化/控制强化），基因升华与异兽素材可触发强化。
    /// 真实系统适配：全部挂接战斗补丁集中加成区；Buff 按 tick 衰减并随存档保存。
    /// </summary>
    public static class SuperMechCombatEnhance
    {
        public class Buff
        {
            public string id;
            public int remaining;   // 剩余tick
            public float value;
        }

        public class CombatEnhanceSaveData
        {
            public Dictionary<string, List<BuffEntry>> buffs = new Dictionary<string, List<BuffEntry>>();
        }

        public class BuffEntry
        {
            public string id;
            public int remaining;
            public float value;
        }

        private static readonly Dictionary<long, List<Buff>> _buffs = new Dictionary<long, List<Buff>>();

        // 数值
        public const float ResistanceMax = 0.90f;     // 抗性上限（原著：上限90%）
        private const float ResistPerRank = 0.02f;    // 每阶位+2%抗性
        private const float ResistPerSublimation = 0.03f; // 基因升华+3%抗性
        private const float PenetratePerRankDiff = 0.05f; // 阶位压制：每级差+5%穿透
        private const float PenetratePerSublimation = 0.02f; // 基因升华+2%穿透
        private const float PsychicStunChance = 0.12f; // 精神系终极知识：12%概率眩晕
        private const float PhysStunChance = 0.25f;    // 力量碾压：25%概率眩晕
        private const float RankDiffForStun = 3f;      // 力量碾压判定：阶位差≥3

        public const string BuffAtk = "sm_buff_atk";       // 攻击强化（+15%伤害）
        public const string BuffDef = "sm_buff_def";       // 防御强化（-10%受击）
        public const float BuffAtkValue = 0.15f;
        public const float BuffDefValue = 0.10f;
        public const int BuffDurationTicks = 200;

        // ============ 伤害分区与抗性 ============

        /// <summary>基础抗性：物理/能量按阶位成长（原著：耐力减物理/能量伤害，精神不减）</summary>
        public static float GetBaseResistance(Actor a, bool psychic)
        {
            if (a == null) return 0f;
            int rank = SuperMechAdvancement.GetExactRankIndex(a);
            float r = rank * ResistPerRank;
            var g = SuperMechGenetics.GetGeneData(a);
            if (g != null) r += g.sublimations * ResistPerSublimation;
            if (psychic) r *= 0.5f; // 精神抗性成长更慢（原著：耐力不减精神伤害）
            return Mathf.Min(ResistanceMax, r);
        }

        /// <summary>攻击者穿透：阶位压制+基因升华（原著：高阶攻击无视低阶部分抗性）</summary>
        public static float GetPenetration(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return 0f;
            int diff = SuperMechAdvancement.GetExactRankIndex(attacker) - SuperMechAdvancement.GetExactRankIndex(target);
            float pen = diff > 0 ? diff * PenetratePerRankDiff : 0f;
            var g = SuperMechGenetics.GetGeneData(attacker);
            if (g != null) pen += g.sublimations * PenetratePerSublimation;
            return Mathf.Min(0.9f, pen);
        }

        /// <summary>攻击者是否为精神系（念力/异能 mind、psi 职业或终极知识），决定走精神抗性分区</summary>
        public static bool IsPsychicAttacker(Actor attacker)
        {
            if (attacker == null) return false;
            if (attacker.hasTrait(SuperMechTraits.ClassPsi) || attacker.hasTrait(SuperMechTraits.ClassMind)) return true;
            return SuperMechKnowledge.HasUltimateKnowledge(attacker, "mind") || SuperMechKnowledge.HasUltimateKnowledge(attacker, "psi");
        }

        /// <summary>实际减免：抗性-穿透（原著：穿透无视抗性）；精神系攻击走精神抗性分区（原著：耐力不减精神伤害）</summary>
        public static float GetEffectiveResistance(Actor target, Actor attacker)
        {
            if (!SuperMechConfig.CombatEnhanceEnabled) return 0f;
            bool psychic = IsPsychicAttacker(attacker);
            float resist = GetBaseResistance(target, psychic);
            float pen = GetPenetration(attacker, target);
            return Mathf.Clamp(resist - pen, 0f, ResistanceMax);
        }

        // ============ Buff 系统 ============

        public static void ApplyBuff(Actor a, string buffId, int durationTicks, float value)
        {
            if (a == null || a.id == null) return;
            if (!_buffs.TryGetValue(a.id, out var list))
            {
                list = new List<Buff>();
                _buffs[a.id] = list;
            }
            foreach (var b in list)
            {
                if (b.id == buffId) { b.remaining = durationTicks; b.value = value; return; }
            }
            list.Add(new Buff { id = buffId, remaining = durationTicks, value = value });
        }

        public static float GetBuffDamageBonus(Actor a)
        {
            if (!SuperMechConfig.CombatEnhanceEnabled || a == null) return 1f;
            if (!_buffs.TryGetValue(a.id, out var list)) return 1f;
            float bonus = 0f;
            foreach (var b in list) if (b.id == BuffAtk) bonus += b.value;
            return 1f + bonus;
        }

        public static float GetBuffDefenseBonus(Actor a)
        {
            if (!SuperMechConfig.CombatEnhanceEnabled || a == null) return 1f;
            if (!_buffs.TryGetValue(a.id, out var list)) return 1f;
            float bonus = 0f;
            foreach (var b in list) if (b.id == BuffDef) bonus += b.value;
            return 1f - bonus;
        }

        /// <summary>控制判定（原著：晕眩是精神攻击者擅用手段/生理眩晕不看精神抗性/力量碾压附加晕眩）</summary>
        public static void TryApplyControl(Actor attacker, Actor target)
        {
            if (!SuperMechConfig.CombatEnhanceEnabled || attacker == null || target == null) return;
            if (!target.isAlive()) return;

            // 1) 精神系终极知识：精神攻击者擅用晕眩，受目标精神抗性影响（系名前缀mind，原著：精神攻击者晕眩手段）
            bool psychicMaster = SuperMechKnowledge.HasUltimateKnowledge(attacker, "mind");
            if (psychicMaster && Random.value < PsychicStunChance)
            {
                float resist = GetBaseResistance(target, true);
                if (Random.value > resist * 0.6f)
                {
                    target.addStatusEffect("stunned", 2f);
                    return;
                }
            }

            // 2) 机械系生理眩晕：不看精神抗性，只看力量碾压（原著第143章：爆鸣手雷生理层面晕眩，判定耐力）
            bool mechUser = attacker.hasTrait(SuperMechTraits.ClassMech);
            int diff = SuperMechAdvancement.GetExactRankIndex(attacker) - SuperMechAdvancement.GetExactRankIndex(target);
            if ((mechUser || diff >= RankDiffForStun) && diff >= 0 && Random.value < PhysStunChance)
            {
                target.addStatusEffect("stunned", 2f);
            }
        }

        public static void TickBuffs()
        {
            var toRemove = new List<long>();
            foreach (var kv in _buffs)
            {
                var list = kv.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    list[i].remaining--;
                    if (list[i].remaining <= 0) list.RemoveAt(i);
                }
                if (list.Count == 0) toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove) _buffs.Remove(id);
        }

        public static List<KeyValuePair<Actor, List<Buff>>> GetBuffRanking(int limit)
        {
            var result = new List<KeyValuePair<Actor, List<Buff>>>();
            foreach (var kv in _buffs)
            {
                var a = FindActorById(kv.Key);
                if (a == null || !a.isAlive()) continue;
                result.Add(new KeyValuePair<Actor, List<Buff>>(a, kv.Value));
            }
            if (limit > 0 && result.Count > limit) result.RemoveRange(limit, result.Count - limit);
            return result;
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units) if (a != null && a.id == id) return a;
            return null;
        }

        public static CombatEnhanceSaveData Save()
        {
            var data = new CombatEnhanceSaveData();
            foreach (var kv in _buffs)
            {
                var list = new List<BuffEntry>();
                foreach (var b in kv.Value)
                    list.Add(new BuffEntry { id = b.id, remaining = b.remaining, value = b.value });
                data.buffs[kv.Key.ToString()] = list;
            }
            return data;
        }

        public static void Load(CombatEnhanceSaveData data)
        {
            _buffs.Clear();
            if (data == null || data.buffs == null) return;
            foreach (var kv in data.buffs)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                var list = new List<Buff>();
                foreach (var b in kv.Value)
                    list.Add(new Buff { id = b.id, remaining = b.remaining, value = b.value });
                _buffs[id] = list;
            }
        }

        public static void Clear() => _buffs.Clear();

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<long>();
            foreach (var id in _buffs.Keys)
                if (!alive.Contains(id)) dead.Add(id);
            foreach (var id in dead)
            {
                _buffs.Remove(id);
                removed++;
            }
            return removed;
        }
    }
}
