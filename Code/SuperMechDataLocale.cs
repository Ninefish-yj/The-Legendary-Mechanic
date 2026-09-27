using System.Collections.Generic;
using NeoModLoader.services;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 数据本地化已全部迁移到Locales/cz.json。
    /// 此类保留为空，仅用于兼容旧代码调用。
    /// 多语言：加en.json即可，无需修改代码。
    /// </summary>
    public static class SuperMechDataLocale
    {
        private static bool _inited;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;
            // 所有数据定义已在cz.json中，运行时无需注册
        }
    }
}
