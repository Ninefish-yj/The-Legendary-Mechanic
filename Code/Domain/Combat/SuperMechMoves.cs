using System.Collections;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>突进斩击：快速突进到目标面前斩击，通用基础招式</summary>
    public class DashSlashMove : SuperMechMoveBase
    {
        public override string MoveId => "dash_slash";
        public override string MoveName => "突进斩击";
        protected override float WindupTime => 0.1f;
        protected override float AttackTime => 0.12f;
        protected override float RecoveryTime => 0.15f;
        protected override float CooldownTime => 2f;
        protected override float ScalePulse => 1.25f;

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 斩击弧线特效
            Color color = new Color(0.8f, 0.9f, 1f, 1f);
            SuperMechCombatVisuals.SpawnSlashArc(attacker.cur_transform_position, target.cur_transform_position, color);
        }
    }

    /// <summary>重击：高伤害大范围冲击，超A级专属</summary>
    public class HeavyStrikeMove : SuperMechMoveBase
    {
        public override string MoveId => "heavy_strike";
        public override string MoveName => "重击";
        protected override float WindupTime => 0.25f;
        protected override float AttackTime => 0.15f;
        protected override float RecoveryTime => 0.3f;
        protected override float CooldownTime => 4f;
        protected override float ScalePulse => 1.5f;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) && SuperMechSupermA.IsSuperA(attacker);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 超A级大范围冲击
            Vector3 pos = target.cur_transform_position;
            SuperMechCombatVisuals.SpawnSuperAEffect(pos, new Color(1f, 0.8f, 0.3f, 1f));
            SuperMechCombatVisuals.TriggerScreenShake(0.3f, 0.5f);
        }
    }

    /// <summary>机械臂冲击：机械师专属，召唤机械臂重击</summary>
    public class MechanicalArmStrikeMove : SuperMechMoveBase
    {
        public override string MoveId => "mech_arm_strike";
        public override string MoveName => "机械臂冲击";
        protected override float WindupTime => 0.2f;
        protected override float AttackTime => 0.15f;
        protected override float RecoveryTime => 0.25f;
        protected override float CooldownTime => 3f;
        protected override float ScalePulse => 1.4f;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Mechanical;
        }

        protected override IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            // 机械臂展开特效
            SuperMechCombatVisuals.SpawnMechSummonEffect(attacker.cur_transform_position);
            yield return base.WindupPhase(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 机械臂重击：金属冲击特效
            Vector3 pos = target.cur_transform_position;
            SuperMechCombatVisuals.SpawnHitEffect(pos, new Color(0.5f, 0.7f, 1f, 1f));
            SuperMechCombatVisuals.TriggerScreenShake(0.2f, 0.3f);
        }
    }

    /// <summary>无人机齐射：机械师专属，召唤无人机弹幕</summary>
    public class DroneBarrageMove : SuperMechMoveBase
    {
        public override string MoveId => "drone_barrage";
        public override string MoveName => "无人机齐射";
        protected override float WindupTime => 0.3f;
        protected override float AttackTime => 0.4f;
        protected override float RecoveryTime => 0.2f;
        protected override float CooldownTime => 5f;
        protected override float ScalePulse => 1.1f;
        protected override bool TriggerHitStop => false;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Mechanical;
        }

        protected override IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            // 无人机部署特效
            SuperMechCombatVisuals.SpawnMechSummonEffect(attacker.cur_transform_position);
            yield return base.WindupPhase(attacker, target);
        }

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            // 无人机齐射：不突进，原地发射多弹幕
            if (attacker.avatar == null || target == null) yield break;

            Color bulletColor = new Color(0.4f, 0.8f, 1f, 1f);
            float elapsed = 0f;
            int shotsFired = 0;
            while (elapsed < AttackTime)
            {
                elapsed += Time.deltaTime;
                if (elapsed > shotsFired * 0.08f && shotsFired < 5)
                {
                    shotsFired++;
                    Vector3 spawnPos = attacker.cur_transform_position + Random.insideUnitSphere * 0.5f;
                    SuperMechCombatVisuals.SpawnProjectile(spawnPos, target.cur_transform_position, bulletColor, 0.15f);
                }
                yield return null;
            }

            OnAttackConnect(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 弹幕命中爆炸
            SuperMechCombatVisuals.SpawnHitEffect(target.cur_transform_position, new Color(0.6f, 0.8f, 1f, 1f));
        }
    }
}
