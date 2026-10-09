using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.73.0 世界树文明入侵（5.0版本；v0.75.2 修正五大树王完整编制，v0.75.5 清理剧情）
    /// 机制：世界树舰队从地图边缘虚空周期降临（树王×1五大树王轮换+叶执行官+圣树使者+树兵）；
    /// 入侵期间星际联合军加成（全文明伤害+5%/减伤+10%）；击杀树王=击退入侵结算（文明科技+60/全体潜能+3）；
    /// 树王冠位由母树授予、可被回收献祭。
    /// 原著：五大树王（祖#1335/秘#1362/镇·武道系/定·机械系/心·封印时空琥珀#1362）/星际联合军#1403。
    /// </summary>
    public static class SuperMechWorldTree
    {
        public const string TreeTrait = "sm_world_tree";       // 世界树单位标记
        public const string TreeKingTrait = "sm_world_tree_king"; // 树王标记（精英）
        public const string TreeExecutiveTrait = "sm_world_tree_exec"; // 叶执行官
        public const string TreeEnvoyTrait = "sm_world_tree_envoy";   // 圣树使者

        public enum EventState { Idle, Invading, Cooldown }

        public class WorldTreeSaveData
        {
            public EventState state = EventState.Idle;
            public int ticksLeft;              // 入侵剩余tick
            public int nextSpawnInTicks = 400; // 距下次入侵
            public int invasionCount;          // 入侵次数
            public int killedTrees;            // 已击杀世界树单位
            public int killedKings;            // 已击杀树王
            public string kingName;            // 当前树王名
        }

        private static readonly WorldTreeSaveData _data = new WorldTreeSaveData();
        public static WorldTreeSaveData Data => _data;

        // 五大树王完整编制（祖#1335/秘#1362魔法系/镇·武道系/定·机械系/心#1362封印时空琥珀）
        // 定树王=机械系/心树王=被主宰分身封印于时空琥珀[1362]；树王冠位由母树授予、可被祖树王回收[1437]）
        private static readonly string[] TreeKingNames = { "祖树王", "秘树王", "镇树王", "定树王", "心树王" };
        private static readonly string[] ExecutiveNames = { "圣树使者·青", "圣树使者·赤", "圣树使者·金", "圣树使者·墨" };

        // 数值
        private const int InvasionDurationTicks = 3600;   // 入侵持续（tick）
        private const float KingDamageMult = 1.6f;        // 树王伤害倍率
        private const float ExecDamageMult = 1.3f;        // 叶执行官伤害倍率
        private const float TreeDamageMult = 1.15f;       // 普通树兵伤害倍率（原著：好战文明）
        private const float UnionDamageBonus = 0.05f;     // 星际联合军：全文明伤害+5%（原著：统合已探索宇宙力量）
        private const float UnionDefenseBonus = 0.10f;    // 星际联合军：全文明减伤+10%
        private const int TechRewardPerInvasion = 60;     // 击退入侵：最强文明科技+60
        private const int PotentialReward = 3;            // 击退入侵：全体觉醒单位潜能+3

        // ============ 状态查询 ============

        public static bool IsInvading => _data.state == EventState.Invading;
        public static bool IsWorldTree(Actor a) => a != null && a.hasTrait(TreeTrait);
        public static bool IsTreeKing(Actor a) => a != null && a.hasTrait(TreeKingTrait);

        /// <summary>星际联合军伤害加成（入侵期间生效，原著：三大文明成立星际联合军全面开战）</summary>
        public static float GetUnionDamageBonus(Actor a)
        {
            if (!IsInvading || a == null) return 1f;
            if (a.hasTrait(TreeTrait)) return 1f;
            return 1f + UnionDamageBonus;
        }

        /// <summary>星际联合军减伤（入侵期间生效）</summary>
        public static float GetUnionDefenseBonus(Actor a)
        {
            if (!IsInvading || a == null) return 1f;
            if (a.hasTrait(TreeTrait)) return 1f;
            return 1f - UnionDefenseBonus;
        }

        /// <summary>世界树单位伤害倍率</summary>
        public static float GetTreeDamageMult(Actor a)
        {
            if (a == null || !a.hasTrait(TreeTrait)) return 1f;
            if (a.hasTrait(TreeKingTrait)) return KingDamageMult;
            if (a.hasTrait(TreeExecutiveTrait)) return ExecDamageMult;
            if (a.hasTrait(TreeEnvoyTrait)) return ExecDamageMult;
            return TreeDamageMult;
        }

        // ============ 主循环 ============

        public static void TickWorldTree(float delta)
        {
            if (!SuperMechConfig.WorldTreeEnabled) return;
            if (World.world == null || World.world.units == null) return;

            if (_data.state == EventState.Idle)
            {
                _data.nextSpawnInTicks--;
                if (_data.nextSpawnInTicks <= 0) StartInvasion();
            }
            else if (_data.state == EventState.Invading)
            {
                _data.ticksLeft--;
                if (_data.ticksLeft <= 0) EndInvasion(false);
                // 树王死亡即击退（原著#1362：心树王被封印于时空琥珀，世界树撤退）
                else if (_data.killedKings >= 1) EndInvasion(true);
            }
        }

        private static void StartInvasion()
        {
            _data.state = EventState.Invading;
            _data.ticksLeft = InvasionDurationTicks;
            _data.invasionCount++;
            _data.killedKings = 0;
            _data.kingName = TreeKingNames[Random.Range(0, TreeKingNames.Length)];

            try
            {
                // 树王 ×1（原著：五大树王之一领军）
                SpawnTreeUnit(TreeKingTrait, _data.kingName, 1);
                // 叶执行官 ×2~4（原著：十叶执行官）
                SpawnTreeUnit(TreeExecutiveTrait, "叶执行官", Random.Range(2, 5));
                // 圣树使者 ×1~2
                SpawnTreeUnit(TreeEnvoyTrait, "圣树使者", Random.Range(1, 3));
                // 树兵 ×5~10
                SpawnTreeUnit(null, "世界树士兵", Random.Range(5, 11));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 世界树入侵生成失败: {e.Message}");
            }

            Debug.Log($"[超神机械师] 【世界树入侵】{_data.kingName}率领世界树舰队降临！星际联合军成立，全文明进入战争状态");
        }

        private static void SpawnTreeUnit(string eliteTrait, string name, int count)
        {
            for (int i = 0; i < count; i++)
            {
                WorldTile tile = SMWorldUtils.FindEdgeTile();
                if (tile == null) continue;
                Actor unit = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
                if (unit == null) continue;
                if (!unit.hasTrait(TreeTrait)) unit.addTrait(TreeTrait);
                if (eliteTrait == TreeKingTrait && !unit.hasTrait(TreeKingTrait)) unit.addTrait(TreeKingTrait);
                else if (eliteTrait == TreeExecutiveTrait && !unit.hasTrait(TreeExecutiveTrait)) unit.addTrait(TreeExecutiveTrait);
                else if (eliteTrait == TreeEnvoyTrait && !unit.hasTrait(TreeEnvoyTrait)) unit.addTrait(TreeEnvoyTrait);
            }
        }

        private static void EndInvasion(bool repelled)
        {
            _data.state = EventState.Cooldown;
            _data.nextSpawnInTicks = (int)SuperMechConfig.WorldTreeInterval;

            if (repelled)
            {
                // 击退奖励：最强文明科技+60，全体觉醒单位潜能+3（原著：战后超A级种子受益）
                Kingdom top = SMWorldUtils.FindStrongestKingdom();
                if (top != null) SuperMechCivilization.AddTechPoints(top, TechRewardPerInvasion);

                if (World.world.units != null)
                {
                    var units = World.world.units.units_only_alive;
                    if (units != null)
                    {
                        foreach (var a in units)
                        {
                            if (a == null || !a.isAlive()) continue;
                            if (SuperMechAwakened.IsAwakened(a)) SuperMechPotential.AddPotential(a, PotentialReward);
                        }
                    }
                }
                Debug.Log($"[超神机械师] 【世界树入侵】树王被击杀，世界树舰队被击退！全文明获得科技与潜能奖励");
            }
            else
            {
                Debug.Log($"[超神机械师] 【世界树入侵】入侵持续结束，世界树舰队撤退（未完全击退）");
            }
        }

        /// <summary>战斗挂接：击杀世界树单位（原著：与树王/使者的战斗）</summary>
        public static void OnTreeKilled(Actor killer, Actor target)
        {
            if (target == null || !target.hasTrait(TreeTrait)) return;
            _data.killedTrees++;
            if (target.hasTrait(TreeKingTrait))
            {
                _data.killedKings++;
                // 树王被击杀：降临者融合世界树力量，觉醒虚拟创世（原著：韩萧融合世界树）
                SuperMechVirtualGenesis.OnWorldTreeUnion();
            }
        }

        // ============ 工具 ============
            return null;
        }

        

        // ============ 存档 ============

        public static WorldTreeSaveData Save() => _data;

        public static void Load(WorldTreeSaveData data)
        {
            if (data == null) return;
            _data.state = data.state;
            _data.ticksLeft = data.ticksLeft;
            _data.nextSpawnInTicks = data.nextSpawnInTicks;
            _data.invasionCount = data.invasionCount;
            _data.killedTrees = data.killedTrees;
            _data.killedKings = data.killedKings;
            _data.kingName = data.kingName;
        }

        public static void Clear()
        {
            _data.state = EventState.Idle;
            _data.ticksLeft = 0;
            _data.nextSpawnInTicks = 400;
            _data.invasionCount = 0;
            _data.killedTrees = 0;
            _data.killedKings = 0;
        }
    }
