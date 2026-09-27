using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechLocaleExport
    {
        private static readonly string FilePath =
            Path.Combine(Application.dataPath, "sm_localeexport_883");

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
                var dict = new SortedDictionary<string, string>();
                if (File.Exists(FilePath))
                {
                    var existing = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(FilePath));
                    if (existing != null)
                        foreach (var kv in existing) dict[kv.Key] = kv.Value;
                }

                int merged = 0;
                if (LocalizedTextManager.instance != null)
                {
                    foreach (string key in LocalizedTextManager.getKeys())
                    {
                        if (!IsOurs(key)) continue;
                        string value = LocalizedTextManager.getText(key);
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
