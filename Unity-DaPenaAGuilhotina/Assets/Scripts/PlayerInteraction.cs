using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input (Input Map)")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    [Header("Referências")]
    public InventoryManager inventoryManager;

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
            IInteractable interactable = currentInteractableObj.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
            else
            {
                Debug.Log("O objeto atual não é interagível.");
            }
        }
        else
        {
            Debug.Log("Não há nada para interagir aqui.");
        }
    }

    // --- FÍSICA 2D ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            currentInteractableObj = other.gameObject;
            Debug.Log($"Você encostou em: {other.name}. Aperte E!");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentInteractableObj == other.gameObject)
        {
            currentInteractableObj = null;
        }
    }
}