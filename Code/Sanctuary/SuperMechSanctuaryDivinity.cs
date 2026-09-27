using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    public static partial class SuperMechSanctuary
    {
        public static void TickDivinity()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (_divinityTriggered.Contains(a.id)) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                float onar = SuperMechAdvancement.CalcOnar(a);
                float qi = SuperMechQi.GetQi(a);
                int qiLv = SuperMechQi.GetLevel(qi);

                if (onar >= DivinityOnarThreshold && qiLv >= DivinityQiLevel)
                {
                    TriggerDivinity(a);
                }
            }
        }

        private static void TriggerDivinity(Actor a)
        {
            _divinityTriggered.Add(a.id);
            Data.total_divinity_ascensions++;

            int sanctuaryIndex = -1;
            if (a.hasTrait(SuperMechTraits.ClassMech)) sanctuaryIndex = 0;
            else if (a.hasTrait(SuperMechTraits.ClassMartial)) sanctuaryIndex = 1;
            else if (a.hasTrait(SuperMechTraits.ClassPsi)) sanctuaryIndex = 2;
            else if (a.hasTrait(SuperMechTraits.ClassMage)) sanctuaryIndex = 3;
            else if (a.hasTrait(SuperMechTraits.ClassMind)) sanctuaryIndex = 4;

            if (sanctuaryIndex >= 0)
            {
                Data.sanctuary_fragments[sanctuaryIndex]++;
                string sname = SanctuaryNames[sanctuaryIndex];
                int personalFragments = Random.Range(1, 4);
                AddAuthority(a, sanctuaryIndex, personalFragments);
                Debug.Log($"[超神机械师] {a.name} 神性蜕变！获得{sname}技能碎片（全局{Data.sanctuary_fragments[sanctuaryIndex]}/{FragmentsToUnlock}，个人权限+{personalFragments}）");

                if (Data.sanctuary_fragments[sanctuaryIndex] >= FragmentsToUnlock
                    && (Data.unlocked_sanctuaries & (1 << sanctuaryIndex)) == 0)
                {
                    Data.unlocked_sanctuaries |= (1 << sanctuaryIndex);
                    Data.total_permission++;
                    Debug.Log($"[超神机械师] {sname}已解锁！权限Lv{Data.total_permission}");

                    if (CountUnlocked() >= 5 && (Data.unlocked_sanctuaries & (1 << 5)) == 0)
                    {
                        Data.sanctuary_fragments[5]++;
                        Debug.Log($"[超神机械师] 五圣所齐聚，第六圣所·信息态钥匙碎片出现！（{Data.sanctuary_fragments[5]}/{FragmentsToUnlock}）");
                        if (Data.sanctuary_fragments[5] >= FragmentsToUnlock)
                        {
                            Data.unlocked_sanctuaries |= (1 << 5);
                            Data.total_permission++;
                            Debug.Log($"[超神机械师] 第六圣所·信息态已解锁！全圣所齐聚！");
                        }
                    }
                }
            }
            else
            {
                Data.key_fragments++;
                Debug.Log($"[超神机械师] {a.name} 神性蜕变！获得圣所钥匙碎片（{Data.key_fragments}）");
            }

            var stats = SuperMechStats.Of(a);
            if (stats != null)
            {
                stats["multiplier_damage"] = ((stats["multiplier_damage"] == 0f ? 1f : stats["multiplier_damage"])) * 1.5f;
                stats["multiplier_health"] = ((stats["multiplier_health"] == 0f ? 1f : stats["multiplier_health"])) * 1.5f;
                stats["intelligence"] = (stats["intelligence"]) + 20f;
            }
            a.addTrait("sm_divinity_ascended");

            Save();
        }

        public static void TickDeadTracking()
        {
            var alive = World.world.units.units_only_alive;
            if (alive == null) return;
            var aliveIds = new HashSet<long>();

            foreach (Actor a in alive)
            {
                if (a == null) continue;
                aliveIds.Add(a.id);
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                int rankIdx = SuperMechAdvancement.GetRankIndex(a);

                var rec = new DeadUnitRecord
                {
                    name = a.name ?? "sm_sanctuary_971",
                    classTrait = GetClassTrait(a),
                    branchTrait = SuperMechBranch.GetBranchTrait(a),
                    stage = SuperMechStage.GetStage(a),
                    rankIndex = rankIdx,
                    qi = SuperMechQi.GetQi(a),
                    qiAttribute = SuperMechQiAttribute.GetAttribute(a),
                    diedAt = 0,
                    reviveCount = GetReviveCount(a)
                };

                if (rankIdx >= 10 && rankIdx < DivineRankIndex)
                {
                    _aliveSnapshot[a.id] = rec;
                }
                else if (rankIdx >= DivineRankIndex)
                {
                    _divineSnapshot[a.id] = rec;
                }
            }

            var deadIds = new List<long>();
            foreach (var kv in _aliveSnapshot)
                if (!aliveIds.Contains(kv.Key)) deadIds.Add(kv.Key);
            foreach (long id in deadIds)
            {
                if (_transcendenceFailed.Contains(id))
                {
                    _aliveSnapshot.Remove(id);
                    _transcendenceFailed.Remove(id);
                    Debug.Log($"[超神机械师] {(_aliveSnapshot.ContainsKey(id) ? _aliveSnapshot[id].name : "sm_sanctuary_971")}（突破失败）化为超神遗力，无法圣所复苏");
                    continue;
                }
                var rec = _aliveSnapshot[id];
                rec.diedAt = System.DateTime.Now.Ticks;
                _deadUnits.Insert(0, rec);
                _aliveSnapshot.Remove(id);
                if (_deadUnits.Count > MaxDeadRecords) _deadUnits.RemoveAt(_deadUnits.Count - 1);
                Debug.Log($"[超神机械师] {rec.name}（S阶）已死亡，圣所记录可复活（已复活{rec.reviveCount}次）");
            }

            var divineDead = new List<long>();
            foreach (var kv in _divineSnapshot)
                if (!aliveIds.Contains(kv.Key)) divineDead.Add(kv.Key);
            foreach (long id in divineDead)
            {
                var rec = _divineSnapshot[id];
                _divineSnapshot.Remove(id);
                if (_divineCooldown.Contains(id)) continue;
                DivineRebirth(rec);
                _divineCooldown.Add(id);
            }
        }

        private static void DivineRebirth(DeadUnitRecord rec)
        {
            try
            {
                WorldTile tile = null;
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    int rx = UnityEngine.Random.Range(5, MapBox.width - 5);
                    int ry = UnityEngine.Random.Range(5, MapBox.height - 5);
                    WorldTile t = World.world.GetTile(rx, ry);
                    if (t != null && t.Type != null && t.Type.ground) { tile = t; break; }
                }
                if (tile == null) return;

                Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
                if (a == null) return;

                if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
                if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);
                if (rec.stage > 0) SuperMechStage.SetStage(a, rec.stage);
                if (rec.rankIndex >= 0 && rec.rankIndex < SuperMechRanks.All.Count)
                    SuperMechAdvancement.SetExactRank(a, rec.rankIndex);
                SuperMechQi.SetQi(a, rec.qi);
                if (!string.IsNullOrEmpty(rec.qiAttribute) && rec.qiAttribute != SuperMechQiAttribute.AttrNone)
                    SuperMechQiAttribute.SetAttribute(a, rec.qiAttribute);
                a.addTrait("sm_divinity_ascended");

                Debug.Log($"[超神机械师] {rec.name}（超神级）信息态重生！在新位置重新生成");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 信息态重生异常: " + e.Message);
            }
        }

        public static List<DeadUnitRecord> GetDeadList() => _deadUnits;

        public static bool CanResurrect()
        {
            return Data.unlocked_sanctuaries != 0 && Data.key_fragments >= ResurrectionCost;
        }

        public static int GetSanctuaryForClass(string classTrait)
        {
            if (classTrait == SuperMechTraits.ClassMech) return 0;
            if (classTrait == SuperMechTraits.ClassMartial) return 1;
            if (classTrait == SuperMechTraits.ClassPsi) return 2;
            if (classTrait == SuperMechTraits.ClassMage) return 3;
            if (classTrait == SuperMechTraits.ClassMind) return 4;
            return -1;
        }

        public static Actor Resurrect(int deadIndex, WorldTile tile)
        {
            if (!CanResurrect())
            {
                Debug.Log("[超神机械师] 无法复活：无圣所解锁或钥匙碎片不足");
                return null;
            }
            if (deadIndex < 0 || deadIndex >= _deadUnits.Count) return null;
            var rec = _deadUnits[deadIndex];

            Data.key_fragments -= ResurrectionCost;
            Data.total_resurrections++;
            _deadUnits.RemoveAt(deadIndex);

            Actor a = World.world.units.createNewUnit("human", tile, pMiracleSpawn: false, pAdultAge: true);
            if (a == null) return null;

            if (!string.IsNullOrEmpty(rec.classTrait)) a.addTrait(rec.classTrait);
            if (!string.IsNullOrEmpty(rec.branchTrait)) a.addTrait(rec.branchTrait);

            int reviveCount = rec.reviveCount + 1;
            SetReviveCount(a, reviveCount);

            float infoLoss = Mathf.Clamp(0.1f * reviveCount, 0.1f, 0.5f);

            int finalRank = rec.rankIndex;
            int finalStage = rec.stage;
            bool rankDropped = false;

            if (Random.value < infoLoss)
            {
                if (finalRank > 0)
                {
                    finalRank--;
                    rankDropped = true;
                    finalStage = Mathf.Max(0, finalStage - Random.Range(1, 3));
                    Debug.Log($"[超神机械师] {rec.name} 复活降阶！{SuperMechRanks.GetRankName(rec.rankIndex)}→{SuperMechRanks.GetRankName(finalRank)}");
                }
            }

            if (finalStage > 0) SuperMechStage.SetStage(a, finalStage);
            if (finalRank >= 0 && finalRank < SuperMechRanks.All.Count)
                SuperMechAdvancement.SetExactRank(a, finalRank);

            SuperMechQi.SetQi(a, rec.qi * (1f - infoLoss));
            if (!string.IsNullOrEmpty(rec.qiAttribute) && rec.qiAttribute != SuperMechQiAttribute.AttrNone)
                SuperMechQiAttribute.SetAttribute(a, rec.qiAttribute);

            var s = SuperMechStats.Of(a);
            if (s != null)
            {
                s["multiplier_damage"] = ((s["multiplier_damage"] == 0f ? 1f : s["multiplier_damage"])) * (1f - infoLoss);
                s["multiplier_health"] = ((s["multiplier_health"] == 0f ? 1f : s["multiplier_health"])) * (1f - infoLoss);
                s["intelligence"] = Mathf.Max(0f, ((s["intelligence"] == 0f ? 5f : s["intelligence"])) * (1f - infoLoss * 0.5f));
            }

            bool canResurrectAgain = finalRank >= 10;
            if (!canResurrectAgain)
            {
                Debug.Log($"[超神机械师] {rec.name} 已降到{SuperMechRanks.GetRankName(finalRank)}，失去圣所复活资格！");
            }

            Save();
            Debug.Log($"[超神机械师] 圣所复活：{rec.name}（第{reviveCount}次复活，信息丢失{infoLoss:P0}{(rankDropped ? "，降阶" : "")}{(canResurrectAgain ? "" : "，失去复活资格")}，消耗{ResurrectionCost}钥匙碎片）");
            return a;
        }

        private static string GetClassTrait(Actor a)
        {
            if (a.hasTrait(SuperMechTraits.ClassMech)) return SuperMechTraits.ClassMech;
            if (a.hasTrait(SuperMechTraits.ClassMartial)) return SuperMechTraits.ClassMartial;
            if (a.hasTrait(SuperMechTraits.ClassPsi)) return SuperMechTraits.ClassPsi;
            if (a.hasTrait(SuperMechTraits.ClassMage)) return SuperMechTraits.ClassMage;
            if (a.hasTrait(SuperMechTraits.ClassMind)) return SuperMechTraits.ClassMind;
            return null;
        }
    }
}
