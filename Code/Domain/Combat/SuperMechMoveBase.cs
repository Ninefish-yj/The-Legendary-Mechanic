using System.Collections;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.213.0 战斗动作系统：招式基类
    /// 参考战斗之神模组的Choreo编排系统，实现前摇/攻击/后摇/冷却的动作生命周期
    /// 每个招式是一个协程驱动的动作序列，通过修改avatar的transform实现动漫式战斗动作
    /// </summary>
    public abstract class SuperMechMoveBase
    {
        public abstract string MoveId { get; }
        public abstract string MoveName { get; }

        // 动作时间参数（秒）
        protected virtual float WindupTime => 0.15f;    // 前摇（蓄力）
        protected virtual float AttackTime => 0.1f;     // 攻击判定帧
        protected virtual float RecoveryTime => 0.2f;   // 后摇（收招）
        protected virtual float CooldownTime => 1.5f;   // 冷却

        // 动作参数
        protected virtual float DashDistance => 1.5f;   // 突进距离
        protected virtual float ScalePulse => 1.3f;     // 攻击时缩放脉冲
        protected virtual bool TriggerHitStop => true;  // 是否触发命中停顿

        /// <summary>检查招式是否可用（冷却、状态等）</summary>
        public virtual bool CanExecute(Actor attacker, Actor target)
        {
            if (attacker == null || target == null) return false;
            if (!attacker.isAlive() || !target.isAlive()) return false;
            return true;
        }

        /// <summary>执行招式（协程入口）</summary>
        public IEnumerator Execute(Actor attacker, Actor target)
        {
            if (!CanExecute(attacker, target)) yield break;

            // 记录原始状态
            Vector3 originalPos = attacker.cur_transform_position;
            Vector3 originalScale = attacker.avatar != null ? attacker.avatar.transform.localScale : Vector3.one;

            try
            {
                // 阶段1：前摇（蓄力）
                yield return WindupPhase(attacker, target);

                // 阶段2：攻击（突进+判定）
                yield return AttackPhase(attacker, target);

                // 阶段3：后摇（收招）
                yield return RecoveryPhase(attacker, target, originalPos, originalScale);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[超神机械师] 招式{MoveId}执行异常: {e.Message}");
                // 异常时恢复原始状态
                RestoreTransform(attacker, originalPos, originalScale);
            }
        }

        /// <summary>前摇阶段：蓄力、缩放、特效</summary>
        protected virtual IEnumerator WindupPhase(Actor attacker, Actor target)
        {
            float elapsed = 0f;
            while (elapsed < WindupTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / WindupTime;
                // 蓄力缩放（略微缩小再放大）
                if (attacker.avatar != null)
                {
                    float scale = 1f - 0.15f * Mathf.Sin(t * Mathf.PI);
                    attacker.avatar.transform.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }

        /// <summary>攻击阶段：突进+判定+特效</summary>
        protected virtual IEnumerator AttackPhase(Actor attacker, Actor target)
        {
            if (attacker.avatar == null || target == null) yield break;

            Vector3 startPos = attacker.cur_transform_position;
            Vector3 targetPos = target.cur_transform_position;
            Vector3 dashDir = (targetPos - startPos).normalized;
            Vector3 endPos = targetPos - dashDir * 0.3f; // 停在目标前面

            // 突进动画
            float elapsed = 0f;
            while (elapsed < AttackTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / AttackTime;
                // 缓动突进
                float ease = 1f - Mathf.Pow(1f - t, 3);
                Vector3 newPos = Vector3.Lerp(startPos, endPos, ease);
                SetActorPosition(attacker, newPos);

                // 攻击缩放脉冲
                float scale = 1f + (ScalePulse - 1f) * Mathf.Sin(t * Mathf.PI);
                attacker.avatar.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            // 攻击判定帧：触发命中效果
            OnAttackConnect(attacker, target);

            // 触发命中停顿
            if (TriggerHitStop && SuperMechConfig.CombatFeelEnabled)
            {
                SuperMechHitStop.OnHit(attacker, target, 50f);
            }
        }

        /// <summary>后摇阶段：回到原位+恢复缩放</summary>
        protected virtual IEnumerator RecoveryPhase(Actor attacker, Actor target, Vector3 originalPos, Vector3 originalScale)
        {
            Vector3 currentPos = attacker.cur_transform_position;
            float elapsed = 0f;

            while (elapsed < RecoveryTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / RecoveryTime;
                // 缓动回位
                float ease = t * t * (3f - 2f * t);
                SetActorPosition(attacker, Vector3.Lerp(currentPos, originalPos, ease));

                if (attacker.avatar != null)
                {
                    attacker.avatar.transform.localScale = Vector3.Lerp(attacker.avatar.transform.localScale, originalScale, ease);
                }
                yield return null;
            }

            RestoreTransform(attacker, originalPos, originalScale);
        }

        /// <summary>攻击命中时调用（子类可重写添加特效）</summary>
        protected virtual void OnAttackConnect(Actor attacker, Actor target)
        {
            // 基类不做额外处理，子类添加特效
        }

        /// <summary>安全设置单位位置</summary>
        protected void SetActorPosition(Actor actor, Vector3 pos)
        {
            if (actor == null) return;
            try
            {
                actor.cur_transform_position = pos;
                actor.current_position = new Vector2(pos.x, pos.y);
            }
            catch (System.Exception) { }
        }

        /// <summary>恢复原始transform状态</summary>
        protected void RestoreTransform(Actor actor, Vector3 pos, Vector3 scale)
        {
            if (actor == null) return;
            try
            {
                SetActorPosition(actor, pos);
                if (actor.avatar != null)
                {
                    actor.avatar.transform.localScale = scale;
                }
            }
            catch (System.Exception) { }
        }
    }
}
