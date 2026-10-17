using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.234 科技树JSON数据加载器
    /// 从模组目录下的 TechTree.json 读取，支持校验和错误提示。
    /// </summary>
    public static class TechDataLoader
    {
        private static TechData.TechTreeData _cached;
        private static bool _loaded;
        private static readonly Dictionary<string, TechData.TechDef> _techById = new Dictionary<string, TechData.TechDef>();
        private static readonly Dictionary<string, TechData.MaterialDef> _materialById = new Dictionary<string, TechData.MaterialDef>();

        /// <summary>科技树数据（懒加载）</summary>
        public static TechData.TechTreeData Data
        {
            get
            {
                if (!_loaded) Load();
                return _cached;
            }
        }

        /// <summary>按ID获取科技定义</summary>
        public static TechData.TechDef GetTech(string id)
        {
            if (!_loaded) Load();
            return _techById.TryGetValue(id, out var t) ? t : null;
        }

        /// <summary>按ID获取材料定义</summary>
        public static TechData.MaterialDef GetMaterial(string id)
        {
            if (!_loaded) Load();
            return _materialById.TryGetValue(id, out var m) ? m : null;
        }

        /// <summary>所有科技</summary>
        public static List<TechData.TechDef> AllTechs
        {
            get
            {
                if (!_loaded) Load();
                return _cached != null ? _cached.techs : new List<TechData.TechDef>();
            }
        }

        /// <summary>所有材料</summary>
        public static List<TechData.MaterialDef> AllMaterials
        {
            get
            {
                if (!_loaded) Load();
                return _cached != null ? _cached.materials : new List<TechData.MaterialDef>();
            }
        }

        /// <summary>加载科技树JSON</summary>
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string path = Path.Combine(Main.ModPath, "TechTree.json");
                if (!File.Exists(path))
                {
                    Debug.LogError("[超神机械师] 科技树文件不存在: " + path);
                    _cached = new TechData.TechTreeData();
                    return;
                }

                string json = File.ReadAllText(path);
                _cached = JsonConvert.DeserializeObject<TechData.TechTreeData>(json);
                if (_cached == null)
                {
                    Debug.LogError("[超神机械师] 科技树JSON解析失败");
                    _cached = new TechData.TechTreeData();
                    return;
                }

                BuildIndexes();
                Validate();
                Debug.Log($"[超神机械师] 科技树加载完成：{_cached.techs.Count}科技 / {_cached.materials.Count}材料 / {_cached.eras.Count}时代");
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师] 科技树加载异常: " + e);
                _cached = new TechData.TechTreeData();
            }
        }

        /// <summary>构建ID索引</summary>
        private static void BuildIndexes()
        {
            _techById.Clear();
            _materialById.Clear();

            foreach (var tech in _cached.techs)
            {
                if (!string.IsNullOrEmpty(tech.id))
                    _techById[tech.id] = tech;
            }
            foreach (var mat in _cached.materials)
            {
                if (!string.IsNullOrEmpty(mat.id))
                    _materialById[mat.id] = mat;
            }
        }

        /// <summary>数据校验：检查重复ID、前置科技不存在等问题</summary>
        private static void Validate()
        {
            int warnings = 0;

            // 检查重复ID
            var techIds = new HashSet<string>();
            foreach (var tech in _cached.techs)
            {
                if (string.IsNullOrEmpty(tech.id))
                {
                    Debug.LogWarning("[超神机械师] 科技树: 存在空ID的科技");
                    warnings++;
                    continue;
                }
                if (!techIds.Add(tech.id))
                {
                    Debug.LogWarning("[超神机械师] 科技树: 重复科技ID " + tech.id);
                    warnings++;
                }
            }

            // 检查前置科技是否存在
            foreach (var tech in _cached.techs)
            {
                if (tech.requires_techs == null) continue;
                foreach (var pre in tech.requires_techs)
                {
                    if (!_techById.ContainsKey(pre))
                    {
                        Debug.LogWarning($"[超神机械师] 科技树: 科技 {tech.id} 的前置 {pre} 不存在");
                        warnings++;
                    }
                }
            }

            // 检查前置材料是否存在
            foreach (var tech in _cached.techs)
            {
                if (tech.requires_materials == null) continue;
                foreach (var mat in tech.requires_materials)
                {
                    if (!_materialById.ContainsKey(mat))
                    {
                        Debug.LogWarning($"[超神机械师] 科技树: 科技 {tech.id} 的材料 {mat} 不存在");
                        warnings++;
                    }
                }
            }

            if (warnings > 0)
                Debug.LogWarning($"[超神机械师] 科技树校验完成：{warnings}个警告");
        }

        /// <summary>重新加载（用于开发期热重载）</summary>
        public static void Reload()
        {
            _loaded = false;
            _cached = null;
            _techById.Clear();
            _materialById.Clear();
            Load();
        }
    }
}
