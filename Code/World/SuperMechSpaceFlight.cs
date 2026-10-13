using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperMech.Code
{
    /// <summary>
    /// 太空飞行系统 - 飞船从一个星球飞到另一个星球的过场动画+随机事件
    /// </summary>
    public static class SuperMechSpaceFlight
    {
        /// <summary>是否正在飞行中</summary>
        public static bool IsFlying = false;

        /// <summary>当前飞行数据</summary>
        public static FlightData CurrentFlight = null;

        /// <summary>飞行事件队列</summary>
        private static readonly List<FlightEvent> _eventQueue = new List<FlightEvent>();

        /// <summary>飞船速度（星图单位/秒）</summary>
        public const float ShipSpeed = 50f;

        /// <summary>飞行数据</summary>
        public class FlightData
        {
            public string fromPlanet;
            public string toPlanet;
            public Vector2 fromPos;
            public Vector2 toPos;
            public float totalDistance;
            public float elapsedTime;
            public float totalTime;
            public List<long> passengerIds; // 乘客单位ID
            public bool isPlayerInitiated; // 是否玩家发起
        }

        /// <summary>飞行事件</summary>
        public class FlightEvent
        {
            public string id;
            public string name;
            public string description;
            public float triggerTime; // 触发时间（飞行进度0-1）
            public Action onTrigger;
            public bool triggered;
        }

        /// <summary>开始太空飞行</summary>
        public static bool StartFlight(string fromPlanet, string toPlanet, List<long> passengerIds = null, bool playerInitiated = true)
        {
            if (IsFlying)
            {
                Debug.LogWarning("[超神机械师]【太空飞行】已有飞行进行中");
                return false;
            }

            var from = SuperMechStarSystem.GetPlanet(fromPlanet);
            var to = SuperMechStarSystem.GetPlanet(toPlanet);

            if (from == null || to == null)
            {
                Debug.LogError("[超神机械师]【太空飞行】星球数据不存在");
                return false;
            }

            // 计算飞行路径
            Vector2 fromPos = new Vector2(from.x, from.y);
            Vector2 toPos = new Vector2(to.x, to.y);
            float distance = Vector2.Distance(fromPos, toPos);
            float flightTime = distance / ShipSpeed;

            // 创建飞行数据
            CurrentFlight = new FlightData
            {
                fromPlanet = fromPlanet,
                toPlanet = toPlanet,
                fromPos = fromPos,
                toPos = toPos,
                totalDistance = distance,
                elapsedTime = 0f,
                totalTime = Mathf.Max(flightTime, 3f), // 最少3秒
                passengerIds = passengerIds ?? new List<long>(),
                isPlayerInitiated = playerInitiated
            };

            // 生成随机事件
            GenerateRandomEvents();

            IsFlying = true;

            Debug.Log($"[超神机械师]【太空飞行】开始飞行: {from.name} -> {to.name}, 距离={distance:F0}, 时间={CurrentFlight.totalTime:F1}秒");

            // 显示飞行动画
            ShowFlightOverlay();

            return true;
        }

        /// <summary>生成随机事件</summary>
        private static void GenerateRandomEvents()
        {
            _eventQueue.Clear();

            if (CurrentFlight == null) return;

            // 随机事件池
            var eventPool = new List<(string id, string name, string desc, float probability)>
            {
                ("meteor", "陨石带", "飞船穿越陨石带，需要小心规避", 0.15f),
                ("pirate", "太空海盗", "遭遇太空海盗袭击！", 0.1f),
                ("signal", "异常信号", "探测到未知信号源", 0.1f),
                ("solar_flare", "太阳耀斑", "恒星耀斑干扰通讯", 0.08f),
                ("derelict", "废弃飞船", "发现一艘废弃飞船", 0.05f),
                ("wormhole", "虫洞", "发现稳定虫洞，可加速航行", 0.03f),
            };

            System.Random rng = new System.Random();
            foreach (var evt in eventPool)
            {
                if (rng.NextDouble() < evt.probability)
                {
                    _eventQueue.Add(new FlightEvent
                    {
                        id = evt.id,
                        name = evt.name,
                        description = evt.desc,
                        triggerTime = (float)rng.NextDouble() * 0.8f + 0.1f, // 10%-90%进度时触发
                        triggered = false
                    });
                }
            }

            // 按触发时间排序
            _eventQueue.Sort((a, b) => a.triggerTime.CompareTo(b.triggerTime));
        }

        /// <summary>飞行Update（每帧调用）</summary>
        public static void FlightUpdate(float dt)
        {
            if (!IsFlying || CurrentFlight == null) return;

            CurrentFlight.elapsedTime += dt;
            float progress = CurrentFlight.elapsedTime / CurrentFlight.totalTime;

            // 检查事件触发
            foreach (var evt in _eventQueue)
            {
                if (!evt.triggered && progress >= evt.triggerTime)
                {
                    evt.triggered = true;
                    TriggerEvent(evt);
                }
            }

            // 更新飞行动画
            UpdateFlightOverlay(progress);

            // 飞行完成
            if (progress >= 1f)
            {
                CompleteFlight();
            }
        }

        /// <summary>触发飞行事件</summary>
        private static void TriggerEvent(FlightEvent evt)
        {
            Debug.Log($"[超神机械师]【太空飞行】事件触发: {evt.name} - {evt.description}");

            // 根据事件类型执行效果
            switch (evt.id)
            {
                case "meteor":
                    // 陨石带：飞行时间+2秒
                    CurrentFlight.totalTime += 2f;
                    break;
                case "pirate":
                    // 太空海盗：乘客可能受伤（简化：飞行时间+3秒）
                    CurrentFlight.totalTime += 3f;
                    break;
                case "signal":
                    // 异常信号：发现新星球（简化：日志）
                    break;
                case "solar_flare":
                    // 太阳耀斑：飞行时间+1秒
                    CurrentFlight.totalTime += 1f;
                    break;
                case "derelict":
                    // 废弃飞船：获得资源（简化：日志）
                    break;
                case "wormhole":
                    // 虫洞：飞行时间减半
                    CurrentFlight.totalTime *= 0.5f;
                    break;
            }
        }

        /// <summary>完成飞行</summary>
        private static void CompleteFlight()
        {
            if (CurrentFlight == null) return;

            Debug.Log($"[超神机械师]【太空飞行】飞行完成: {CurrentFlight.fromPlanet} -> {CurrentFlight.toPlanet}");

            string toPlanet = CurrentFlight.toPlanet;
            var passengers = CurrentFlight.passengerIds;

            // 隐藏飞行动画
            HideFlightOverlay();

            // 重置状态
            IsFlying = false;
            CurrentFlight = null;
            _eventQueue.Clear();

            // 通知多世界系统飞行完成，加载目标星球
            SuperMechMultiWorld.OnFlightComplete(toPlanet);

            // 乘客在目标星球生成（延迟到世界加载后）
            // 这里先记录，世界加载完成后由MultiWorld处理
        }

        /// <summary>显示飞行动画覆盖层</summary>
        private static GameObject _flightOverlay;
        private static Image _flightBg;
        private static Text _flightText;
        private static Text _flightProgress;
        private static RectTransform _shipIcon;
        private static RectTransform _pathLine;

        private static void ShowFlightOverlay()
        {
            try
            {
                // 创建覆盖层
                _flightOverlay = new GameObject("SpaceFlightOverlay");
                _flightOverlay.transform.SetParent(SuperMechWindowManager.GetCanvasTransform(), false);

                var rt = _flightOverlay.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // 背景
                _flightBg = _flightOverlay.AddComponent<Image>();
                _flightBg.color = new Color(0f, 0f, 0.05f, 0.95f);
                _flightBg.raycastTarget = true; // 拦截点击

                // 标题
                var titleGo = new GameObject("FlightTitle");
                titleGo.transform.SetParent(_flightOverlay.transform, false);
                var titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 0.85f);
                titleRt.anchorMax = new Vector2(0.5f, 0.85f);
                titleRt.sizeDelta = new Vector2(600, 40);
                _flightText = titleGo.AddComponent<Text>();
                _flightText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _flightText.fontSize = 24;
                _flightText.color = new Color(0.5f, 0.8f, 1f);
                _flightText.alignment = TextAnchor.MiddleCenter;
                _flightText.text = LocalizedTextManager.getText("sm_spaceflight_title");

                // 进度文本
                var progGo = new GameObject("FlightProgress");
                progGo.transform.SetParent(_flightOverlay.transform, false);
                var progRt = progGo.AddComponent<RectTransform>();
                progRt.anchorMin = new Vector2(0.5f, 0.15f);
                progRt.anchorMax = new Vector2(0.5f, 0.15f);
                progRt.sizeDelta = new Vector2(400, 30);
                _flightProgress = progGo.AddComponent<Text>();
                _flightProgress.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _flightProgress.fontSize = 16;
                _flightProgress.color = new Color(0.7f, 0.7f, 0.7f);
                _flightProgress.alignment = TextAnchor.MiddleCenter;
                _flightProgress.text = "0%";

                // 飞船图标
                var shipGo = new GameObject("ShipIcon");
                shipGo.transform.SetParent(_flightOverlay.transform, false);
                _shipIcon = shipGo.AddComponent<RectTransform>();
                _shipIcon.anchorMin = new Vector2(0.1f, 0.5f);
                _shipIcon.anchorMax = new Vector2(0.1f, 0.5f);
                _shipIcon.sizeDelta = new Vector2(32, 32);
                var shipImg = shipGo.AddComponent<Image>();
                shipImg.color = new Color(1f, 0.8f, 0.3f);
                shipImg.raycastTarget = false;

                // 生成星星背景
                GenerateStarfield(_flightOverlay.transform);
            }
            catch (Exception e)
            {
                Debug.LogError("[超神机械师]【太空飞行】显示覆盖层失败: " + e.Message);
            }
        }

        /// <summary>生成星星背景</summary>
        private static void GenerateStarfield(Transform parent)
        {
            System.Random rng = new System.Random();
            for (int i = 0; i < 100; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(parent, false);
                var starRt = starGo.AddComponent<RectTransform>();
                starRt.anchorMin = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
                starRt.anchorMax = starRt.anchorMin;
                float size = rng.Next(1, 4);
                starRt.sizeDelta = new Vector2(size, size);
                var starImg = starGo.AddComponent<Image>();
                starImg.color = new Color(1f, 1f, 1f, (float)rng.NextDouble() * 0.8f + 0.2f);
                starImg.raycastTarget = false;
            }
        }

        /// <summary>更新飞行动画</summary>
        private static void UpdateFlightOverlay(float progress)
        {
            if (_flightProgress != null)
            {
                _flightProgress.text = $"{progress * 100:F0}%";
            }

            if (_shipIcon != null)
            {
                // 飞船从左飞到右
                float x = Mathf.Lerp(0.1f, 0.9f, progress);
                _shipIcon.anchorMin = new Vector2(x, 0.5f);
                _shipIcon.anchorMax = new Vector2(x, 0.5f);
            }

            if (_flightText != null && CurrentFlight != null)
            {
                var from = SuperMechStarSystem.GetPlanet(CurrentFlight.fromPlanet);
                var to = SuperMechStarSystem.GetPlanet(CurrentFlight.toPlanet);
                if (from != null && to != null)
                {
                    _flightText.text = $"{from.name} → {to.name}";
                }
            }
        }

        /// <summary>隐藏飞行动画覆盖层</summary>
        private static void HideFlightOverlay()
        {
            if (_flightOverlay != null)
            {
                UnityEngine.Object.Destroy(_flightOverlay);
                _flightOverlay = null;
                _flightBg = null;
                _flightText = null;
                _flightProgress = null;
                _shipIcon = null;
            }
        }

        /// <summary>清理</summary>
        public static void Clear()
        {
            HideFlightOverlay();
            IsFlying = false;
            CurrentFlight = null;
            _eventQueue.Clear();
        }
    }
}
