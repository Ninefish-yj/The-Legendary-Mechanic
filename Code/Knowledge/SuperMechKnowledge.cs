using System.Collections.Generic;
using NeoModLoader.services;
using NeoModLoader.api;
using UnityEngine;

namespace SuperMech.Code
{


    public static class SuperMechKnowledge
    {
        private static readonly string[] Tiers = { "sm_tier_basic", "sm_tier_advanced", "sm_tier_high", "sm_tier_cutting", "sm_tier_ultimate" };

        private static readonly string[][] Mech = {
            new[] { "sm_know_000","sm_know_001","sm_know_002","sm_know_003","sm_know_004" },
            new[] { "sm_know_005","sm_know_006","sm_know_007","sm_know_008","sm_know_009" },
            new[] { "sm_know_010","sm_know_011","sm_know_012","sm_know_013","sm_know_014" },
        };
        private static readonly string[][] MechAdv = {
            new[] { "sm_know_015","sm_know_016","sm_know_017","sm_know_018","sm_know_019" },
            new[] { "sm_know_020","sm_know_021","sm_know_022","sm_know_023","sm_know_024" },
            new[] { "sm_know_025","sm_know_026","","","" },
        };
        private static readonly string[][] MechHigh = {
            new[] { "sm_know_027","sm_know_028","","","" },
            new[] { "sm_know_029","sm_know_030","sm_know_031","sm_know_032","" },
            new[] { "sm_know_033","sm_know_034","sm_know_035","","" },
        };
        private static readonly string[][] MechTop = {
            new[] { "sm_know_036","sm_know_037","sm_know_038","","" },
            new[] { "sm_know_039","sm_know_040","sm_know_041","","" },
            new[] { "sm_know_042","sm_know_043","sm_know_044","","" },
        };
        private static readonly string[][] MechUlt = {
            new[] { "sm_know_045","sm_know_046","","","" },
            new[] { "sm_know_047","sm_know_048","","","" },
            new[] { "sm_know_049","sm_know_050","","","" },
        };

        private static readonly string[][] Martial = {
            new[] { "sm_know_051","sm_know_052","sm_know_053","sm_know_054","sm_know_055" },
            new[] { "sm_know_056","sm_know_057","sm_know_058","sm_know_059","" },
            new[] { "sm_know_060","sm_know_061","sm_know_062","sm_know_063","sm_know_064" },
        };
        private static readonly string[][] MartialAdv = {
            new[] { "sm_know_065","sm_know_066","sm_know_067","sm_know_068","sm_know_069" },
            new[] { "sm_know_070","sm_know_071","sm_know_072","sm_know_073","" },
            new[] { "sm_know_074","sm_know_075","sm_know_076","sm_know_077","sm_know_078" },
        };
        private static readonly string[][] MartialHigh = {
            new[] { "sm_know_079","sm_know_080","sm_know_081","","" },
            new[] { "sm_know_082","sm_know_083","sm_know_084","","" },
            new[] { "sm_know_085","sm_know_086","sm_know_087","","" },
        };
        private static readonly string[][] MartialTop = {
            new[] { "sm_know_088","sm_know_089","sm_know_090","","" },
            new[] { "sm_know_091","sm_know_092","sm_know_093","","" },
            new[] { "sm_know_094","sm_know_095","sm_know_096","","" },
        };
        private static readonly string[][] MartialUlt = {
            new[] { "sm_know_097","sm_know_098","","","" },
            new[] { "sm_know_099","sm_know_100","","","" },
            new[] { "sm_know_101","sm_know_102","","","" },
        };

        private static readonly string[][] Mage = {
            new[] { "sm_know_103","sm_know_104","sm_know_105","sm_know_106","sm_know_107" },
            new[] { "sm_know_108","sm_know_109","sm_know_110","sm_know_111","" },
            new[] { "sm_know_112","sm_know_113","sm_know_114","sm_know_115","" },
        };
        private static readonly string[][] MageAdv = {
            new[] { "sm_know_116","sm_know_117","sm_know_118","sm_know_119","" },
            new[] { "sm_know_120","sm_know_121","sm_know_122","sm_know_123","" },
            new[] { "sm_know_124","sm_know_125","sm_know_126","sm_know_127","" },
        };
        private static readonly string[][] MageHigh = {
            new[] { "sm_know_128","sm_know_129","sm_know_130","","" },
            new[] { "sm_know_131","sm_know_132","sm_know_133","sm_know_134","" },
            new[] { "sm_know_135","sm_know_136","sm_know_137","","" },
        };
        private static readonly string[][] MageTop = {
            new[] { "sm_know_138","sm_know_139","sm_know_140","","" },
            new[] { "sm_know_141","sm_know_142","sm_know_143","","" },
            new[] { "sm_know_144","sm_know_145","sm_know_146","","" },
        };
        private static readonly string[][] MageUlt = {
            new[] { "sm_know_147","sm_know_148","","","" },
            new[] { "sm_know_149","sm_know_150","","","" },
            new[] { "sm_know_151","sm_know_152","","","" },
        };

        private static readonly string[][] Mind = {
            new[] { "sm_know_153","sm_know_154","sm_know_155","sm_know_156","sm_know_157" },
            new[] { "sm_know_158","sm_know_159","sm_know_160","sm_know_161","" },
            new[] { "sm_know_162","sm_know_163","sm_know_164","sm_know_165","" },
        };
        private static readonly string[][] MindAdv = {
            new[] { "sm_know_166","sm_know_167","sm_know_168","sm_know_169","sm_know_170" },
            new[] { "sm_know_171","sm_know_172","sm_know_173","","" },
            new[] { "sm_know_174","sm_know_175","sm_know_176","sm_know_177","" },
        };
        private static readonly string[][] MindHigh = {
            new[] { "sm_know_178","sm_know_179","sm_know_180","sm_know_181","" },
            new[] { "sm_know_182","sm_know_183","sm_know_184","","" },
            new[] { "sm_know_185","sm_know_186","sm_know_187","sm_know_188","" },
        };
        private static readonly string[][] MindTop = {
            new[] { "sm_know_189","sm_know_190","sm_know_191","","" },
            new[] { "sm_know_192","sm_know_193","sm_know_194","","" },
            new[] { "sm_know_195","sm_know_196","sm_know_197","","" },
        };
        private static readonly string[][] MindUlt = {
            new[] { "sm_know_198","sm_know_199","","","" },
            new[] { "sm_know_200","sm_know_201","","","" },
            new[] { "sm_know_202","sm_know_203","","","" },
        };

        private static readonly string[][] Psi = {
            new[] { "sm_know_204","sm_know_205","sm_know_206","","" },
            new[] { "sm_know_207","sm_know_208","sm_know_209","","" },
            new[] { "sm_know_210","sm_know_211","sm_know_212","","" },
        };
        private static readonly string[][] PsiAdv = {
            new[] { "sm_know_213","sm_know_214","sm_know_215","","" },
            new[] { "sm_know_216","sm_know_217","sm_know_218","","" },
            new[] { "sm_know_219","sm_know_220","sm_know_221","","" },
        };
        private static readonly string[][] PsiHigh = {
            new[] { "sm_know_222","sm_know_223","sm_know_224","","" },
            new[] { "sm_know_225","sm_know_226","sm_know_227","","" },
            new[] { "sm_know_228","sm_know_229","sm_know_230","","" },
        };
        private static readonly string[][] PsiTop = {
            new[] { "sm_know_231","sm_know_232","sm_know_233","","" },
            new[] { "sm_know_234","sm_know_235","sm_know_236","","" },
            new[] { "sm_know_237","sm_know_238","sm_know_239","","" },
        };
        private static readonly string[][] PsiUlt = {
            new[] { "sm_know_240","sm_know_241","sm_know_242","","" },
            new[] { "sm_know_243","sm_know_244","sm_know_245","","" },
            new[] { "sm_know_246","sm_know_247","sm_know_248","","" },
        };

        public class KnowledgeDef
        {
            public string id;
            public string name;
            public string desc;
            public string prefix;
            public int tier;
            public int branch;
            public int cost;
            public string icon;
        }

        private static readonly Dictionary<string, KnowledgeDef> _allKnowledge = new Dictionary<string, KnowledgeDef>();
        private static readonly Dictionary<long, HashSet<string>> _unlocked = new Dictionary<long, HashSet<string>>();

        public static void Register()
        {
            int count = 0;
            count += RegisterTree("mech", "sm_tree_mech",
                new[] { "sm_know_249", "sm_know_250", "sm_know_251" },
                new[] { Mech, MechAdv, MechHigh, MechTop, MechUlt });
            count += RegisterTree("martial", "sm_tree_martial",
                new[] { "sm_know_252", "sm_know_253", "sm_know_254" },
                new[] { Martial, MartialAdv, MartialHigh, MartialTop, MartialUlt });
            count += RegisterTree("mage", "sm_tree_mage",
                new[] { "sm_know_255", "sm_know_256", "sm_know_257" },
                new[] { Mage, MageAdv, MageHigh, MageTop, MageUlt });
            count += RegisterTree("mind", "sm_tree_mind",
                new[] { "sm_know_258", "sm_know_259", "sm_know_260" },
                new[] { Mind, MindAdv, MindHigh, MindTop, MindUlt });
            count += RegisterTree("psi", "sm_tree_psi",
                new[] { "sm_know_261", "sm_know_262", "sm_know_263" },
                new[] { Psi, PsiAdv, PsiHigh, PsiTop, PsiUlt });

            Debug.Log($"[超神机械师] 五系知识树注册完成，共 {count} 个知识节点（内部字典，不注册为特质）");
        }

        private static readonly string[] BookIconsByTier = {
            "ui/Icons/iconBooks",
            "ui/Icons/iconBooksRead",
            "ui/Icons/iconBooksWritten",
            "ui/Icons/iconBooks",
            "ui/Icons/iconBooksDestroyed"
        };

        private static readonly string[][] BookIconsByClass = {
            new[] { "ui/Icons/iconBooks", "ui/Icons/iconBooksRead", "ui/Icons/iconBooksWritten" },
            new[] { "ui/Icons/iconBooks", "ui/Icons/iconBooksRead", "ui/Icons/iconBooksWritten" },
            new[] { "ui/Icons/iconBooks", "ui/Icons/iconBooksRead", "ui/Icons/iconBooksWritten" },
            new[] { "ui/Icons/iconBooks", "ui/Icons/iconBooksRead", "ui/Icons/iconBooksWritten" },
            new[] { "ui/Icons/iconBooks", "ui/Icons/iconBooksRead", "ui/Icons/iconBooksWritten" }
        };

        private static string GetKnowledgeIcon(string prefix, int branch, int tier, int knowledgeIdx)
        {
            if (tier >= 0 && tier < BookIconsByTier.Length)
                return BookIconsByTier[tier];
            return "ui/Icons/iconBooks";
        }

        private static int RegisterTree(string prefix, string treeName, string[] branchNames, string[][][] tiers)
        {
            int n = 0;
            for (int ti = 0; ti < tiers.Length; ti++)
            {
                for (int bi = 0; bi < tiers[ti].Length; bi++)
                {
                    for (int ki = 0; ki < tiers[ti][bi].Length; ki++)
                    {
                        string kn = tiers[ti][bi][ki];
                        if (string.IsNullOrEmpty(kn)) continue;
                        string id = $"sm_know_{prefix}_{ti}_{bi}_{ki}";
                        LocalizedTextManager.add("trait_" + id, LocalizedTextManager.getText(kn), pReplace: true);
                        LocalizedTextManager.add("trait_" + id + "_info", $"{LocalizedTextManager.getText(treeName)}·{branchNames[bi]}·{LocalizedTextManager.getText(Tiers[ti])}{LocalizedTextManager.getText("sm_ui_knowledge")}", pReplace: true);
                        var def = new KnowledgeDef
                        {
                            id = id,
                            name = kn,
                            desc = $"{LocalizedTextManager.getText(treeName)}·{branchNames[bi]}·{LocalizedTextManager.getText(Tiers[ti])}{LocalizedTextManager.getText("sm_ui_knowledge")}",
                            prefix = prefix,
                            tier = ti,
                            branch = bi,
                            cost = (ti + 1) * 2,
                            icon = GetKnowledgeIcon(prefix, bi, ti, ki)
                        };
                        _allKnowledge[id] = def;
                        n++;
                    }
                }
            }
            return n;
        }

        public static bool IsUnlocked(Actor a, string knowledgeId)
        {
            if (a == null) return false;
            if (_unlocked.TryGetValue(a.id, out var set)) return set.Contains(knowledgeId);
            return false;
        }

        public static bool Unlock(Actor a, string knowledgeId)
        {
            if (a == null || !_allKnowledge.ContainsKey(knowledgeId)) return false;
            if (!_unlocked.TryGetValue(a.id, out var set))
            {
                set = new HashSet<string>();
                _unlocked[a.id] = set;
            }
            return set.Add(knowledgeId);
        }

        public static int GetUnlockedCount(Actor a, string prefix)
        {
            if (a == null) return 0;
            if (!_unlocked.TryGetValue(a.id, out var set)) return 0;
            int count = 0;
            string key = "sm_know_" + prefix + "_";
            foreach (var id in set) if (id.StartsWith(key)) count++;
            return count;
        }

        public static int GetTierKnowledgeCount(Actor a, string prefix, int tier)
        {
            if (a == null) return 0;
            if (!_unlocked.TryGetValue(a.id, out var set)) return 0;
            int count = 0;
            string key = $"sm_know_{prefix}_{tier}_";
            foreach (var id in set) if (id.StartsWith(key)) count++;
            return count;
        }

        public static List<KnowledgeDef> GetUnlockedList(Actor a, string prefix)
        {
            var list = new List<KnowledgeDef>();
            if (a == null || !_unlocked.TryGetValue(a.id, out var set)) return list;
            string key = "sm_know_" + prefix + "_";
            foreach (var id in set)
            {
                if (id.StartsWith(key) && _allKnowledge.TryGetValue(id, out var def)) list.Add(def);
            }
            return list;
        }

        public static List<KnowledgeDef> GetAllByTier(string prefix, int tier)
        {
            var list = new List<KnowledgeDef>();
            string key = $"sm_know_{prefix}_{tier}_";
            foreach (var kv in _allKnowledge)
            {
                if (kv.Key.StartsWith(key)) list.Add(kv.Value);
            }
            list.Sort((a, b) => a.branch.CompareTo(b.branch));
            return list;
        }

        public static List<KnowledgeDef> GetAllByPrefix(string prefix)
        {
            var list = new List<KnowledgeDef>();
            string key = $"sm_know_{prefix}_";
            foreach (var kv in _allKnowledge)
            {
                if (kv.Key.StartsWith(key)) list.Add(kv.Value);
            }
            list.Sort((a, b) =>
            {
                int tierCompare = a.tier.CompareTo(b.tier);
                if (tierCompare != 0) return tierCompare;
                return a.branch.CompareTo(b.branch);
            });
            return list;
        }

        public static KnowledgeDef GetDef(string id)
        {
            _allKnowledge.TryGetValue(id, out var def);
            return def;
        }

        public static List<KnowledgeDef> GetTierBranchList(string prefix, int tier, int branch)
        {
            var list = new List<KnowledgeDef>();
            string key = $"sm_know_{prefix}_{tier}_{branch}_";
            foreach (var kv in _allKnowledge)
            {
                if (kv.Key.StartsWith(key)) list.Add(kv.Value);
            }
            return list;
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            return SuperMechCleanup.CleanDict(_unlocked, alive);
        }

        public static void Clear() { _unlocked.Clear(); }

        public static string GetPrefixForClass(string cls)
        {
            switch (cls)
            {
                case "sm_class_martial": return "martial";
                case "sm_class_psi": return "psi";
                case "sm_class_mage": return "mage";
                case "sm_class_mind": return "mind";
                default: return "mech";
            }
        }
    }
}
