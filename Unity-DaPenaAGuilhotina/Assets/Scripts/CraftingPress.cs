using System.Collections.Generic;
using UnityEngine;

public class CraftingPress : MonoBehaviour
{
    [Header("Slots da Prensa")]
    public UISlotHandler slotInput1;
    public UISlotHandler slotInput2;
    public UISlotHandler slotOutput;

    [Header("Configurações")]
    public List<Recipe> recipes;
    public InventoryManager inventoryManager;


    public void CombineItems() 
    {
     
        if (slotInput1.item == null || slotInput2.item == null) return;

        Recipe validRecipe = null;


        foreach (var recipe in recipes)
        {
            bool matchDirect = slotInput1.item.itemID == recipe.itemID1 && slotInput2.item.itemID == recipe.itemID2;

            bool matchReverse = slotInput1.item.itemID == recipe.itemID2 && slotInput2.item.itemID == recipe.itemID1;

            if (matchDirect || matchReverse)
            {
                validRecipe = recipe;
                break;
            }
        }

       
        if (validRecipe != null)
        {
            
            if (slotOutput.item == null || slotOutput.item.itemID == validRecipe.resultItem.itemID)
            {
                ConsumeItem(slotInput1);
                ConsumeItem(slotInput2);
                ProduceItem(validRecipe.resultItem);
            }
            else
            {
                Debug.Log("O slot de saída está ocupado com outro item!");
            }
        }
        else
        {
            Debug.Log("Combinação inválida!");
        }
    }

    private void ConsumeItem(UISlotHandler slot)
    {
        slot.item.itemAmt--;
        if (slot.item.itemAmt <= 0)
        {
            inventoryManager.ClearItemSlot(slot);
        }
        else
        {
            slot.itemCount.text = slot.item.itemAmt.ToString();
        }
    }

    private void ProduceItem(Item resultPrefab)
    {
        if (slotOutput.item == null)
        {
            
            Item newItem = resultPrefab.Clone();
            newItem.itemAmt = 1;
            inventoryManager.PlaceInInventory(slotOutput, newItem);
        }
        else
        {
            Item stackItem = resultPrefab.Clone();
            stackItem.itemAmt = 1;
            inventoryManager.StackInInventory(slotOutput, stackItem);
        }
    }
}