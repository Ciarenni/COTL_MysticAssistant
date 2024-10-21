using HarmonyLib;
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
        public bool BoughtRelic { get; private set; }

        private List<InventoryItem> _shopInventory = new List<InventoryItem>();

        private List<InventoryItem.ITEM_TYPE> _limitedStockTypes = new List<InventoryItem.ITEM_TYPE>();

        private List<string> _followerSkinsAvailableFromMysticShop = new List<string>();

        private List<StructureBrain.TYPES> _decorationsAvailableFromMysticShop = new List<StructureBrain.TYPES>();

        private List<TarotCards.Card> _tarotCardsAvailableFromMysticShop = new List<TarotCards.Card>();

        private List<RelicType> _relicsAvailableFromMysticShop = new List<RelicType>();

        private int maxCountCrystalDoctrineStone = 24;//default to 24 as that is the max as of the unholy alliance update

        public MysticAssistantInventoryManager(Interaction_MysticShop instance)
        {
            ResetInventory();
            //get the max amount of doctrine stones a player is allowed to buy from the mystic shop from the private variable on the Interaction_MysticShop that controls this.
            //this should support future updates adding more crystal doctrine stones automatically as long as they dont change the variable name
            maxCountCrystalDoctrineStone = (int)Traverse.Create(instance).Field("maxAmountOfCrystalDoctrines").GetValue();

            //because of the way i read the original code, there is a distinct chance that players who used this mod before 2.1.0 have had the flag for having
            //Aym and Baal as followers set incorrectly. to resolve this, we can check the list of living and dead followers to see if they have ever had them.
            //the flag is then set appropriate for each of these, which should enable players to be able to acquire them when they were previously locked out from it.
            //sorry about that everyone!
            bool playerHasAym = DataManager.Instance.Followers.Exists(f => f.Name == "Aym") || DataManager.Instance.Followers_Dead.Exists(f => f.Name == "Aym");
            DataManager.Instance.HasAymSkin = playerHasAym;
            bool playerHasBaal = DataManager.Instance.Followers.Exists(f => f.Name == "Baal") || DataManager.Instance.Followers_Dead.Exists(f => f.Name == "Baal");
            DataManager.Instance.HasBaalSkin = playerHasBaal;
        }

        public void ResetInventory()
        {
            _followerSkinsAvailableFromMysticShop.Clear();
            _decorationsAvailableFromMysticShop.Clear();
            _tarotCardsAvailableFromMysticShop.Clear();
            _relicsAvailableFromMysticShop.Clear();
            _shopInventory.Clear();
            _limitedStockTypes.Clear();

            PopulateShopInventory();
            SetAllBoughtFlags(false);
        }

        #region Populate shop methods

        private void PopulateShopInventory()
        {
            PopulateFollowerSkinShopList();
            PopulateDecorationShopList();
            PopulateTarotCardShopList();
            PopulateRelicShopList();

            int shopStock;
            //use this list to sort the out of stock items to the end of the shop list
            List<InventoryItem.ITEM_TYPE> outOfStockList = new List<InventoryItem.ITEM_TYPE>();
            foreach (TraderTrackerItems item in MysticAssistantInventoryInfo.GetMysticAssistantShopItemTypeList())
            {
                //if a quantity is not set, it will default to 1, and that is confusing, so set the stock of everything to 99.
                //it doesnt matter what it is because im not subtracting from the quantity for unlimited items anyway.
                shopStock = 99;

                //if the item has a limited stock, get that value and use it instead
                //talisman pieces and doctrine stones will count as infinite as buying more than is normally offered is part of the intended QoL of this mod
                if(_limitedStockTypes.Contains(item.itemForTrade))
                {
                    shopStock = GetItemListCountByItemType(item.itemForTrade);
                    if(shopStock == 0)
                    {
                        outOfStockList.Add(item.itemForTrade);
                        continue;
                    }
                }
                //set the stock for crystal doctrine stones. i had previously made these infinitely available, thinking of them like talisman pieces, but these are
                //more like the other limited stock items as they only come from the shop
                else if(item.itemForTrade == InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE)
                {
                    shopStock = maxCountCrystalDoctrineStone - DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop;
                    if (shopStock == 0)
                    {
                        outOfStockList.Add(item.itemForTrade);
                        continue;
                    }
                }
                else if(item.itemForTrade == InventoryItem.ITEM_TYPE.BLACK_GOLD)
                {
                    shopStock = 100;
                }

                _shopInventory.Add(new InventoryItem(item.itemForTrade, shopStock));
            }

            //this is certainly not the best way to do this, you could make some kind of tuple list with the item_type and the stock and sort that by stock and populate the shop list using that
            //but this is kind of a last minute addition to what i expect to really be the last major (and maybe even minor) update to this mod
            //so im going to be a bit lazy and not do a big overhaul to support that when this works just fine
            foreach (InventoryItem.ITEM_TYPE outOfStockItem in outOfStockList)
            {
                _shopInventory.Add(new InventoryItem(outOfStockItem, 0));
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

        private void PopulateRelicShopList()
        {
            //unlike the other limited stock items, it seems there's no DataManager list managing what relics are available from the mystic shop
            //they are just hard coded in the original code almost exactly like how i have them here
            if (!DataManager.Instance.PlayerFoundRelics.Contains(RelicType.SpawnBlackGoop))
            {
                _relicsAvailableFromMysticShop.Add(RelicType.SpawnBlackGoop);
            }

            if (!DataManager.Instance.PlayerFoundRelics.Contains(RelicType.UnlimitedFervour))
            {
                _relicsAvailableFromMysticShop.Add(RelicType.UnlimitedFervour);
            }

            _limitedStockTypes.Add(InventoryItem.ITEM_TYPE.SOUL_FRAGMENT);
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
                //this is actually for relics despite the item_type
                case InventoryItem.ITEM_TYPE.SOUL_FRAGMENT:
                    return _relicsAvailableFromMysticShop.Count;
                default:
                    return 0;
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
                //this is actually for relics despite the item_type
                case InventoryItem.ITEM_TYPE.SOUL_FRAGMENT:
                    _relicsAvailableFromMysticShop.RemoveAt(index);
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

        public RelicType GetRelicByIndex(int index)
        {
            return _relicsAvailableFromMysticShop[index];
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
            BoughtRelic = flag;
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

        public void SetBoughtRelicFlag(bool showFlag)
        {
            BoughtRelic = showFlag;
        }

        #endregion
    }
}
