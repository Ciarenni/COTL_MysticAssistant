using System.Collections.Generic;

namespace MysticAssistant
{
    internal static class MysticAssistantInventoryInfo
    {
        private const int MAX_COUNT_DARK_NECKLACE = 1;
        private const int MAX_COUNT_LIGHT_NECKLACE = 1;
        private const int MAX_COUNT_DOCTRINE_STONE = 24;//max is 24 as of the unholy alliance update
        private const int MAX_COUNT_TALISMAN_PIECES = 12;//max is 12 as of the unholy alliance update

        public static bool CheckForBoughtQuantityWarning(TraderTrackerItems chosenItem)
        {
            //check what the player is buying to see if they need to be warned about it
            switch (chosenItem.itemForTrade)
            {

                case InventoryItem.ITEM_TYPE.Necklace_Dark:
                    if (DataManager.Instance.HasAymSkin || Inventory.GetItemQuantity(InventoryItem.ITEM_TYPE.Necklace_Dark) >= MAX_COUNT_DARK_NECKLACE)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.Necklace_Light:
                    if (DataManager.Instance.HasBaalSkin || Inventory.GetItemQuantity(InventoryItem.ITEM_TYPE.Necklace_Light) >= MAX_COUNT_LIGHT_NECKLACE)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE:
                    if (DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop >= MAX_COUNT_DOCTRINE_STONE)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.TALISMAN:
                    if (DataManager.Instance.TalismanPiecesReceivedFromMysticShop > MAX_COUNT_TALISMAN_PIECES)
                    {
                        return true;
                    }
                    break;
                default:
                    //not one of the normally limited items i am making infinitely available, no warning needed
                    return false;
            }

            return false;
        }

        //create the non-inventory item objects for everything that will be for sale in the mod shop
        //prices are in terms of god tears
        public static List<TraderTrackerItems> GetMysticAssistantShopItemTypeList()
        {
            TraderTrackerItems necklaceDarkTTI = new TraderTrackerItems
            {
                //item_type id 124
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Dark,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                //i dont think this LastDayChecked is actually doing anything in this instance.
                //i believe its used for the offering chest in camp (where the player can sell items) to adjust prices based on last time an item was sold
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceLightTTI = new TraderTrackerItems
            {
                //item_type id 125
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Light,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceGoldSkullTTI = new TraderTrackerItems
            {
                //item_type id 127
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Gold_Skull,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceDemonicTTI = new TraderTrackerItems
            {
                //item_type id 123
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Demonic,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceLoyaltyTTI = new TraderTrackerItems
            {
                //item_type id 122
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Loyalty,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceMissionaryTTI = new TraderTrackerItems
            {
                //item_type id 126
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Missionary,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems crystalDoctrineStoneTTI = new TraderTrackerItems
            {
                //item_type id 121
                itemForTrade = InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems talismanTTI = new TraderTrackerItems
            {
                //item_type id 114
                itemForTrade = InventoryItem.ITEM_TYPE.TALISMAN,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems followerSkinTTI = new TraderTrackerItems
            {
                //skins are added to the player in DataManager SetFollowerSkinUnlocked
                //item_type id 52
                itemForTrade = InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems decorationTTI = new TraderTrackerItems
            {
                //skins are added to the player in DataManager SetFollowerSkinUnlocked
                //item_type id 164
                itemForTrade = InventoryItem.ITEM_TYPE.FOUND_ITEM_DECORATION_ALT,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            var ttiList = new List<TraderTrackerItems>
            {
                necklaceGoldSkullTTI,
                necklaceDemonicTTI,
                necklaceLoyaltyTTI,
                necklaceMissionaryTTI,
                necklaceLightTTI,
                necklaceDarkTTI,
                crystalDoctrineStoneTTI,
                talismanTTI,
                followerSkinTTI,
                decorationTTI
            };

            return ttiList;
        }
    }
}
