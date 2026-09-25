using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 五系觉醒选择窗口：点击单位后弹出，选择觉醒系别。
    /// 参考蛊真人的简洁弹窗设计。
    /// </summary>
    public static class SuperMechAwakenWindow
    {
        private static SMWindowFrame _window;
        private static Actor _target;

        private static readonly (string traitId, string name, string desc, string aspect)[] Classes =
        {
            (SuperMechTraits.ClassMech,    "机械系", "械感天赋·机械知识树·一人成军",     "神器"),
            (SuperMechTraits.ClassMartial, "武道系", "体魄天赋·御气技巧树·近战格斗大师", "神体"),
            (SuperMechTraits.ClassPsi,     "异能系", "异能潜力·基因树·天赋神通",         "神通"),
            (SuperMechTraits.ClassMage,    "魔法系", "魔法天赋·魔法知识树·规则掌控",     "神权"),
            (SuperMechTraits.ClassMind,    "念力系", "精神天赋·精神修炼树·灵魂攻击",     "神魂"),
        };

        public static void Show(Actor target)
        {
            if (target == null) return;
            _target = target;

            if (_window == null)
            {
                _window = SMWindowFrame.Create("五系觉醒", 280, 340);
                BuildContent();
            }
            _window.Show();
        }

        private static void BuildContent()
        {
            if (_window == null) return;

            // 说明文字
            _window.AddLabel("选择觉醒系别（原著：超能者自然觉醒，天赋决定系别）",
                10f, -55f, 260f, 30f, 12, TextAnchor.MiddleCenter);

            // 五个系别按钮
            float y = -95f;
            foreach (var (traitId, name, descText, aspect) in Classes)
            {
                string btnText = $"{name}（{aspect}）";
                var btn = _window.AddButton(btnText, 20f, y, 240f, 32f, () => Awaken(traitId, name));
                // tooltip
                var tip = btn.gameObject.AddComponent<WorldTip>();
                if (tip.text != null) tip.text.text = $"{name}\n{descText}";
                y -= 40f;
            }

            // 取消按钮
            _window.AddButton("取消", 20f, y, 240f, 32f, () => _window.Hide(),
                new Color(0.3f, 0.15f, 0.15f));
        }

        private static void Awaken(string traitId, string className)
        {
            if (_target == null) return;

            // 移除已有觉醒（替换系别）
            _target.removeTrait(SuperMechTraits.ClassMech);
            _target.removeTrait(SuperMechTraits.ClassMartial);
            _target.removeTrait(SuperMechTraits.ClassPsi);
            _target.removeTrait(SuperMechTraits.ClassMage);
            _target.removeTrait(SuperMechTraits.ClassMind);

            _target.addTrait(traitId);
            SuperMechStage.SetStage(_target, 1); // 入门者
            SuperMechSpecialty.AssignRandomSpecialty(_target);

            Debug.Log($"[超神机械师] {_target.name} 觉醒为{className}");
            _window?.Hide();
        }
    }
}
