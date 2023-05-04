using BepInEx;
using HarmonyLib;
using Lamb.UI;
using Lamb.UI.MainMenu;
using src.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MysticAssistant
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class MysticAssistant : BaseUnityPlugin
    {
        private static readonly Type patchType = typeof(MysticAssistant);

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony harmony = new Harmony(id: "cultofthelamb.ciarenni.mysticassistant.main");

            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "Start"), postfix: new HarmonyMethod(patchType, nameof(SetUpMysticAssistant)));
            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "OnSecondaryInteract"), prefix: new HarmonyMethod(patchType, nameof(PrefixSecondaryInteract)));
        }

        public static void SetUpMysticAssistant(Interaction_MysticShop __instance)
        {
            __instance.SecondaryLabel = "Mod label";
            __instance.HasSecondaryInteraction = true;   
        }

        public static void PrefixSecondaryInteract(Interaction_MysticShop __instance, StateMachine state)
        {
            //disable player movement so they don't move while navigating the shop
            state.CURRENT_STATE = StateMachine.State.InActive;

            //hide the HUD because it looks nice that way
            HUD_Manager.Instance.Hide(false, 0, false);

            TraderTrackerItems godTearTTI = new TraderTrackerItems
            {
                //119
                itemForTrade = InventoryItem.ITEM_TYPE.GOD_TEAR,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            List<TraderTrackerItems> shopList = GetMysticAssistantShopList();
            TraderTracker TraderInfo = new TraderTracker();
            TraderInfo.itemsToTrade = shopList;

            var itemsForSale = new List<InventoryItem>();
            foreach(var item in shopList)
            {
                itemsForSale.Add(new InventoryItem(item.itemForTrade));
            }

            UIItemSelectorOverlayController itemSelector = MonoSingleton<UIManager>.Instance.ShowItemSelector(itemsForSale, new ItemSelector.Params
            {
                Key = "mystic_assistant_shop",
                Context = ItemSelector.Context.Buy,
                Offset = new Vector2(0f, 150f),
                ShowEmpty = true,
                RequiresDiscovery = false,
                HideQuantity = true
            });
            
            itemSelector.CostProvider = delegate (InventoryItem.ITEM_TYPE item)
            {
                Console.WriteLine("Attempting to get cost");
                return GetTradeItem(TraderInfo, item);
            };

            UIItemSelectorOverlayController itemSelector4 = itemSelector;
            itemSelector4.OnItemChosen = (Action<InventoryItem.ITEM_TYPE>)Delegate.Combine(itemSelector4.OnItemChosen, new Action<InventoryItem.ITEM_TYPE>(delegate (InventoryItem.ITEM_TYPE chosenItem)
            {
                TraderTrackerItems tradeItem = GetTradeItem(TraderInfo, chosenItem);   
                Inventory.ChangeItemQuantity((int)godTearTTI.itemForTrade, -tradeItem.SellPriceActual, 0);
                Inventory.ChangeItemQuantity((int)chosenItem, 1, 0);
                AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
                ResourceCustomTarget.Create(__instance.gameObject, PlayerFarming.Instance.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate () { }, true);
            }));

            UIItemSelectorOverlayController itemSelector2 = itemSelector;
            itemSelector2.OnCancel = (Action)Delegate.Combine(itemSelector2.OnCancel, new Action(delegate ()
            {
                HUD_Manager.Instance.Show(0, false);
            }));

            UIItemSelectorOverlayController itemSelector3 = itemSelector;
            itemSelector3.OnHidden = (Action)Delegate.Combine(itemSelector3.OnHidden, new Action(delegate ()
            {
                state.CURRENT_STATE = StateMachine.State.Idle;
                itemSelector = null;
            }));
        }

        private static TraderTrackerItems GetTradeItem(TraderTracker traderInfo, InventoryItem.ITEM_TYPE item)
        {
            foreach (TraderTrackerItems traderTrackerItems in traderInfo.itemsToTrade)
            {
                if (traderTrackerItems.itemForTrade == item)
                {
                    return traderTrackerItems;
                }
            }
            return null;
        }

        private static List<TraderTrackerItems> GetMysticAssistantShopList()
        {
            TraderTrackerItems necklaceDarkTTI = new TraderTrackerItems
            {
                //124
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Dark,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceLightTTI = new TraderTrackerItems
            {
                //125
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Light,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceGoldSkullTTI = new TraderTrackerItems
            {
                //127
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Gold_Skull,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceDemonicTTI = new TraderTrackerItems
            {
                //123
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Demonic,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceLoyaltyTTI = new TraderTrackerItems
            {
                //122
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Loyalty,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            TraderTrackerItems necklaceMissionaryTTI = new TraderTrackerItems
            {
                //126
                itemForTrade = InventoryItem.ITEM_TYPE.Necklace_Missionary,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            return new List<TraderTrackerItems>
            {
                necklaceDarkTTI,
                necklaceLightTTI,
                necklaceGoldSkullTTI,
                necklaceDemonicTTI,
                necklaceLoyaltyTTI,
                necklaceMissionaryTTI
            };
        }
    }
}
