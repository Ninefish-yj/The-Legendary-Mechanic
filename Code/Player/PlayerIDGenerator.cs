using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 玩家ID生成器
    /// 单一职责：只负责生成和管理玩家（降临者）ID
    /// 从Player中提取，遵循单一职责原则
    /// 原著：玩家用网名而非真名，ID在同一世界不重复
    /// </summary>
    public static class PlayerIDGenerator
    {
        /// <summary>原著风格固定玩家ID（高优先级，参考【肉包打狗】【狂刀怒剑】）</summary>
        private static readonly string[] EarthPlayerFixedIDs = {
            "肉包打狗", "狂刀怒剑", "极乐迪斯科", "飞云之下", "慕辰",
            "夜雨听风", "孤星逐日", "星海漫游者", "虚空行者", "量子幽灵",
            "次元旅者", "混沌钓叟", "极光之翼", "暗影刺客", "银河摆渡人",
            "时间拾荒者", "梦境编织者", "符文大师", "灵能学徒", "钛合金直男",
            "咸鱼翻身", "摸鱼真君", "社畜本畜", "卷王之王", "键盘侠",
            "嘴强王者", "理论大师", "实践矮子", "云玩家", "内测大佬",
            "公测萌新", "肝帝", "仓鼠党", "外观党", "成就猎人",
            "风景党", "剧情党", "PVP狂人", "PVE咸鱼", "副本刷子",
            "战场收割者", "公会会长", "散人玩家", "独行侠", "氪金大佬",
            "零充豹子头", "欧皇附体", "非酋本酋", "脸黑如碳", "手残党",
            "意识流", "走位风骚", "输出全靠吼", "躺赢专家", "下饭操作",
            "菜鸡互啄", "泉水指挥官", "团战祭品", "人头狗", "赛博朋克",
            "蒸汽朋克", "机械公敌", "黑客帝国", "盗梦空间", "星际穿越",
            "火星救援", "银翼杀手", "星球大战", "星际迷航", "铁血战士",
            "变形金刚", "环太平洋", "AI觉醒", "机甲驾驶员", "星舰指挥官",
            "清风徐来", "明月几时有", "把酒问青天", "落花人独立", "微雨燕双飞",
            "人生若只如初见", "何事秋风悲画扇", "一蓑烟雨任平生", "大漠孤烟直", "长河落日圆",
            "星垂平野阔", "月涌大江流", "天地一沙鸥", "会当凌绝顶", "一览众山小",
            "行到水穷处", "坐看云起时", "空山新雨后", "明月松间照", "清泉石上流",
            "麻辣烫", "小龙虾", "烧烤", "火锅", "奶茶",
            "肥宅快乐水", "薯片", "泡面", "炸鸡", "可乐",
            "橘猫", "哈士奇", "柴犬", "柯基", "布偶猫",
            "熊猫", "企鹅", "水獭", "海獭", "仓鼠",
            "卖女孩的小火柴", "采姑娘的小蘑菇", "偷井盖的贼", "抢银行的劫匪", "碰瓷的老大爷",
            "广场舞大妈", "小区保安", "外卖小哥", "快递员", "网约车司机",
            "程序猿", "产品经理", "设计师", "运营狗", "市场部",
            "甲方爸爸", "乙方孙子", "需求又变了", "这个需求很简单", "怎么实现我不管",
            "明天上线", "今晚加班", "bug修不完", "玩家007", "玩家404",
            "玩家520", "玩家666", "PlayerUnknown", "NoobMaster", "ProGamer",
        };

        /// <summary>已使用的网名（避免重复）</summary>
        private static readonly HashSet<string> _usedPlayerIDs = new HashSet<string>();

        /// <summary>韩萧的固定ID（原著：黑星）</summary>
        public const string HanXiaoID = "黑星";

        /// <summary>生成唯一玩家ID</summary>
        public static string GenerateUniqueID()
        {
            // 优先从固定ID池中选取未使用的
            var available = new List<string>();
            foreach (var id in EarthPlayerFixedIDs)
            {
                if (!_usedPlayerIDs.Contains(id))
                    available.Add(id);
            }

            if (available.Count > 0)
            {
                string id = available[Random.Range(0, available.Count)];
                _usedPlayerIDs.Add(id);
                return id;
            }

            // 固定ID用完后随机生成
            int suffix = Random.Range(1000, 9999);
            string newID = $"玩家{suffix}";
            while (_usedPlayerIDs.Contains(newID))
            {
                suffix = Random.Range(1000, 9999);
                newID = $"玩家{suffix}";
            }
            _usedPlayerIDs.Add(newID);
            return newID;
        }

        /// <summary>检查ID是否已使用</summary>
        public static bool IsUsed(string id) => _usedPlayerIDs.Contains(id);

        /// <summary>标记ID为已使用</summary>
        public static void MarkUsed(string id) => _usedPlayerIDs.Add(id);

        /// <summary>重置已使用ID（世界切换时调用）</summary>
        public static void Reset() => _usedPlayerIDs.Clear();

        /// <summary>获取已使用ID数量</summary>
        public static int UsedCount => _usedPlayerIDs.Count;

        /// <summary>获取所有已使用ID（用于存档）</summary>
        public static HashSet<string> GetUsedIDs() => new HashSet<string>(_usedPlayerIDs);

        /// <summary>批量恢复已使用ID（用于读档）</summary>
        public static void LoadUsedIDs(IEnumerable<string> ids)
        {
            _usedPlayerIDs.Clear();
            if (ids != null)
            {
                foreach (var id in ids) _usedPlayerIDs.Add(id);
            }
        }
    }
}
