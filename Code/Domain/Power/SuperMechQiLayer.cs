using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力层次系统。
    /// 原著精确阈值：Lv1=10, Lv2=50, Lv3=100, Lv4=200, Lv5=400,
    /// Lv6=1000（分水岭）, Lv7=2000, Lv8=3000, Lv9=5000, Lv10=9000,
    /// Lv11=1万, Lv12=2万, Lv13=3万, Lv14=4万, Lv15=5万, Lv16=6万,
    /// Lv17=8万, Lv18=10万, Lv19=12万, Lv20=15万, Lv21=18万,
    /// Lv25=29万, Lv29=48万
    /// 第六级是分水岭，获得"气力属性强化"，属性作用变得突出。
    /// 磁属性气力可以用在制造和使用机械上，加快速度、提高质量、强化威力。
    /// </summary>
    public static class SuperMechQiLayer
    {
        /// <summary>最大层次等级（原著到Lv29）</summary>
        public const int MaxLayer = 29;

        /// <summary>分水岭等级（达到此等级有质变：气力属性强化）</summary>
        public const int BreakthroughLayer = 6;

        /// <summary>层次提升所需的气力值阈值（原著精确数值）</summary>
        public static readonly float[] LayerQiThresholds =
        {
            0f,        // Lv0（未入门）
            10f,       // Lv1
            50f,       // Lv2
            100f,      // Lv3
            200f,      // Lv4
            400f,      // Lv5
            1000f,     // Lv6（分水岭：气力属性强化）
            2000f,     // Lv7
            3000f,     // Lv8
            5000f,     // Lv9
            9000f,     // Lv10
            10000f,    // Lv11
            20000f,    // Lv12
            30000f,    // Lv13
            40000f,    // Lv14
            50000f,    // Lv15
            60000f,    // Lv16
            80000f,    // Lv17
            100000f,   // Lv18
            120000f,   // Lv19
            150000f,   // Lv20
            180000f,   // Lv21
            210000f,   // Lv22（插值）
            250000f,   // Lv23（插值）
            270000f,   // Lv24（插值）
            290000f,   // Lv25
            340000f,   // Lv26（插值）
            390000f,   // Lv27（插值）
            440000f,   // Lv28（插值）
            480000f    // Lv29（圆满）
        };

        /// <summary>层次被动加成（每级的基础倍率，随等级非线性增长）</summary>
        public static readonly float[] LayerDamageBonus = BuildBonusArray(1.00f, 0.05f, 0.08f, 6);
        public static readonly float[] LayerHealthBonus = BuildBonusArray(1.00f, 0.08f, 0.10f, 6);
        public static readonly float[] LayerSpeedBonus = BuildBonusArray(1.00f, 0.03f, 0.04f, 6);
        public static readonly float[] LayerRegenBonus = BuildBonusArray(1.00f, 0.10f, 0.12f, 6);

        /// <summary>
        /// 原著属性加成系数（幂函数：累积加成 = a × level^2.5）
        /// Lv29总加成：力量+12480、敏捷+13640、耐力+17200、智力+22845、神秘+13590、机械亲和+14670%
        /// </summary>
        private const float AttrExponent = 2.5f;
        private const float StrengthCoeff = 2.755f;    // Lv29 = 12480
        private const float AgilityCoeff = 3.012f;     // Lv29 = 13640
        private const float EnduranceCoeff = 3.798f;   // Lv29 = 17200
        private const float IntelligenceCoeff = 5.044f; // Lv29 = 22845
        private const float MysteryCoeff = 3.001f;     // Lv29 = 13590
        private const float MechAffinityCoeff = 3.239f; // Lv29 = 14670%

        /// <summary>获取力量属性加成（累积值，随层次非线性增长）</summary>
        public static float GetStrengthBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return StrengthCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>获取敏捷属性加成</summary>
        public static float GetAgilityBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return AgilityCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>获取耐力属性加成</summary>
        public static float GetEnduranceBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return EnduranceCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>获取智力属性加成</summary>
        public static float GetIntelligenceBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return IntelligenceCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>获取神秘属性加成</summary>
        public static float GetMysteryBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return MysteryCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>获取机械亲和度加成（百分比）</summary>
        public static float GetMechAffinityBonus(Actor a)
        {
            int layer = GetEffectiveLayer(a);
            return MechAffinityCoeff * Mathf.Pow(Mathf.Max(1, layer), AttrExponent);
        }

        /// <summary>
        /// 获取有效层次（气力低于当前层次阈值时，丧失该层次加成）。
        /// 原著：气力减少到低于某等级标准，丧失该等级属性加成。
        /// </summary>
        public static int GetEffectiveLayer(Actor a)
        {
            if (a == null) return 0;
            float currentQi = SuperMechQi.GetQi(a);
            if (currentQi <= 0f) return 0;

            // 从最高层次往下找，找到第一个气力≥阈值的层次
            int maxLayer = GetLayer(a);
            for (int i = maxLayer; i >= 1; i--)
            {
                if (currentQi >= LayerQiThresholds[i])
                    return i;
            }
            return 0;
        }

        /// <summary>构建加成数组：Lv1~5线性增长，Lv6+分水岭后加速增长</summary>
        private static float[] BuildBonusArray(float baseVal, float earlyStep, float lateStep, int breakthrough)
        {
            float[] arr = new float[MaxLayer + 1];
            arr[0] = baseVal;
            for (int i = 1; i <= MaxLayer; i++)
            {
                float step = (i < breakthrough) ? earlyStep : lateStep;
                arr[i] = arr[i - 1] + step;
            }
            return arr;
        }

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

        /// <summary>是否达到分水岭（Lv6+，气力属性强化）</summary>
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

        /// <summary>
        /// 磁属性气力对机械的加成（Lv6分水岭后生效）。
        /// 磁属性气力可以用在制造和使用机械上，加快速度、提高质量、强化威力。
        /// </summary>
        public static float GetMagneticMechBonus(Actor a)
        {
            if (!IsBreakthrough(a)) return 1f;
            if (SuperMechQiAttribute.GetAttribute(a) != SuperMechQiAttribute.AttrMagnetic) return 1f;
            int layer = GetLayer(a);
            // 每高于分水岭1级，机械威力+5%，制造速度+3%
            return 1f + (layer - BreakthroughLayer) * 0.05f;
        }

        /// <summary>获取层次名称（本地化）</summary>
        public static string GetLayerName(int layer)
        {
            if (layer <= 0) return LocalizedTextManager.getText("sm_qi_layer_0");
            if (layer >= MaxLayer) return LocalizedTextManager.getText($"sm_qi_layer_{MaxLayer}");
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
                if (!SuperMechActorContextRegistry.IsSuperMech(a)) continue;
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
