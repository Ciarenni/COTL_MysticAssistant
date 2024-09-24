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
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;

namespace MysticAssistant
{
    [BepInPlugin("ciarenni.cultofthelamb.mysticassistant", "Mystic Assistant", "2.0.0")]
    public class MysticAssistant : BaseUnityPlugin
    {
        private static readonly Type patchType = typeof(MysticAssistant);
        private static string SHOP_CONTEXT_KEY = "mystic_assistant_shop";
        private static MysticAssistantInventoryManager _inventoryManager = new MysticAssistantInventoryManager();
        //create a list of actions to run through when the shop is closed, such as the talisman piece adding screen or the crystal doctrine tutorial
        private static List<Action> _postShopActions = new List<Action>();

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
            //reset the inventory manager each time the shop is accessed, to make sure we have the most up to date information
            _inventoryManager.ResetInventory();
            //track whether the player has bought a talisman piece or a doctrine stone from the shop this visit. used to determine if the associated screens should be shown

            _postShopActions.Clear();
            
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

            //set the player states to inactive so they arent running around while the shop is open
            PlayerFarming playerFarming = state.GetComponent<PlayerFarming>();
            PlayerFarming.SetStateForAllPlayers(StateMachine.State.InActive, false, null);
            playerFarming.GoToAndStop(playerFarming.transform.position, playerFarming.LookToObject, false, false, null, 20f, true, null, true, true, true, true, null);

            //set up the item selector to be our shop, mimicing what the seed shop does
            UIItemSelectorOverlayController shopItemSelector = MonoSingleton<UIManager>.Instance.ShowItemSelector(
                playerFarming,
                _inventoryManager.GetShopInventory(),
                new ItemSelector.Params
                {
                    Key = SHOP_CONTEXT_KEY,
                    Context = ItemSelector.Context.Buy,
                    Offset = new Vector2(0f, 150f),
                    ShowEmpty = true,
                    RequiresDiscovery = false,
                    HideQuantity = false,
                    ShowCoins = false,
                    AllowInputOnlyFromPlayer = playerFarming
                }
            );
            if (__instance.InputOnlyFromInteractingPlayer)
            {
                MonoSingleton<UINavigatorNew>.Instance.AllowInputOnlyFromPlayer = playerFarming;
            }

            //set up a delegate that returns the cost of the item based on the item_type passed to it
            shopItemSelector.CostProvider = GetTraderTrackerItemFromItemType;

            //set up what happens when the player confirms the item from the selector. combine the current actions OnItemChosen is doing with the new behaviour we want
            shopItemSelector.OnItemChosen = (Action<InventoryItem.ITEM_TYPE>)Delegate.Combine(
                shopItemSelector.OnItemChosen,
                new Action<InventoryItem.ITEM_TYPE>(delegate (InventoryItem.ITEM_TYPE chosenItemType)
                {
                    //get the non-inventory of the chosen item
                    TraderTrackerItems tradeItem = GetTraderTrackerItemFromItemType(chosenItemType);
                    //check to see if the user is attempting to buy something that might not be needed
                    if (MysticAssistantInventoryInfo.CheckForBoughtQuantityWarning(tradeItem))
                    {
                        UIMenuConfirmationWindow uimenuConfirmationWindow = shopItemSelector.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
                        uimenuConfirmationWindow.Configure("Confirm purchase", "You have already bought the maximum legitimate amount of this item. Are you sure you want to buy more?", false);
                        uimenuConfirmationWindow.OnConfirm = (Action)Delegate.Combine(uimenuConfirmationWindow.OnConfirm, new Action(delegate ()
                        {
                            //if they acknowledge the warning, proceed to the purchase
                            GivePlayerBoughtItem(__instance, shopItemSelector, playerFarming, tradeItem, chosenItemType);
                        }));
                    }
                    else
                    {
                        //if no warning needs to be shown, just proceed to the purchase
                        GivePlayerBoughtItem(__instance, shopItemSelector, playerFarming, tradeItem, chosenItemType);
                    }
                }));

            //on canceling out of the shop, show the HUD again
            shopItemSelector.OnCancel = (Action)Delegate.Combine(
                shopItemSelector.OnCancel,
                new Action(delegate ()
                {
                    //TODO uncomment
                    //SetMysticShopInteractable(__instance, false);
                    //__instance.StartCoroutine(RunTutorialsCoroutine(__instance, state));
                    HUD_Manager.Instance.Show(0, false);
                }));

            //on hiding the shop, we need to run through any screens that might need to be shown to the player.
            //this is done in a coroutine that will also set the player's state back to idle from inactive.
            //setting player back to idle needs to be done in the coroutine so that it can properly wait for the post shop screens to be done displaying
            //so that control isnt returned too early causing the player to accidentally use the primary interact on the mystic shop.
            shopItemSelector.OnHidden += new Action(delegate ()
            {
                //im not entirely sure why i need to set the state to inactive here when the player should still be inactive from the initial mystic assistant interaction
                //but if i dont, it allows the player to move and interact with the talisman key piece screen still up.
                //this is especially bad because if they player does not move and hits the Accept button, they are standing right next to the Mystic Shopkeeper
                //and will spend a god tear to spin the wheel without realizing it.
                //setting the state to inactive again prevents that from happening.
                //speculation: because this is attached to the Interaction_MysticShop interaction, the state being changed in the actual function is what causes this.
                //  i have no easy way to verify this and it honestly doesn't matter because this is the cleanest, easiest solution to the problem.
                PlayerFarming.SetStateForAllPlayers(StateMachine.State.InActive, false, null);
                //TODO uncomment
                //SetMysticShopInteractable(__instance, false);
                __instance.StartCoroutine(RunTutorialsCoroutine(__instance, state));
                shopItemSelector = null;
            });

            //making this a local function to limit what all needs to be passed to it
            
        }

        private static void GivePlayerBoughtItem(Interaction_MysticShop __instance, UIItemSelectorOverlayController shopItemSelector, PlayerFarming playerFarming,
                                                    TraderTrackerItems boughtItem, InventoryItem.ITEM_TYPE boughtItemType)
        {
            //deduct the item's cost from the player's inventory of the currency
            Inventory.ChangeItemQuantity((int)InventoryItem.ITEM_TYPE.GOD_TEAR, -boughtItem.SellPriceActual, 0);

            //add item to player's inventory/collection and set or adjust game flags as appropriate
            switch (boughtItemType)
            {
                case InventoryItem.ITEM_TYPE.Necklace_Dark:
                    Inventory.ChangeItemQuantity((int)boughtItemType, 1, 0);
                    DataManager.Instance.HasAymSkin = true;
                    break;
                case InventoryItem.ITEM_TYPE.Necklace_Light:
                    Inventory.ChangeItemQuantity((int)boughtItemType, 1, 0);
                    DataManager.Instance.HasBaalSkin = true;
                    break;
                case InventoryItem.ITEM_TYPE.CRYSTAL_DOCTRINE_STONE:
                    Inventory.ChangeItemQuantity((int)boughtItemType, 1, 0);
                    DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop++;
                    //if the player has not bought a doctrine stone this visit, check if the tutorial needs to be shown, and add it to the post shop actions list if so
                    if (!_inventoryManager.BoughtCrystalDoctrineStone)
                    {
                        //on the off chance that someone is using this mod to get their first "forgotten doctrine" stone, we need to make sure that system is unlocked for them.
                        //im just assuming this works, i dont know how to edit my existing save to test it, and i dont have one laying around in this very specific state.
                        //so hopefully i read the code right and this does what i think it does! i tested it by inverting the check causing my save that would otherwise
                        //skip this if block to instead trigger it. so in theory, if someone needs to see the tutorial stuff, they will.
                        UpgradeSystem.UnlockAbility(UpgradeSystem.Type.Ritual_CrystalDoctrine, false);
                        if (DataManager.Instance.TryRevealTutorialTopic(TutorialTopic.CrystalDoctrine))
                        {
                            _postShopActions.Add(ShowCrystalDoctrineTutorial);
                            _postShopActions.Add(ShowCrystalDoctrineInMenu);
                        }
                        _inventoryManager.SetBoughtCrystalDoctrineStoneFlag(true);
                    }
                    break;
                case InventoryItem.ITEM_TYPE.TALISMAN:
                    Inventory.KeyPieces++;//because the talisman pieces are not in the standard inventory, their addition needs to be handled like this.
                    DataManager.Instance.TalismanPiecesReceivedFromMysticShop++;
                    //if the player has not bought a talisman piece this visit, add the talisman piece being added animation to the post shop actions list
                    if (!_inventoryManager.BoughtKeyPiece)
                    {
                        _postShopActions.Add(ShowNewTalismanPieceAnimation);
                        _inventoryManager.SetBoughtKeyPieceStoneFlag(true);
                    }
                    break;
                case InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN:
                    //randomly select a skin from the available pool. there are 14 skins available from the shop (as of the unholy alliance update), so while
                    //i wrote this mod to give players agency over what they get from the mystic shop, having a shop inventory that includes:
                    //      every skin
                    //      every relic
                    //      every tarot card
                    //      every decoration
                    //would be quite the lengthy list, especially visually. this is the compromise i have settled on.
                    int skinIndex = UnityEngine.Random.Range(0, _inventoryManager.GetCountOfAvailableFollowerSkins() - 1);
                    string skinToUnlock = _inventoryManager.GetFollowerSkinNameByIndex(skinIndex);
                    DataManager.SetFollowerSkinUnlocked(skinToUnlock);
                    _inventoryManager.RemoveFollowerSkinFromShopList(skinToUnlock);
                    //we only need to adjust the stock of items we want to truly be limited: things with limited size collections like follower skins
                    _inventoryManager.ChangeShopStockByQuantity(boughtItemType, -1);

                    if (!_inventoryManager.BoughtFollowerSkin)
                    {
                        _postShopActions.Add(ShowUnlockedFollowerSkins);
                        _inventoryManager.SetBoughtFollowerSkinStoneFlag(true);
                    }
                    break;
            }
            //play a pop sound
            AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
            //create a god tear that zips to the mystic shop, to look nice
            ResourceCustomTarget.Create(__instance.gameObject, playerFarming.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate () { }, true);

            //update the item selector label
            AccessTools.Method(typeof(UIItemSelectorOverlayController), "RefreshContextText").Invoke(shopItemSelector, new object[] { });
        }

        private static IEnumerator RunTutorialsCoroutine(Interaction_MysticShop instance, StateMachine state)
        {
            //loop of the list of post shop actions and invoke them, waiting for the menus from each one to be closed before advancing to the next
            foreach(Action psa in _postShopActions)
            {
                psa.Invoke();
                while (UIMenuBase.ActiveMenus.Count > 0)
                {
                    yield return null;
                }
            }

            //TODO uncomment
            //yield return new WaitForSeconds(1);
            //SetMysticShopInteractable(instance, true);

            //returns control back to the player(s) and sets them back to idle from inactive
            foreach (PlayerFarming playerFarming in PlayerFarming.players)
            {
                if (playerFarming.GoToAndStopping)
                {
                    playerFarming.AbortGoTo(true);
                }
            }
            PlayerFarming.SetStateForAllPlayers((LetterBox.IsPlaying || MMConversation.isPlaying) ? StateMachine.State.InActive : StateMachine.State.Idle, false, null);
            state.CURRENT_STATE = StateMachine.State.Idle;
        }

        private static void ShowCrystalDoctrineTutorial()
        {
            //shows the crystal doctrine tutorial pop ups
            UITutorialOverlayController menu = MonoSingleton<UIManager>.Instance.ShowTutorialOverlay(TutorialTopic.CrystalDoctrine, 0f);
            menu.Show();
        }

        private static void ShowCrystalDoctrineInMenu()
        {
            //shows where crystal doctrine upgrade are tracked in the menu
            UIPlayerUpgradesMenuController uiplayerUpgradesMenuController = MonoSingleton<UIManager>.Instance.PlayerUpgradesMenuTemplate.Instantiate();
            uiplayerUpgradesMenuController.ShowCrystalUnlock();
        }

        private static void ShowNewTalismanPieceAnimation()
        {
            //shows the animation for a talisman piece being added to the current talisman being built
            UIKeyScreenOverlayController keyScreenManager = MonoSingleton<UIManager>.Instance.KeyScreenTemplate.Instantiate();
            keyScreenManager.Show();
        }

        private static void ShowUnlockedFollowerSkins()
        {
            //taken from the FollowerSkinShop class, this just shows the unlocked forms screen
            UIFollowerFormsMenuController followerFormsMenuInstance = MonoSingleton<UIManager>.Instance.FollowerFormsMenuTemplate.Instantiate();
            followerFormsMenuInstance.Show(false);
        }

        private static void SetMysticShopInteractable(Interaction_MysticShop instance, bool activeFlag)
        {
            instance.Interactable = activeFlag;
        }

        //this isnt referenced or used at this point, keeping it around for notes
        private static void GivePlayerNewFollowerSkin(List<string> possibleSkins)
        {
            //TODO can i just... make new item_types for each of the follower skins? i probably can, right?
            //there is an InventoryIconMapping class that seems to be connecting ITEM_TYPEs with images to be displayed.
            //i think the images for followers are ultimately SkeletonGraphics.
            //i went from UIFollowerFormsMenucontroller to IndoctrinationFormItem to IndoctrinationCharacterItem to SkeletonGraphic.
            //this seems to loop back around into WorshipperData which has a list of WorshipperData.CharacterSkin that has SkeletonData in it.
            //on further review, im pretty sure the IndoctrinationCharacterItem is what is populating the unlocked follower skin menu
            List<WorshipperData.SkinAndData> temp = WorshipperData.Instance.GetSkinsAll();
            WorshipperData.SkinAndData firstSkin = temp[0];
            Console.WriteLine($"first skin: {firstSkin.Skin[0].Skin}");
            //so to get this working, i would need to modify the InventoryIconMapping._itemMap Dictionary to include a new ITEM_TYPE and a Sprite.
            //and then i would need to figure out how to get what i assume are SkeletonGraphics to display as Sprites, let alone figuring out where the hell those are stored/addressed.
            //but thats if i wanted to show the actual form. i could still do that and just show the rolled up scroll and display the follower name...
            //so what i need to do is:
            //1) modify InventoryItem to expand the ITEM_TYPE enum with all the follower forms. or just cast an int as the ITEM_TYPE. current max ITEM_TYPE value is 164. start in the 1000s though
            //2) figure out where it is setting what will be the shop label for that InventoryItem and make it say the follower name
            //3) add a new entry to the InventoryIconMapping private variable _itemMap with these new ITEM_TYPEs
            //4) add each individual skin to the shop list here

        }

        //this is pulled from the seed shop's interaction functionality, as the authentic mystic shop does not include it and it's needed for the CostProvider on the ItemSelector
        //given a list of non-inventory items and an item_type, return the non-inventory item of that type from the provided list
        private static TraderTrackerItems GetTraderTrackerItemFromItemType(InventoryItem.ITEM_TYPE specifiedType)
        {
            //loop over all items in the shop's list
            foreach (TraderTrackerItems shopItem in MysticAssistantInventoryInfo.GetMysticAssistantShopItemTypeList())
            {
                //if the item from the list matches the specified type
                if (shopItem.itemForTrade == specifiedType)
                {
                    return shopItem;
                }
            }
            return null;
        }
    }
}
