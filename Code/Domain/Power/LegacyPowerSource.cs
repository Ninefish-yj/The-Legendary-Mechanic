namespace SuperMech.Code
{
    /// <summary>超神遗力来源记录（原著还原：继承好处也要继承债务）
    /// 原著第1398章：超神遗力凝聚了死者进阶身亡时逸散的生命精华、灵魂意识、核心能量
    /// 内部有残存意识体，继承者需承受与死者死因相同类型的负荷（弱化版恶性变异）
    /// </summary>
    public class LegacyPowerSource
    {
        public string sourceName;        // 死者名字
        public int sourceRank;           // 死者阶位
        public float sourceEnergy;       // 死者能级
        public float deathDamage;        // 承伤基准
        public string deathType;         // 死因：cosmic_assimilation/genome_collapse/cell_independence/infinite_proliferation/other
        public string loadType;          // 负荷类型：mental/genetic/physical/energy
        public string consciousnessName; // 残存意识体称呼（如"提尔修斯"）
        public string wishText;          // 遗愿文本
        public bool consciousnessGone;   // 残存意识是否已消亡（继承成功后消亡）
    }
}
