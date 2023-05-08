using BepInEx;
using HarmonyLib;
using Lamb.UI;
using Lamb.UI.MainMenu;
using src.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace MysticAssistant
{
    [BepInPlugin("ciarenni.cultofthelamb.mysticassistant", "Mystic Assistant", "1.0.1")]
    public class MysticAssistant : BaseUnityPlugin
    {
        private static readonly Type patchType = typeof(MysticAssistant);
        private static string SHOP_CONTEXT_KEY = "mystic_assistant_shop";

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin ciarenni.cultofthelamb.mysticassistant is loaded!");

            Harmony harmony = new Harmony(id: "ciarenni.cultofthelamb.mysticassistant");

            //patch the modified methods as pre- and post-fix as appropriate
            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "Start"), postfix: new HarmonyMethod(patchType, nameof(PostfixEnableMysticAssistantOnTheMysticShop)));
            harmony.Patch(AccessTools.Method(typeof(UIItemSelectorOverlayController), "RefreshContextText"), prefix: new HarmonyMethod(patchType, nameof(PrefixRefreshContextTextForAssistant)));
            harmony.Patch(AccessTools.Method(typeof(UIItemSelectorOverlayController), "OnItemClicked"), prefix: new HarmonyMethod(patchType, nameof(PrefixOnItemClickedForAssistant)));
            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "OnSecondaryInteract"), prefix: new HarmonyMethod(patchType, nameof(PrefixSecondaryInteract)));
        }

        public static void PostfixEnableMysticAssistantOnTheMysticShop(Interaction_MysticShop __instance)
        {
            //tell the mystic shop that it has a secondary interaction and set up the label for it
            __instance.HasSecondaryInteraction = true;
            __instance.SecondaryLabel = DataManager.Instance.MysticKeeperName + "'s assistant";
        }

        public static bool PrefixRefreshContextTextForAssistant(UIItemSelectorOverlayController __instance,
            ItemSelector.Params ____params,
            ItemSelector.Category ____category,
            TextMeshProUGUI ____buttonPromptText,
            string ____addtionalText,
            string ____contextString)
        {
            //check the params to see if the ItemSelector being accessed is the one added in the mod.
            //no matter what, if the player is looking at the mod shop, we want to only run this modified method and always skip the authentic method
            if (____params.Key != SHOP_CONTEXT_KEY)
            {
                return true;
            }

            //the rest of the code in this method is taken almost verbatim from the original code.
            //the general gist is it checks if the player is buying items or selling items (the player is buying from the assistant, so the Context is Buy)
            //then, based on the Context, it determines the value of the item and how many the player currently has in the inventory, along with
            //the name of the highlighted/selected item, then sets that as the label.
            //the only changes are to use God Tears instead of the coins, and changes to variables to make it work as a prefix Harmony method
            if (____params.Context == ItemSelector.Context.Sell || ____params.Context == ItemSelector.Context.Buy)
            {
                //if the UIItemSelector has a CostProvider method set up, invoke it using the most recent item highlighted/selected to get the non-inventory variant of the item 
                TraderTrackerItems traderTrackerItems = __instance.CostProvider?.Invoke(____category.MostRecentItem);
                //if the CostProvider isn't set up, or the non-inventory variant of the item isn't found for some reason, bail out
                if (traderTrackerItems == null)
                {
                    return false;
                }

                if (____params.Context == ItemSelector.Context.Buy)
                {
                    //if the item has an offset, calculate the % of the base sell price compared to the actual (base + offset), store it in _additionalText
                    if (traderTrackerItems.SellOffset > 0)
                    {
                        float num = (float)traderTrackerItems.SellPrice / (float)traderTrackerItems.SellPriceActual;
                        num *= 100f;
                        ____addtionalText = " <color=red>+ " + Math.Round((double)num, 0) + "%</color> ";
                    }
                    //set up the label for the selected item using a localized string (_contextString), the localized name of the most recent item highlighted/selected,
                    //the image of the currency item (god tear for this mod), the actual cost (along with current quantity of currency item), and sticking the _additionalText on the end
                    ____buttonPromptText.text = string.Format(____contextString, InventoryItem.LocalizedName(____category.MostRecentItem) ?? "", CostFormatter.FormatCost(InventoryItem.ITEM_TYPE.GOD_TEAR, traderTrackerItems.SellPriceActual, true, false)) + ____addtionalText;
                    return false;
                }
                ____buttonPromptText.text = string.Format(____contextString, InventoryItem.LocalizedName(____category.MostRecentItem), CostFormatter.FormatCost(InventoryItem.ITEM_TYPE.GOD_TEAR, traderTrackerItems.BuyPriceActual, true, true)) + ____addtionalText;
                return false;
            }
            else
            {
                //if the Context isn't Buy or Sell (such as a farm plot which has a Context of SetLabel), then set up the label with the localized name of the most recent item highlighted/selected, and stick the _additionalText on the end
                ____buttonPromptText.text = string.Format(____contextString, InventoryItem.LocalizedName(____category.MostRecentItem)) + ____addtionalText;
            }

            return false;
        }

        public static bool PrefixOnItemClickedForAssistant(UIItemSelectorOverlayController __instance,
            ItemSelector.Params ____params,
            GenericInventoryItem item)
        {
            //check the params to see if the ItemSelector being accessed is the one added in the mod.
            //if it isnt, short circuit out and run the actual RefreshContextText method
            //if it is, run the modified code and don't run the original
            if (____params.Key != SHOP_CONTEXT_KEY)
            {
                return true;
            }

            //get the MethodInfo for the private method GetItemQuantity from the UIItemSelectorOverlayController,
            //so it can be leveraged in the copied code just as it is in the original, rather than having to port MORE code into here.
            //the method will return the quantity of the item from the UIItemSelector's inventory (if it has a custom one assigned, i.e. a shop), or the player's inventory otherwise.
            MethodInfo getItemQuantity = AccessTools.Method(typeof(UIItemSelectorOverlayController), "GetItemQuantity");

            //the rest of the code in this method is taken almost verbatim from the original code.
            //the only changes are to use God Tears instead of the coins, and changes to variables to make it work as a prefix Harmony method
            if (____params.Context == ItemSelector.Context.SetLabel)
            {
                //this context is used for an item selector that is not meant to be a shop, such as choosing what seeds to plant at a farm plot
                //which means it doesn't need to check to see if it exists in the player's inventory
                //i.e. if you use a farm plot and have 0 pumpkin seeds, pumpkin seeds will still be displayed as an option,
                //but with a quantity label of 0 and unable to be chosen
                Choose();
                return false;
            }

            //if the item has a quantity greater than 0 in whichever inventory it is in, the player's or the UIItemSelector's
            //get the quantity of the item in either the UIItemSelector's inventory (if it has one set)
            if ((int)getItemQuantity.Invoke(__instance, new object[] { item.Type }) > 0)
            {
                //if the context is NOT Buy, allow the the player to pick it, then bail
                if (____params.Context != ItemSelector.Context.Buy)
                {
                    Choose();
                    return false;
                }
                //at this point, the Context can only be Buy, so we need to get the non-inventory variant of the item so we can get it's actual (base + offset) cost
                TraderTrackerItems traderTrackerItems = __instance.CostProvider?.Invoke(item.Type);
                if (traderTrackerItems != null && Inventory.GetItemQuantity(InventoryItem.ITEM_TYPE.GOD_TEAR) >= traderTrackerItems.SellPriceActual)
                {
                    //if the player has enough of the currency to cover the item's cost, allow them to pick it, then bail
                    Choose();
                    return false;
                }
            }
            //if the item has not been successfully bought or sold, whichever is appropriate, shake the item in the view and play a noise
            item.Shake();
            AudioManager.Instance.PlayOneShot("event:/ui/negative_feedback");

            //im personally not a fan of local functions, but it gets the job done
            void Choose()
            {
                //invoke the OnItemChosen delegate for the UIItemSelector, which can have actions added to it in implementations of the UIItemSelector,
                //as seen in PrefixSecondaryInteract where i set up the shopItemSelector
                __instance.OnItemChosen?.Invoke(item.Type);
                if (____params.HideOnSelection)
                {
                    //if the UIItemSelector should be hidden on closing, like a farm plot, hide it after selection
                    __instance.Hide();
                }
                else
                {
                    //otherwise, update the quantities (which will also update the label)
                    __instance.UpdateQuantities();
                }
            }

            return false;
        }

        public static void PrefixSecondaryInteract(Interaction_MysticShop __instance, StateMachine state)
        {
            //for reasons unknown to me, even though this method is specified to be prefixed to Interaction_MysticShop,
            //it is being added to other interactions as well. it was reported that the shop popped up when using the
            //secondary interact on beds (specifically grand shelters), the town shrine, both of which i was able to replicate,
            //and on the fertilizer box, which i was not able to replicate.
            //but this check should fix all of those anyway
            if (__instance.GetType() != typeof(Interaction_MysticShop))
            {
                Console.WriteLine("instance is not type of Interaction_MysticShop, skipping adding secondary interaction");
                return;
            }

            //disable player movement so they don't move while navigating the shop
            state.CURRENT_STATE = StateMachine.State.InActive;

            //hide the HUD because it looks nice that way
            HUD_Manager.Instance.Hide(false, 0, false);

            TraderTrackerItems godTearTTI = new TraderTrackerItems
            {
                //item_type id 119
                itemForTrade = InventoryItem.ITEM_TYPE.GOD_TEAR,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            //get the list of items, as a shop item type, that will be available to buy from the mod shop
            List<TraderTrackerItems> shopList = GetMysticAssistantShopList();

            //using the list of shop items, get a list of the inventory version of the same items
            var itemsForSale = new List<InventoryItem>();
            foreach (var item in shopList)
            {
                itemsForSale.Add(new InventoryItem(item.itemForTrade));
            }

            //set up the item selector to be our shop, mimicing what the seed shop does
            UIItemSelectorOverlayController shopItemSelector = MonoSingleton<UIManager>.Instance.ShowItemSelector(itemsForSale, new ItemSelector.Params
            {
                Key = SHOP_CONTEXT_KEY,
                Context = ItemSelector.Context.Buy,
                Offset = new Vector2(0f, 150f),
                ShowEmpty = true,
                RequiresDiscovery = false,
                HideQuantity = true,
                ShowCoins = false
            });

            //set up a delegate that returns the cost of the item based on the item_type passed to it
            shopItemSelector.CostProvider = delegate (InventoryItem.ITEM_TYPE item)
            {
                return GetTradeItem(shopList, item);
            };

            //set up what happens when the player confirms the item from the selector. combine the current actions OnItemChosen is doing with the new behaviour we want
            shopItemSelector.OnItemChosen = (Action<InventoryItem.ITEM_TYPE>)Delegate.Combine(
                shopItemSelector.OnItemChosen,
                new Action<InventoryItem.ITEM_TYPE>(delegate (InventoryItem.ITEM_TYPE chosenItem)
                {
                    //get the non-inventory of the chosen item
                    TraderTrackerItems tradeItem = GetTradeItem(shopList, chosenItem);
                    //deduct the item's cost from the player's inventory of the currency
                    Inventory.ChangeItemQuantity((int)godTearTTI.itemForTrade, -tradeItem.SellPriceActual, 0);
                    //add 1 of the chosen item to the player's inventory
                    Inventory.ChangeItemQuantity((int)chosenItem, 1, 0);
                    //play a pop sound
                    AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
                    //create a god tear that zips to the mystic shop, to look nice
                    ResourceCustomTarget.Create(__instance.gameObject, PlayerFarming.Instance.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate () { }, true);
                }));

            //on canceling out of the shop, show the HUD again
            shopItemSelector.OnCancel = (Action)Delegate.Combine(
                shopItemSelector.OnCancel,
                new Action(delegate ()
                {
                    HUD_Manager.Instance.Show(0, false);
                }));

            //on hiding the shop, set the player's state back to idle so they can once again controller the lamb
            shopItemSelector.OnHidden = (Action)Delegate.Combine(
                shopItemSelector.OnHidden,
                new Action(delegate ()
                {
                    state.CURRENT_STATE = StateMachine.State.Idle;
                    shopItemSelector = null;
                }));
        }

        //this is pulled from the seed shop's interaction functionality, as the authentic mystic shop does not include it and it's needed for the mod
        //given a list of non-inventory items and an item_type, return the non-inventory item of that type from the provided list
        private static TraderTrackerItems GetTradeItem(List<TraderTrackerItems> listOfItems, InventoryItem.ITEM_TYPE item)
        {
            foreach (TraderTrackerItems traderTrackerItems in listOfItems)
            {
                if (traderTrackerItems.itemForTrade == item)
                {
                    return traderTrackerItems;
                }
            }
            return null;
        }

        //create the non-inventory item objects for everything that will be for sale in the mod shop
        //prices are in terms of god tears
        private static List<TraderTrackerItems> GetMysticAssistantShopList()
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
