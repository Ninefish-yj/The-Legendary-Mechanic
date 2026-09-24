using System;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所状态窗口：显示6圣所解锁进度、碎片数量、跨存档信息。
    /// </summary>
    public static class SuperMechSanctuaryWindow
    {
        private static SMWindowFrame _frame;

        public static void Show()
        {
            if (_frame == null) Init();
            if (_frame == null) return;
            _frame.Show();
            Refresh();
        }

        private static void Init()
        {
            try
            {
                _frame = SMWindowFrame.Create("超神机械师·六圣所", 560f, 480f);
                if (_frame == null) return;
                Debug.Log("[超神机械师] 圣所状态窗口创建成功");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 圣所窗口初始化失败: " + e.Message);
            }
        }

        private static void Refresh()
        {
            if (_frame == null) return;
            _frame.ClearContent();
            float y = -8f;
            const float x = 12f;

            _frame.AddLabel("六圣所（跨存档·跨迭代）", x, y, 520f, 26f, 16, TextAnchor.MiddleCenter);
            y -= 34f;

            string[] names = { "第一圣所·机械", "第二圣所·武道", "第三圣所·异能", "第四圣所·魔法", "第五圣所·念力", "第六圣所·信息态" };
            string[] descs = {
                "机械系技能碎片，神性蜕变后解锁",
                "武道系技能碎片，神性蜕变后解锁",
                "异能系技能碎片，神性蜕变后解锁",
                "魔法系技能碎片，神性蜕变后解锁",
                "念力系技能碎片，神性蜕变后解锁",
                "信息态技术资料，克制世界树，需前五齐聚"
            };

            var data = SuperMechSanctuary.Data;
            for (int i = 0; i < 6; i++)
            {
                bool unlocked = (data.unlocked_sanctuaries & (1 << i)) != 0;
                int fragments = data.sanctuary_fragments[i];

                Color bg = unlocked ? new Color(0.15f, 0.25f, 0.15f, 0.8f) : new Color(0.12f, 0.12f, 0.15f, 0.8f);
                _frame.AddButton($"{(unlocked ? "✓" : "✗")} {names[i]}  [碎片:{fragments}]", x, y, 520f, 44f, () => { }, bg);
                _frame.AddLabel(descs[i], x + 30f, y - 18f, 480f, 18f, 11);
                y -= 52f;
            }

            y -= 8f;
            _frame.AddLabel("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", x, y, 520f, 14f, 10);
            y -= 22f;

            int total = 0;
            foreach (int f in data.sanctuary_fragments) total += f;
            int unlockedCount = 0;
            for (int i = 0; i < 6; i++) if ((data.unlocked_sanctuaries & (1 << i)) != 0) unlockedCount++;
            _frame.AddLabel($"碎片总数: {total}    已解锁: {unlockedCount}/6    钥匙碎片: {data.key_fragments}", x, y, 520f, 20f, 13);
            y -= 24f;
            _frame.AddLabel("神性蜕变门槛: 78000欧纳 + 气力Lv21（原著ch1039）", x, y, 520f, 20f, 12);
        }
    }
}
