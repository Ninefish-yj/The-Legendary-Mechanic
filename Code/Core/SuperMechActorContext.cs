using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// ActorContext：统一的per-actor数据容器。
    /// 替代30+个静态字典（_qiData[id]、_stageData[id]...），所有系统的数据集中在这里。
    /// 清理死单位时只需清一个对象。
    /// 各系统逐步迁移：新数据直接放这里，老数据保留在原静态类中作为兼容层。
    /// </summary>
    public class SuperMechActorContext
    {
        // === 基础身份 ===
        public long actorId;
        public string name;

        // === 成长体系 ===
        public int exactRank = -1;       // 精确阶位索引（0=F~13=X）
        public int stage = 0;            // 职业阶段
        public string classTrait = "";   // 职业特质ID
        public int qiLevel = 0;          // 气力等级
        public float qiCurrent = 0f;
        public float qiMax = 0f;
        public int potential = 0;        // 潜能点
        public int awakenedLevel = 0;    // 觉醒等级
        public float awakenedXp = 0f;

        // === 能力体系 ===
        public int divinityPoints = 0;   // 神性点
        public int divinityProfLayers = 0;
        public int divinitySpeciesLayers = 0;
        public bool divinityTriggered = false;
        public int infoStateLevel = 0;   // 信息态等级
        public bool transcended = false; // 是否超神
        public float heritage = 0f;      // 传承点（浮点增长率）

        // === 圣所 ===
        public int sanctuaryVisits = 0;
        public int reviveCount = 0;
        public int keyFragments = 0;     // 钥匙碎片
        public int keyMaterials = 0;     // 钥匙材料

        // === 势力 ===
        public string factionId = null;
        public bool isFactionLeader = false;
        public long summonerId = -1;       // v0.57.0 召唤者ID（召唤物专用）
        public float expireAge = -1f;      // 召唤物过期年龄（虚拟创世暂时性产物）

        // === 状态标记 ===
        public bool isDescendant = false; // 降临者
        public bool hasTalent = false;    // 是否有超能者天赋
        public float lastCombatTime = 0f;

        // === 扩展：任意系统的自定义数据 ===
        public Dictionary<string, object> custom = new Dictionary<string, object>();

        public T GetCustom<T>(string key, T defaultValue = default)
        {
            if (custom.TryGetValue(key, out var val) && val is T typed) return typed;
            return defaultValue;
        }

        public void SetCustom(string key, object val)
        {
            custom[key] = val;
        }
    }

    /// <summary>ActorContext 全局注册表</summary>
    public static class SuperMechActorContextRegistry
    {
        private static readonly Dictionary<long, SuperMechActorContext> _contexts = new Dictionary<long, SuperMechActorContext>();

        /// <summary>获取或创建Actor的Context</summary>
        public static SuperMechActorContext Get(Actor a)
        {
            if (a == null) return null;
            if (!_contexts.TryGetValue(a.id, out var ctx))
            {
                ctx = new SuperMechActorContext { actorId = a.id, name = a.name };
                _contexts[a.id] = ctx;
            }
            return ctx;
        }

        /// <summary>尝试获取（不存在返回null）</summary>
        public static SuperMechActorContext TryGet(long id)
        {
            _contexts.TryGetValue(id, out var ctx);
            return ctx;
        }

        /// <summary>清理死单位</summary>
        public static int CleanupDead(HashSet<long> alive)
        {
            var toRemove = new List<long>();
            foreach (var id in _contexts.Keys)
                if (!alive.Contains(id)) toRemove.Add(id);
            foreach (var id in toRemove) _contexts.Remove(id);
            return toRemove.Count;
        }

        public static void Clear()
        {
            _contexts.Clear();
        }

        public static int Count => _contexts.Count;

        // === 通用判断（避免Power层反向依赖Class层）===

        /// <summary>是否超能者单位（有天赋或职业特质）</summary>
        public static bool IsSuperMech(Actor a)
        {
            if (a == null) return false;
            return a.hasTrait(SuperMechTraits.ClassMech)
                || a.hasTrait(SuperMechTraits.ClassPsi)
                || a.hasTrait(SuperMechTraits.ClassMind)
                || a.hasTrait(SuperMechTraits.ClassMartial)
                || a.hasTrait(SuperMechTraits.ClassMage)
                || a.hasTrait("sm_talent");
        }

        /// <summary>是否已觉醒</summary>
        public static bool IsAwakened(Actor a)
        {
            return a != null && a.hasTrait("sm_awakened");
        }

        /// <summary>从ActorContext读取阶位（无Context返回0）</summary>
        public static int GetRank(Actor a)
        {
            if (a == null) return 0;
            var ctx = TryGet(a.id);
            if (ctx != null && ctx.exactRank >= 0) return ctx.exactRank;
            return 0;
        }

        /// <summary>从ActorContext读取职业阶段（无Context返回0）</summary>
        public static int GetStage(Actor a)
        {
            if (a == null) return 0;
            var ctx = TryGet(a.data.id);
            if (ctx != null && ctx.stage > 0) return ctx.stage;
            return 0;
        }
    }
}
