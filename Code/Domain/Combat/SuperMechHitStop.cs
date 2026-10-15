using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.201 打击感增强系统（参考战斗之神模组）
    /// 命中停顿(HitStop)、冲击闪光(ImpactFlash)、冲击波(Shockwave)、拖影残像(Afterimage)
    /// 让战斗从"数值对砍"变成有打击感的动漫式战斗</summary>
    public static class SuperMechHitStop
    {
        private static GameObject _root;
        private static float _hitStopLeft;
        private static float _hitStopCooldown;
        private const float HitStopDuration = 0.06f;  // 停顿时长（真实时间）
        private const float HitStopCooldown = 1.2f;  // 冷却（避免连续停顿卡顿）
        private const float HitStopTimeScale = 0.08f; // 停顿时的时间缩放

        /// <summary>是否正在命中停顿中</summary>
        public static bool IsActive => _hitStopLeft > 0f;

        /// <summary>攻击命中时触发打击感</summary>
        public static void OnHit(Actor attacker, Actor target, float damage)
        {
            if (!SuperMechConfig.CombatFeelEnabled) return;
            if (attacker == null || target == null) return;
            if (damage < 5f) return; // 小伤害不触发

            Vector3 pos = target.transform != null ? target.cur_transform_position : Vector3.zero;

            // 1. 命中停顿（大伤害或超A战斗时触发）
            bool bigHit = damage > 30f || SuperMechSupermA.IsSuperA(attacker) || SuperMechSupermA.IsSuperA(target);
            if (bigHit) RequestHitStop(pos);

            // 2. 冲击闪光
            SpawnImpactFlash(pos, bigHit);

            // 3. 冲击波
            if (bigHit) SpawnShockwave(pos, bigHit ? 2.5f : 1.5f);

            // 4. 超A级别：额外震动+大范围冲击
            if (SuperMechSupermA.IsSuperA(attacker) || SuperMechSupermA.IsSuperA(target))
            {
                SuperMechCombatVisuals.TriggerScreenShake(0.2f, 0.4f);
                SpawnShockwave(pos, 4f);
            }
        }

        /// <summary>请求命中停顿</summary>
        private static void RequestHitStop(Vector3 pos)
        {
            EnsureRoot();
            float now = Time.unscaledTime;
            if (now < _hitStopCooldown) return;

            // 只在相机附近触发（避免远处战斗卡顿）
            if (Camera.main != null)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, pos);
                if (dist > 30f) return;
            }

            _hitStopCooldown = now + HitStopCooldown;
            _hitStopLeft = HitStopDuration;
            Time.timeScale = HitStopTimeScale;
        }

        /// <summary>每帧更新（由MonoBehaviour驱动）</summary>
        public static void Tick()
        {
            if (_hitStopLeft > 0f)
            {
                _hitStopLeft -= Time.unscaledDeltaTime;
                if (_hitStopLeft <= 0f)
                {
                    Time.timeScale = 1f; // 恢复正常速度
                }
            }
        }

        /// <summary>冲击闪光（白色/职业色闪光）</summary>
        private static void SpawnImpactFlash(Vector3 pos, bool big)
        {
            EnsureRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ImpactFlash";
            go.transform.position = pos + new Vector3(0, 0, -0.5f);
            go.transform.localScale = Vector3.one * (big ? 1.2f : 0.6f);
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(1f, 1f, 1f, 0.9f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<FlashAnimator>();
            anim.Init(go, 0.12f);
        }

        /// <summary>冲击波（扩散圆环）</summary>
        private static void SpawnShockwave(Vector3 pos, float size)
        {
            EnsureRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Shockwave";
            go.transform.position = pos + new Vector3(0, 0, 0.2f);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = Vector3.one * 0.3f;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(1f, 0.95f, 0.8f, 0.5f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<ShockwaveAnimator>();
            anim.Init(go, 0.35f, size);
        }

        /// <summary>拖影残像（快速移动时生成）</summary>
        public static void SpawnAfterimage(Actor actor)
        {
            if (!SuperMechConfig.CombatFeelEnabled || actor?.avatar == null) return;
            EnsureRoot();

            var go = new GameObject("Afterimage", typeof(SpriteRenderer));
            go.transform.position = actor.cur_transform_position;
            go.transform.localScale = actor.avatar.transform.localScale;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.6f, 0.8f, 1f, 0.4f);
            // 尝试复制单位的Sprite
            var originalSr = actor.avatar.GetComponent<SpriteRenderer>();
            if (originalSr != null && originalSr.sprite != null)
            {
                sr.sprite = originalSr.sprite;
                sr.flipX = originalSr.flipX;
            }

            var anim = go.AddComponent<AfterimageAnimator>();
            anim.Init(go, 0.25f);
        }

        private static void EnsureRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("SuperMechHitStop");
                Object.DontDestroyOnLoad(_root);
                _root.AddComponent<HitStopRunner>();
            }
        }

        /// <summary>命中停顿驱动</summary>
        private class HitStopRunner : MonoBehaviour
        {
            void Update() { Tick(); }
        }

        /// <summary>闪光动画</summary>
        private class FlashAnimator : MonoBehaviour
        {
            private GameObject _go;
            private float _life;
            private float _maxLife;

            public void Init(GameObject go, float duration)
            {
                _go = go; _maxLife = duration;
            }

            void Update()
            {
                _life += Time.unscaledDeltaTime;
                if (_life >= _maxLife) { Destroy(_go); return; }
                float t = _life / _maxLife;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one * 1.5f, t);
                var r = GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(1f, 1f, 1f, 0.9f * (1f - t));
            }
        }

        /// <summary>冲击波动画</summary>
        private class ShockwaveAnimator : MonoBehaviour
        {
            private GameObject _go;
            private float _life;
            private float _maxLife;
            private float _targetSize;

            public void Init(GameObject go, float duration, float targetSize)
            {
                _go = go; _maxLife = duration; _targetSize = targetSize;
            }

            void Update()
            {
                _life += Time.unscaledDeltaTime;
                if (_life >= _maxLife) { Destroy(_go); return; }
                float t = _life / _maxLife;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one * _targetSize, t);
                var r = GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(1f, 0.95f, 0.8f, 0.5f * (1f - t));
            }
        }

        /// <summary>残像动画</summary>
        private class AfterimageAnimator : MonoBehaviour
        {
            private GameObject _go;
            private float _life;
            private float _maxLife;

            public void Init(GameObject go, float duration)
            {
                _go = go; _maxLife = duration;
            }

            void Update()
            {
                _life += Time.unscaledDeltaTime;
                if (_life >= _maxLife) { Destroy(_go); return; }
                float t = _life / _maxLife;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.4f * (1f - t));
            }
        }
    }
}
