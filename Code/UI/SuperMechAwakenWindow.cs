using UnityEngine;
using NeoModLoader.services;

namespace SuperMech.Code
{
    public static class SuperMechAwakenWindow
    {
        private static SMWindowFrame _window;
        private static Actor _target;

        private static readonly (string traitId, string nameKey, string descKey, string aspectKey)[] Classes =
        {
            (SuperMechTraits.ClassMech,    "sm_class_mech",    "sm_awaken_desc_mech",    "sm_aspect_mech"),
            (SuperMechTraits.ClassMartial, "sm_class_martial", "sm_awaken_desc_martial", "sm_aspect_martial"),
            (SuperMechTraits.ClassPsi,     "sm_class_psi",     "sm_awaken_desc_psi",     "sm_aspect_psi"),
            (SuperMechTraits.ClassMage,    "sm_class_mage",    "sm_awaken_desc_mage",    "sm_aspect_mage"),
            (SuperMechTraits.ClassMind,    "sm_class_mind",    "sm_awaken_desc_mind",    "sm_aspect_mind"),
        };

        public static void Show(Actor target)
        {
            if (target == null) return;
            _target = target;

            if (_window == null)
            {
                _window = SMWindowFrame.Create(LocalizedTextManager.getText("sm_awaken_title"), 280, 340);
                BuildContent();
            }
            _window.Show();
        }

        private static void BuildContent()
        {
            if (_window == null) return;

            _window.AddLabel(LocalizedTextManager.getText("sm_awaken_desc"),
                10f, -55f, 260f, 30f, 12, TextAnchor.MiddleCenter);

            float y = -95f;
            foreach (var (traitId, nameKey, descKey, aspectKey) in Classes)
            {
                string name = LocalizedTextManager.getText(nameKey);
                string aspect = LocalizedTextManager.getText(aspectKey);
                string btnText = $"{name}（{aspect}）";
                var btn = _window.AddButton(btnText, 20f, y, 240f, 32f, () => Awaken(traitId, name));
                y -= 40f;
            }

            _window.AddButton(LocalizedTextManager.getText("sm_ui_cancel"), 20f, y, 240f, 32f, () => _window.Hide(),
                new Color(0.3f, 0.15f, 0.15f));
        }

        private static void Awaken(string traitId, string className)
        {
            if (_target == null) return;

            _target.removeTrait(SuperMechTraits.ClassMech);
            _target.removeTrait(SuperMechTraits.ClassMartial);
            _target.removeTrait(SuperMechTraits.ClassPsi);
            _target.removeTrait(SuperMechTraits.ClassMage);
            _target.removeTrait(SuperMechTraits.ClassMind);

            _target.addTrait(traitId);
            SuperMechStage.SetStage(_target, 1);
            SuperMechSpecialty.AssignRandomSpecialty(_target);

            if (!_target.hasTrait("sm_rank_00_f"))
                _target.addTrait("sm_rank_00_f");
            SuperMechAdvancement.SetExactRank(_target, 0);

            Debug.Log($"[超神机械师] {_target.name} 觉醒为{className}，获得F阶");
            _window?.Hide();
        }

        public static void ClearStaticState()
        {
            _window = null;
            _target = null;
        }
    }
}
