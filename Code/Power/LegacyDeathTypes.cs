using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>死因→负荷类型映射（原著：经历弱化版恶性变异，与死者进阶时危机类型相同）
    /// </summary>
    public static class LegacyDeathTypes
    {
        public const string CosmicAssimilation = "cosmic_assimilation";  // 宇宙同化→精神负荷
        public const string GenomeCollapse = "genome_collapse";          // 基因崩溃→基因负荷
        public const string CellIndependence = "cell_independence";      // 细胞独立→肉体负荷
        public const string InfiniteProliferation = "infinite_proliferation"; // 无限增殖→能量负荷
        public const string Other = "other";                              // 其他→综合负荷

        public static string GetLoadType(string deathType)
        {
            switch (deathType)
            {
                case CosmicAssimilation: return "mental";
                case GenomeCollapse: return "genetic";
                case CellIndependence: return "physical";
                case InfiniteProliferation: return "energy";
                default: return "physical";
            }
        }

        public static string GetDeathTypeName(string deathType)
        {
            switch (deathType)
            {
                case CosmicAssimilation: return LocalizedTextManager.getText("sm_legacy_death_cosmic");
                case GenomeCollapse: return LocalizedTextManager.getText("sm_legacy_death_genome");
                case CellIndependence: return LocalizedTextManager.getText("sm_legacy_death_cell");
                case InfiniteProliferation: return LocalizedTextManager.getText("sm_legacy_death_prolif");
                default: return LocalizedTextManager.getText("sm_legacy_death_other");
            }
        }

        public static string GetLoadTypeName(string loadType)
        {
            switch (loadType)
            {
                case "mental": return LocalizedTextManager.getText("sm_legacy_load_mental");
                case "genetic": return LocalizedTextManager.getText("sm_legacy_load_genetic");
                case "physical": return LocalizedTextManager.getText("sm_legacy_load_physical");
                case "energy": return LocalizedTextManager.getText("sm_legacy_load_energy");
                default: return LocalizedTextManager.getText("sm_legacy_load_physical");
            }
        }

        /// <summary>随机一个死因（突破失败时恶性变异类型）
        /// </summary>
        public static string RandomDeathType()
        {
            float r = Random.value;
            if (r < 0.3f) return CosmicAssimilation;
            if (r < 0.55f) return GenomeCollapse;
            if (r < 0.8f) return CellIndependence;
            if (r < 0.95f) return InfiniteProliferation;
            return Other;
        }
    }
}
