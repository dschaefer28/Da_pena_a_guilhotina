using UnityEngine;

public class LootInteractable : MonoBehaviour, IInteractable
{
    [Header("Item que o jogador vai receber")]
    public Item itemToGive;
    public int amountToGive = 1;

    [Header("Configurações")]
    public bool destroyAfterLoot = false; 
    private bool alreadyLooted = false;

    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("LootInteractable não encontrou InventoryManager na cena.");
        }
    }

    public void Interact()
    {
        if (alreadyLooted || itemToGive == null || inventoryManager == null) return;

        Item itemClone = itemToGive.Clone();
        itemClone.itemAmt = amountToGive;

        bool foiGuardado = inventoryManager.AddItem(itemClone);

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