using System.Collections;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.197 战斗视觉特效系统
    /// 符合原著的战斗表现：职业差异化攻击颜色、伤害飘字、超A屏幕震动、大范围能量特效</summary>
    public static class SuperMechCombatVisuals
    {
        private static GameObject _effectRoot;
        private static Camera _mainCamera;
        private static float _shakeIntensity;
        private static float _shakeDuration;
        private static Vector3 _originalCamPos;

        /// <summary>攻击时触发视觉特效</summary>
        public static void OnAttack(Actor attacker, Actor target, float damage, AttackType attackType)
        {
            if (attacker == null || target == null) return;
            if (!SuperMechConfig.CombatEffectsEnabled) return;

            try
            {
                Vector3 targetPos = GetActorPosition(target);
                Color attackColor = GetAttackColor(attacker);

                // 1. 伤害飘字
                ShowDamageText(targetPos, (int)damage, attackColor);

                // 2. 命中特效（职业差异化颜色）
                SpawnHitEffect(targetPos, attackColor);

                // 3. 超A级别：屏幕震动+大范围特效
                if (SuperMechSupermA.IsSuperA(attacker) || SuperMechSupermA.IsSuperA(target))
                {
                    TriggerScreenShake(0.15f, 0.3f);
                    SpawnSuperAEffect(targetPos, attackColor);
                }

                // 4. 机械师：机械召唤特效
                if (attacker.hasTrait(SuperMechTraits.ClassMechanic))
                {
                    if (Random.value < 0.15f) SpawnMechSummonEffect(GetActorPosition(attacker));
                }

                // 5. 法师：法术特效
                if (attacker.hasTrait(SuperMechTraits.ClassMage) && Random.value < 0.2f)
                {
                    SpawnSpellEffect(targetPos, GetSpellColor(attacker));
                }
            }
            catch (System.Exception e)
            {
                // 特效失败不影响战斗
            }
        }

        /// <summary>获取职业攻击颜色（原著风格）</summary>
        private static Color GetAttackColor(Actor attacker)
        {
            if (attacker.hasTrait(SuperMechTraits.ClassMechanic)) return new Color(0.4f, 0.8f, 1f, 1f);    // 机械师：青蓝（械力）
            if (attacker.hasTrait(SuperMechTraits.ClassMartial)) return new Color(1f, 0.6f, 0.3f, 1f);    // 武道：橙红（气力）
            if (attacker.hasTrait(SuperMechTraits.ClassMage)) return new Color(0.7f, 0.4f, 1f, 1f);       // 法师：紫色（魔力）
            if (attacker.hasTrait(SuperMechTraits.ClassPsi)) return new Color(0.4f, 1f, 0.7f, 1f);        // 念力：青绿（念力）
            if (attacker.hasTrait(SuperMechTraits.ClassPower)) return new Color(1f, 0.4f, 0.5f, 1f);      // 异能：粉红（精神力）
            return new Color(1f, 1f, 1f, 0.8f); // 普通：白色
        }

        /// <summary>获取法术颜色</summary>
        private static Color GetSpellColor(Actor attacker)
        {
            string mageType = SuperMechMageType.GetMageType(attacker);
            switch (mageType)
            {
                case "fire": return new Color(1f, 0.4f, 0.2f, 1f);
                case "ice": return new Color(0.4f, 0.8f, 1f, 1f);
                case "lightning": return new Color(1f, 0.9f, 0.3f, 1f);
                case "arcane": return new Color(0.7f, 0.4f, 1f, 1f);
                default: return new Color(0.6f, 0.4f, 1f, 1f);
            }
        }

        /// <summary>获取单位世界位置</summary>
        private static Vector3 GetActorPosition(Actor actor)
        {
            if (actor?.transform != null)
                return actor.transform.position;
            return Vector3.zero;
        }

        /// <summary>确保特效根对象存在</summary>
        private static void EnsureEffectRoot()
        {
            if (_effectRoot == null)
            {
                _effectRoot = new GameObject("SuperMechCombatEffects");
                Object.DontDestroyOnLoad(_effectRoot);
            }
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera != null) _originalCamPos = _mainCamera.transform.position;
            }
        }

        /// <summary>伤害飘字</summary>
        private static void ShowDamageText(Vector3 position, int damage, Color color)
        {
            EnsureEffectRoot();
            var go = new GameObject("DamageText", typeof(TextMesh));
            go.transform.position = position + new Vector3(0, 0.5f, -1f);
            var tm = go.GetComponent<TextMesh>();
            tm.text = damage.ToString();
            tm.fontSize = 24;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            if (tm.font == null) tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 飘动动画
            var mover = go.AddComponent<DamageTextMover>();
            mover.Init(go, tm);
        }

        /// <summary>命中特效（扩散圆环）</summary>
        private static void SpawnHitEffect(Vector3 position, Color color)
        {
            EnsureEffectRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HitEffect";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.2f;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(color.r, color.g, color.b, 0.6f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<EffectAnimator>();
            anim.Init(go, 0.25f, Vector3.one * 0.8f, true);
        }

        /// <summary>超A大范围能量特效</summary>
        private static void SpawnSuperAEffect(Vector3 position, Color color)
        {
            EnsureEffectRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SuperAEffect";
            go.transform.position = position + new Vector3(0, 0, 0.5f);
            go.transform.localScale = Vector3.one * 0.5f;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(color.r, color.g, color.b, 0.3f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<EffectAnimator>();
            anim.Init(go, 0.5f, Vector3.one * 3f, true);
        }

        /// <summary>机械师召唤特效</summary>
        private static void SpawnMechSummonEffect(Vector3 position)
        {
            EnsureEffectRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MechSummon";
            go.transform.position = position + new Vector3(Random.Range(-1f, 1f), 0, 0);
            go.transform.localScale = Vector3.one * 0.15f;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(0.4f, 0.8f, 1f, 0.7f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<EffectAnimator>();
            anim.Init(go, 0.4f, Vector3.one * 0.4f, false);
        }

        /// <summary>法术特效</summary>
        private static void SpawnSpellEffect(Vector3 position, Color color)
        {
            EnsureEffectRoot();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SpellEffect";
            go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0f, 0.5f), 0);
            go.transform.localScale = Vector3.one * 0.15f;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(color.r, color.g, color.b, 0.8f);
            Object.Destroy(go.GetComponent<Collider>());

            var anim = go.AddComponent<EffectAnimator>();
            anim.Init(go, 0.35f, Vector3.one * 0.5f, true);
        }

        /// <summary>屏幕震动</summary>
        public static void TriggerScreenShake(float intensity, float duration)
        {
            EnsureEffectRoot();
            if (_mainCamera == null) return;
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(_shakeDuration, duration);

            if (_shakeRunner == null)
            {
                _shakeRunner = _effectRoot.AddComponent<ShakeRunner>();
                _shakeRunner.StartShake(_mainCamera, ref _shakeIntensity, ref _shakeDuration, _originalCamPos);
            }
        }

        private static ShakeRunner _shakeRunner;

        /// <summary>伤害飘字动画组件</summary>
        private class DamageTextMover : MonoBehaviour
        {
            private GameObject _go;
            private TextMesh _tm;
            private float _life = 0;
            private const float MaxLife = 0.8f;

            public void Init(GameObject go, TextMesh tm) { _go = go; _tm = tm; }

            void Update()
            {
                _life += Time.deltaTime;
                if (_life >= MaxLife) { Destroy(_go); return; }
                float t = _life / MaxLife;
                transform.position += Vector3.up * Time.deltaTime * 1.5f;
                if (_tm != null) _tm.color = new Color(_tm.color.r, _tm.color.g, _tm.color.b, 1f - t);
            }
        }

        /// <summary>通用特效动画组件</summary>
        private class EffectAnimator : MonoBehaviour
        {
            private GameObject _go;
            private float _life = 0;
            private float _maxLife;
            private Vector3 _targetScale;
            private bool _fadeOut;
            private Renderer _renderer;

            public void Init(GameObject go, float duration, Vector3 targetScale, bool fade)
            {
                _go = go; _maxLife = duration; _targetScale = targetScale; _fadeOut = fade;
                _renderer = go.GetComponent<Renderer>();
            }

            void Update()
            {
                _life += Time.deltaTime;
                if (_life >= _maxLife) { Destroy(_go); return; }
                float t = _life / _maxLife;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.2f, _targetScale, t);
                if (_fadeOut && _renderer != null)
                {
                    var c = _renderer.material.color;
                    _renderer.material.color = new Color(c.r, c.g, c.b, c.a * (1f - t));
                }
            }
        }

        /// <summary>屏幕震动驱动</summary>
        private class ShakeRunner : MonoBehaviour
        {
            private Camera _cam;
            private float _intensity;
            private float _duration;
            private Vector3 _origin;

            public void StartShake(Camera cam, ref float intensity, ref float duration, Vector3 origin)
            {
                _cam = cam; _intensity = intensity; _duration = duration; _origin = origin;
                intensity = 0; duration = 0;
            }

            void Update()
            {
                if (_duration <= 0 || _cam == null) { Destroy(this); return; }
                _duration -= Time.deltaTime;
                float t = Mathf.Clamp01(_duration / 0.3f);
                _cam.transform.position = _origin + Random.insideUnitSphere * _intensity * t;
                if (_duration <= 0) _cam.transform.position = _origin;
            }
        }
    }
}
