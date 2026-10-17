using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.213.0 战斗编排器（Choreographer）
    /// 管理所有正在进行的招式协程，决定何时触发什么招式
    /// 参考战斗之神模组的Choreo系统，在getHit补丁中接管攻击动作
    /// </summary>
    public static class SuperMechChoreographer
    {
        private static GameObject _root;
        private static MonoBehaviourHost _host;
        private static readonly Dictionary<string, SuperMechMoveBase> _moves = new Dictionary<string, SuperMechMoveBase>();
        private static readonly HashSet<string> _activeActors = new HashSet<string>();
        private static readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        /// <summary>初始化招式库（在Main.OnLoad中调用）</summary>
        public static void Init()
        {
            EnsureRoot();
            RegisterMove(new DashSlashMove());
            RegisterMove(new HeavyStrikeMove());
            RegisterMove(new MechanicalArmStrikeMove());
            RegisterMove(new DroneBarrageMove());
            Debug.Log("[超神机械师] 战斗编排器初始化完成，注册招式数: " + _moves.Count);
        }

        private static void EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("SuperMechChoreographer");
                Object.DontDestroyOnLoad(_root);
                _host = _root.AddComponent<MonoBehaviourHost>();
            }
        }

        private static void RegisterMove(SuperMechMoveBase move)
        {
            if (move != null && !_moves.ContainsKey(move.MoveId))
            {
                _moves[move.MoveId] = move;
            }
        }

        /// <summary>在getHit补丁中调用：尝试触发自定义招式</summary>
        public static bool TryTriggerMove(Actor attacker, Actor target)
        {
            if (!SuperMechConfig.CombatChoreoEnabled) return false;
            if (attacker == null || target == null) return false;
            if (_host == null) EnsureRoot();

            string actorId = attacker.data?.id ?? "";
            if (string.IsNullOrEmpty(actorId)) return false;
            if (_activeActors.Contains(actorId)) return false;

            SuperMechMoveBase move = SelectMove(attacker, target);
            if (move == null) return false;

            string cooldownKey = actorId + "_" + move.MoveId;
            if (_cooldowns.TryGetValue(cooldownKey, out float cdEnd) && Time.time < cdEnd)
                return false;

            _activeActors.Add(actorId);
            _cooldowns[cooldownKey] = Time.time + move.CooldownTime;
            _host.StartCoroutine(RunMove(move, attacker, target, actorId));
            return true;
        }

        /// <summary>根据攻击者状态选择合适的招式</summary>
        private static SuperMechMoveBase SelectMove(Actor attacker, Actor target)
        {
            var prof = SuperMechProfession.GetProfession(attacker);
            float r = Random.value;

            // 机械师：机械臂冲击 / 无人机齐射
            if (prof == SuperMechProfession.ProfessionType.Mechanical)
            {
                if (r < 0.25f && _moves.ContainsKey("mech_arm_strike"))
                    return _moves["mech_arm_strike"];
                if (r < 0.4f && _moves.ContainsKey("drone_barrage"))
                    return _moves["drone_barrage"];
            }

            // 武道系：突进斩击
            if (prof == SuperMechProfession.ProfessionType.Martial)
            {
                if (r < 0.3f && _moves.ContainsKey("dash_slash"))
                    return _moves["dash_slash"];
            }

            // 超A级：重击
            if (SuperMechSupermA.IsSuperA(attacker))
            {
                if (r < 0.2f && _moves.ContainsKey("heavy_strike"))
                    return _moves["heavy_strike"];
            }

            // 通用：低概率突进斩击
            if (r < 0.08f && _moves.ContainsKey("dash_slash"))
                return _moves["dash_slash"];

            return null;
        }

        private static IEnumerator RunMove(SuperMechMoveBase move, Actor attacker, Actor target, string actorId)
        {
            yield return move.Execute(attacker, target);
            _activeActors.Remove(actorId);
        }

        /// <summary>MonoBehaviour宿主，用于启动协程</summary>
        private class MonoBehaviourHost : MonoBehaviour { }
    }
}
