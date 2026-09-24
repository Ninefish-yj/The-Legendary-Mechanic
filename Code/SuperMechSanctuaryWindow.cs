using System;
using System.Collections.Generic;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所状态窗口：6圣所进度 + 复活列表 + 次级维度入口。
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
                _frame = SMWindowFrame.Create("超神机械师·六圣所", 580f, 720f);
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

            _frame.AddLabel("六圣所（跨存档·跨迭代）", x, y, 540f, 26f, 16, TextAnchor.MiddleCenter);
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
                _frame.AddButton($"{(unlocked ? "✓" : "✗")} {names[i]}  [碎片:{fragments}]", x, y, 540f, 36f, () => { }, bg);
                _frame.AddLabel(descs[i], x + 30f, y - 14f, 500f, 14f, 10);
                y -= 42f;
            }

            y -= 4f;
            int total = 0;
            foreach (int f in data.sanctuary_fragments) total += f;
            int unlockedCount = 0;
            for (int i = 0; i < 6; i++) if ((data.unlocked_sanctuaries & (1 << i)) != 0) unlockedCount++;
            _frame.AddLabel($"碎片:{total}  解锁:{unlockedCount}/6  钥匙:{data.key_fragments}  进入:{data.total_visits}次(权限)  复活:{data.total_resurrections}次", x, y, 540f, 18f, 12);
            y -= 24f;

            // ===== 圣所复活（ch1134/ch1214：仅超A级可复活，超神级靠信息态重生）=====
            _frame.AddLabel("◆ 圣所复活（S~SS阶，任意圣所+5钥匙，复活后气力80%/伤害-20%/生命-20%）", x, y, 540f, 20f, 13);
            y -= 26f;

            bool canRes = SuperMechSanctuary.CanResurrect();
            var deadList = SuperMechSanctuary.GetDeadList();
            if (deadList.Count == 0)
            {
                _frame.AddLabel("暂无死者记录（S~SS阶超能者死亡后自动记录；X阶超神级信息态自动重生）", x, y, 540f, 18f, 11);
                y -= 24f;
            }
            else
            {
                int showCount = Math.Min(deadList.Count, 5);
                for (int i = 0; i < showCount; i++)
                {
                    var rec = deadList[i];
                    string rankName = rec.rankIndex >= 0 && rec.rankIndex < SuperMechRanks.All.Count
                        ? SuperMechRanks.All[rec.rankIndex].name : "?";
                    string btnText = $"{rec.name} [{rankName}]  消耗5钥匙复活";
                    Color btnBg = canRes ? new Color(0.2f, 0.15f, 0.1f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.8f);
                    int idx = i;
                    _frame.AddButton(btnText, x, y, 540f, 30f, () =>
                    {
                        // 在当前选中单位位置或地图中心复活
                        WorldTile tile = GetSpawnTile();
                        if (tile != null)
                        {
                            SuperMechSanctuary.Resurrect(idx, tile);
                            Refresh();
                        }
                    }, btnBg);
                    y -= 34f;
                }
                if (deadList.Count > 5)
                {
                    _frame.AddLabel($"...还有{deadList.Count - 5}个死者记录", x, y, 540f, 16f, 10);
                    y -= 20f;
                }
            }

            y -= 6f;
            // ===== 次级维度（ch1071）=====
            _frame.AddLabel("◆ 次级维度（A阶以上可进入，获得限时强化）", x, y, 540f, 20f, 13);
            y -= 26f;

            Actor selected = MoveCamera.getFocusUnit();
            var dims = SuperMechDimension.GetAvailableDimensions(selected);
            if (dims.Count == 0)
            {
                _frame.AddLabel("选中一个A阶以上超能者查看可进入维度", x, y, 540f, 18f, 11);
            }
            else
            {
                foreach (var dim in dims)
                {
                    bool canEnter = SuperMechDimension.CanEnter(selected, dim);
                    Color dimBg = canEnter ? new Color(0.1f, 0.15f, 0.25f, 0.9f) : new Color(0.15f, 0.15f, 0.15f, 0.8f);
                    var dimRef = dim;
                    _frame.AddButton($"{dim.name} — {dim.desc}", x, y, 540f, 32f, () =>
                    {
                        if (SuperMechDimension.Enter(selected, dimRef))
                            Refresh();
                    }, dimBg);
                    y -= 36f;
                }
            }
        }

        private static WorldTile GetSpawnTile()
        {
            try
            {
                Actor focus = MoveCamera.getFocusUnit();
                if (focus != null && focus.current_tile != null) return focus.current_tile;
            }
            catch { }
            // 回退：地图中心附近找一个可走地块
            try
            {
                int cx = World.world.width / 2;
                int cy = World.world.height / 2;
                for (int r = 0; r < 10; r++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        for (int dy = -r; dy <= r; dy++)
                        {
                            WorldTile t = World.world.GetTile(cx + dx, cy + dy);
                            if (t != null && t.Type != null && t.Type.ground) return t;
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
