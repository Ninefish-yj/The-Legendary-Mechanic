using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperMech.Code
{
    public class SMKnowledgeGraph3D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float Radius = 90f;
        private const float NodeScaleMin = 0.7f;
        private const float NodeScaleMax = 1.3f;
        private const float DragSpeed = 0.3f;

        private static readonly Color ColorLocked = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        private static readonly Color ColorUnlockable = new Color(0.3f, 0.5f, 0.9f, 0.9f);
        private static readonly Color ColorUnlocked = new Color(0.9f, 0.75f, 0.2f, 1f);
        private static readonly Color ColorAxonDefault = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color ColorAxonActive = new Color(0.3f, 0.8f, 1f, 0.4f);
        private static readonly Color ColorImpulse = new Color(0.5f, 0.9f, 1f, 1f);

        public class KnowledgeNode
        {
            public string id;
            public string name;
            public string icon;
            public int tier; // 0=基础,1=进阶,2=高端,3=尖端,4=终极
            public bool unlocked;
            public bool unlockable;
            public int cost;
            public Vector3 spherePos;
            public GameObject gameObject;
            public Image image;
            public Button button;
            public float spawnTimer; // 自动冲动生成计时器
        }

        private class Axon
        {
            public KnowledgeNode from;
            public KnowledgeNode to;
            public GameObject lineObj;
            public Image lineImage;
            public bool active;
        }

        private class NerveImpulse
        {
            public Axon axon;
            public GameObject obj;
            public Image image;
            public float progress;
            public float speed;
            public int wave; // 剩余分裂次数
            public KnowledgeNode source; // 来源节点（避免往回传）
        }

        private List<KnowledgeNode> _nodes = new List<KnowledgeNode>();
        private List<Axon> _axons = new List<Axon>();
        private List<NerveImpulse> _impulses = new List<NerveImpulse>();

        private GameObject _graphContainer; // 旋转的容器
        private GameObject _nodesParent;
        private GameObject _axonsParent;
        private GameObject _impulsesParent;

        private bool _isDragging;
        private Vector2 _lastDragPos;
        private float _rotationY;
        private float _rotationX;
        private float _targetRotationY;
        private float _targetRotationX;
        private float _axonHighlight; // 轴突高亮强度（拖拽时增加）

        private Actor _actor;
        private string _prefix;

        public void Init(Actor actor, string prefix, Transform parent)
        {
            _actor = actor;
            _prefix = prefix;

            Clear();

            _graphContainer = new GameObject("Graph3D", typeof(RectTransform));
            _graphContainer.transform.SetParent(parent, false);
            RectTransform grt = _graphContainer.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.localPosition = Vector3.zero;
            float w = parent.GetComponent<RectTransform>().rect.width;
            float h = parent.GetComponent<RectTransform>().rect.height;
            if (w <= 10f) w = 340f;
            if (h <= 10f) h = 220f;
            grt.sizeDelta = new Vector2(w, h);
            Debug.Log($"[超神机械师] 3D知识图谱初始化: prefix={_prefix}, 容器大小={w}x{h}, 知识数={SuperMechKnowledge.GetAllByPrefix(_prefix)?.Count ?? 0}");

            CreateBackground(_graphContainer.transform);

            _axonsParent = new GameObject("Axons", typeof(RectTransform));
            _axonsParent.transform.SetParent(_graphContainer.transform, false);
            _nodesParent = new GameObject("Nodes", typeof(RectTransform));
            _nodesParent.transform.SetParent(_graphContainer.transform, false);
            _impulsesParent = new GameObject("Impulses", typeof(RectTransform));
            _impulsesParent.transform.SetParent(_graphContainer.transform, false);

            foreach (Transform t in new[] { _axonsParent.transform, _nodesParent.transform, _impulsesParent.transform })
            {
                RectTransform rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.localPosition = Vector3.zero;
                rt.sizeDelta = _graphContainer.GetComponent<RectTransform>().sizeDelta;
            }

            GenerateNodes();
            GenerateAxons();
            _rotationY = 30f;
            _rotationX = 15f;
            _targetRotationY = _rotationY;
            _targetRotationX = _rotationX;
            UpdateNodes();
            UpdateAxons();
            UpdateGraphTransform();
        }

        private void CreateBackground(Transform parent)
        {
            Color themeColor = GetThemeColor(_prefix);
            System.Random rng = new System.Random(_prefix.GetHashCode() + 42);

            GameObject bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(parent, false);
            bgGo.transform.SetAsFirstSibling();
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.04f, 0.07f, 0.95f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            GameObject glowGo = new GameObject("CenterGlow", typeof(RectTransform));
            glowGo.transform.SetParent(bgGo.transform, false);
            Image glowImg = glowGo.AddComponent<Image>();
            Color glowColor = themeColor;
            glowColor.a = 0.06f;
            glowImg.color = glowColor;
            glowImg.raycastTarget = false;
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(200f, 200f);

            int starCount = 30;
            for (int i = 0; i < starCount; i++)
            {
                GameObject star = new GameObject("Star_" + i, typeof(RectTransform));
                star.transform.SetParent(bgGo.transform, false);
                Image starImg = star.AddComponent<Image>();
                Color starColor = (rng.Next(0, 3) == 0) ? themeColor : Color.white;
                starColor.a = 0.1f + (float)rng.NextDouble() * 0.3f;
                starImg.color = starColor;
                starImg.raycastTarget = false;
                RectTransform starRt = star.GetComponent<RectTransform>();
                starRt.anchorMin = new Vector2(0f, 0f);
                starRt.anchorMax = new Vector2(0f, 0f);
                starRt.pivot = new Vector2(0.5f, 0.5f);
                float x = (float)rng.NextDouble() * 400f - 200f;
                float y = (float)rng.NextDouble() * 200f - 100f;
                starRt.anchoredPosition = new Vector2(x, y);
                float size = 0.8f + (float)rng.NextDouble() * 1.5f;
                starRt.sizeDelta = new Vector2(size, size);
            }

            GameObject labelGo = new GameObject("ThemeLabel", typeof(RectTransform));
            labelGo.transform.SetParent(bgGo.transform, false);
            Text labelText = labelGo.AddComponent<Text>();
            labelText.text = GetThemeName(_prefix);
            labelText.fontSize = 11;
            labelText.color = themeColor;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.fontStyle = FontStyle.Bold;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (labelText.font == null) labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.anchoredPosition = new Vector2(6f, -4f);
            labelRt.sizeDelta = new Vector2(100f, 16f);
        }

        private Color GetThemeColor(string prefix)
        {
            switch (prefix)
            {
                case "mech": return new Color(0.4f, 0.7f, 1f);    // 机械系-蓝
                case "martial": return new Color(1f, 0.5f, 0.3f);  // 武道系-橙
                case "psi": return new Color(0.8f, 0.4f, 1f);      // 异能系-紫
                case "mage": return new Color(0.4f, 1f, 0.6f);     // 魔法系-绿
                case "mind": return new Color(1f, 0.8f, 0.3f);     // 念力系-金
                default: return new Color(0.6f, 0.6f, 0.6f);
            }
        }

        private Color GetTierColor(int tier)
        {
            switch (tier)
            {
                case 0: return new Color(0.6f, 0.6f, 0.65f);  // 基础-灰
                case 1: return new Color(0.4f, 0.8f, 0.5f);   // 进阶-绿
                case 2: return new Color(0.4f, 0.6f, 1f);    // 高端-蓝
                case 3: return new Color(0.8f, 0.4f, 1f);    // 尖端-紫
                case 4: return new Color(1f, 0.8f, 0.3f);    // 终极-金
                default: return new Color(0.6f, 0.6f, 0.65f);
            }
        }

        private string GetThemeName(string prefix)
        {
            switch (prefix)
            {
                case "mech": return LocalizedTextManager.getText("sm_tree_mech");
                case "martial": return LocalizedTextManager.getText("sm_tree_martial");                case "psi": return LocalizedTextManager.getText("sm_tree_psi");
                case "mage": return LocalizedTextManager.getText("sm_tree_mage");
                case "mind": return LocalizedTextManager.getText("sm_tree_mind");
                default: return LocalizedTextManager.getText("sm_tree_generic");
            }
        }

        private void GenerateNodes()
        {
            List<SuperMechKnowledge.KnowledgeDef> allKnowledge = SuperMechKnowledge.GetAllByPrefix(_prefix);
            if (allKnowledge == null || allKnowledge.Count == 0) return;

            int total = allKnowledge.Count;
            for (int i = 0; i < total; i++)
            {
                var def = allKnowledge[i];
                KnowledgeNode node = new KnowledgeNode
                {
                    id = def.id,
                    name = LocalizedTextManager.getText(def.name),
                    icon = def.icon,
                    tier = def.tier,
                    unlocked = SuperMechKnowledge.IsUnlocked(_actor, def.id),
                    cost = def.cost
                };
                int actualCost = SuperMechPotential.GetActualCost(_actor, def.id, def.cost);
                node.unlockable = !node.unlocked && SuperMechPotential.GetPotential(_actor) >= actualCost;

                float tierRadius = Radius * (0.7f + node.tier * 0.08f);
                node.spherePos = GetPositionOnSphere(i, total, tierRadius);

                CreateNodeGameObject(node);
                _nodes.Add(node);
            }
        }

        private Vector3 GetPositionOnSphere(int index, int total, float radius)
        {
            float phi = Mathf.Acos(1f - (float)(2 * (index + 1)) / (float)total);
            float theta = Mathf.PI * (1f + Mathf.Sqrt(5f)) * (float)index;
            float x = radius * Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = radius * Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = radius * Mathf.Cos(phi);
            return new Vector3(x, y, z);
        }

        private void CreateNodeGameObject(KnowledgeNode node)
        {
            GameObject go = new GameObject("Node_" + node.id, typeof(RectTransform));
            go.transform.SetParent(_nodesParent.transform, false);

            float nodeSize = 32f + node.tier * 4f;

            if (node.unlocked)
            {
                GameObject glowGo = new GameObject("Glow", typeof(RectTransform));
                glowGo.transform.SetParent(go.transform, false);
                Image glowImg = glowGo.AddComponent<Image>();
                Color glowColor = GetTierColor(node.tier);
                glowColor.a = 0.25f;
                glowImg.color = glowColor;
                glowImg.raycastTarget = false;
                RectTransform glowRt = glowGo.GetComponent<RectTransform>();
                glowRt.anchorMin = new Vector2(0.5f, 0.5f);
                glowRt.anchorMax = new Vector2(0.5f, 0.5f);
                glowRt.pivot = new Vector2(0.5f, 0.5f);
                glowRt.sizeDelta = new Vector2(nodeSize * 1.8f, nodeSize * 1.8f);
            }

            GameObject borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(go.transform, false);
            Image borderImg = borderGo.AddComponent<Image>();
            Color borderColor;
            if (node.unlocked) borderColor = GetTierColor(node.tier);
            else if (node.unlockable) borderColor = new Color(0.4f, 0.7f, 1f);
            else borderColor = new Color(0.3f, 0.3f, 0.35f);
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0.5f, 0.5f);
            borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.pivot = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(nodeSize + 4, nodeSize + 4);

            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.1f, 0.9f);
            bgImg.raycastTarget = false;
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(nodeSize, nodeSize);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            Image iconImg = iconGo.AddComponent<Image>();
            if (!string.IsNullOrEmpty(node.icon))
            {
                try { iconImg.sprite = SpriteTextureLoader.getSprite(node.icon); } catch { }
            }
            if (iconImg.sprite == null)
            {
                iconImg.sprite = SpriteTextureLoader.getSprite("ui/Icons/actor_traits/iconStrong");
            }
            iconImg.color = node.unlocked ? Color.white : new Color(1f, 1f, 1f, node.unlockable ? 0.85f : 0.35f);
            iconImg.raycastTarget = false;
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(nodeSize * 0.65f, nodeSize * 0.65f);

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.15f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            btn.colors = cb;
            btn.targetGraphic = bgImg;

            string nodeId = node.id;
            btn.onClick.AddListener(() =>
            {
                if (SuperMechKnowledge.Unlock(_actor, nodeId))
                {
                    foreach (var n in _nodes)
                    {
                        if (n.id == nodeId)
                        {
                            n.unlocked = true;
                            n.unlockable = false;
                            UpdateNodeVisual(n);
                        }
                        else
                        {
                            n.unlocked = SuperMechKnowledge.IsUnlocked(_actor, n.id);
                            int aCost = SuperMechPotential.GetActualCost(_actor, n.id, n.cost);
                            n.unlockable = !n.unlocked && SuperMechPotential.GetPotential(_actor) >= aCost;
                            UpdateNodeVisual(n);
                        }
                    }
                    UpdateAxonsVisual();
                }
            });

            TipButton tip = go.AddComponent<TipButton>();
            tip.textOnClick = $"{LocalizedTextManager.getText(node.name)}\n{LocalizedTextManager.getText("sm_graph_tier")}: {GetTierName(node.tier)}\n{LocalizedTextManager.getText("sm_graph_cost")}: {node.cost}{LocalizedTextManager.getText("sm_graph_potential")}\n{(node.unlocked ? LocalizedTextManager.getText("sm_graph_unlocked") : (node.unlockable ? LocalizedTextManager.getText("sm_graph_click_unlock") : LocalizedTextManager.getText("sm_graph_locked")))}";

            node.gameObject = go;
            node.image = iconImg;
            node.button = btn;
        }

        private void UpdateNodeVisual(KnowledgeNode node)
        {
            if (node.gameObject == null) return;

            float depth = 0.5f;
            if (node.gameObject != null)
            {
                depth = Mathf.InverseLerp(-Radius, Radius, node.gameObject.transform.localPosition.z);
            }

            Image border = node.gameObject.transform.Find("Border")?.GetComponent<Image>();
            if (border != null)
            {
                Color baseColor;
                if (node.unlocked) baseColor = GetTierColor(node.tier);
                else if (node.unlockable) baseColor = new Color(0.4f, 0.7f, 1f);
                else baseColor = new Color(0.25f, 0.25f, 0.3f);

                float brightness = 0.4f + depth * 0.6f;
                border.color = new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);
            }

            if (node.image != null)
            {
                float baseAlpha = node.unlocked ? 1f : (node.unlockable ? 0.85f : 0.35f);
                float depthAlpha = 0.5f + depth * 0.5f;
                node.image.color = new Color(1f, 1f, 1f, baseAlpha * depthAlpha);
            }

            Transform glow = node.gameObject.transform.Find("Glow");
            if (glow != null)
            {
                glow.gameObject.SetActive(node.unlocked);
                Image glowImg = glow.GetComponent<Image>();
                if (glowImg != null)
                {
                    Color gc = glowImg.color;
                    gc.a = node.unlocked ? (0.3f + depth * 0.5f) : 0f;
                    glowImg.color = gc;
                }
            }
        }

        private void GenerateAxons()
        {
            if (_nodes.Count < 2) return;

            float maxDist = 250f / Mathf.Sqrt(_nodes.Count) * 1.5f;
            for (int i = 0; i < _nodes.Count - 1; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    float d = Vector3.Distance(_nodes[i].spherePos, _nodes[j].spherePos);
                    if (d <= maxDist)
                    {
                        if (_axons.Exists(a =>
                            (a.from == _nodes[i] && a.to == _nodes[j]) ||
                            (a.from == _nodes[j] && a.to == _nodes[i])))
                            continue;

                        Axon axon = new Axon
                        {
                            from = _nodes[i],
                            to = _nodes[j],
                            active = _nodes[i].unlocked && _nodes[j].unlocked
                        };
                        CreateAxonGameObject(axon);
                        _axons.Add(axon);
                    }
                }
            }
        }

        private void CreateAxonGameObject(Axon axon)
        {
            GameObject go = new GameObject("Axon", typeof(RectTransform));
            go.transform.SetParent(_axonsParent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = axon.active ? ColorAxonActive : ColorAxonDefault;
            img.raycastTarget = false;
            axon.lineObj = go;
            axon.lineImage = img;
        }

        private void UpdateAxons()
        {
            foreach (var axon in _axons)
            {
                if (axon.lineObj == null || axon.from.gameObject == null || axon.to.gameObject == null) continue;

                Vector3 fromPos = axon.from.gameObject.transform.localPosition;
                Vector3 toPos = axon.to.gameObject.transform.localPosition;
                Vector3 mid = (fromPos + toPos) / 2f;
                float dist = Vector3.Distance(fromPos, toPos);
                float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;

                RectTransform rt = axon.lineObj.GetComponent<RectTransform>();
                rt.localPosition = mid;
                rt.sizeDelta = new Vector2(dist, 2f);
                rt.localRotation = Quaternion.Euler(0, 0, angle);

                float avgZ = (fromPos.z + toPos.z) / 2f;
                float alpha = Mathf.InverseLerp(-Radius, Radius, avgZ);
                Color baseColor = axon.active ? ColorAxonActive : ColorAxonDefault;
                if (_axonHighlight > 0.01f)
                {
                    baseColor = Color.Lerp(baseColor, new Color(0.3f, 1f, 1f, 0.6f), _axonHighlight);
                }
                baseColor.a *= 0.3f + alpha * 0.7f;
                axon.lineImage.color = baseColor;
            }
        }

        private void UpdateAxonsVisual()
        {
            foreach (var axon in _axons)
            {
                axon.active = axon.from.unlocked && axon.to.unlocked;
                if (axon.active && Random.value < 0.3f)
                {
                    SpawnImpulse(axon);
                }
            }
        }

        private void SpawnImpulse(Axon axon, int wave = 2, KnowledgeNode source = null)
        {
            if (_impulses.Count > 40) return; // 限制数量

            GameObject go = new GameObject("Impulse", typeof(RectTransform));
            go.transform.SetParent(_impulsesParent.transform, false);
            Image img = go.AddComponent<Image>();
            img.color = ColorImpulse;
            img.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(6, 6);

            NerveImpulse impulse = new NerveImpulse
            {
                axon = axon,
                obj = go,
                image = img,
                progress = 0f,
                speed = Random.Range(0.8f, 1.8f),
                wave = wave,
                source = source
            };
            _impulses.Add(impulse);
        }

        private void UpdateImpulses()
        {
            for (int i = _impulses.Count - 1; i >= 0; i--)
            {
                var imp = _impulses[i];
                if (imp.axon.lineObj == null || imp.axon.from.gameObject == null || imp.axon.to.gameObject == null)
                {
                    Destroy(imp.obj);
                    _impulses.RemoveAt(i);
                    continue;
                }

                imp.progress += Time.deltaTime * imp.speed;
                if (imp.progress >= 1f)
                {
                    KnowledgeNode target = (imp.source == imp.axon.from) ? imp.axon.to : imp.axon.from;
                    Destroy(imp.obj);
                    _impulses.RemoveAt(i);

                    if (imp.wave > 0 && target != null && target.unlocked)
                    {
                        FireImpulseFromNode(target, imp.wave - 1, imp.source);
                    }
                    continue;
                }

                Vector3 fromPos = imp.axon.from.gameObject.transform.localPosition;
                Vector3 toPos = imp.axon.to.gameObject.transform.localPosition;
                Vector3 pos = Vector3.Lerp(fromPos, toPos, imp.progress);
                imp.obj.transform.localPosition = pos;

                float pulse = 0.5f + Mathf.Sin(Time.time * 10f) * 0.5f;
                Color c = ColorImpulse;
                c.a *= pulse;
                imp.image.color = c;
            }
        }

        private void FireImpulseFromNode(KnowledgeNode node, int wave, KnowledgeNode ignore = null)
        {
            List<Axon> connected = new List<Axon>();
            foreach (var axon in _axons)
            {
                if ((axon.from == node || axon.to == node) && axon.active)
                {
                    KnowledgeNode other = (axon.from == node) ? axon.to : axon.from;
                    if (other != ignore && other.unlocked)
                        connected.Add(axon);
                }
            }
            if (connected.Count == 0) return;

            int count = Mathf.Min(connected.Count, Random.Range(1, 3));
            for (int i = 0; i < count; i++)
            {
                Axon axon = connected[Random.Range(0, connected.Count)];
                SpawnImpulse(axon, wave, node);
            }
        }

        private void UpdateNodes()
        {
            foreach (var node in _nodes)
            {
                if (node.gameObject == null) continue;

                Vector3 rotated = ApplyRotation(node.spherePos);
                node.gameObject.transform.localPosition = rotated;

                float depth = Mathf.InverseLerp(-Radius, Radius, rotated.z);
                float scale = NodeScaleMin + depth * (NodeScaleMax - NodeScaleMin);
                node.gameObject.transform.localScale = Vector3.one * scale;

                node.gameObject.transform.SetSiblingIndex(Mathf.RoundToInt(depth * 100));

                UpdateNodeVisual(node);
            }
        }

        private Vector3 ApplyRotation(Vector3 pos)
        {
            float cosY = Mathf.Cos(_rotationY * Mathf.Deg2Rad);
            float sinY = Mathf.Sin(_rotationY * Mathf.Deg2Rad);
            float x1 = pos.x * cosY - pos.z * sinY;
            float z1 = pos.x * sinY + pos.z * cosY;

            float cosX = Mathf.Cos(_rotationX * Mathf.Deg2Rad);
            float sinX = Mathf.Sin(_rotationX * Mathf.Deg2Rad);
            float y2 = pos.y * cosX - z1 * sinX;
            float z2 = pos.y * sinX + z1 * cosX;

            return new Vector3(x1, y2, z2);
        }

        private void UpdateGraphTransform()
        {
        }

        void Update()
        {
            if (!_isDragging)
            {
                _rotationY = Mathf.Lerp(_rotationY, _targetRotationY, Time.deltaTime * 5f);
                _rotationX = Mathf.Lerp(_rotationX, _targetRotationX, Time.deltaTime * 5f);
                _targetRotationY += Time.deltaTime * 2f;
            }

            _axonHighlight = Mathf.Lerp(_axonHighlight, 0f, Time.deltaTime * 2f);

            UpdateAutoImpulseSpawn();

            UpdateNodes();
            UpdateAxons();
            UpdateImpulses();
        }

        private void UpdateAutoImpulseSpawn()
        {
            foreach (var node in _nodes)
            {
                if (!node.unlocked) continue;
                node.spawnTimer -= Time.deltaTime;
                if (node.spawnTimer <= 0f)
                {
                    node.spawnTimer = Random.Range(3f, 8f); // 3-8秒生成一次
                    FireImpulseFromNode(node, 1);
                }
            }
        }

        private void FireImpulseBurst()
        {
            int count = 0;
            foreach (var node in _nodes)
            {
                if (node.unlocked && count < 5)
                {
                    FireImpulseFromNode(node, 2);
                    count++;
                }
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _lastDragPos = eventData.position;
            _axonHighlight = 0.5f; // 拖拽开始时高亮轴突
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            Vector2 delta = eventData.position - _lastDragPos;
            _targetRotationY += delta.x * DragSpeed;
            _targetRotationX -= delta.y * DragSpeed;
            _targetRotationX = Mathf.Clamp(_targetRotationX, -60f, 60f);
            _lastDragPos = eventData.position;
            _axonHighlight = Mathf.Max(_axonHighlight, 0.4f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            FireImpulseBurst();
        }

        private string GetTierName(int tier)
        {
            string[] names = { LocalizedTextManager.getText("sm_tier_basic"), LocalizedTextManager.getText("sm_tier_advanced"), LocalizedTextManager.getText("sm_tier_high"), LocalizedTextManager.getText("sm_tier_cutting"), LocalizedTextManager.getText("sm_tier_ultimate") };
            return tier >= 0 && tier < names.Length ? names[tier] : LocalizedTextManager.getText("sm_tier_unknown");
        }

        public void Clear()
        {
            if (_graphContainer != null)
            {
                Destroy(_graphContainer);
                _graphContainer = null;
            }
            _nodes.Clear();
            _axons.Clear();
            _impulses.Clear();
        }
    }
}
