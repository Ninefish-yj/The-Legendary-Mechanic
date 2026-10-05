using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.76.0 异神·顶层超A之一（用户修正定位：异神只是顶层超A级之一，能存在是三大文明的默许，
    /// 不是威压宇宙的天灾；v0.74 天灾框架[周期降临/复刻成长/封印琥珀/终局决战]整体删除）
    /// 原著依据：
    ///  1. 异神="异能之神"，凶名赫赫的超A级顶级强者（ch712 黑雾身躯/红色光点/异神分身）；
    ///  2. 顶层超A之一——与韩萧、麦尼逊等同量级的个体伟力代表（ch713+）；
    ///  3. 存在依赖三大文明默许（ch1002/1018：三大文明可默许亦可清算超A级）；
    ///  4. 异神遗产（ch1008：进化方块/技术资料，谁得到谁继承准宇宙级文明核心技术）。
    /// 机制：异神作为顶层超A个体，在世界出现首个X阶超A级时降临一次（不周期循环）；
    /// 属于超A个体（SuperMechSupermA 计数/清算/协会一视同仁）；被击杀后遗落异神遗产
    /// （最强文明科技+30，原著：异神遗产=技术资料）；不再封印复活。
    /// </summary>
    public static class SuperMechEsGod
    {
        public const string EsGodTrait = "sm_esgod";        // 异神标记（顶层超A）

        public enum EventState { Idle, Active, Defeated }

        public class EsGodSaveData
        {
            public EventState state = EventState.Idle;
            public long esGodId = -1;            // 异神单位id
            public bool heritageClaimed;         // 异神遗产是否已掉落
        }

        private static readonly EsGodSaveData _data = new EsGodSaveData();
        public static EsGodSaveData Data => _data;

        // 数值（原著：巅峰超A级·异能之神——顶层超A个体，接近但未达超神级）
        private const float EsGodDamageMult = 2.5f;        // 异神伤害倍率（原著：顶层超A碾压普通单位）
        private const int TechHeritageReward = 30;          // 异神遗产：最强文明科技+30（原著ch1008技术资料）
        private const int PotentialHeritageReward = 3;      // 异神遗产：全体觉醒潜能+3

        // ============ 状态查询 ============

        public static bool IsActive => _data.state == EventState.Active;
        public static bool IsEsGod(Actor a) => a != null && a.hasTrait(EsGodTrait);

        /// <summary>异神伤害倍率：×2.5（原著：顶层超A·异能之神，个体伟力巅峰）</summary>
        public static float GetEsGodDamageMult(Actor a)
        {
            if (a == null || !a.hasTrait(EsGodTrait)) return 1f;
            return EsGodDamageMult;
        }

        // ============ 主循环 ============

        public static void TickEsGod(float delta)
        {
            if (!SuperMechConfig.EsGodEnabled) return;
            if (World.world == null || World.world.units == null) return;

            if (_data.state == EventState.Idle)
            {
                // 降临条件：世界已出现X阶超A级个体（原著：超A级是宇宙中的大人物，异神为顶层之一）
                if (SuperMechSupermA.CountSuperA() >= 1)
                    SpawnEsGod();
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
                Debug.Log($"[超神机械师] 【异神】顶层超A·异能之神现身（三大文明默许下的个体伟力）");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 异神降临失败: {e.Message}");
            }
        }

        /// <summary>战斗挂接：异神被击杀→遗落异神遗产（原著ch1008：异神遗产=进化方块/技术资料，
        /// 谁得到谁继承准宇宙级文明核心技术）——不再封印复活</summary>
        public static void OnEsGodKilled(Actor killer, Actor target)
        {
            if (target == null || !target.hasTrait(EsGodTrait)) return;
            if (_data.esGodId != target.id) return;

            if (!_data.heritageClaimed)
            {
                _data.heritageClaimed = true;
                Kingdom top = FindStrongestKingdom();
                if (top != null) SuperMechCivilization.AddTechPoints(top, TechHeritageReward);

                if (World.world.units != null && World.world.units.units_only_alive != null)
                {
                    foreach (var a in World.world.units.units_only_alive)
                    {
                        if (a == null || !a.isAlive()) continue;
                        if (SuperMechAwakened.IsAwakened(a)) SuperMechPotential.AddPotential(a, PotentialHeritageReward);
                    }
                }
                Debug.Log($"[超神机械师] 【异神遗产】异神被击杀，遗落异神遗产：最强文明科技+{TechHeritageReward}、全体觉醒潜能+{PotentialHeritageReward}");
            }
            _data.state = EventState.Defeated;
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
            _data.esGodId = data.esGodId;
            _data.heritageClaimed = data.heritageClaimed;
        }

        public static void Clear()
        {
            _data.state = EventState.Idle;
            _data.esGodId = -1;
            _data.heritageClaimed = false;
        }
    }
}
