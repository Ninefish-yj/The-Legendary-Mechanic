using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.75.0 锻炉权限树（原著细还原；v0.75.2 修正职业链为原著完整13阶）
    /// 原著依据：
    ///  1. 第287章起建立"锻炉权限树"：所有能力必须匹配具体权限等级与资源阈值，杜绝战力膨胀（红袖读书百科）；
    ///  2. 机械系进阶知识是机密：五级权限以上才有资格查阅（起点#47）；
    ///  3. 机械系完整职业链（起点专栏原文）：机械入门者→机械师学徒→见习机械师→磁环机械师→数据机械师→
    ///     战争机械师→虚拟机械师→星海机械师→真理机械师→使徒机械师→帝皇机械师→主宰机械师→神座机械师，
    ///     最终突破超A级界限成为"超神机械师"（X级，机械师的尽头）；
    ///     转职需要知识/等级/资源：转职数据机械师95级(B级)[356]、转职真理机械师需学习【异态能量】知识[661]、
    ///     神座机械师为后期转职[1388]。
    /// 真实系统适配：机械系单位击杀累积锻炉资源，资源达阈值自动提升权限等级（1~13，对应原著13阶职业）；
    /// 权限等级决定机械伤害加成与机械称号；权限与资源阈值挂钩（原著：能力必须匹配权限等级与资源阈值）。
    /// </summary>
    public static class SuperMechForge
    {
        public class ForgeSaveData
        {
            public Dictionary<string, int> permission = new Dictionary<string, int>(); // actorId -> 权限等级
            public Dictionary<string, int> resources = new Dictionary<string, int>(); // actorId -> 已投入资源
        }

        private static readonly ForgeSaveData _data = new ForgeSaveData();
        public static ForgeSaveData Data => _data;

        // 原著机械系完整职业链13阶（起点专栏原文：机械入门者→…→神座机械师；超神机械师为X级尽头，权限13级对应13阶）
        private static readonly (int perm, string title)[] Titles =
        {
            (1,  "机械入门者"),
            (2,  "机械师学徒"),
            (3,  "见习机械师"),
            (4,  "磁环机械师"),
            (5,  "数据机械师"),
            (6,  "战争机械师"),
            (7,  "虚拟机械师"),
            (8,  "星海机械师"),
            (9,  "真理机械师"),
            (10, "使徒机械师"),
            (11, "帝皇机械师"),
            (12, "主宰机械师"),
            (13, "神座机械师"),
        };

        // 数值（原著：资源阈值驱动，杜绝战力膨胀）
        private const int MaxPermission = 13;           // 权限上限（原著13阶职业）
        private const int ResourcePerKill = 1;          // 机械系每击杀+1锻炉资源
        private const int ThresholdBase = 10;           // 权限1级阈值
        private const int ThresholdPerLevel = 5;        // 每级递增5资源
        private const float DamagePerPermission = 0.015f; // 每权限级+1.5%机械伤害（上限13级→19.5%）

        // ============ 权限查询 ============

        public static bool IsMechanic(Actor a)
        {
            return a != null && a.hasTrait(SuperMechTraits.ClassMech);
        }

        public static int GetPermission(Actor a)
        {
            if (a == null) return 0;
            return _data.permission.TryGetValue(a.id.ToString(), out int p) ? p : 0;
        }

        public static int GetResource(Actor a)
        {
            if (a == null) return 0;
            return _data.resources.TryGetValue(a.id.ToString(), out int r) ? r : 0;
        }

        /// <summary>下一级权限所需资源（原著：资源阈值）</summary>
        public static int GetNextThreshold(int currentPerm)
        {
            if (currentPerm >= MaxPermission) return -1;
            return ThresholdBase + currentPerm * ThresholdPerLevel;
        }

        /// <summary>机械称号（原著：机械系职业链）</summary>
        public static string GetTitle(int perm)
        {
            string title = "未入阶";
            foreach (var t in Titles)
                if (perm >= t.perm) title = t.title;
            return title;
        }

        /// <summary>机械伤害加成（原著：能力必须匹配权限等级，权限越高机械战力越强）</summary>
        public static float GetMechDamageBonus(Actor a)
        {
            if (!SuperMechConfig.ForgeEnabled || a == null) return 1f;
            int perm = GetPermission(a);
            if (perm <= 0 || !IsMechanic(a)) return 1f;
            return 1f + perm * DamagePerPermission;
        }

        // ============ 战斗挂接 ============

        /// <summary>机械系单位击杀：累积锻炉资源并自动提升权限（原著：资源阈值驱动成长）</summary>
        public static void OnMechKill(Actor killer, Actor target)
        {
            if (!SuperMechConfig.ForgeEnabled || killer == null) return;
            if (!IsMechanic(killer)) return;

            string key = killer.id.ToString();
            _data.resources[key] = GetResource(killer) + ResourcePerKill;

            // 自动升级：资源≥阈值即消耗并提升权限
            int perm = GetPermission(killer);
            int threshold = GetNextThreshold(perm);
            if (threshold > 0 && _data.resources[key] >= threshold)
            {
                _data.resources[key] -= threshold;
                _data.permission[key] = perm + 1;
                Debug.Log($"[超神机械师] 锻炉权限提升：{(killer.name != null ? killer.name : "机械师")} 权限{perm + 1}级（{GetTitle(perm + 1)}）");
            }
        }

        // ============ 存档 ============

        public static ForgeSaveData Save() => _data;

        public static void Load(ForgeSaveData data)
        {
            _data.permission.Clear();
            _data.resources.Clear();
            if (data == null) return;
            if (data.permission != null)
                foreach (var kv in data.permission) _data.permission[kv.Key] = kv.Value;
            if (data.resources != null)
                foreach (var kv in data.resources) _data.resources[kv.Key] = kv.Value;
        }

        public static void Clear()
        {
            _data.permission.Clear();
            _data.resources.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<string>();
            foreach (var kv in _data.permission)
            {
                if (!long.TryParse(kv.Key, out long id)) continue;
                if (!alive.Contains(id)) dead.Add(kv.Key);
            }
            foreach (var key in dead)
            {
                _data.permission.Remove(key);
                _data.resources.Remove(key);
                removed++;
            }
            return removed;
        }
    }
}
