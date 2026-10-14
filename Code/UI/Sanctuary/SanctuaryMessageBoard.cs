using System.Text;

namespace SuperMech.Code
{
    /// <summary>
    /// 圣所留言板业务逻辑
    /// 单一职责：只负责留言数据的获取、权限过滤、内容构建
    /// 从SanctuaryView中提取，遵循UI与业务逻辑分离原则
    /// </summary>
    public static class SanctuaryMessageBoard
    {
        /// <summary>原著留言：各迭代终极文明的留言（chapter1267）</summary>
        private static string[] _sanctuaryMessages;
        private static string[] SanctuaryMessages => _sanctuaryMessages ??= new[]
        {
            LocalizedTextManager.getText("sm_san_msg_1"),
            LocalizedTextManager.getText("sm_san_msg_2"),
            LocalizedTextManager.getText("sm_san_msg_3"),
            LocalizedTextManager.getText("sm_san_msg_4"),
            LocalizedTextManager.getText("sm_san_msg_5"),
            LocalizedTextManager.getText("sm_san_msg_6"),
            LocalizedTextManager.getText("sm_san_msg_7"),
        };

        /// <summary>总留言数</summary>
        public static int TotalCount => SanctuaryMessages.Length;

        /// <summary>根据权限计算可见留言数量</summary>
        public static int GetVisibleCount(int permission)
        {
            if (permission <= 0) return 1;
            if (permission == 1) return 3;
            if (permission == 2) return 5;
            return 7; // 3+碎片全部可见
        }

        /// <summary>构建留言板显示内容（含权限遮挡）</summary>
        public static string BuildDisplayContent(int permission)
        {
            var sb = new StringBuilder();
            int visible = GetVisibleCount(permission);
            for (int i = 0; i < SanctuaryMessages.Length; i++)
            {
                if (i >= visible)
                {
                    sb.AppendLine(LocalizedTextManager.getText("sm_san_msg_no_perm"));
                    sb.AppendLine("");
                    continue;
                }
                string msg = ApplyMask(SanctuaryMessages[i], permission, i);
                sb.AppendLine(string.Format(LocalizedTextManager.getText("sm_san_msg_num"), i + 1));
                sb.AppendLine(msg);
                sb.AppendLine("");
            }
            sb.AppendLine(LocalizedTextManager.getText("sm_san_msg_hint"));
            return sb.ToString();
        }

        /// <summary>根据权限对留言内容进行遮挡（███）</summary>
        private static string ApplyMask(string message, int permission, int index)
        {
            // 权限越低，遮挡越多；第一条基本介绍不遮挡
            if (permission >= 3 || index <= 0) return message;
            // 简单遮挡：将部分███保留，权限低时更多内容被替换为███
            if (permission == 1 && index >= 2)
            {
                // 低权限：只显示前半部分
                int half = message.Length / 2;
                return message.Substring(0, half) + LocalizedTextManager.getText("sm_san_msg_mask");
            }
            return message;
        }
    }
}
