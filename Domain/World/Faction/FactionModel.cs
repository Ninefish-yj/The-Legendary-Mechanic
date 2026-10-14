using System.Collections.Generic;

namespace SuperMech.Code
{
    /// <summary>
    /// 势力数据模型（从SuperMechFaction拆分）
    /// 包含势力关系枚举、政体枚举和势力数据类
    /// </summary>
    public static class FactionModel
    {
        public enum FactionRelation { Neutral = 0, Allied = 1, Hostile = 2 }
        public enum FactionGovernment { Autocracy = 0, Council = 1, Theocracy = 2 }

        public class FactionData
        {
            public string id;
            public string name;
            public long leaderId;
            public FactionGovernment government = FactionGovernment.Autocracy;
            public int power = 0;           // 势力综合实力
            public int memberCount = 0;
            public Dictionary<string, FactionRelation> relations = new Dictionary<string, FactionRelation>();
            public int warWeariness = 0;    // 战争厌战度
            public int age = 0;             // 势力存续时间
        }
    }
}
