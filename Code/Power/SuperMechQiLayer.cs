using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力层次系统。
    /// 气力从资源池变成有层次的成长轴。
    /// Lv1~Lv10，每级被动加成，第六级为分水岭（质变）。
    /// 层次与气力属性联动，不同属性的层次加成方向不同。
    /// </summary>
    public static class SuperMechQiLayer
    {
        /// <summary>最大层次等级</summary>
        public const int MaxLayer = 10;

        /// <summary>分水岭等级（达到此等级有质变）</summary>
        public const int BreakthroughLayer = 6;

        /// <summary>层次提升所需的气力值阈值</summary>
        public static readonly float[] LayerQiThresholds =
        {
            0f,       // Lv0（未入门）
            50f,      // Lv1
            200f,     // Lv2
            500f,     // Lv3
            1200f,    // Lv4
            3000f,    // Lv5
            8000f,    // Lv6（分水岭）
            20000f,   // Lv7
            50000f,   // Lv8
            120000f,  // Lv9
            300000f   // Lv10（圆满）
        };

        /// <summary>层次被动加成（每级的基础倍率）</summary>
        public static readonly float[] LayerDamageBonus =
        {
            1.00f, 1.05f, 1.12f, 1.20f, 1.30f, 1.42f,
            1.60f, 1.80f, 2.05f, 2.35f, 2.70f  // Lv6起加成陡增
        };

        public static readonly float[] LayerHealthBonus =
        {
            1.00f, 1.08f, 1.18f, 1.30f, 1.45f, 1.62f,
            1.85f, 2.10f, 2.40f, 2.75f, 3.15f
        };

        public static readonly float[] LayerSpeedBonus =
        {
            1.00f, 1.03f, 1.06f, 1.10f, 1.14f, 1.19f,
            1.25f, 1.32f, 1.40f, 1.49f, 1.60f
        };

        /// <summary>层次气力恢复加成（每级恢复速度提升）</summary>
        public static readonly float[] LayerRegenBonus =
        {
            1.00f, 1.10f, 1.22f, 1.35f, 1.50f, 1.68f,
            1.90f, 2.15f, 2.45f, 2.80f, 3.20f
        };

        private static readonly Dictionary<long, int> _layer = new Dictionary<long, int>();

        /// <summary>获取单位的气力层次等级</summary>
        public static int GetLayer(Actor a)
        {
            if (a == null) return 0;
            if (_layer.TryGetValue(a.data.id, out int layer)) return layer;
            return CalculateLayer(a);
        }

        /// <summary>根据气力值计算层次等级</summary>
        public static int CalculateLayer(Actor a)
        {
            if (a == null) return 0;
            float qiMax = SuperMechQi.GetQiMax(a);
            if (qiMax <= 0f)
            {
                float qi = SuperMechQi.GetQi(a);
                if (qi <= 0f) return 0;
                qiMax = qi;
            }

            int layer = 0;
            for (int i = 1; i <= MaxLayer; i++)
            {
                if (qiMax >= LayerQiThresholds[i])
                    layer = i;
                else
                    break;
            }

            _layer[a.data.id] = layer;
            return layer;
        }

        /// <summary>是否达到分水岭（Lv6+）</summary>
        public static bool IsBreakthrough(Actor a)
        {
            return GetLayer(a) >= BreakthroughLayer;
        }

        /// <summary>获取层次伤害加成倍率</summary>
        public static float GetDamageBonus(Actor a)
        {
            int layer = Mathf.Clamp(GetLayer(a), 0, MaxLayer);
            return LayerDamageBonus[layer];
        }

        /// <summary>获取层次生命加成倍率</summary>
        public static float GetHealthBonus(Actor a)
        {
            int layer = Mathf.Clamp(GetLayer(a), 0, MaxLayer);
            return LayerHealthBonus[layer];
        }

        /// <summary>获取层次速度加成倍率</summary>
        public static float GetSpeedBonus(Actor a)
        {
            int layer = Mathf.Clamp(GetLayer(a), 0, MaxLayer);
            return LayerSpeedBonus[layer];
        }

        /// <summary>获取层次气力恢复加成倍率</summary>
        public static float GetRegenBonus(Actor a)
        {
            int layer = Mathf.Clamp(GetLayer(a), 0, MaxLayer);
            return LayerRegenBonus[layer];
        }

        /// <summary>获取层次名称（本地化）</summary>
        public static string GetLayerName(int layer)
        {
            if (layer <= 0) return LocalizedTextManager.getText("sm_qi_layer_0");
            if (layer >= MaxLayer) return LocalizedTextManager.getText("sm_qi_layer_10");
            return LocalizedTextManager.getText($"sm_qi_layer_{layer}");
        }

        /// <summary>获取层次描述文本</summary>
        public static string GetLayerText(Actor a)
        {
            int layer = GetLayer(a);
            string name = GetLayerName(layer);
            if (layer >= BreakthroughLayer)
            {
                return $"{name}（{LocalizedTextManager.getText("sm_qi_layer_breakthrough")}）";
            }
            return name;
        }

        /// <summary>获取下一层次所需气力值</summary>
        public static float GetNextLayerQiRequired(Actor a)
        {
            int layer = GetLayer(a);
            if (layer >= MaxLayer) return -1f; // 已满级
            return LayerQiThresholds[layer + 1];
        }

        /// <summary>获取当前层次进度（0~1）</summary>
        public static float GetLayerProgress(Actor a)
        {
            int layer = GetLayer(a);
            if (layer >= MaxLayer) return 1f;
            float current = SuperMechQi.GetQiMax(a);
            float floor = LayerQiThresholds[layer];
            float ceil = LayerQiThresholds[layer + 1];
            if (ceil <= floor) return 0f;
            return Mathf.Clamp01((current - floor) / (ceil - floor));
        }

        /// <summary>Tick时刷新层次缓存</summary>
        public static void TickRefresh()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;
                CalculateLayer(a);
            }
        }

        public static void Clear() { _layer.Clear(); }

        public static void Clear(Actor a)
        {
            if (a != null) _layer.Remove(a.data.id);
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_layer, alive);
        }
    }
}
