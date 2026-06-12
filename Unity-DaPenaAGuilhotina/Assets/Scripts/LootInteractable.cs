using UnityEngine;

public class LootInteractable : MonoBehaviour
{
    [Header("Item que o jogador vai receber")]
    public Item itemToGive;
    public int amountToGive = 1;

    [Header("Configurações")]
    public bool destroyAfterLoot = false; 
    private bool alreadyLooted = false;   

    public void Interact(InventoryManager inventory)
    {
        if (alreadyLooted || itemToGive == null) return;

        Item itemClone = itemToGive.Clone();
        itemClone.itemAmt = amountToGive;

        bool foiGuardado = inventory.AddItem(itemClone);

        if (foiGuardado)
        {
            Debug.Log($"Você coletou: {itemToGive.name}!");
            
            if (destroyAfterLoot)
            {
                Destroy(gameObject); 
            }
            else
            {
                alreadyLooted = true;
                Debug.Log("A estante agora está vazia.");
            }
        }
    }
}