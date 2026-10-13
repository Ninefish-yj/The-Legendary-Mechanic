using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>自定义生物AI系统（v0.138.0）
    /// 为机械军团、异化怪物、虚空龙、星兽等自定义生物添加独立AI行为
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechCreatureAI
    {
        // === 机械军团AI ===
        private static readonly Dictionary<long, int> _lastMechAction = new Dictionary<long, int>();
        private const int MechActionInterval = 16;  // 机械单位每16tick决策一次

        // === 异化怪物AI ===
        private static readonly Dictionary<long, int> _lastInfectedAction = new Dictionary<long, int>();
        private const int InfectedActionInterval = 8;  // 异化怪物更频繁决策

        // === 虚空龙/星兽AI ===
        private static readonly Dictionary<long, int> _lastBossAction = new Dictionary<long, int>();
        private const int BossActionInterval = 32;  // Boss级单位决策间隔更长

        /// <summary>自定义生物AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.units == null) return;

            int mechProcessed = 0;
            int infectedProcessed = 0;
            int bossProcessed = 0;

            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null || !a.isAlive()) continue;

                // 判断生物类型
                if (IsMechUnit(a))
                {
                    if (mechProcessed >= 20) continue;
                    if (a.age - _lastMechAction.GetValueOrDefault(a.id, 0) < MechActionInterval) continue;
                    _lastMechAction[a.id] = a.age;
                    TickMechAI(a);
                    mechProcessed++;
                }
                else if (IsInfected(a))
                {
                    if (infectedProcessed >= 15) continue;
                    if (a.age - _lastInfectedAction.GetValueOrDefault(a.id, 0) < InfectedActionInterval) continue;
                    _lastInfectedAction[a.id] = a.age;
                    TickInfectedAI(a);
                    infectedProcessed++;
                }
                else if (IsBossCreature(a))
                {
                    if (bossProcessed >= 5) continue;
                    if (a.age - _lastBossAction.GetValueOrDefault(a.id, 0) < BossActionInterval) continue;
                    _lastBossAction[a.id] = a.age;
                    TickBossAI(a);
                    bossProcessed++;
                }
            }
        }

        // ========== 机械军团AI ==========

        /// <summary>是否为机械军团单位</summary>
        private static bool IsMechUnit(Actor a)
        {
            if (a == null || a.asset == null) return false;
            string id = a.asset.id ?? "";
            return id.StartsWith("sm_mech_");
        }

        /// <summary>机械单位AI：群体作战+优先高威胁目标+低血量撤退</summary>
        private static void TickMechAI(Actor mech)
        {
            if (mech == null || !mech.isAlive()) return;

            // 不灭械兵：低血量自我修复
            if (mech.asset.id == "sm_mech_immortal" && mech.getHealthRatio() < 0.5f)
            {
                // 自我修复：恢复少量生命
                float heal = mech.getMaxHealth() * 0.05f;
                mech.data.health = (int)Mathf.Min(mech.getMaxHealth(), mech.data.health + heal);
                return;
            }

            // 低血量撤退（除了重装械兵和机械战神）
            if (mech.getHealthRatio() < 0.25f &&
                mech.asset.id != "sm_mech_heavy" &&
                mech.asset.id != "sm_mech_god")
            {
                // 撤退：远离最近的敌人
                Actor nearestEnemy = FindNearestEnemy(mech, 20f);
                if (nearestEnemy != null)
                {
                    MoveAwayFrom(mech, nearestEnemy);
                    return;
                }
            }

            // 寻找目标：优先攻击威胁最高的敌人
            Actor target = FindHighestThreatTarget(mech, 30f);
            if (target != null)
            {
                // 群体作战：如果附近有友方机械单位，一起攻击
                int nearbyAllies = CountNearbyMechAllies(mech, 15f);
                if (nearbyAllies >= 2 || mech.asset.id == "sm_mech_god")
                {
                    // 群体优势或Boss级：主动攻击
                    AttackTarget(mech, target);
                }
                else if (Random.value < 0.6f)
                {
                    // 数量不足：60%概率攻击，40%概率等待援军
                    AttackTarget(mech, target);
                }
            }
            else
            {
                // 无目标：向最近的友方机械单位聚集
                Actor nearestAlly = FindNearestMechAlly(mech, 40f);
                if (nearestAlly != null && Vector2.Distance(GetPos(mech), GetPos(nearestAlly)) > 5f)
                {
                    MoveTowards(mech, nearestAlly);
                }
            }
        }

        // ========== 异化怪物AI ==========

        /// <summary>是否为异化怪物</summary>
        private static bool IsInfected(Actor a)
        {
            if (a == null || a.asset == null) return false;
            return a.asset.id == "sm_infected" || a.hasTrait("sm_infected_trait");
        }

        /// <summary>异化怪物AI：无差别攻击+感染传播+群体冲锋</summary>
        private static void TickInfectedAI(Actor infected)
        {
            if (infected == null || !infected.isAlive()) return;

            // 寻找最近的任何生物（无差别攻击）
            Actor target = FindNearestAnyTarget(infected, 25f);
            if (target != null)
            {
                // 群体冲锋：附近有3个以上异化怪物时加速
                int nearbyInfected = CountNearbyInfected(infected, 10f);
                if (nearbyInfected >= 3)
                {
                    // 群体冲锋：提升移动速度
                    infected.stats["speed"] = Mathf.Max(infected.stats["speed"] * 1.2f, 1f);
                }
                AttackTarget(infected, target);

                // 攻击有概率感染目标
                if (Random.value < 0.15f && !IsInfected(target) && !target.hasTrait("sm_player_hanxiao"))
                {
                    // 感染目标（简化：添加异化特质）
                    target.addTrait("sm_infected_trait");
                    Debug.Log($"[超神机械师] {target.name} 被异化为异化怪物");
                }
            }
            else
            {
                // 无目标：随机游荡
                if (Random.value < 0.3f)
                {
                    MoveRandom(infected);
                }
            }
        }

        // ========== Boss级生物AI ==========

        /// <summary>是否为Boss级生物（虚空龙、星兽、机械战神）</summary>
        private static bool IsBossCreature(Actor a)
        {
            if (a == null || a.asset == null) return false;
            string id = a.asset.id ?? "";
            return id == "sm_void_dragon" || id == "sm_star_beast" ||
                   id == "sm_mech_god" || id == "sm_worldtree_soldier";
        }

        /// <summary>Boss级AI：范围攻击+领地意识+特殊技能</summary>
        private static void TickBossAI(Actor boss)
        {
            if (boss == null || !boss.isAlive()) return;

            // 寻找范围内的敌人
            Actor target = FindNearestEnemy(boss, 40f);
            if (target != null)
            {
                // Boss级：主动攻击
                AttackTarget(boss, target);

                // 范围攻击：如果附近有多个敌人，进行范围攻击
                int nearbyEnemies = CountNearbyEnemies(boss, 15f);
                if (nearbyEnemies >= 3 && Random.value < 0.3f)
                {
                    // 范围攻击：对附近所有敌人造成伤害
                    AoEAttack(boss, 15f, boss.stats["attack"] * 0.5f);
                }
            }
            else
            {
                // 无目标：领地巡逻（在生成点附近移动）
                if (Random.value < 0.2f)
                {
                    MoveRandom(boss);
                }
            }
        }

        // ========== 通用辅助方法 ==========

        private static Actor FindNearestEnemy(Actor a, float maxDist)
        {
            if (a == null || World.world == null || World.world.units == null) return null;
            Actor best = null;
            float bestDist = maxDist;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (other.kingdom == a.kingdom && a.kingdom != null) continue; // 同王国不攻击
                float dist = Vector2.Distance(GetPos(a), GetPos(other));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }
            return best;
        }

        private static Actor FindHighestThreatTarget(Actor a, float maxDist)
        {
            if (a == null || World.world == null || World.world.units == null) return null;
            Actor best = null;
            float bestThreat = 0f;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (other.kingdom == a.kingdom && a.kingdom != null) continue;
                float dist = Vector2.Distance(GetPos(a), GetPos(other));
                if (dist > maxDist) continue;
                // 威胁值 = 攻击力 / 距离
                float threat = other.stats["attack"] / Mathf.Max(dist, 1f);
                if (threat > bestThreat)
                {
                    bestThreat = threat;
                    best = other;
                }
            }
            return best;
        }

        private static Actor FindNearestAnyTarget(Actor a, float maxDist)
        {
            if (a == null || World.world == null || World.world.units == null) return null;
            Actor best = null;
            float bestDist = maxDist;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (IsInfected(other)) continue; // 不攻击同类
                float dist = Vector2.Distance(GetPos(a), GetPos(other));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }
            return best;
        }

        private static Actor FindNearestMechAlly(Actor a, float maxDist)
        {
            if (a == null || World.world == null || World.world.units == null) return null;
            Actor best = null;
            float bestDist = maxDist;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (!IsMechUnit(other)) continue;
                float dist = Vector2.Distance(GetPos(a), GetPos(other));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = other;
                }
            }
            return best;
        }

        private static int CountNearbyMechAllies(Actor a, float radius)
        {
            int count = 0;
            if (a == null || World.world == null || World.world.units == null) return count;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (!IsMechUnit(other)) continue;
                if (Vector2.Distance(GetPos(a), GetPos(other)) < radius) count++;
            }
            return count;
        }

        private static int CountNearbyInfected(Actor a, float radius)
        {
            int count = 0;
            if (a == null || World.world == null || World.world.units == null) return count;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (!IsInfected(other)) continue;
                if (Vector2.Distance(GetPos(a), GetPos(other)) < radius) count++;
            }
            return count;
        }

        private static int CountNearbyEnemies(Actor a, float radius)
        {
            int count = 0;
            if (a == null || World.world == null || World.world.units == null) return count;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == a) continue;
                if (other.kingdom == a.kingdom && a.kingdom != null) continue;
                if (Vector2.Distance(GetPos(a), GetPos(other)) < radius) count++;
            }
            return count;
        }

        private static void AttackTarget(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return;
            // 移动向目标（原版AI会处理攻击）
            MoveTowards(attacker, target);
        }

        private static void AoEAttack(Actor attacker, float radius, float damage)
        {
            if (attacker == null || World.world == null || World.world.units == null) return;
            foreach (Actor other in World.world.units.units_only_alive)
            {
                if (other == null || !other.isAlive() || other == attacker) continue;
                if (other.kingdom == attacker.kingdom && attacker.kingdom != null) continue;
                if (Vector2.Distance(GetPos(attacker), GetPos(other)) < radius)
                {
                    // 造成范围伤害
                    other.getHit(damage, false, AttackType.Other, attacker);
                }
            }
        }

        private static void MoveTowards(Actor a, Actor target)
        {
            if (a == null || target == null) return;
            try
            {
                WorldTile tile = World.world.GetTile((int)target.current_position.x, (int)target.current_position.y);
                if (tile != null) a.goTo(tile, false, false);
            }
            catch { }
        }

        private static void MoveAwayFrom(Actor a, Actor threat)
        {
            if (a == null || threat == null) return;
            try
            {
                Vector2 pos = GetPos(a);
                Vector2 threatPos = GetPos(threat);
                Vector2 dir = (pos - threatPos).normalized;
                WorldTile tile = World.world.GetTile((int)(pos.x + dir.x * 10f), (int)(pos.y + dir.y * 10f));
                if (tile != null) a.goTo(tile, false, false);
            }
            catch { }
        }

        private static void MoveRandom(Actor a)
        {
            if (a == null) return;
            try
            {
                Vector2 pos = GetPos(a);
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(3f, 10f);
                WorldTile tile = World.world.GetTile((int)(pos.x + Mathf.Cos(angle) * dist), (int)(pos.y + Mathf.Sin(angle) * dist));
                if (tile != null) a.goTo(tile, false, false);
            }
            catch { }
        }

        private static Vector2 GetPos(Actor a)
        {
            if (a == null) return Vector2.zero;
            return new Vector2(a.current_position.x, a.current_position.y);
        }

        /// <summary>清理死亡单位数据</summary>
        public static int CleanupDead(System.Collections.Generic.HashSet<long> alive)
        {
            int removed = 0;
            removed += CleanupDict(_lastMechAction, alive);
            removed += CleanupDict(_lastInfectedAction, alive);
            removed += CleanupDict(_lastBossAction, alive);
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
            _lastMechAction.Clear();
            _lastInfectedAction.Clear();
            _lastBossAction.Clear();
        }
    }
}
