using System;
using System.IO;
using UnityEngine;

public static class MMInventoryBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/Inventory20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);

        GameObject player = null;
        GameObject pickupGo = null;
        GameObject repeatGo = null;

        try
        {
            player = new GameObject("MM_InventoryQA_Player");
            MMInventory inventory = player.AddComponent<MMInventory>();
            inventory.showInventory = false;
            inventory.EditorClearForQa();

            bool invalidPass =
                !inventory.AddItem("", 1, "Invalid") &&
                !inventory.AddItem("qa.relic", 0, "Relic") &&
                !inventory.RemoveItem("qa.relic", 1) &&
                !inventory.HasItem("qa.relic", 1);

            bool addPass =
                inventory.AddItem("qa.relic", 2, "Ancient Relic") &&
                inventory.AddItem("qa.relic", 3) &&
                inventory.GetCount("qa.relic") == 5 &&
                inventory.HasItem("qa.relic", 5) &&
                inventory.UniqueItemCount == 1;

            bool insufficientPass =
                !inventory.RemoveItem("qa.relic", 6) &&
                inventory.GetCount("qa.relic") == 5;

            bool removePass =
                inventory.RemoveItem("qa.relic", 2) &&
                inventory.GetCount("qa.relic") == 3 &&
                inventory.RemoveItem("qa.relic", 3) &&
                inventory.GetCount("qa.relic") == 0 &&
                inventory.UniqueItemCount == 0;

            bool renameSetup =
                inventory.AddItem("qa.scroll", 1, "Old Scroll") &&
                inventory.SetDisplayName("qa.scroll", "Royal Scroll");
            MMInventoryEntry scroll = null;
            foreach (MMInventoryEntry entry in inventory.Items)
                if (entry.id == "qa.scroll")
                    scroll = entry;

            bool renamePass =
                renameSetup &&
                scroll != null &&
                scroll.displayName == "Royal Scroll" &&
                scroll.quantity == 1;

            pickupGo = new GameObject("MM_InventoryQA_Pickup");
            pickupGo.AddComponent<SphereCollider>();
            MMItemPickup pickup = pickupGo.AddComponent<MMItemPickup>();
            pickup.itemId = "qa.gem";
            pickup.displayName = "Azure Gem";
            pickup.quantity = 2;
            pickup.collectOnce = true;
            pickup.deactivateOnCollect = true;

            bool pickupInitialPass =
                pickup.CanInteract(player) &&
                pickup.Prompt == "Pick up Azure Gem";

            pickup.Interact(player);

            bool pickupPass =
                inventory.GetCount("qa.gem") == 2 &&
                pickup.CollectedCount == 1 &&
                !pickupGo.activeSelf &&
                !pickup.CanInteract(player);

            repeatGo = new GameObject("MM_InventoryQA_RepeatPickup");
            repeatGo.AddComponent<SphereCollider>();
            MMItemPickup repeat =
                repeatGo.AddComponent<MMItemPickup>();
            repeat.itemId = "qa.coin";
            repeat.displayName = "Coin";
            repeat.quantity = 3;
            repeat.collectOnce = false;
            repeat.deactivateOnCollect = false;

            repeat.Interact(player);
            repeat.Interact(player);

            bool repeatPass =
                repeatGo.activeSelf &&
                repeat.CollectedCount == 2 &&
                inventory.GetCount("qa.coin") == 6;

            bool pass =
                invalidPass &&
                addPass &&
                insufficientPass &&
                removePass &&
                renamePass &&
                pickupInitialPass &&
                pickupPass &&
                repeatPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "invalid_operations|pass=" + invalidPass,
                    "stacking|pass=" + addPass +
                    "|count=5|unique=1",
                    "insufficient_remove|pass=" + insufficientPass +
                    "|count=5",
                    "remove_to_zero|pass=" + removePass +
                    "|count=0|unique=0",
                    "rename|pass=" + renamePass +
                    "|name=" + (scroll != null
                        ? scroll.displayName
                        : "NULL"),
                    "pickup_initial|pass=" + pickupInitialPass +
                    "|prompt=" + pickup.Prompt,
                    "one_shot_pickup|pass=" + pickupPass +
                    "|gemCount=" + inventory.GetCount("qa.gem") +
                    "|collectedCount=" + pickup.CollectedCount +
                    "|active=" + pickupGo.activeSelf,
                    "repeat_pickup|pass=" + repeatPass +
                    "|coinCount=" + inventory.GetCount("qa.coin") +
                    "|collectedCount=" + repeat.CollectedCount,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Inventory batch QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (repeatGo)
                UnityEngine.Object.DestroyImmediate(repeatGo);
            if (pickupGo)
                UnityEngine.Object.DestroyImmediate(pickupGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}
