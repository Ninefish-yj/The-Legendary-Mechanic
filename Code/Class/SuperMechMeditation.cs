using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 冥想属性修炼（贴原著冥想设定）
    /// 念力/法师/心念师通过冥想提升智力与法力
    /// 存储方式：HashSet（非特质），修炼状态不显示在特质面板
    /// </summary>
    public static class SuperMechMeditation
    {
        public const string PsiResonance = "sm_pcult_resonance";
        public const string ManaMeditation = "sm_pcult_meditation";
        public const string MindTrain = "sm_pcult_mind_train";

        // 单位修炼状态存储（actor id -> 修炼类型集合）
        private static readonly HashSet<long> _psiResonance = new HashSet<long>();
        private static readonly HashSet<long> _manaMeditation = new HashSet<long>();
        private static readonly HashSet<long> _mindTrain = new HashSet<long>();

        public static void Register()
        {
            // 不再注册为ActorTrait，修炼状态是纯数据
            // 本地化文本仍需注册（用于UI显示）
            LocalizedTextManager.add("trait_" + PsiResonance, LocalizedTextManager.getText("sm_med_psi_resonance"), pReplace: true);
            LocalizedTextManager.add("trait_" + PsiResonance + "_info", LocalizedTextManager.getText("sm_med_psi_resonance_info"), pReplace: true);
            LocalizedTextManager.add("trait_" + ManaMeditation, LocalizedTextManager.getText("sm_med_mana_meditation"), pReplace: true);
            LocalizedTextManager.add("trait_" + ManaMeditation + "_info", LocalizedTextManager.getText("sm_med_mana_meditation_info"), pReplace: true);
            LocalizedTextManager.add("trait_" + MindTrain, LocalizedTextManager.getText("sm_med_mind_train"), pReplace: true);
            LocalizedTextManager.add("trait_" + MindTrain + "_info", LocalizedTextManager.getText("sm_med_mind_train_info"), pReplace: true);
        }

        public static bool HasCultivation(Actor a, string type)
        {
            if (a == null) return false;
            switch (type)
            {
                case PsiResonance: return _psiResonance.Contains(a.id);
                case ManaMeditation: return _manaMeditation.Contains(a.id);
                case MindTrain: return _mindTrain.Contains(a.id);
            }
            return false;
        }

        public static void AddCultivation(Actor a, string type)
        {
            if (a == null) return;
            switch (type)
            {
                case PsiResonance: _psiResonance.Add(a.id); break;
                case ManaMeditation: _manaMeditation.Add(a.id); break;
                case MindTrain: _mindTrain.Add(a.id); break;
            }
        }

        public static void RemoveCultivation(Actor a, string type)
        {
            if (a == null) return;
            switch (type)
            {
                case PsiResonance: _psiResonance.Remove(a.id); break;
                case ManaMeditation: _manaMeditation.Remove(a.id); break;
                case MindTrain: _mindTrain.Remove(a.id); break;
            }
        }

        /// <summary>冥想修炼tick：念力/法师/心念师持续提升智力与法力（属性修炼，不直接涨气力）</summary>
        public static void TickCultivation()
        {
            foreach (Actor a in World.world.units.units_only_alive)
            {
                if (a == null) continue;
                var s = SuperMechStats.Of(a);
                if (s == null) continue;

                if (_psiResonance.Contains(a.id) && a.hasTrait(SuperMechTraits.ClassPsi))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
                if (_manaMeditation.Contains(a.id) && a.hasTrait(SuperMechTraits.ClassMage))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.6f;
                    float mana = s["mana"];
                    if (mana < 1000) s["mana"] = mana + 3f;
                }
                if (_mindTrain.Contains(a.id) && a.hasTrait(SuperMechTraits.ClassMind))
                {
                    float cur = s["intelligence"];
                    if (cur < 200) s["intelligence"] = cur + 0.5f;
                }
            }
        }

        public static void Clear() { _psiResonance.Clear(); _manaMeditation.Clear(); _mindTrain.Clear(); }
        public static void Clear(Actor a)
        {
            if (a != null) { _psiResonance.Remove(a.id); _manaMeditation.Remove(a.id); _mindTrain.Remove(a.id); }
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var dead = new List<long>();
            foreach (var id in _psiResonance) if (!alive.Contains(id)) dead.Add(id);
            foreach (var id in dead) { _psiResonance.Remove(id); removed++; }
            dead.Clear();
            foreach (var id in _manaMeditation) if (!alive.Contains(id)) dead.Add(id);
            foreach (var id in dead) { _manaMeditation.Remove(id); removed++; }
            dead.Clear();
            foreach (var id in _mindTrain) if (!alive.Contains(id)) dead.Add(id);
            foreach (var id in dead) { _mindTrain.Remove(id); removed++; }
            return removed;
        }
    }
}
