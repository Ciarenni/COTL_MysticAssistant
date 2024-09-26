using System;
using System.Collections.Generic;
using System.Linq;

namespace MysticAssistant
{
    internal class MysticAssistantInventoryManager
    {
        public bool BoughtKeyPiece { get; private set; }

        public bool BoughtCrystalDoctrineStone { get; private set; }

        public bool BoughtFollowerSkin { get; private set; }

        public bool BoughtDecoration { get; private set; }

        public bool BoughtTarotCard { get; private set; }

        private List<InventoryItem> _shopInventory = new List<InventoryItem>();

        private List<string> _followerSkinsAvailableFromMysticShop = new List<string>();

        private List<StructureBrain.TYPES> _decorationsAvailableFromMysticShop = new List<StructureBrain.TYPES>();

        private List<TarotCards.Card> _tarotCardsAvailableFromMysticShop = new List<TarotCards.Card>();

        private List<InventoryItem.ITEM_TYPE> _limitedStockTypes = new List<InventoryItem.ITEM_TYPE>();

        public MysticAssistantInventoryManager()
        {
            ResetInventory();
        }

        public void ResetInventory()
        {
            _followerSkinsAvailableFromMysticShop.Clear();
            _decorationsAvailableFromMysticShop.Clear();
            _tarotCardsAvailableFromMysticShop.Clear();
            _shopInventory.Clear();

            PopulateShopInventory();
            SetAllBoughtFlags(false);
        }

        #region Populate shop methods

        private void PopulateShopInventory()
        {
            PopulateFollowerSkinShopList();
            PopulateDecorationShopList();
            PopulateTarotCardShopList();

            foreach (TraderTrackerItems item in MysticAssistantInventoryInfo.GetMysticAssistantShopItemTypeList())
            {
                //set the limited stock on items that are not infinite.
                //talisman pieces and doctrine stones will count as infinite as buying more than is normally offered is part of the intended QoL of this mod
                if(_limitedStockTypes.Contains(item.itemForTrade))
                {
                    _shopInventory.Add(new InventoryItem(item.itemForTrade, GetItemListCountByItemType(item.itemForTrade)));
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

            _limitedStockTypes.Add(InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN);
        }

        private void PopulateDecorationShopList()
        {
            //loop over the list of decorations available from the mystic shop, if a given decoration is not unlocked, add it to the shop stock
            foreach (StructureBrain.TYPES deco in DataManager.MysticShopKeeperDecorations.ToList())
            {
                if (!DataManager.Instance.UnlockedStructures.Contains(deco))
                {
                    _decorationsAvailableFromMysticShop.Add(deco);
                }
            }

            _limitedStockTypes.Add(InventoryItem.ITEM_TYPE.FOUND_ITEM_DECORATION_ALT);
        }

        private void PopulateTarotCardShopList()
        {
            foreach (TarotCards.Card card in TarotCards.MysticCards)
            {
                if (!DataManager.Instance.PlayerFoundTrinkets.Contains(card))
                {
                    _tarotCardsAvailableFromMysticShop.Add(card);
                }
            }

            _limitedStockTypes.Add(InventoryItem.ITEM_TYPE.TRINKET_CARD);
        }

        #endregion

        #region Shop inventory functions

        public List<InventoryItem> GetShopInventory()
        {
            return _shopInventory;
        }

        public void ChangeShopStockByQuantity(InventoryItem.ITEM_TYPE itemType, int quantity)
        {
            _shopInventory.First(s => s.type == (int)itemType).quantity += quantity;
        }

        //this function and the RemoveItemFromList function are kind of half-assed generic functions so i didnt have to make a whole bunch of different functions that
        //all did the same thing on different lists. the thing that keeps me from being able to do that without a generic is that each of the lists returns a different type of object.
        //unfortunately, i could not make an actual generic class that worked with all of these because when i tried that the whole thing just didnt work.
        //the secondary interaction wasnt even available on the mystic shop. so it broke the whole mod and i didnt feel it was worth the time and effort
        //to troubleshoot and figure it out when i can just do this and not break the entire mod
        public int GetItemListCountByItemType(InventoryItem.ITEM_TYPE itemType)
        {
            switch(itemType)
            {
                case InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN:
                    return _followerSkinsAvailableFromMysticShop.Count;
                case InventoryItem.ITEM_TYPE.FOUND_ITEM_DECORATION_ALT:
                    return _decorationsAvailableFromMysticShop.Count;
                case InventoryItem.ITEM_TYPE.TRINKET_CARD:
                    return _tarotCardsAvailableFromMysticShop.Count;
                default:
                    return -1;
            }
        }

        public void RemoveItemFromListByTypeAndIndex(InventoryItem.ITEM_TYPE itemType, int index)
        {
            switch (itemType)
            {
                case InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN:
                    _followerSkinsAvailableFromMysticShop.RemoveAt(index);
                    break;
                case InventoryItem.ITEM_TYPE.FOUND_ITEM_DECORATION_ALT:
                    _decorationsAvailableFromMysticShop.RemoveAt(index);
                    break;
                case InventoryItem.ITEM_TYPE.TRINKET_CARD:
                    _tarotCardsAvailableFromMysticShop.RemoveAt(index);
                    break;
            }
        }

        public string GetFollowerSkinNameByIndex(int index)
        {
            return _followerSkinsAvailableFromMysticShop[index];
        }

        public StructureBrain.TYPES GetDecorationByIndex(int index)
        {
            return _decorationsAvailableFromMysticShop[index];
        }

        public TarotCards.Card GetTarotCardByIndex(int index)
        {
            return _tarotCardsAvailableFromMysticShop[index];
        }

        #endregion

        #region Post shop screen flags

        public void SetAllBoughtFlags(bool flag)
        {
            BoughtKeyPiece = flag;
            BoughtCrystalDoctrineStone = flag;
            BoughtFollowerSkin = flag;
            BoughtDecoration = flag;
            BoughtTarotCard = flag;
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

        public void SetBoughtDecorationFlag(bool showFlag)
        {
            BoughtDecoration = showFlag;
        }

        public void SetBoughtTarotCardFlag(bool showFlag)
        {
            BoughtTarotCard = showFlag;
        }

        #endregion
    }
}
