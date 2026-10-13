using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>教育AI系统（v0.147.0）
    /// 超能者培养：知识传承、师徒系统、学院培训
    /// 高阶级超能者有概率指导低阶级单位
    /// 由 SuperMechUnifiedTick 调度</summary>
    public static class SuperMechEducationAI
    {
        private static readonly Dictionary<long, long> _mentor = new Dictionary<long, long>();  // 徒弟→师父
        private static readonly Dictionary<long, int> _studentCount = new Dictionary<long, int>(); // 师父→徒弟数
        private const float EducationInterval = 100f;  // 每100时间单位进行一次教育活动
        private const int MaxStudents = 3;  // 每个师父最多3个徒弟

        /// <summary>教育AI总入口</summary>
        public static void Tick()
        {
            if (World.world == null || World.world.units == null) return;
            double now = World.world.getCurWorldTime();

            // 师徒配对（每100时间单位）
            if ((int)(now / EducationInterval) != (int)((now - 1) / EducationInterval))
            {
                MatchMentorStudent();
            }

            // 知识传承（每tick少量传承）
            TransmitKnowledge();
        }

        /// <summary>师徒配对</summary>
        private static void MatchMentorStudent()
        {
            if (World.world == null || World.world.units == null) return;

            var allUnits = World.world.units.units_only_alive;
            if (allUnits == null) return;

            // 收集候选师父（A阶以上，徒弟数未满）
            var mentors = new List<Actor>();
            foreach (var a in allUnits)
            {
                if (a == null || !a.isAlive()) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank >= 8) // A阶以上可做师父
                {
                    int students = _studentCount.TryGetValue(a.id, out int sc) ? sc : 0;
                    if (students < MaxStudents) mentors.Add(a);
                }
            }

            if (mentors.Count == 0) return;

            // 为低阶单位找师父
            foreach (var student in allUnits)
            {
                if (student == null || !student.isAlive()) continue;
                if (_mentor.ContainsKey(student.id)) continue; // 已有师父

                int studentRank = SuperMechAdvancement.GetExactRankIndex(student);
                if (studentRank >= 8) continue; // 高阶不需要师父

                // 找同文明的最近师父
                Actor bestMentor = null;
                float bestDist = float.MaxValue;
                foreach (var m in mentors)
                {
                    if (m.kingdom != student.kingdom) continue; // 同文明
                    if (student.current_tile == null || m.current_tile == null) continue;
                    float dist = Mathf.Abs(student.current_tile.x - m.current_tile.x)
                               + Mathf.Abs(student.current_tile.y - m.current_tile.y);
                    if (dist < 30f && dist < bestDist)
                    {
                        bestDist = dist;
                        bestMentor = m;
                    }
                }

                if (bestMentor != null && Random.value < 0.3f)
                {
                    _mentor[student.id] = bestMentor.id;
                    _studentCount[bestMentor.id] = _studentCount.TryGetValue(bestMentor.id, out int sc) ? sc + 1 : 1;
                    if (SuperMechConfig.LogVerbose && Random.value < 0.1f)
                        Debug.Log($"[超神机械师]【教育AI】{GetActorName(student)} 拜 {GetActorName(bestMentor)} 为师");
                }
            }
        }

        /// <summary>知识传承</summary>
        private static void TransmitKnowledge()
        {
            if (World.world == null || World.world.units == null) return;

            var toRemove = new List<long>();
            foreach (var kv in _mentor)
            {
                long studentId = kv.Key;
                long mentorId = kv.Value;

                Actor student = WorldAPI.getActor(studentId);
                Actor mentor = WorldAPI.getActor(mentorId);

                if (student == null || !student.isAlive() || mentor == null || !mentor.isAlive())
                {
                    toRemove.Add(studentId);
                    continue;
                }

                // 师徒距离近时传承知识
                if (student.current_tile != null && mentor.current_tile != null)
                {
                    float dist = Mathf.Abs(student.current_tile.x - mentor.current_tile.x)
                               + Mathf.Abs(student.current_tile.y - mentor.current_tile.y);
                    if (dist < 10f && Random.value < 0.05f)
                    {
                        // 传承经验
                        float mentorXp = SuperMechAwakened.GetXp(mentor);
                        float transmit = mentorXp * 0.001f;
                        SuperMechAwakened.SetXp(student, SuperMechAwakened.GetXp(student) + transmit);

                        // 传承知识（直接给经验加成）
                        if (Random.value < 0.01f)
                        {
                            SuperMechAwakened.SetXp(student, SuperMechAwakened.GetXp(student) + 2f);
                        }
                    }
                }

                // 徒弟阶位超过师父时出师
                int studentRank = SuperMechAdvancement.GetExactRankIndex(student);
                int mentorRank = SuperMechAdvancement.GetExactRankIndex(mentor);
                if (studentRank >= mentorRank)
                {
                    toRemove.Add(studentId);
                    if (SuperMechConfig.LogVerbose && Random.value < 0.1f)
                        Debug.Log($"[超神机械师]【教育AI】{GetActorName(student)} 出师（阶位{studentRank}≥师父{mentorRank}）");
                }
            }

            // 清理出师/死亡的师徒关系
            foreach (var id in toRemove)
            {
                if (_mentor.TryGetValue(id, out long mid))
                {
                    if (_studentCount.ContainsKey(mid))
                        _studentCount[mid] = Mathf.Max(0, _studentCount[mid] - 1);
                }
                _mentor.Remove(id);
            }
        }

        /// <summary>获取单位名</summary>
        private static string GetActorName(Actor a)
        {
            return a?.data?.name ?? $"单位{a?.id ?? 0}";
        }

        /// <summary>获取单位师父（供其他系统查询）</summary>
        public static long GetMentor(long actorId)
        {
            return _mentor.TryGetValue(actorId, out long m) ? m : 0;
        }

        /// <summary>获取单位徒弟数（供其他系统查询）</summary>
        public static int GetStudentCount(long actorId)
        {
            return _studentCount.TryGetValue(actorId, out int c) ? c : 0;
        }

        /// <summary>判断是否有师父</summary>
        public static bool HasMentor(long actorId)
        {
            return _mentor.ContainsKey(actorId);
        }

        /// <summary>清理数据</summary>
        public static void Clear()
        {
            _mentor.Clear();
            _studentCount.Clear();
        }
    }
}
