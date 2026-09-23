using NeoModLoader.api;
using NeoModLoader.services;
using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 气力等级系统（原著面板数据）。
    /// 气力是超能者核心能量，机械系磁环后改称械力。
    /// 等级阈值来自原著面板：Lv3=120, Lv6=1230, Lv10=9770, Lv15=52270, Lv19=128452, Lv21=182075。
    /// 设计：气力值用字典追踪，等级特质用未注册组隐藏（不在特质编辑器显示），
    /// 单位面板通过 SuperMechUnitWindow 显示"气力：128452【Lv19】"。
    /// </summary>
    public static class SuperMechQi
    {
        // 40级阈值（原著数据点 + 插值/外推）。
        // 原著最终面板：Lv29=481200。越往后越难涨，Lv40门槛223万，正常游戏几乎不可能达到，等效无封顶。
        public static readonly float[] Thresholds = {
            10f,       // Lv1  (ch2)
            50f,       // Lv2  (ch2)
            100f,      // Lv3  (ch2/ch5: 120)
            200f,      // Lv4  (ch2/ch54: 200)
            400f,      // Lv5  (ch2)
            1000f,     // Lv6  (ch2/ch146: 1230, 分水岭)
            2000f,     // Lv7  (插值)
            2500f,     // Lv8  (ch262: 3070)
            5000f,     // Lv9  (ch356: 5210)
            8000f,     // Lv10 (ch531: 9770)
            15000f,    // Lv11 (ch630: 17580)
            22000f,    // Lv12 (ch676: 25530)
            30000f,    // Lv13 (ch735: 35530)
            42000f,    // Lv14 (插值)
            50000f,    // Lv15 (ch760: 52270)
            65000f,    // Lv16 (插值)
            75000f,    // Lv17 (ch854: 82120)
            100000f,   // Lv18 (插值)
            120000f,   // Lv19 (ch945/ch948: 128452)
            150000f,   // Lv20 (插值)
            170000f,   // Lv21 (ch1028: 182075)
            194000f,   // Lv22 (插值)
            221000f,   // Lv23 (插值)
            252000f,   // Lv24 (插值)
            287000f,   // Lv25 (插值)
            327000f,   // Lv26 (插值)
            373000f,   // Lv27 (插值)
            425000f,   // Lv28 (插值)
            450000f,   // Lv29 (最终面板: 481200)
            553000f,   // Lv30 (外推)
            636000f,   // Lv31 (外推)
            731000f,   // Lv32 (外推)
            841000f,   // Lv33 (外推)
            967000f,   // Lv34 (外推)
            1112000f,  // Lv35 (外推)
            1279000f,  // Lv36 (外推)
            1471000f,  // Lv37 (外推)
            1692000f,  // Lv38 (外推)
            1946000f,  // Lv39 (外推)
            2238000f,  // Lv40 (外推, 等效封顶)
        };

        public static readonly string[] LevelNames = {
            "Lv1", "Lv2", "Lv3", "Lv4", "Lv5",
            "Lv6（分水岭）", "Lv7", "Lv8", "Lv9", "Lv10",
            "Lv11", "Lv12", "Lv13", "Lv14", "Lv15",
            "Lv16", "Lv17", "Lv18", "Lv19", "Lv20",
            "Lv21", "Lv22", "Lv23", "Lv24", "Lv25",
            "Lv26", "Lv27", "Lv28", "Lv29", "Lv30",
            "Lv31", "Lv32", "Lv33", "Lv34", "Lv35",
            "Lv36", "Lv37", "Lv38", "Lv39", "Lv40",
        };

        // 气力值追踪（unit.id -> qi值）
        private static readonly Dictionary<long, float> _qiMap = new Dictionary<long, float>();

        // 隐藏组：不注册到 ActorTraitGroupLibrary，因此不在特质编辑器显示
        private const string HiddenGroup = "sm_qi_hidden";

        public static string GetId(int level) => $"sm_qi_{level}";

        public static void Register()
        {
            for (int i = 0; i < Thresholds.Length; i++)
            {
                int lv = i + 1;
                string id = GetId(lv);

                var t = new ActorTrait
                {
                    id = id,
                    path_icon = "ui/Icons/actor_traits/iconHardSkin",
                    group_id = HiddenGroup,  // 未注册组 → 不在特质编辑器显示
                    needs_to_be_explored = false,
                    base_stats = new BaseStats()
                };
                // 属性加成：随等级递增，Lv6后解锁伤害加成（分水岭）
                t.base_stats["warfare"] = lv;
                t.base_stats["multiplier_health"] = 1f + lv * 0.15f;
                if (lv >= 6)
                {
                    t.base_stats["multiplier_damage"] = 1f + (lv - 5) * 0.15f;
                }
                AssetManager.traits.add(t);
            }
            Debug.Log($"[超神机械师] 气力等级注册完成：{Thresholds.Length} 级（隐藏特质，单位面板显示）");
        }

        /// <summary>获取单位气力值。</summary>
        public static float GetQi(Actor a)
        {
            if (a == null) return 0;
            float v;
            _qiMap.TryGetValue(a.id, out v);
            return v;
        }

        /// <summary>给单位增加气力值（提炼法/升级/战斗等）。</summary>
        public static void AddQi(Actor a, float amount)
        {
            if (a == null) return;
            float cur = GetQi(a);
            _qiMap[a.id] = cur + amount;
        }

        /// <summary>根据气力值计算等级。</summary>
        public static int GetLevel(float qiValue)
        {
            int lv = 0;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (qiValue >= Thresholds[i]) lv = i + 1;
            }
            return lv;
        }

        /// <summary>每5秒tick：自然增长气力 + 同步等级特质。</summary>
        public static void TickQiLevels()
        {
            var units = World.world.units.units_only_alive;
            if (units == null) return;
            foreach (Actor a in units)
            {
                if (a == null) continue;
                if (!SuperMechAdvancement.IsSuperMechUnit(a)) continue;

                // 自然增长（模拟持续修炼）
                AddQi(a, 0.5f);

                float qi = GetQi(a);
                int targetLv = GetLevel(qi);

                // 移除旧等级特质，添加当前等级特质
                for (int lv = 1; lv <= Thresholds.Length; lv++)
                {
                    string id = GetId(lv);
                    if (lv == targetLv)
                    {
                        if (targetLv > 0 && !a.hasTrait(id)) a.addTrait(id);
                    }
                    else if (a.hasTrait(id))
                    {
                        a.removeTrait(id);
                    }
                }
            }
        }
    }
}
