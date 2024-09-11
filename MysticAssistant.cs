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
            //track whether the player has bought a talisman piece or a doctrine stone from the shop this visit. used to determine if the associated screens should be shown
            bool boughtKeyPiece = false;
            bool boughtDoctrineStone = false;
            //create a list of actions to run through when the shop is closed, such as the talisman piece adding screen or the crystal doctrine tutorial
            List<Action> postShopActions = new List<Action>();
            
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

            //set the player states to inactive so they arent running around while the shop is open
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
                __instance.StartCoroutine(RunTutorialsCoroutine(postShopActions, state));
                shopItemSelector = null;
            });

            //making this a local function to limit what all needs to be passed to it
            void GivePlayerBoughtItem(TraderTrackerItems boughtItem, InventoryItem.ITEM_TYPE boughtItemType)
            {
                //deduct the item's cost from the player's inventory of the currency
                Inventory.ChangeItemQuantity((int)godTearTTI.itemForTrade, -boughtItem.SellPriceActual, 0);
                
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
                        if(!boughtDoctrineStone)
                        {
                            //on the off chance that someone is using this mod to get their first "forgotten doctrine" stone, we need to make sure that system is unlocked for them.
                            //im just assuming this works, i dont know how to edit my existing save to test it, and i dont have one laying around in this very specific state.
                            //so hopefully i read the code right and this does what i think it does! i tested it by inverting the check causing my save that would otherwise
                            //skip this if block to instead trigger it. so in theory, if someone needs to see the tutorial stuff, they will.
                            UpgradeSystem.UnlockAbility(UpgradeSystem.Type.Ritual_CrystalDoctrine, false);
                            //**************************
                            //TODO UNIVERT THIS CHECK, VERY IMPORTANT, DO NOT FORGET
                            //**************************
                            if (!DataManager.Instance.TryRevealTutorialTopic(TutorialTopic.CrystalDoctrine))
                            {
                                postShopActions.Add(ShowCrystalDoctrineTutorial);
                                postShopActions.Add(ShowCrystalDoctrineInMenu);
                            }
                            boughtDoctrineStone = true;
                        }
                        break;
                    case InventoryItem.ITEM_TYPE.TALISMAN:
                        Inventory.KeyPieces++;//because the talisman pieces are not in the standard inventory, their addition needs to be handled like this.
                        DataManager.Instance.TalismanPiecesReceivedFromMysticShop++;
                        //if the player has not bought a talisman piece this visit, add the talisman piece being added animation to the post shop actions list
                        if(!boughtKeyPiece)
                        {
                            postShopActions.Add(ShowNewTalismanPieceAnimation);
                            boughtKeyPiece = true;
                        }
                        break;
                    //TODO add the stuff to allow players to buy the mystic shop follower skins
                    case InventoryItem.ITEM_TYPE.FOUND_ITEM_FOLLOWERSKIN:
                        GivePlayerNewFollowerSkin();
                        postShopActions.Add(ShowUnlockedFollowerSkins);
                        break;
                }
                //play a pop sound
                AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
                //create a god tear that zips to the mystic shop, to look nice
                ResourceCustomTarget.Create(__instance.gameObject, playerFarming.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate () { }, true);

                //update the item selector label
                AccessTools.Method(typeof(UIItemSelectorOverlayController), "RefreshContextText").Invoke(shopItemSelector, new object[] { });
            }
        }

        private static IEnumerator RunTutorialsCoroutine(List<Action> postShopActions, StateMachine state)
        {
            //loop of the list of post shop actions and invoke them, waiting for the menus from each one to be closed before advancing to the next
            foreach(Action psa in postShopActions)
            {
                psa.Invoke();
                while (UIMenuBase.ActiveMenus.Count > 0)
                {
                    yield return null;
                }
            }

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
            UIPlayerUpgradesMenuController uiplayerUpgradesMenuController = MonoSingleton<UIManager>.Instance.PlayerUpgradesMenuTemplate.Instantiate<UIPlayerUpgradesMenuController>();
            uiplayerUpgradesMenuController.ShowCrystalUnlock();
        }

        private static void ShowNewTalismanPieceAnimation()
        {
            //shows the animation for a talisman piece being added to the current talisman being built
            UIKeyScreenOverlayController keyScreenManager = MonoSingleton<UIManager>.Instance.KeyScreenTemplate.Instantiate<UIKeyScreenOverlayController>();
            keyScreenManager.Show();
        }

        private static void ShowUnlockedFollowerSkins()
        {
            UIFollowerFormsMenuController followerFormsMenuInstance = MonoSingleton<UIManager>.Instance.FollowerFormsMenuTemplate.Instantiate<UIFollowerFormsMenuController>();
            followerFormsMenuInstance.Show(false);
        }

        private static void GivePlayerNewFollowerSkin()
        {
            //TODO probably dont need to build this list every time the player buys a skin, move this somewhere else
            List<string> shopSkins = DataManager.MysticShopKeeperSkins.ToList();
            List<string> possibleSkins = new List<string>();
            StringBuilder sb = new StringBuilder();
            DataManager.Instance.FollowerSkinsUnlocked.ForEach(u => sb.Append(u + ","));
            //Console.WriteLine($"all unlocked skins: {sb}");
            sb.Clear();

            shopSkins.ForEach(x => sb.Append(x + ","));
            Console.WriteLine($"all possible shop skins: {sb}");

            Console.WriteLine($"possible skin count: {shopSkins.Count}");
            for (int i = 0; i < shopSkins.Count - 1; i++)
            {
                Console.Write($"current skin: {shopSkins[i]}");
                if (!DataManager.GetFollowerSkinUnlocked(shopSkins[i]))
                {
                    Console.WriteLine("removing");
                    possibleSkins.Add(shopSkins[i]);
                }
                Console.WriteLine("");
            }

            sb.Clear();
            possibleSkins.ForEach(p => sb.Append(p + ","));
            Console.WriteLine($"possible skins: {sb}");
            //[Info: Console] all unlocked skins: Cat,Dog,Pig,Deer,Fox,Rabbit,Boss Mama Worm,Hedgehog,Boss Mama Maggot,Boss Burrow Worm,Horse,Shrew,
            //      Boss Beholder 1,Cow,Fish,Deer_ritual,Unicorn,Boss Egg Hopper,Boss Flying Burp Frog, Boss Mortar Hopper, Giraffe, Red Panda,
            //      Boss Spider Jump,Pangolin,Bear,Capybara,Boss Millipede Poisoner,Beetle,Boss Scorpion, Bat, Boss Beholder 2,Bison,Frog,Fennec Fox,
            //      Seahorse, Boss Spiker,Boss Charger, Axolotl, Boss Scuttle Turret, Crocodile, Otter, Hippo, Duck, Elephant, Boss Death Cat, Squirrel,
            //      Chicken, TwitchDog, Rhino, TwitchDogAlt, Nightwolf, TwitchCat, Raccoon, TwitchMouse, CultLeader 1,Penguin,Shrimp,Koala,CultLeader 2,
            //      Eagle,Owl,Lion,TwitchPoggers,Badger,Boss Beholder 3,Boss Beholder 4,CultLeader 3,CultLeader 4,Kiwi,Pelican,
            //[Info: Console] all possible shop skins: Penguin,Lion,Shrimp,Koala,Owl,Volvy,TwitchPoggers,TwitchDog,TwitchDogAlt,TwitchCat,TwitchMouse,StarBunny,Pelican,Kiwi,
            //[Info: Console] possible skins: Lion,Koala,Volvy,TwitchDog,TwitchCat,StarBunny,Kiwi,
            //it should just be Volvy and StarBunny. why is it not?

            Console.WriteLine($"mystic shop keeper skins count: {possibleSkins.Count}");
            int skinIndex = UnityEngine.Random.Range(0, possibleSkins.Count - 1);
            Console.WriteLine($"skin list random index: {skinIndex}");
            Console.WriteLine($"skin name at index: {possibleSkins[skinIndex]}");
            DataManager.SetFollowerSkinUnlocked(possibleSkins[skinIndex]);
            possibleSkins.RemoveAt(skinIndex);
        }

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
                    if (DataManager.Instance.CrystalDoctrinesReceivedFromMysticShop >= 24)//max is 24 as of the unholy alliance update
                    {
                        return true;
                    }
                    break;
                case InventoryItem.ITEM_TYPE.TALISMAN:
                    if (DataManager.Instance.TalismanPiecesReceivedFromMysticShop > 12)//max is 12 as of the unholy alliance update
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

            var ttiList =  new List<TraderTrackerItems>
            {
                necklaceGoldSkullTTI,
                necklaceDemonicTTI,
                necklaceLoyaltyTTI,
                necklaceMissionaryTTI,
                necklaceLightTTI,
                necklaceDarkTTI,
                crystalDoctrineStoneTTI,
                talismanTTI,
                followerSkinTTI
            };

            return ttiList;

        }
    }
}
