using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.74.0+ 异神·最终决战（B线最高强度单体事件；v0.75.4/0.75.5 按原著文件核查+剧情清理）
    /// 机制：异神为巅峰超A级单体威胁，周期降临（伤害×2.5）；击杀单位触发【异能·复刻】成长
    /// （每击杀+5%伤害，上限+50%）；首次被击杀=封印于时空琥珀（冷却后卷土重来）；
    /// 再次击杀=最终决战结算：击杀者继承异神之力（伤害+30%）、全体觉醒单位潜能+10、最强文明科技+100。
    /// 原著章节：703（身份）/805（战力/基因链负荷）/1040（核心能力/封印）/1392（夺能）/1415（超神级战力）。
    /// 注：原著无"三招秒杀/灵魂逃脱"表述（"三招"全文0次），相关措辞已修正。
    /// </summary>
    public static class SuperMechEsGod
    {
        public const string EsGodTrait = "sm_esgod";        // 异神标记
        public const string InheritorTrait = "sm_esgod_power"; // 异神之力（终局后由击杀者继承）

        public enum EventState { Idle, Active, SoulEscaped, Defeated }

        public class EsGodSaveData
        {
            public EventState state = EventState.Idle;
            public int nextSpawnInTicks = 600;   // 距下次降临
            public int replicateCount;           // 异能·复刻层数（击杀数）
            public int soulEscapes;              // 封印/卷土重来次数（首次封印于时空琥珀）
            public bool finalDefeated;           // 是否已被终局消灭
            public long esGodId = -1;            // 异神单位id
        }

        private static readonly EsGodSaveData _data = new EsGodSaveData();
        public static EsGodSaveData Data => _data;

        // 数值（原著：巅峰超A级·异能之神——全模组最强单体反派，接近但未达超神级）
        private const float EsGodDamageMult = 2.5f;        // 异神伤害倍率（原著：巅峰超A级碾压普通单位）
        private const float ReplicateGainPerKill = 0.05f;  // 异能·复刻：每击杀+5%伤害（原著：夺取异能变强）
        private const float ReplicateMax = 0.50f;          // 复刻成长上限+50%
        private const int SoulEscapeCooldown = 900;        // 封印时空琥珀后卷土重来冷却
        private const int PotentialFinalReward = 10;       // 最终决战奖励：全体觉醒潜能+10
        private const int TechFinalReward = 100;           // 最终决战奖励：最强文明科技+100
        private const float InheritorDamageBonus = 0.30f;  // 继承者伤害+30%（终局后由击杀者继承）

        // ============ 状态查询 ============

        public static bool IsActive => _data.state == EventState.Active;
        public static bool IsEsGod(Actor a) => a != null && a.hasTrait(EsGodTrait);

        /// <summary>异神伤害倍率：基础×2.5 + 异能·复刻成长（原著：夺取异能、以基因链储存变强）</summary>
        public static float GetEsGodDamageMult(Actor a)
        {
            if (a == null || !a.hasTrait(EsGodTrait)) return 1f;
            return EsGodDamageMult * (1f + Mathf.Min(ReplicateMax, _data.replicateCount * ReplicateGainPerKill));
        }

        /// <summary>继承者异神之力加成（终局后由击杀者继承）</summary>
        public static float GetInheritorBonus(Actor a)
        {
            if (a == null || !a.hasTrait(InheritorTrait)) return 1f;
            return 1f + InheritorDamageBonus;
        }

        // ============ 主循环 ============

        public static void TickEsGod(float delta)
        {
            if (!SuperMechConfig.EsGodEnabled) return;
            if (World.world == null || World.world.units == null) return;

            if (_data.state == EventState.Idle)
            {
                _data.nextSpawnInTicks--;
                if (_data.nextSpawnInTicks <= 0) SpawnEsGod();
            }
            else if (_data.state == EventState.Active)
            {
                // 异神死亡处理由战斗补丁触发（OnEsGodKilled）
            }
            else if (_data.state == EventState.SoulEscaped)
            {
                // 卷土重来（原著：异神被关进时空琥珀后卷土重来[1040回顾/1417/1429]）
                _data.nextSpawnInTicks--;
                if (_data.nextSpawnInTicks <= 0) SpawnEsGod();
            }
        }

        private static void SpawnEsGod()
        {
            WorldTile tile = FindEdgeTile();
            if (tile == null) return;
            try
            {
                Actor god = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
                if (god == null) return;
                if (!god.hasTrait(EsGodTrait)) god.addTrait(EsGodTrait);
                _data.esGodId = god.id;
                _data.state = EventState.Active;
                Debug.Log($"[超神机械师] 【异神降临】巅峰超A级·异能之神降临（异能·复刻层数 {_data.replicateCount}）！");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 异神降临失败: {e.Message}");
            }
        }

        /// <summary>战斗挂接：异神击杀单位→异能·复刻成长（原著：近身接触吸收异能基因）</summary>
        public static void OnEsGodKill(Actor killer, Actor target)
        {
            if (killer == null || !killer.hasTrait(EsGodTrait)) return;
            _data.replicateCount++;
        }

        /// <summary>战斗挂接：异神被击杀（原著：被封印于时空琥珀后卷土重来[1040/1417/1429]/最终决战[1392/1415]）</summary>
        public static void OnEsGodKilled(Actor killer, Actor target)
        {
            if (target == null || !target.hasTrait(EsGodTrait)) return;
            if (_data.esGodId != target.id) return;

            if (_data.finalDefeated)
            {
                // 已最终决战消灭过，不再复活
                _data.state = EventState.Defeated;
                return;
            }

            // 已卷土重来过一次：本次为再次决战的终局（原著#1392：能力被夺走）
            if (_data.soulEscapes > 0)
            {
                ConfirmFinalDefeat(killer, target);
                return;
            }

            // 首次击杀：异神被封印于时空琥珀（原著#1040），冷却后卷土重来
            _data.soulEscapes++;
            _data.state = EventState.SoulEscaped;
            _data.nextSpawnInTicks = SoulEscapeCooldown;
            Debug.Log($"[超神机械师] 【异神】异神被击退，封印于时空琥珀！将在 {SoulEscapeCooldown} tick 后卷土重来");
        }

        /// <summary>异神最终决战确认：异神卷土重来后再次被击杀时，由战斗补丁调用，判定终局（原著第1392章还施彼身）</summary>
        public static void ConfirmFinalDefeat(Actor killer, Actor target)
        {
            if (target == null || !target.hasTrait(EsGodTrait)) return;
            if (_data.esGodId != target.id) return;

            // 异神养成计划（原著#1040）：能力由继承者获得——击杀者获得异神之力
            if (killer != null && killer.isAlive())
            {
                if (!killer.hasTrait(InheritorTrait)) killer.addTrait(InheritorTrait);
            }

            // 终局奖励：全体觉醒单位潜能+10、最强文明科技+100
            Kingdom top = FindStrongestKingdom();
            if (top != null) SuperMechCivilization.AddTechPoints(top, TechFinalReward);

            if (World.world.units != null)
            {
                var units = World.world.units.units_only_alive;
                if (units != null)
                {
                    foreach (var a in units)
                    {
                        if (a == null || !a.isAlive()) continue;
                        if (SuperMechAwakened.IsAwakened(a)) SuperMechPotential.AddPotential(a, PotentialFinalReward);
                    }
                }
            }

            _data.finalDefeated = true;
            _data.state = EventState.Defeated;
            Debug.Log($"[超神机械师] 【异神终局】异神被彻底消灭！能力由继承者获得（伤害+30%），全文明获得终局奖励");
        }

        // ============ 工具 ============

        private static WorldTile FindEdgeTile()
        {
            if (World.world == null) return null;
            int w = MapBox.width, h = MapBox.height;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                int x = Random.value < 0.5f ? Random.Range(0, Mathf.Max(1, w / 8)) : Random.Range(w - w / 8, w);
                int y = Random.Range(0, h);
                if (x < 0 || y < 0 || x >= w || y >= h) continue;
                WorldTile t = World.world.GetTile(x, y);
                if (t != null && t.Type != TileLibrary.deep_ocean && t.Type != TileLibrary.close_ocean && t.Type != TileLibrary.mountains)
                    return t;
            }
            return null;
        }

        private static Kingdom FindStrongestKingdom()
        {
            try
            {
                if (World.world == null || World.world.kingdoms == null || World.world.kingdoms.list == null) return null;
                Kingdom best = null;
                int bestLevel = -1;
                foreach (var k in World.world.kingdoms.list)
                {
                    if (k == null || k.wild || !k.isCiv()) continue;
                    int lv = (int)SuperMechCivilization.GetCivLevelFromKingdom(k);
                    if (lv > bestLevel) { bestLevel = lv; best = k; }
                }
                return best;
            }
            catch { return null; }
        }

        // ============ 存档 ============

        public static EsGodSaveData Save() => _data;

        public static void Load(EsGodSaveData data)
        {
            if (data == null) return;
            _data.state = data.state;
            _data.nextSpawnInTicks = data.nextSpawnInTicks;
            _data.replicateCount = data.replicateCount;
            _data.soulEscapes = data.soulEscapes;
            _data.finalDefeated = data.finalDefeated;
            _data.esGodId = data.esGodId;
        }

        public static void Clear()
        {
            _data.state = EventState.Idle;
            _data.nextSpawnInTicks = 600;
            _data.replicateCount = 0;
            _data.soulEscapes = 0;
            _data.finalDefeated = false;
            _data.esGodId = -1;
        }
    }
}
