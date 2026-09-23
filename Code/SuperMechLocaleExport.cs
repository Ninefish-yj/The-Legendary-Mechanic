using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// 本地化自动导出：
    /// 模组各系统通过 LocalizedTextManager.add 运行时注册中文，本导出器在游戏启动后
    /// 枚举所有 sm_ 开头的本地化键值，连同静态 cz.json 中手写的内容，合并写回一份
    /// 完整的 Locales/cz.json，避免“半静态半运行时”导致静态文件不全。
    /// </summary>
    public static class SuperMechLocaleExport
    {
        private static readonly string FilePath =
            Path.Combine(Application.dataPath, "../Mods/超神机械师/Locales/cz.json");

        // 仅收录本模组自己的前缀，避免把游戏原版上千条文本混进来
        private static readonly string[] Prefixes =
            { "trait_sm_", "power_sm_", "supermach." };

        private static bool IsOurs(string key)
        {
            foreach (var p in Prefixes) if (key.StartsWith(p)) return true;
            return false;
        }

        public static void Export()
        {
            try
            {
                // 1) 以现有静态 cz.json 为底（保留手写阶位中文，防止非中文界面下被原始key覆盖）
                var dict = new SortedDictionary<string, string>();
                if (File.Exists(FilePath))
                {
                    var existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(FilePath));
                    if (existing != null)
                        foreach (var kv in existing) dict[kv.Key] = kv.Value;
                }

                // 2) 枚举运行时本地化文本，合并本模组条目
                int merged = 0;
                if (LocalizedTextManager.instance != null)
                {
                    foreach (string key in LocalizedTextManager.getKeys())
                    {
                        if (!IsOurs(key)) continue;
                        string value = LocalizedTextManager.getText(key);
                        // 缺失时 getText 会返回 key 本身或空串，这种无效值不覆盖已有内容
                        if (string.IsNullOrEmpty(value) || value == key) continue;
                        if (!dict.TryGetValue(key, out var old) || old != value)
                        {
                            dict[key] = value;
                            merged++;
                        }
                        else
                        {
                            dict[key] = value;
                        }
                    }
                }

                // 3) 排序写回（按名称排序，便于版本管理 diff）
                string dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var sorted = new SortedDictionary<string, string>(dict);
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(sorted, Formatting.Indented));

                Debug.Log($"[超神机械师] 本地化导出完成：cz.json 共 {sorted.Count} 条（本次更新 {merged} 条）");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[超神机械师] 本地化导出失败: " + e.Message);
            }
        }
    }
}
