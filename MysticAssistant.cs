using BepInEx;
using HarmonyLib;
using Lamb.UI;
using MMTools;
using src.Extensions;
using src.UI;
using src.UI.Overlays.TutorialOverlay;
using src.UINavigator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace MysticAssistant
{
    [BepInPlugin("ciarenni.cultofthelamb.mysticassistant", "Mystic Assistant", "2.0.0")]
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

            Console.WriteLine("Mystic Assistant postfix applied to MysticShop");
        }

        public static bool PrefixRefreshContextTextForAssistant(UIItemSelectorOverlayController __instance,
            ItemSelector.Params ____params,
            ItemSelector.Category ____category,
            TextMeshProUGUI ____buttonPromptText,
            string ____addtionalText,
            string ____contextString)
        {
            Console.WriteLine("prefix for context text starting");

            //check the params to see if the ItemSelector being accessed is the one added in the mod.
            //no matter what, if the player is looking at the mod shop, we want to only run this modified method and always skip the authentic method
            if (____params.Key != SHOP_CONTEXT_KEY)
            {
                Console.WriteLine("interaction is not the mystic shop, skipping");
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

            Console.WriteLine("prefix for context text ending");
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
            Console.WriteLine("mystic assistant secondary action applied to mystic shop");
            bool boughtKeyPiece = false;
            bool boughtDoctrineStone = false;

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

            PlayerFarming playerFarming = state.GetComponent<PlayerFarming>();
            PlayerFarming.SetStateForAllPlayers(StateMachine.State.InActive, false, null);
            playerFarming.GoToAndStop(playerFarming.transform.position, playerFarming.LookToObject, false, false, null, 20f, true, null, true, true, true, true, null);

            //set up the item selector to be our shop, mimicing what the seed shop does
            UIItemSelectorOverlayController shopItemSelector = MonoSingleton<UIManager>.Instance.ShowItemSelector(playerFarming, itemsForSale, new ItemSelector.Params
            {
                Key = SHOP_CONTEXT_KEY,
                Context = ItemSelector.Context.Buy,
                Offset = new Vector2(0f, 150f),
                ShowEmpty = true,
                RequiresDiscovery = false,
                HideQuantity = true,
                ShowCoins = false,
                AllowInputOnlyFromPlayer = playerFarming
            });
            if (__instance.InputOnlyFromInteractingPlayer)
            {
                MonoSingleton<UINavigatorNew>.Instance.AllowInputOnlyFromPlayer = playerFarming;
            }

            //set up a delegate that returns the cost of the item based on the item_type passed to it
            shopItemSelector.CostProvider = delegate (InventoryItem.ITEM_TYPE item)
            {
                return GetTradeItem(shopList, item);
            };

            //set up what happens when the player confirms the item from the selector. combine the current actions OnItemChosen is doing with the new behaviour we want
            shopItemSelector.OnItemChosen = (Action<InventoryItem.ITEM_TYPE>)Delegate.Combine(
                shopItemSelector.OnItemChosen,
                new Action<InventoryItem.ITEM_TYPE>(delegate (InventoryItem.ITEM_TYPE chosenItemType)
                {
                    //get the non-inventory of the chosen item
                    TraderTrackerItems tradeItem = GetTradeItem(shopList, chosenItemType);
                    //check to see if the user is attempting to buy something that might not be needed
                    if (DisplayBoughtQuantityWarning(tradeItem))
                    {
                        UIMenuConfirmationWindow uimenuConfirmationWindow = shopItemSelector.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                        uimenuConfirmationWindow.Configure("Confirm purchase", "You have already bought the maximum legitimate amount of this item. Are you sure you want to buy more?", false);
                        uimenuConfirmationWindow.OnConfirm = (Action)Delegate.Combine(uimenuConfirmationWindow.OnConfirm, new Action(delegate ()
                        {
                            //if they acknowledge the warning, proceed to the purchase
                            GivePlayerBoughtItem(tradeItem, chosenItemType);
                        }));
                        uimenuConfirmationWindow.OnCancel = (Action)Delegate.Combine(uimenuConfirmationWindow.OnCancel, new Action(delegate ()
                        {
                            //if they decline and back out, dont proceed to the purchase
                            return;
                        }));
                    }
                    else
                    {
                        //if no warning needs to be shown, just proceed to the purchase
                        GivePlayerBoughtItem(tradeItem, chosenItemType);
                    }
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
                    //if the player bought a talisman fragment, we want to show the talisman fragment UI of adding the pieces together, so check if we need to show that
                    if (boughtKeyPiece)
                    {
                        //im not entirely sure why i need to set the state to inactive here when the player should still be inactive from the initial mystic assistant interaction
                        //but if i dont, it allows the player to move and interact with the talisman key piece screen still up.
                        //this is especially bad because if they player does not move and hits the Accept button, they are standing right next to the Mystic Shopkeeper
                        //and will spend a god tear to spin the wheel without realizing it.
                        //setting the state to inactive again prevents that from happening.
                        //speculation: because this is attached to the Interaction_MysticShop interaction, the state being changed in the actual function is what causes this.
                        //  i have no easy way to verify this and it honestly doesn't matter because this is the cleanest, easiest solution to the problem.
                        PlayerFarming.SetStateForAllPlayers(StateMachine.State.InActive, false, null);
                        UIKeyScreenOverlayController keyScreenManager = MonoSingleton<UIManager>.Instance.KeyScreenTemplate.Instantiate<UIKeyScreenOverlayController>();
                        keyScreenManager.Show();
                        keyScreenManager.OnHidden += new Action(delegate ()
                        {
                            foreach (PlayerFarming playerFarming in PlayerFarming.players)
                            {
                                if (playerFarming.GoToAndStopping)
                                {
                                    playerFarming.AbortGoTo(true);
                                }
                            }
                            PlayerFarming.SetStateForAllPlayers((LetterBox.IsPlaying || MMConversation.isPlaying) ? StateMachine.State.InActive : StateMachine.State.Idle, false, null);
                            state.CURRENT_STATE = StateMachine.State.Idle;
                            shopItemSelector = null;
                        });
                    }
                    //this needs to not be an else-if block, its very possible a player would buy both a stone and a talisman
                    else if(boughtDoctrineStone)
                    {
                        TryShowCrystalDoctrineTutorial();
                    }
                    //separate (and unfortunately duplicate, ill probably move this into a method later) the code for ending the mystic assistant interaction.
                    //this needs to be done to support the talisman key piece screen being shown when appropriate
                    else
                    {
                        foreach (PlayerFarming playerFarming in PlayerFarming.players)
                        {
                            if (playerFarming.GoToAndStopping)
                            {
                                playerFarming.AbortGoTo(true);
                            }
                        }
                        PlayerFarming.SetStateForAllPlayers((LetterBox.IsPlaying || MMConversation.isPlaying) ? StateMachine.State.InActive : StateMachine.State.Idle, false, null);
                        state.CURRENT_STATE = StateMachine.State.Idle;
                        shopItemSelector = null;
                    }
                }));

            //making this a local function to limit what all needs to be passed to it
            void GivePlayerBoughtItem(TraderTrackerItems boughtItem, InventoryItem.ITEM_TYPE boughtItemType)
            {
                //deduct the item's cost from the player's inventory of the currency
                Inventory.ChangeItemQuantity((int)godTearTTI.itemForTrade, -boughtItem.SellPriceActual, 0);
                //add 1 of the chosen item to the player's inventory
                Inventory.ChangeItemQuantity((int)boughtItemType, 1, 0);//TODO this need to be modified to not add whole talismans and whole commandment stone pieces to the inventory

                //set or adjust flags appropriately based on purchased item
                switch (boughtItemType)
                {
                    case InventoryItem.ITEM_TYPE.Necklace_Dark:
                        DataManager.Instance.HasAymSkin = true;
                        break;
                    case InventoryItem.ITEM_TYPE.Necklace_Light:
                        DataManager.Instance.HasBaalSkin = true;
                        break;
                    case InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE:
                        DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop++;
                        boughtDoctrineStone = true;
                        break;
                    case InventoryItem.ITEM_TYPE.TALISMAN:
                        //TODO figure out how to correctly adjust the number of talisman pieces the player currently has,
                        //so that can be set to 0 (after a purchase, because that always rounds up to the next full talisman)


                        Inventory.KeyPieces++;
                        boughtKeyPiece = true;
                        //TODO i think the talisman stuff is sorted now. look at doing the same for commandment stone fragments, as i think that is what the mystic actually gives

                        //GiveTalismanReward(__instance, ____keyPiecePrefab);//, ____godTearTarget);
                        



                        //NOTE: look at Inventory.KeyPieces++ in Interaction_KeyPiece
                        //DataManager.Instance.TalismanPiecesReceivedFromMysticShop += 1;// GetTalismanPiecesRemainingToNearestWholeTalisman();
                        break;
                    //TODO add the stuff to allow players to buy the mystic shop follower skins
                }
                //play a pop sound
                AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
                //create a god tear that zips to the mystic shop, to look nice
                ResourceCustomTarget.Create(__instance.gameObject, playerFarming.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate () { }, true);

                //update the item selector label
                AccessTools.Method(typeof(UIItemSelectorOverlayController), "RefreshContextText").Invoke(shopItemSelector, new object[] { });
            }

            
        }

        private static void TryShowCrystalDoctrineTutorial()
        {
            //on the off chance that someone is using this mod to get their first "forgotten doctrine" stone, we need to make sure that system is unlocked for them.
            //im just assuming this works, i dont know how to edit my existing save to test it, and i dont have one laying around in this very specific state.
            //so hopefully i read the code right and this does what i think it does!
            UpgradeSystem.UnlockAbility(UpgradeSystem.Type.Ritual_CrystalDoctrine, false);
            if (DataManager.Instance.TryRevealTutorialTopic(TutorialTopic.CrystalDoctrine))
            {
                UITutorialOverlayController menu = MonoSingleton<UIManager>.Instance.ShowTutorialOverlay(TutorialTopic.CrystalDoctrine, 0f);
                menu.Show();
                menu.OnHidden += new Action(delegate ()
                {
                    UIPlayerUpgradesMenuController uiplayerUpgradesMenuController = MonoSingleton<UIManager>.Instance.PlayerUpgradesMenuTemplate.Instantiate<UIPlayerUpgradesMenuController>();
                    uiplayerUpgradesMenuController.ShowCrystalUnlock();
                });
            }
        }

        //private static IEnumerator GiveTalismanReward(Interaction_MysticShop __instance, Interaction_KeyPiece ____keyPiecePrefab)//, Transform ____godTearTarget)
        //{
        //    //Inventory.KeyPieces++;
        //    //UIKeyScreenOverlayController uikeyScreenOverlayController = MonoSingleton<UIManager>.Instance.ShowKeyScreen();
        //    //uikeyScreenOverlayController.OnHidden = (Action)Delegate.Combine(uikeyScreenOverlayController.OnHidden, new Action(delegate ()
        //    //{
        //    //    if (!DataManager.Instance.HadFirstTempleKey && Inventory.TempleKeys > 0 && DataManager.Instance.TryRevealTutorialTopic(TutorialTopic.Fleeces))
        //    //    {
        //    //        UITutorialOverlayController uitutorialOverlayController = MonoSingleton<UIManager>.Instance.ShowTutorialOverlay(TutorialTopic.Fleeces, 0f);
        //    //        uitutorialOverlayController.OnHidden = (Action)Delegate.Combine(uitutorialOverlayController.OnHidden, new Action(delegate ()
        //    //        {
        //    //            ObjectiveManager.Add(new Objectives_Custom("Objectives/GroupTitles/UnlockFleece", Objectives.CustomQuestTypes.UnlockFleece, -1, -1f), false);
        //    //            DataManager.Instance.HadFirstTempleKey = true;
        //    //        }));
        //    //    }
        //    //}));
        //    //____keyPiecePrefab.Particles.Stop();
        //    //yield return new WaitForSeconds(0.5f);
        //    //____keyPiecePrefab.Image.enabled = false;






        //    //Interaction_KeyPiece keyPiece = UnityEngine.Object.Instantiate<Interaction_KeyPiece>(____keyPiecePrefab, ____godTearTarget.position, Quaternion.identity, __instance.transform.parent);
        //    //keyPiece.transform.localScale = Vector3.zero;
        //    //keyPiece.transform.DOScale(Vector3.one, 2f).SetEase(Ease.OutBack);
        //    //AudioManager.Instance.PlayOneShot("event:/Stings/Choir_Short", ____godTearTarget.position);
        //    //GameManager.GetInstance().OnConversationNext(keyPiece.gameObject, 6f);
        //    //yield return new WaitForSeconds(1.5f);
        //    //keyPiece.transform.DOMove(__instance.playerFarming.transform.position + new Vector3(0f, 1f, -1f), 1f, false).SetEase(Ease.InBack);
        //    //AudioManager.Instance.PlayOneShot("event:/player/float_follower", keyPiece.gameObject);
        //    //yield return new WaitForSeconds(1f);
        //    //keyPiece.OnInteract(__instance.playerFarming.state);
        //    //DataManager.Instance.TalismanPiecesReceivedFromMysticShop++;
        //    //yield return new WaitForSeconds(2.5f);
        //    //UnityEngine.Object.Destroy(keyPiece.gameObject);
        //    //keyPiece = null;
        //}

        //this is pulled from the seed shop's interaction functionality, as the authentic mystic shop does not include it and it's needed for the mod
        //given a list of non-inventory items and an item_type, return the non-inventory item of that type from the provided list
        private static TraderTrackerItems GetTradeItem(List<TraderTrackerItems> shopList, InventoryItem.ITEM_TYPE specifiedType)
        {
            //loop over all items in the shop's list
            foreach (TraderTrackerItems shopItem in shopList)
            {
                //if the item from the list matches the specified type
                if (shopItem.itemForTrade == specifiedType)
                {
                    //check if the requested type is of ITEM_TYPE.TALISMAN. if it is, we need to do some match for the cost
                    //if (specifiedType == InventoryItem.ITEM_TYPE.TALISMAN)
                    //{
                    //    Console.WriteLine("current talisman pieces count: " + DataManager.Instance.TalismanPiecesReceivedFromMysticShop);
                    //    Console.WriteLine("remaining to next full piece: " + GetTalismanPiecesRemainingToNearestWholeTalisman());
                    //    shopItem.SellPrice = GetTalismanPiecesRemainingToNearestWholeTalisman();
                    //}

                    return shopItem;
                }
            }
            return null;
        }

        private static bool DisplayBoughtQuantityWarning(TraderTrackerItems chosenItem)
        {
            //check what the player is buying to see if they need to be warned about it
            switch (chosenItem.itemForTrade)
            {
                
                case InventoryItem.ITEM_TYPE.Necklace_Dark:
                    if(DataManager.Instance.HasAymSkin || Inventory.GetItemQuantity(InventoryItem.ITEM_TYPE.Necklace_Dark) >= 1)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.Necklace_Light:
                    if (DataManager.Instance.HasBaalSkin || Inventory.GetItemQuantity(InventoryItem.ITEM_TYPE.Necklace_Light) >= 1)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE:
                    if (DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop >= 20)
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.TALISMAN:
                    if ((DataManager.Instance.TalismanPiecesReceivedFromMysticShop + GetTalismanPiecesRemainingToNearestWholeTalisman()) > 12)
                    {
                        return true;
                    }
                    break;
                default:
                    //not one of the limited items
                    return false;
            }

            return false;
        }

        private static int GetTalismanPiecesRemainingToNearestWholeTalisman()
        {
            //im doing something different with the talisman pieces from the mystic shop. normally, you get a talisman PIECE from the mystic shop.
            //this is a KEY_PIECE in the ITEM_TYPE enumeration. however, there's a whole bunch of stuff tied to rewarding a player with a talisman piece.
            //theres animations that play, special effects, etc. im just trying to make this thing work, and am choosing to operate on the assumption that
            //the player knows how the talisman pieces work by now. so instead of doing the whole bit, we're just using math to figure out how many talisman pieces
            //the player needs to get the next whole one from the shop, and having them buy specifically that amount. the save file flag is correctly incremented in the
            //local function GivePlayerBoughtItem() in PrefixSecondaryInteract(), and the adjustment of the cost is handled in GetTradeItem().
            //the combination of the 2 ensures that the player is still spending 1 god tear per talisman piece (as intended by the devs), 
            return 4 - DataManager.Instance.TalismanPiecesReceivedFromMysticShop % 4;
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

            TraderTrackerItems talismanPieceTTI = new TraderTrackerItems
            {
                //item_type id 37
                itemForTrade = InventoryItem.ITEM_TYPE.KEY_PIECE,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

            var ttiList =  new List<TraderTrackerItems>
            {
                necklaceGoldSkullTTI,
                necklaceDemonicTTI,
                necklaceLoyaltyTTI,
                necklaceMissionaryTTI,
                necklaceLightTTI,
                necklaceDarkTTI,
                crystalDoctrineStoneTTI,
                talismanTTI
            };

            return ttiList;

        }
    }
}
