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
        protected override bool Cinematic => true;

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

    /// <summary>闪避：快速侧移躲避，通用基础招式（不造成伤害）</summary>
    public class DodgeMove : SuperMechMoveBase
    {
        public override string MoveId => "dodge";
        public override string MoveName => "闪避";
        protected override float WindupTime => 0.05f;
        protected override float AttackTime => 0.15f;
        protected override float RecoveryTime => 0.1f;
        protected override float CooldownTime => 1.5f;
        protected override float ScalePulse => 0.9f;
        protected override bool TriggerHitStop => false;
        protected override bool DashToTarget => false;

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            // 闪避：向侧面快速移动，不突进目标
            if (attacker.avatar == null) yield break;

            Vector3 originalPos = attacker.avatar.transform.position;
            Vector3 dodgeDir = Random.insideUnitSphere.normalized * 1.5f;
            dodgeDir.z = 0;
            Vector3 dodgePos = originalPos + dodgeDir;

            float elapsed = 0f;
            while (elapsed < AttackTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / AttackTime;
                attacker.avatar.transform.position = Vector3.Lerp(originalPos, dodgePos, t);
                yield return null;
            }
        }
    }

    /// <summary>连击：武道系专属，快速三连击</summary>
    public class ComboStrikeMove : SuperMechMoveBase
    {
        public override string MoveId => "combo_strike";
        public override string MoveName => "连击";
        protected override float WindupTime => 0.08f;
        protected override float AttackTime => 0.3f;
        protected override float RecoveryTime => 0.2f;
        protected override float CooldownTime => 2.5f;
        protected override float ScalePulse => 1.15f;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Martial;
        }

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            if (attacker.avatar == null || target == null) yield break;

            Color fistColor = new Color(1f, 0.6f, 0.3f, 1f);
            int hits = 0;
            float elapsed = 0f;
            while (elapsed < AttackTime)
            {
                elapsed += Time.deltaTime;
                if (elapsed > hits * 0.1f && hits < 3)
                {
                    hits++;
                    SuperMechCombatVisuals.SpawnSlashArc(attacker.cur_transform_position, target.cur_transform_position, fistColor);
                    SuperMechCombatVisuals.SpawnHitEffect(target.cur_transform_position, fistColor);
                }
                yield return null;
            }
        }
    }

    /// <summary>武道必杀：武道系超A专属，蓄力后全力一击</summary>
    public class MartialUltimateMove : SuperMechMoveBase
    {
        public override string MoveId => "martial_ultimate";
        public override string MoveName => "武道必杀";
        protected override float WindupTime => 0.4f;
        protected override float AttackTime => 0.15f;
        protected override float RecoveryTime => 0.4f;
        protected override float CooldownTime => 6f;
        protected override float ScalePulse => 1.6f;
        protected override bool Cinematic => true;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Martial &&
                   SuperMechSupermA.IsSuperA(attacker);
        }

        protected override IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            // 蓄力：身体发光脉动
            if (attacker.avatar != null)
            {
                Color auraColor = new Color(1f, 0.4f, 0.2f, 0.6f);
                SuperMechCombatVisuals.SpawnSuperAEffect(attacker.cur_transform_position, auraColor);
            }
            yield return base.WindupPhase(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 全力一击：大范围冲击+强震动
            SuperMechCombatVisuals.SpawnSuperAEffect(target.cur_transform_position, new Color(1f, 0.5f, 0.2f, 1f));
            SuperMechCombatVisuals.TriggerScreenShake(0.5f, 0.6f);
        }
    }

    /// <summary>法术吟唱：魔法系专属，吟唱后释放法术</summary>
    public class SpellCastMove : SuperMechMoveBase
    {
        public override string MoveId => "spell_cast";
        public override string MoveName => "法术吟唱";
        protected override float WindupTime => 0.35f;
        protected override float AttackTime => 0.2f;
        protected override float RecoveryTime => 0.25f;
        protected override float CooldownTime => 3.5f;
        protected override float ScalePulse => 1.2f;
        protected override bool DashToTarget => false;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Mage;
        }

        protected override IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            // 吟唱：法阵展开特效
            Color magicColor = new Color(0.6f, 0.4f, 1f, 1f);
            SuperMechCombatVisuals.SpawnSuperAEffect(attacker.cur_transform_position, magicColor);
            yield return base.WindupPhase(attacker, target);
        }

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            // 法术：发射魔法弹
            if (attacker.avatar == null || target == null) yield break;

            Color magicColor = new Color(0.7f, 0.5f, 1f, 1f);
            SuperMechCombatVisuals.SpawnProjectile(attacker.cur_transform_position, target.cur_transform_position, magicColor, 0.25f);
            yield return new WaitForSeconds(AttackTime);
            OnAttackConnect(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            SuperMechCombatVisuals.SpawnHitEffect(target.cur_transform_position, new Color(0.7f, 0.5f, 1f, 1f));
        }
    }

    /// <summary>魔法爆发：魔法系超A专属，大范围魔法爆发</summary>
    public class MagicBurstMove : SuperMechMoveBase
    {
        public override string MoveId => "magic_burst";
        public override string MoveName => "魔法爆发";
        protected override float WindupTime => 0.5f;
        protected override float AttackTime => 0.3f;
        protected override float RecoveryTime => 0.35f;
        protected override float CooldownTime => 7f;
        protected override float ScalePulse => 1.5f;
        protected override bool DashToTarget => false;
        protected override bool Cinematic => true;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Mage &&
                   SuperMechSupermA.IsSuperA(attacker);
        }

        protected override IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            // 大范围法阵展开
            Color magicColor = new Color(0.8f, 0.3f, 1f, 0.7f);
            SuperMechCombatVisuals.SpawnSuperAEffect(attacker.cur_transform_position, magicColor);
            yield return base.WindupPhase(attacker, target);
        }

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            // 多方向魔法弹齐射
            if (attacker.avatar == null || target == null) yield break;

            Color magicColor = new Color(0.8f, 0.4f, 1f, 1f);
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.8f;
                offset.z = 0;
                SuperMechCombatVisuals.SpawnProjectile(attacker.cur_transform_position + offset, target.cur_transform_position, magicColor, 0.2f);
                yield return new WaitForSeconds(0.05f);
            }
            OnAttackConnect(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            SuperMechCombatVisuals.SpawnSuperAEffect(target.cur_transform_position, new Color(0.8f, 0.4f, 1f, 1f));
            SuperMechCombatVisuals.TriggerScreenShake(0.35f, 0.4f);
        }
    }

    /// <summary>空间切割：异能系专属，空间裂隙斩击</summary>
    public class SpatialCutMove : SuperMechMoveBase
    {
        public override string MoveId => "spatial_cut";
        public override string MoveName => "空间切割";
        protected override float WindupTime => 0.15f;
        protected override float AttackTime => 0.18f;
        protected override float RecoveryTime => 0.2f;
        protected override float CooldownTime => 3f;
        protected override float ScalePulse => 1.3f;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            return base.CanExecute(attacker, target) &&
                   SuperMechProfession.GetProfession(attacker) == SuperMechProfession.ProfessionType.Psi;
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            // 空间切割：紫色裂隙特效
            Color spatialColor = new Color(0.4f, 0.2f, 0.8f, 1f);
            SuperMechCombatVisuals.SpawnSlashArc(attacker.cur_transform_position, target.cur_transform_position, spatialColor);
            SuperMechCombatVisuals.SpawnHitEffect(target.cur_transform_position, spatialColor);
        }
    }

    /// <summary>精神冲击：异能/念力系专属，精神力冲击波</summary>
    public class PsychicBlastMove : SuperMechMoveBase
    {
        public override string MoveId => "psychic_blast";
        public override string MoveName => "精神冲击";
        protected override float WindupTime => 0.25f;
        protected override float AttackTime => 0.2f;
        protected override float RecoveryTime => 0.25f;
        protected override float CooldownTime => 4f;
        protected override float ScalePulse => 1.25f;
        protected override bool DashToTarget => false;

        public override bool CanExecute(Actor attacker, Actor target)
        {
            var prof = SuperMechProfession.GetProfession(attacker);
            return base.CanExecute(attacker, target) &&
                   (prof == SuperMechProfession.ProfessionType.Psi || prof == SuperMechProfession.ProfessionType.Mind);
        }

        protected override IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            // 精神冲击：环形波纹扩散
            if (attacker.avatar == null || target == null) yield break;

            Color psychicColor = new Color(0.3f, 0.8f, 0.9f, 1f);
            SuperMechCombatVisuals.SpawnProjectile(attacker.cur_transform_position, target.cur_transform_position, psychicColor, 0.2f);
            yield return new WaitForSeconds(AttackTime);
            OnAttackConnect(attacker, target);
        }

        protected override void OnAttackConnect(Actor attacker, Actor target)
        {
            SuperMechCombatVisuals.SpawnHitEffect(target.cur_transform_position, new Color(0.3f, 0.8f, 0.9f, 1f));
            SuperMechCombatVisuals.TriggerScreenShake(0.15f, 0.25f);
        }
    }
}
