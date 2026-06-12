using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input (Input Map)")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    [Header("Referências")]
    public InventoryManager inventoryManager;

    private PressInteractable currentPress;

    private void OnEnable()
    {
        if (interactAction != null) interactAction.action.Enable();
        if (toggleInventoryAction != null) toggleInventoryAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null) interactAction.action.Disable();
        if (toggleInventoryAction != null) toggleInventoryAction.action.Disable();
    }

    private void Start()
    {
        if (toggleInventoryAction != null)
            toggleInventoryAction.action.performed += ctx => OnToggleInventory();

        if (interactAction != null)
            interactAction.action.performed += ctx => OnInteract();
    }

    private void OnToggleInventory()
    {
        if (inventoryManager != null)
        {
            inventoryManager.ToggleInventory();
        }
    }
    private GameObject currentInteractableObj;

    private void OnInteract()
    {
        if (currentInteractableObj != null)
        {
            // Se for uma prensa, abre a prensa
            PressInteractable press = currentInteractableObj.GetComponent<PressInteractable>();
            if (press != null) press.Interact();

            // Se for um item coletável, pega o item
            LootInteractable loot = currentInteractableObj.GetComponent<LootInteractable>();
            if (loot != null) loot.Interact(inventoryManager);
        }
        else
        {
            Debug.Log("Não há nada para interagir aqui.");
        }
    }

    // --- FÍSICA 2D ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Agora aceita tanto a tag da Prensa quanto a tag de itens pegáveis
        if (other.CompareTag("Prensa") || other.CompareTag("Coletavel"))
        {
            currentInteractableObj = other.gameObject;
            Debug.Log($"Você encostou em: {other.name}. Aperte E!");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentInteractableObj == other.gameObject)
        {
            // Fecha a prensa se o jogador se afastar
            PressInteractable press = currentInteractableObj.GetComponent<PressInteractable>();
            if (press != null) press.ClosePressUI();

            currentInteractableObj = null;
        }
    }
}