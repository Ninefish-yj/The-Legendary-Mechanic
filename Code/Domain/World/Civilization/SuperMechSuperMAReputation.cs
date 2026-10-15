using System.Collections.Generic;
using UnityEngine;

namespace SuperMech.Code
{
    /// <summary>v0.199 超A协会声望与商店系统
    /// MMO方向：玩家通过击杀高阶敌人获得协会声望，可在协会商店兑换装备和材料</summary>
    public static class SuperMechSuperMAReputation
    {
        // 玩家协会声望
        private static int _playerReputation = 0;
        public static int PlayerReputation => _playerReputation;

        // 商店物品定义
        public class ShopItem
        {
            public string id;
            public string nameKey;
            public string descKey;
            public int cost;          // 声望消耗
            public string rewardType; // "equip" / "material" / "inal"
            public string rewardId;   // 装备ID或材料ID
            public int rewardAmount;  // 数量
        }

        public static readonly List<ShopItem> ShopItems = new List<ShopItem>
        {
            new ShopItem { id="shop_blue_helm", nameKey="sm_shop_blue_helm", descKey="sm_shop_blue_helm_desc", cost=100, rewardType="equip", rewardId="sm_eq_blue_helmet", rewardAmount=1 },
            new ShopItem { id="shop_lp_armor", nameKey="sm_shop_lp_armor", descKey="sm_shop_lp_armor_desc", cost=300, rewardType="equip", rewardId="sm_eq_lp_armor", rewardAmount=1 },
            new ShopItem { id="shop_purple_weapon", nameKey="sm_shop_purple_weapon", descKey="sm_shop_purple_weapon_desc", cost=500, rewardType="equip", rewardId="sm_eq_purple_weapon", rewardAmount=1 },
            new ShopItem { id="shop_key_material", nameKey="sm_shop_key_mat", descKey="sm_shop_key_mat_desc", cost=200, rewardType="material", rewardId="sanctuary_key", rewardAmount=3 },
            new ShopItem { id="shop_inal", nameKey="sm_shop_inal", descKey="sm_shop_inal_desc", cost=50, rewardType="inal", rewardId=null, rewardAmount=5000 },
        };

        /// <summary>增加玩家声望（击杀S阶及以上敌人时调用）</summary>
        public static void AddReputation(Actor killer, int amount)
        {
            if (killer == null) return;
            // 只有玩家单位能获得声望（降临者）
            if (!SuperMechPlayer.IsPlayer(killer)) return;
            _playerReputation += amount;
            Debug.Log($"[超神机械师] 协会声望+{amount}，当前{_playerReputation}");
        }

        /// <summary>购买商店物品</summary>
        public static bool BuyItem(Actor player, string shopItemId)
        {
            if (player == null || !SuperMechPlayer.IsPlayer(player)) return false;
            var item = ShopItems.Find(x => x.id == shopItemId);
            if (item == null) return false;
            if (_playerReputation < item.cost) return false;

            _playerReputation -= item.cost;

            switch (item.rewardType)
            {
                case "equip":
                    SuperMechEquipBag.AddToBag(player, item.rewardId);
                    break;
                case "material":
                    SuperMechSanctuary.GrantKeyMaterials(item.rewardAmount, "协会商店兑换");
                    break;
                case "inal":
                    SuperMechInal.AddInal(player, item.rewardAmount);
                    break;
            }
            return true;
        }

        /// <summary>根据击杀目标阶位计算声望奖励</summary>
        public static int GetKillReputation(Actor target)
        {
            if (target == null) return 0;
            int rank = SuperMechAdvancement.GetExactRankIndex(target);
            if (rank < 8) return 0;      // A阶以下无声望
            if (rank < 10) return 10;    // A阶=10声望
            if (rank < 12) return 50;    // S阶=50声望
            return 200;                   // 超A=200声望
        }
    }
}
