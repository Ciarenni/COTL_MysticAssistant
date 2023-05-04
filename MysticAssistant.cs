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
        //static FieldInfo f_someField = AccessTools.Field(typeof(Interaction_MysticShop), nameof(Interaction_MysticShop.Label));
        //static MethodInfo m_MyExtraMethod = SymbolExtensions.GetMethodInfo(() => MysticAssistant.AssistantSecondaryLabel());
        private static readonly Type patchType = typeof(MysticAssistant);

        //private void Awake2()
        //{
        //    // Plugin startup logic
        //    Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        //    Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        //    // my patch
        //    //harmony.Patch(
        //    //    AccessTools.Method(typeof(MainMenu), "Start"),
        //    //    new HarmonyMethod(AccessTools.Method(typeof(MysticAssistant), "MysticAssistantPatch"))
        //    //    );

        //    //var classProcessor = harmony.CreateClassProcessor(typeof(Interaction_MysticShop));
        //    //classProcessor.Patch().Add(AccessTools.Method(typeof(MysticAssistant), "TranspilerAssistant"));

        //    harmony.Patch(
        //        AccessTools.Method(typeof(Interaction_MysticShop), "GetLabel"),
        //        new HarmonyMethod(AccessTools.Method(typeof(TranspilerClass), "Transpiler"))//try splitting the transpiler method into its own class and boostrapping it with PatchAll(?) like the harmony wiki says
        //        );

        //    // Plugin startup logic
        //    Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        //}

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Harmony harmony = new Harmony(id: "cultofthelamb.ciarenni.mysticassistant.main");

            // my patch
            //harmony.Patch(
            //    AccessTools.Method(typeof(MainMenu), "Start"),
            //    new HarmonyMethod(AccessTools.Method(typeof(MysticAssistant), "MysticAssistantPatch"))
            //    );

            //var classProcessor = harmony.CreateClassProcessor(typeof(Interaction_MysticShop));
            //classProcessor.Patch().Add(AccessTools.Method(typeof(MysticAssistant), "TranspilerAssistant"));

            //harmony.Patch(
            //    AccessTools.Method(typeof(Interaction_MysticShop), "GetLabel"),
            //    new HarmonyMethod(AccessTools.Method(typeof(TranspilerClass), "Transpiler"))//try splitting the transpiler method into its own class and boostrapping it with PatchAll(?) like the harmony wiki says
            //    );

            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "Start"), postfix: new HarmonyMethod(patchType, nameof(SetUpMysticAssistant)));
            harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), "OnSecondaryInteract"), prefix: new HarmonyMethod(patchType, nameof(PrefixSecondaryInteract)));
            //harmony.Patch(AccessTools.Method(typeof(Interaction_MysticShop), nameof(Interaction_MysticShop.OnInteract)), transpiler: new HarmonyMethod(patchType, nameof(TranspilerAssistant)));

            Console.WriteLine("prefixes: " + harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo).SelectMany(p => p.Prefixes).Count());
            Console.WriteLine("postfixes: " + harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo).SelectMany(p => p.Postfixes).Count());
            Console.WriteLine("transpilers: " + harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo).SelectMany(p => p.Transpilers).Count());
            Console.WriteLine("finalizers: " + harmony.GetPatchedMethods().Select(Harmony.GetPatchInfo).SelectMany(p => p.Finalizers).Count());
        }

        [HarmonyPrefix]
        public static void MysticAssistantPatch(MainMenu __instance)
        {
            UIMenuConfirmationWindow uimenuConfirmationWindow = __instance.Push<UIMenuConfirmationWindow>(MonoSingleton<UIManager>.Instance.ConfirmationWindowTemplate);
            uimenuConfirmationWindow.Configure("Welcome into my modded game", "This is my first mod and I created a popup ! :)");

            uimenuConfirmationWindow.OnConfirm = (Action)Delegate.Combine(uimenuConfirmationWindow.OnConfirm, new Action(delegate ()
            {
                Console.WriteLine("popup launched !");
            }));
            uimenuConfirmationWindow.OnCancel = (Action)Delegate.Combine(uimenuConfirmationWindow.OnCancel, new Action(delegate ()
            {
                Console.WriteLine("popup canceled :<");
            }));
        }

        public static void SetUpMysticAssistant(Interaction_MysticShop __instance)
        {
            Console.WriteLine("POSTfix start");
            __instance.SecondaryLabel = "Mod label";
            __instance.HasSecondaryInteraction = true;

            

            
        }

        public static void PrefixSecondaryInteract(Interaction_MysticShop __instance, StateMachine state)
        {
            
            Console.WriteLine("prefix secondary");

            HUD_Manager.Instance.Hide(false, 0, false);

            

            TraderTrackerItems godTearTTI = new TraderTrackerItems
            {
                itemForTrade = InventoryItem.ITEM_TYPE.GOD_TEAR,
                BuyPrice = 1,
                BuyOffset = 0,
                SellPrice = 1,
                SellOffset = 0,
                LastDayChecked = TimeManager.CurrentDay
            };

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

            TraderTracker TraderInfo = new TraderTracker();
            TraderInfo.itemsToTrade = new List<TraderTrackerItems>
            {
                necklaceDarkTTI,
                necklaceLightTTI,
                necklaceGoldSkullTTI,
                necklaceDemonicTTI,
                necklaceLoyaltyTTI,
                necklaceMissionaryTTI
            };

            state.CURRENT_STATE = StateMachine.State.InActive;

            var itemsForSale = new List<InventoryItem>();
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Dark, 999));
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Light, 999));
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Gold_Skull, 999));
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Demonic, 999));
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Loyalty, 999));
            itemsForSale.Add(new InventoryItem(InventoryItem.ITEM_TYPE.Necklace_Missionary, 999));


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
                Console.WriteLine("chosen item: " + chosenItem.ToString());
                Console.WriteLine("chosen item id: " + (int)chosenItem);
                TraderTrackerItems tradeItem = GetTradeItem(TraderInfo, chosenItem);
                
                Inventory.ChangeItemQuantity((int)godTearTTI.itemForTrade, -tradeItem.SellPriceActual, 0);
                Inventory.ChangeItemQuantity((int)chosenItem, 1, 0);
                AudioManager.Instance.PlayOneShot("event:/followers/pop_in", __instance.gameObject);
                ResourceCustomTarget.Create(__instance.gameObject, PlayerFarming.Instance.transform.position, InventoryItem.ITEM_TYPE.GOD_TEAR, delegate ()
                {
                    //if (__instance is UnityEngine.Component)
                    //{
                    //    Console.WriteLine("instance is a component");
                    //    Console.WriteLine("instance position: " + __instance.gameObject.transform.position.ToString());
                    //    Console.WriteLine("player position: " + PlayerFarming.Instance.transform.position.ToString());
                    //    InventoryItem.Spawn(chosenItem, 1, PlayerFarming.Instance.transform.position, 4f, null);
                    //}

                }, true);
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
            Console.WriteLine("getting cost");
            Console.WriteLine("item is type: " + item.ToString());
            foreach (TraderTrackerItems traderTrackerItems in traderInfo.itemsToTrade)
            {
                if (traderTrackerItems.itemForTrade == item)
                {
                    Console.WriteLine("item found, cost actual: " + traderTrackerItems.SellPriceActual);
                    return traderTrackerItems;
                }
            }
            Console.WriteLine("item not found");
            return null;
        }

        //private TraderTrackerItems SetCostProvider(InventoryItem.ITEM_TYPE itemType)
        //{
        //    TraderTrackerItems godTearTTI = new TraderTrackerItems();
        //    godTearTTI.BuyPrice = 1;
        //    godTearTTI.BuyOffset = 0;
        //    godTearTTI.SellPrice = 1;
        //    godTearTTI.SellOffset = 0;
        //    godTearTTI.LastDayChecked = TimeManager.CurrentDay;

        //    return godTearTTI;
        //}

        ////[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> TranspilerAssistant(IEnumerable<CodeInstruction> instructions)
        //{
        //    Console.WriteLine("transpiler on interact");

        //    List<CodeInstruction> instructionsList = instructions.ToList();
        //    for(int i = 0; i < instructionsList.Count; i++)
        //    {
        //        CodeInstruction instruction = instructionsList[i];
        //        Console.WriteLine(instruction);
        //        yield return instruction;
        //    }

        //    //foreach (var instruction in instructionsList)
        //    //{
        //    //    Console.WriteLine(instruction.ToString());
        //    //}

        //    //return instructions;




        //    //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    //{
        //    //    // do something
        //    //}

        //    //if (this.DeadWorshipper.StructureInfo == null)
        //    //{
        //    //    base.SecondaryLabel = "";
        //    //    return;
        //    //}
        //    //if (this.DeadWorshipper.StructureInfo.BodyWrapped)
        //    //{
        //    //    if (this.DeadWorshipper.followerInfo != null && this.DeadWorshipper.followerInfo.Necklace != InventoryItem.ITEM_TYPE.NONE)
        //    //    {
        //    //        this.SecondaryInteractable = true;
        //    //        base.SecondaryLabel = ScriptLocalization.Interactions.TakeLoot;
        //    //        return;
        //    //    }
        //    //    this.SecondaryInteractable = false;
        //    //    base.SecondaryLabel = "";
        //    //    return;
        //    //}
        //    //else
        //    //{
        //    //    if (this.DeadWorshipper.StructureInfo.Rotten)
        //    //    {
        //    //        base.SecondaryLabel = this.sHarvestRottenMeat;
        //    //        return;
        //    //    }
        //    //    base.SecondaryLabel = this.sHarvestMeat;
        //    //    return;
        //    //}
        //}
    }

    //[HarmonyPatch(typeof(Interaction_MysticShop))]
    //[HarmonyPatch(nameof(Interaction_MysticShop.GetLabel))]
    //public static class TranspilerClass
    //{
    //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        Console.WriteLine("getting label");
    //        foreach (var instruction in instructions)
    //        {
    //            Console.WriteLine(instruction.ToString());
    //        }

    //        return instructions;
    //    }
    //}
}
