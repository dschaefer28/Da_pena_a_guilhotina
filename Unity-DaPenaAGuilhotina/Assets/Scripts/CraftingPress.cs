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

        if (recipes == null) return;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            // Ignora receitas mal configuradas em vez de estourar NullReference ao montar o dicionário
            if (!recipe.IsValid())
            {
                Debug.LogWarning($"[CraftingPress] Receita '{recipe.name}' ignorada: ingredientes ou resultado ausentes.", this);
                continue;
            }

            string key1 = $"{recipe.ingrediente1.itemID}_{recipe.ingrediente2.itemID}";
            string key2 = $"{recipe.ingrediente2.itemID}_{recipe.ingrediente1.itemID}";

            recipeDictionary.TryAdd(key1, recipe);
            recipeDictionary.TryAdd(key2, recipe);
        }
    }

    private bool SlotsConfigurados()
    {
        if (slotInput1 == null || slotInput2 == null || slotOutput == null)
        {
            Debug.LogError("[CraftingPress] Slots de entrada/saída não atribuídos no Inspector.", this);
            return false;
        }
        return true;
    }

    private InventoryManager Inventario =>
        GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;

    public void CombineItems()
    {
        if (!SlotsConfigurados()) return;

        if (recipeDictionary == null) Start(); // caso CombineItems seja chamado antes do Start (ordem de execução)

        if (slotInput1.item == null || slotInput2.item == null)
        {
            Debug.Log("Faltam ingredientes nos slots!");
            GameFeedback.Show("Coloque duas evidências nos espaços da prensa.");
            return;
        }

        if (Inventario == null)
        {
            Debug.LogError("[CraftingPress] InventoryManager indisponível no GameManager.", this);
            return;
        }

        string attemptKey = $"{slotInput1.item.itemID}_{slotInput2.item.itemID}";

        if (recipeDictionary.TryGetValue(attemptKey, out Recipe validRecipe) && validRecipe.resultItem != null)
        {
            if (slotInput1.item.itemAmt <= 0 || slotInput2.item.itemAmt <= 0) return;
            if (validRecipe.caso != null && GameManager.Instance.casoEscolhido != validRecipe.caso)
            {
                GameFeedback.Show("Estas evidências pertencem a outro caso.");
                return;
            }
            if (GameManager.Instance.WasPublished(validRecipe.resultItem))
            {
                GameFeedback.Show("Este panfleto já foi publicado.");
                return;
            }
            if (slotOutput.item == null || slotOutput.item.itemID == validRecipe.resultItem.itemID)
            {
                ConsumeItem(slotInput1);
                ConsumeItem(slotInput2);
                ProduceItem(validRecipe.resultItem);
                GameManager.Instance.RegisterPublication(validRecipe.resultItem);
                GameFeedback.Show($"Panfleto impresso: {validRecipe.resultItem.DisplayName}\nRecolha o resultado para continuar.");
                GameFeedback.PlaySound("event:/carimbo");
                
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
                GameFeedback.Show("Recolha o panfleto anterior antes de imprimir.");
            }
        }
        else
        {
            Debug.Log("Combinação inválida! Esta mistura não gera um panfleto reconhecido.");
            GameFeedback.Show("Essas evidências não formam um panfleto. Tente outra combinação.");
        }
    }

    public bool ReturnItemsToInventory()
    {
        if (Inventario == null) return false;
        foreach (var slot in new[] { slotInput1, slotInput2, slotOutput })
        {
            if (slot == null || slot.item == null) continue;
            if (!Inventario.AddItem(slot.item, false))
            {
                GameFeedback.Show("Libere espaço no inventário para guardar os itens da prensa.");
                return false;
            }
            Inventario.ClearItemSlot(slot);
        }
        return true;
    }

    private void ConsumeItem(UISlotHandler slot)
    {
        if (slot == null || slot.item == null) return;

        slot.item.itemAmt--;
        if (slot.item.itemAmt <= 0)
        {
            Inventario.ClearItemSlot(slot);
        }
        else if (slot.itemCount != null)
        {
            slot.itemCount.text = slot.item.itemAmt.ToString();
        }
    }

    private void ProduceItem(Item resultPrefab)
    {
        if (resultPrefab == null) return;

        if (slotOutput.item == null)
        {
            Item newItem = resultPrefab.Clone();
            newItem.itemAmt = 1;
            Inventario.PlaceInInventory(slotOutput, newItem);
        }
        else
        {
            slotOutput.item.itemAmt++;
            if (slotOutput.itemCount != null)
                slotOutput.itemCount.text = slotOutput.item.itemAmt.ToString();
        }
    }
}
