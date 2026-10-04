using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>
    /// v0.65.0 跨文明贸易机制（原著向，基于真实势力系统实现）
    /// 原著设定：
    ///  1. 星海中的文明/势力之间开展跨星域贸易，商队往返运送物资；
    ///  2. 敌对关系无法建立贸易通道，已建立的通道在关系恶化后自动暂停；
    ///  3. 商队在途可能遭异兽/劫掠者袭击，商队被摧毁则贸易受损、需要重新出队；
    ///  4. 贸易带来文明发展收益（科技值）与个体成长（潜能），收益随贸易次数累积；
    ///  5. 原著约束（硬编码守卫）：贸易绝不涉及圣所信息档案、原始异能体等核心特殊事物。
    /// 原版适配：商队复用原版单位生成/移动接口，不新增底层实体；存档挂接统一 SaveData。
    /// </summary>
    public static class SuperMechTrade
    {
        public class TradeRoute
        {
            public string id;
            public string factionA;
            public string factionB;
            public int openedAtTick;
            public float progress;      // 0~1 在途商队行程进度
            public long caravanId;      // 当前在途商队单位ID（0=无在途商队）
            public int deliveries;      // 累计完成贸易次数
            public bool active = true;  // 关系敌对时自动暂停
            public int tickCounter;     // 出队冷却计数
        }

        public class TradeSaveData
        {
            public List<TradeRouteEntry> routes = new List<TradeRouteEntry>();
        }

        public class TradeRouteEntry
        {
            public string id;
            public string factionA;
            public string factionB;
            public int openedAtTick;
            public float progress;
            public int deliveries;
            public bool active;
            public int tickCounter;
        }

        private static readonly List<TradeRoute> _routes = new List<TradeRoute>();
        private static readonly Dictionary<long, string> _caravanRoute = new Dictionary<long, string>();

        private const int MaxRoutes = 12;
        private const float CaravanSpeedPerTick = 0.012f; // 每次Tick商队行程进度
        private const int RespawnCooldownTicks = 30;      // 商队抵达/被摧毁后重新出队冷却
        private const float PlunderChance = 0.12f;        // 每次Tick遭劫掠概率
        private const float PlunderRadius = 6f;           // 劫掠者搜索半径（格）
        private const float PlunderDamage = 30f;          // 单次劫掠对商队造成的伤害
        private const float PlunderProgressLoss = 0.25f;  // 劫掠后商队进度损失
        private const int DeliveryPotential = 3;          // 每次交付成员潜能收益
        private const int LeaderPotential = 6;            // 领袖额外潜能收益
        private const float DeliveryTech = 15f;           // 每次交付文明科技值收益
        private const float DeliveryCooldown = 8f;        // 交付后短暂冷却（防止同一tick重复结算）

        private static int _idCounter = 1;

        public static List<TradeRoute> GetAllRoutes() => _routes;

        /// <summary>
        /// 原著约束守卫：圣所信息档案、原始异能体、圣所碎片等核心特殊事物禁止纳入贸易。
        /// 当前贸易只运送通用物资，收益为科技/潜能，本函数供UI与逻辑统一校验。
        /// </summary>
        public static bool IsForbiddenTradeItem(string itemId)
        {
            return itemId == "sanctuary_archive"
                || itemId == "primal_ability"
                || itemId == "sanctuary_fragment"
                || (itemId != null && itemId.StartsWith("primal_"));
        }

        /// <summary>尝试在两个势力之间建立贸易通道</summary>
        public static bool TryOpenRoute(string factionA, string factionB)
        {
            if (!SuperMechConfig.TradeEnabled) return false;
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB) || factionA == factionB) return false;

            var fa = GetFactionById(factionA);
            var fb = GetFactionById(factionB);
            if (fa == null || fb == null) return false;

            // 原著：敌对关系无法建立贸易通道
            if (SuperMechFaction.GetRelation(factionA, factionB) == SuperMechFaction.FactionRelation.Hostile) return false;

            // 双方领袖必须存活
            if (!IsLeaderAlive(fa) || !IsLeaderAlive(fb)) return false;

            // 已有通道检测（双向）
            foreach (var r in _routes)
            {
                if ((r.factionA == factionA && r.factionB == factionB)
                    || (r.factionA == factionB && r.factionB == factionA)) return false;
            }
            if (_routes.Count >= MaxRoutes) return false;

            var route = new TradeRoute
            {
                id = "trade_" + (_idCounter++),
                factionA = factionA,
                factionB = factionB,
                openedAtTick = UnityEngine.Time.frameCount,
                active = true,
                progress = 0f,
                deliveries = 0,
                tickCounter = 0
            };
            _routes.Add(route);
            SpawnCaravan(route);

            SuperMechEventBus.Publish("TradeRouteOpened", new TradeRouteEvent
            {
                routeId = route.id, factionA = factionA, factionB = factionB
            });
            Debug.Log($"[超神机械师] 贸易通道建立：{fa.name} ↔ {fb.name}");
            return true;
        }

        /// <summary>主动中止贸易通道</summary>
        public static void RemoveRoute(string routeId)
        {
            for (int i = _routes.Count - 1; i >= 0; i--)
            {
                var r = _routes[i];
                if (r.id != routeId) continue;
                // 销毁在途商队
                if (r.caravanId != 0)
                {
                    _caravanRoute.Remove(r.caravanId);
                    var c = FindActorById(r.caravanId);
                    if (c != null && c.isAlive()) c.dieAndDestroy(AttackType.Other);
                }
                _routes.RemoveAt(i);
                Debug.Log($"[超神机械师] 贸易通道已中止：{r.id}");
                return;
            }
        }

        /// <summary>获取某王国关联的活跃贸易通道数（用于王国面板显示）</summary>
        public static int GetRouteCountForKingdom(Kingdom k)
        {
            if (k == null) return 0;
            int count = 0;
            foreach (var r in _routes)
            {
                var fa = GetFactionById(r.factionA);
                var fb = GetFactionById(r.factionB);
                if (fa == null || fb == null) continue;
                var ka = SuperMechFaction.GetFactionCivilization(fa);
                var kb = SuperMechFaction.GetFactionCivilization(fb);
                if (ka == k || kb == k) count++;
            }
            return count;
        }

        /// <summary>每Tick更新所有贸易通道（商队移动/劫掠/交付/冷却）</summary>
        public static void TickTradeRoutes(float delta)
        {
            if (!SuperMechConfig.TradeEnabled) return;
            if (World.world == null || World.world.units == null) return;
            var units = World.world.units.units_only_alive;
            if (units == null) return;

            for (int i = _routes.Count - 1; i >= 0; i--)
            {
                var route = _routes[i];
                var fa = GetFactionById(route.factionA);
                var fb = GetFactionById(route.factionB);
                // 势力消亡 → 通道失效
                if (fa == null || fb == null)
                {
                    if (route.caravanId != 0) _caravanRoute.Remove(route.caravanId);
                    _routes.RemoveAt(i);
                    continue;
                }

                // 原著：敌对关系自动封锁通道
                var rel = SuperMechFaction.GetRelation(route.factionA, route.factionB);
                if (rel == SuperMechFaction.FactionRelation.Hostile)
                {
                    route.active = false;
                    route.tickCounter = 0;
                    continue;
                }
                route.active = true;

                Actor caravan = route.caravanId != 0 ? FindActorById(route.caravanId) : null;
                if (caravan == null || !caravan.isAlive())
                {
                    // 商队被摧毁/失踪 → 贸易受损，冷却后重新出队
                    if (route.caravanId != 0) _caravanRoute.Remove(route.caravanId);
                    route.caravanId = 0;
                    route.progress = 0f;
                    route.tickCounter++;
                    if (route.tickCounter >= RespawnCooldownTicks)
                    {
                        route.tickCounter = 0;
                        SpawnCaravan(route);
                    }
                    continue;
                }

                // 冷却/交付结算间隔
                if (route.tickCounter > 0)
                {
                    route.tickCounter--;
                    continue;
                }

                // 商队向目标势力领袖移动
                Actor target = FindActorById(fb.leaderId);
                if (target == null || !target.isAlive() || target.current_tile == null || caravan.current_tile == null)
                    continue;

                float dist = Mathf.Abs(caravan.current_tile.x - target.current_tile.x)
                           + Mathf.Abs(caravan.current_tile.y - target.current_tile.y);
                if (dist > 3f)
                {
                    caravan.moveTo(target.current_tile);
                    route.progress = Mathf.Min(1f, route.progress + CaravanSpeedPerTick * delta);
                }
                else
                {
                    // 抵达 → 交付结算
                    Deliver(route, fa, fb);
                    if (caravan.isAlive()) caravan.dieAndDestroy(AttackType.Other);
                    _caravanRoute.Remove(route.caravanId);
                    route.caravanId = 0;
                    route.progress = 0f;
                    route.tickCounter = RespawnCooldownTicks / 2;
                    continue;
                }

                // 劫掠判定（原著：商队在途可能遭异兽/劫掠者袭击）
                if (route.progress > 0.05f && Random.value < PlunderChance)
                {
                    TryPlunder(route, caravan, units);
                }
            }
        }

        private static void Deliver(TradeRoute route, SuperMechFaction.FactionData fa, SuperMechFaction.FactionData fb)
        {
            // 成员潜能收益（每人限一次，遍历全部成员，上限保护性能）
            int processed = 0;
            foreach (var mid in fa.memberIds)
            {
                if (processed >= 20) break;
                var m = FindActorById(mid);
                if (m == null || !m.isAlive()) continue;
                SuperMechPotential.AddPotential(m, m.id == fa.leaderId ? LeaderPotential : DeliveryPotential);
                processed++;
            }
            processed = 0;
            foreach (var mid in fb.memberIds)
            {
                if (processed >= 20) break;
                var m = FindActorById(mid);
                if (m == null || !m.isAlive()) continue;
                SuperMechPotential.AddPotential(m, m.id == fb.leaderId ? LeaderPotential : DeliveryPotential);
                processed++;
            }

            // 文明科技收益
            var ka = SuperMechFaction.GetFactionCivilization(fa);
            var kb = SuperMechFaction.GetFactionCivilization(fb);
            if (ka != null && !ka.wild) SuperMechCivilization.AddTechPoints(ka, DeliveryTech);
            if (kb != null && !kb.wild) SuperMechCivilization.AddTechPoints(kb, DeliveryTech);

            route.deliveries++;
            SuperMechEventBus.Publish("TradeDelivered", new TradeRouteEvent
            {
                routeId = route.id, factionA = route.factionA, factionB = route.factionB
            });
            Debug.Log($"[超神机械师] 贸易交付完成：{fa.name} ↔ {fb.name}（第{route.deliveries}次）");
        }

        private static void TryPlunder(TradeRoute route, Actor caravan, List<Actor> units)
        {
            if (caravan.current_tile == null) return;
            foreach (var a in units)
            {
                if (a == null || !a.isAlive() || a.id == caravan.id) continue;
                if (a.current_tile == null) continue;
                float dist = Mathf.Abs(a.current_tile.x - caravan.current_tile.x)
                           + Mathf.Abs(a.current_tile.y - caravan.current_tile.y);
                if (dist > PlunderRadius) continue;
                // 只劫掠异势力、有一定战力的单位（避免误伤友军/平民）
                if (a.kingdom == caravan.kingdom) continue;
                int rank = SuperMechAdvancement.GetExactRankIndex(a);
                if (rank < 6) continue;

                // 劫掠生效
                caravan.data.health -= (int)PlunderDamage;
                route.progress = Mathf.Max(0f, route.progress - PlunderProgressLoss);
                if (caravan.data.health <= 0)
                {
                    caravan.dieAndDestroy(AttackType.Other);
                    _caravanRoute.Remove(route.caravanId);
                    route.caravanId = 0;
                    route.progress = 0f;
                    route.tickCounter = RespawnCooldownTicks;
                    Debug.Log($"[超神机械师] 贸易商队被劫掠摧毁！通道{route.id}受损，等待重新出队");
                }
                else
                {
                    Debug.Log($"[超神机械师] 贸易商队遭袭，货损{PlunderProgressLoss * 100:F0}%");
                }
                return; // 每tick至多一次劫掠结算
            }
        }

        private static void SpawnCaravan(TradeRoute route)
        {
            if (World.world == null || World.world.units == null) return;
            var fa = GetFactionById(route.factionA);
            if (fa == null) return;
            Actor leader = FindActorById(fa.leaderId);
            WorldTile tile = (leader != null && leader.current_tile != null) ? leader.current_tile : FindLandTile();
            if (tile == null) return;

            Actor caravan = World.world.units.spawnNewUnit("human", tile, false, true, 6f, null, false, true);
            if (caravan == null) return;
            caravan.name = "贸易商队";
            _caravanRoute[caravan.id] = route.id;
            route.caravanId = caravan.id;
            route.progress = 0f;
        }

        private static WorldTile FindLandTile()
        {
            if (World.world == null) return null;
            int cx = MapBox.width / 2, cy = MapBox.height / 2;
            for (int r = 0; r < 40; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int x = cx + dx, y = cy + dy;
                        if (x < 0 || y < 0 || x >= MapBox.width || y >= MapBox.height) continue;
                        WorldTile t = World.world.GetTile(x, y);
                        if (t != null && t.Type != TileLibrary.deep_ocean && t.Type != TileLibrary.close_ocean && t.Type != TileLibrary.mountains)
                            return t;
                    }
                }
            }
            return null;
        }

        private static SuperMechFaction.FactionData GetFactionById(string id)
        {
            foreach (var f in SuperMechFaction.GetAllFactions())
                if (f.id == id) return f;
            return null;
        }

        private static bool IsLeaderAlive(SuperMechFaction.FactionData f)
        {
            var leader = FindActorById(f.leaderId);
            return leader != null && leader.isAlive();
        }

        private static Actor FindActorById(long id)
        {
            if (World.world == null || World.world.units == null) return null;
            var units = World.world.units.units_only_alive;
            if (units == null) return null;
            foreach (var a in units)
                if (a != null && a.id == id) return a;
            return null;
        }

        public static TradeSaveData Save()
        {
            var data = new TradeSaveData();
            foreach (var r in _routes)
            {
                data.routes.Add(new TradeRouteEntry
                {
                    id = r.id, factionA = r.factionA, factionB = r.factionB,
                    openedAtTick = r.openedAtTick, progress = r.progress,
                    deliveries = r.deliveries, active = r.active, tickCounter = r.tickCounter
                });
            }
            return data;
        }

        public static void Load(TradeSaveData data)
        {
            _routes.Clear();
            _caravanRoute.Clear();
            if (data == null || data.routes == null) return;
            foreach (var e in data.routes)
            {
                _routes.Add(new TradeRoute
                {
                    id = e.id, factionA = e.factionA, factionB = e.factionB,
                    openedAtTick = e.openedAtTick, progress = e.progress,
                    deliveries = e.deliveries, active = e.active, tickCounter = e.tickCounter,
                    caravanId = 0 // 在途商队不跨存档恢复，读档后冷却重新出队
                });
            }
        }

        public static void Clear()
        {
            _routes.Clear();
            _caravanRoute.Clear();
        }

        public static int CleanupDead(HashSet<long> alive)
        {
            int removed = 0;
            var deadCaravanIds = new List<long>();
            foreach (var kv in _caravanRoute)
                if (!alive.Contains(kv.Key)) deadCaravanIds.Add(kv.Key);
            foreach (var id in deadCaravanIds)
            {
                _caravanRoute.Remove(id);
                removed++;
            }
            return removed;
        }
    }

    /// <summary>贸易事件（事件总线）</summary>
    public class TradeRouteEvent
    {
        public string routeId;
        public string factionA;
        public string factionB;
    }
}
