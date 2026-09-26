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
                _frame = SMWindowFrame.Create(LocalizedTextManager.getText("sm_sanctuary_title"), 580f, 720f);
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

            _frame.AddLabel(LocalizedTextManager.getText("sm_sanctuary_subtitle"), x, y, 540f, 26f, 16, TextAnchor.MiddleCenter);
            y -= 34f;

            string[] nameKeys = { "sm_sanctuary_1", "sm_sanctuary_2", "sm_sanctuary_3", "sm_sanctuary_4", "sm_sanctuary_5", "sm_sanctuary_6" };
            string[] names = System.Array.ConvertAll(nameKeys, k => LocalizedTextManager.getText(k));
            string[] descKeys = { "sm_sanctuary_desc_1", "sm_sanctuary_desc_2", "sm_sanctuary_desc_3", "sm_sanctuary_desc_4", "sm_sanctuary_desc_5", "sm_sanctuary_desc_6" };
            string[] descs = System.Array.ConvertAll(descKeys, k => LocalizedTextManager.getText(k));

            var data = SuperMechSanctuary.Data;
            for (int i = 0; i < 6; i++)
            {
                int sanctuaryIndex = i; // 闭包捕获
                bool unlocked = (data.unlocked_sanctuaries & (1 << i)) != 0;
                int fragments = data.sanctuary_fragments[i];
                Color bg = unlocked ? new Color(0.15f, 0.25f, 0.15f, 0.8f) : new Color(0.12f, 0.12f, 0.15f, 0.8f);
                string btnText = unlocked
                    ? $"✓ {names[i]}  [碎片:{fragments}]  [点击进入]"
                    : $"✗ {names[i]}  [碎片:{fragments}/3]";
                _frame.AddButton(btnText, x, y, 540f, 36f, () =>
                {
                    if (!unlocked)
                    {
                        Debug.Log($"[超神机械师] {names[sanctuaryIndex]}未解锁，需集齐3碎片");
                        return;
                    }
                    // 获取当前光标附近的单位
                    Actor selected = World.world.getActorNearCursor();
                    if (selected == null)
                    {
                        Debug.Log("[超神机械师] 请先将鼠标移到一个单位上再进入圣所");
                        return;
                    }
                    if (SuperMechSanctuary.EnterSanctuary(selected, sanctuaryIndex))
                    {
                        Debug.Log($"[超神机械师] {selected.name} 进入{names[sanctuaryIndex]}");
                        Refresh();
                    }
                }, bg);
                _frame.AddLabel(descs[i], x + 30f, y - 14f, 500f, 14f, 10);
                y -= 42f;
            }

            y -= 4f;
            int total = 0;
            foreach (int f in data.sanctuary_fragments) total += f;
            int unlockedCount = 0;
            for (int i = 0; i < 6; i++) if ((data.unlocked_sanctuaries & (1 << i)) != 0) unlockedCount++;
            _frame.AddLabel($"{LocalizedTextManager.getText(\"sm_sanctuary_fragments\")}:{total}  {LocalizedTextManager.getText(\"sm_sanctuary_unlocked\")}:{unlockedCount}/6  {LocalizedTextManager.getText(\"sm_sanctuary_keys\")}:{data.key_fragments}  {LocalizedTextManager.getText(\"sm_sanctuary_visits\")}:{data.total_visits}  {LocalizedTextManager.getText(\"sm_sanctuary_resurrect\")}:{data.total_resurrections}", x, y, 540f, 18f, 12);
            y -= 24f;

            // ===== 圣所复活（ch1134/ch1214：仅超A级可复活，超神级靠信息态重生）=====
            _frame.AddLabel(LocalizedTextManager.getText("sm_sanctuary_resurrect_title"), x, y, 540f, 20f, 13);
            y -= 26f;

            bool canRes = SuperMechSanctuary.CanResurrect();
            var deadList = SuperMechSanctuary.GetDeadList();
            if (deadList.Count == 0)
            {
                _frame.AddLabel(LocalizedTextManager.getText("sm_sanctuary_no_dead"), x, y, 540f, 18f, 11);
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
                    // 显示复活次数和降阶风险
                    int nextRevive = rec.reviveCount + 1;
                    float infoLoss = Mathf.Clamp(0.1f * nextRevive, 0.1f, 0.5f);
                    string riskText = nextRevive >= 4 ? "⚠高风险" : nextRevive >= 2 ? "有风险" : "低风险";
                    string btnText = $"{rec.name} [{rankName}]  已复活{rec.reviveCount}次  下次信息丢失{infoLoss:P0}({riskText})  [5钥匙]";
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
            _frame.AddLabel(LocalizedTextManager.getText("sm_sanctuary_dimension_title"), x, y, 540f, 20f, 13);
            y -= 26f;

            Actor selected = MoveCamera.getFocusUnit();
            var dims = SuperMechDimension.GetAvailableDimensions(selected);
            if (dims.Count == 0)
            {
                _frame.AddLabel(LocalizedTextManager.getText("sm_sanctuary_dimension_hint"), x, y, 540f, 18f, 11);
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
                int cx = MapBox.width / 2;
                int cy = MapBox.height / 2;
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
