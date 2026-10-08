using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 势力名称生成器
    /// 单一职责：只负责生成原著风格的势力名称
    /// 从Faction中提取，遵循单一职责原则
    /// 原著：炎黄联盟是降临者专属前缀，虚灵/机械/赤色是原住民前缀
    /// </summary>
    public static class FactionNameGenerator
    {
        // 降临者(地球文化)前缀
        private static readonly string[] DescendantPrefixes = {
            "炎黄", "华夏", "神州", "中华", "黑星", "龙", "星辰", "昆仑",
            "蓬莱", "方丈", "九州", "五岳", "长江", "黄河", "青龙", "朱雀"
        };

        // 原住民(异星科幻)前缀
        private static readonly string[] NativePrefixes = {
            "虚灵", "机械", "赤色", "圣约", "萌芽", "血金", "暗网", "银辉",
            "苍穹", "星渊", "破晓", "雷霆", "寒霜", "铁血", "暗夜", "星辉",
            "曜日", "苍蓝", "紫金", "破碎", "永恒", "自由", "荣耀", "深渊",
            "极光", "混沌", "虚空", "曜石", "赤焰", "幽影"
        };

        // 通用后缀
        private static readonly string[] FactionSuffixes = {
            "军团", "教派", "协会", "组织", "联盟", "帝国", "商会", "共和国",
            "联邦", "王朝", "神国", "公社", "王国", "公国", "教廷", "议会",
            "财团", "集团", "学院", "研究院", "佣兵公会", "家族", "圣殿",
            "舰队", "共同体", "联合体", "阵线", "骑士团", "兄弟会", "评议会",
            "元老院", "部落", "氏族", "帮会", "密社", "隐修会", "堡垒",
            "道统", "冒险团", "护卫队", "革命军"
        };

        private static int _nameCounter = 0;

        /// <summary>生成势力名称（前缀+后缀，不带序号）</summary>
        /// <param name="isDescendant">是否为降临者创建（使用地球文化前缀）</param>
        public static string GenerateName(bool isDescendant)
        {
            string[] prefixes = isDescendant ? DescendantPrefixes : NativePrefixes;
            string prefix = prefixes[Random.Range(0, prefixes.Length)];
            string suffix = FactionSuffixes[Random.Range(0, FactionSuffixes.Length)];
            return prefix + suffix;
        }

        /// <summary>获取下一个势力ID序号（递增）</summary>
        public static int GetNextId()
        {
            return ++_nameCounter;
        }

        /// <summary>获取当前序号（不递增）</summary>
        public static int GetCurrentId()
        {
            return _nameCounter;
        }

        /// <summary>设置序号（读档时恢复）</summary>
        public static void SetCounter(int value)
        {
            _nameCounter = value;
        }

        /// <summary>重置名称计数器（世界切换时调用）</summary>
        public static void Reset()
        {
            _nameCounter = 0;
        }
    }
}
