using System.Collections.Generic;
using System.Linq;

namespace MysticAssistant
{
    internal class MysticAssistantInventoryManager
    {
        public bool BoughtKeyPiece { get; private set; }

        public bool BoughtCrystalDoctrineStone { get; private set; }

        public bool BoughtFollowerSkin { get; private set; }

        private List<string> _followerSkinsAvailableFromMysticShop = new List<string>();

        private List<InventoryItem> _shopInventory = new List<InventoryItem>();

        public MysticAssistantInventoryManager()
        {
            ResetInventory();
        }

        public void ResetInventory()
        {
            _followerSkinsAvailableFromMysticShop.Clear();
            _shopInventory.Clear();

            PopulateShopInventory();
            SetAllBoughtFlags(false);
        }

        #region Populate shop methods

        private void PopulateShopInventory()
        {
            PopulateFollowerSkinShopList();

            foreach (TraderTrackerItems item in MysticAssistantInventoryInfo.GetMysticAssistantShopItemTypeList())
            {
                //set the limited stock on items that are not infinite.
                //talisman pieces and doctrine stones will count as infinite as buying more than is normally offered is part of the intended QoL of this mod
                if (item.itemForTrade == InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN)
                {
                    _shopInventory.Add(new InventoryItem(item.itemForTrade, _followerSkinsAvailableFromMysticShop.Count));
                }
                else
                {
                    _shopInventory.Add(new InventoryItem(item.itemForTrade/*,limited quantity goes here*/));
                }
            }
        }

        private void PopulateFollowerSkinShopList()
        {
            //loop over the list of skins available from the mystic shop, if a given skin is not unlocked, add it to the shop stock
            foreach (string skinString in DataManager.MysticShopKeeperSkins.ToList())
            {
                if (!DataManager.GetFollowerSkinUnlocked(skinString))
                {
                    _followerSkinsAvailableFromMysticShop.Add(skinString);
                }
            }
        }

        #endregion

        #region Shop inventory functions

        public List<InventoryItem> GetShopInventory()
        {
            return _shopInventory;
        }

        public string GetFollowerSkinNameByIndex(int index)
        {
            return _followerSkinsAvailableFromMysticShop[index];
        }

        public int GetCountOfAvailableFollowerSkins()
        {
            return _followerSkinsAvailableFromMysticShop.Count;
        }

        public void RemoveFollowerSkinFromShopList(string skinName)
        {
            _followerSkinsAvailableFromMysticShop.Remove(skinName);
        }

        public void ChangeShopStockByQuantity(InventoryItem.ITEM_TYPE itemType, int quantity)
        {
            _shopInventory.First(s => s.type == (int)itemType).quantity += quantity;
        }

        #endregion

        #region Post shop screen flags

        public void SetAllBoughtFlags(bool flag)
        {
            BoughtKeyPiece = flag;
            BoughtCrystalDoctrineStone = flag;
            BoughtFollowerSkin = flag;
        }

        public void SetBoughtKeyPieceFlag(bool showFlag)
        {
            BoughtKeyPiece = showFlag;
        }

        public void SetBoughtCrystalDoctrineStoneFlag(bool showFlag)
        {
            BoughtCrystalDoctrineStone = showFlag;
        }

        public void SetBoughtFollowerSkinFlag(bool showFlag)
        {
            BoughtFollowerSkin = showFlag;
        }

        #endregion
    }
}
