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
    
    private Dictionary<string, Recipe> recipeDictionary;

    private void Start()
    {
        recipeDictionary = new Dictionary<string, Recipe>();
        
        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;
            
            string key1 = $"{recipe.itemID1}_{recipe.itemID2}";
            string key2 = $"{recipe.itemID2}_{recipe.itemID1}";

            recipeDictionary.TryAdd(key1, recipe);
            recipeDictionary.TryAdd(key2, recipe);
        }
    }

    public void CombineItems() 
    {
        if (slotInput1.item == null || slotInput2.item == null) 
        {
            Debug.Log("Faltam ingredientes nos slots!");
            return;
        }

        string attemptKey = $"{slotInput1.item.itemID}_{slotInput2.item.itemID}";

        if (recipeDictionary.TryGetValue(attemptKey, out Recipe validRecipe))
        {
            if (slotOutput.item == null || slotOutput.item.itemID == validRecipe.resultItem.itemID)
            {
                ConsumeItem(slotInput1);
                ConsumeItem(slotInput2);
                ProduceItem(validRecipe.resultItem);
                
                // Dispara a consequência matemática da receita (GDD)
                GameManager.Instance.AplicarImpactoPanfleto(
                    validRecipe.publicOpinionImpact, 
                    validRecipe.stateOpinionImpact, 
                    validRecipe.moneyReward
                );
            }
            else
            {
                Debug.Log("O slot de saída está ocupado com outro item!");
            }
        }
        else
        {
            Debug.Log("Combinação inválida! Esta mistura não gera um panfleto reconhecido.");
        }
    }

    private void ConsumeItem(UISlotHandler slot)
    {
        slot.item.itemAmt--;
        if (slot.item.itemAmt <= 0)
        {
            GameManager.Instance.inventoryManager.ClearItemSlot(slot);
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
            GameManager.Instance.inventoryManager.PlaceInInventory(slotOutput, newItem);
        }
        else
        {
            slotOutput.item.itemAmt++;
            slotOutput.itemCount.text = slotOutput.item.itemAmt.ToString();
        }
    }
}