using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    private List<GameObject> interactablesInRange = new List<GameObject>();

    private void OnEnable()
{
    if (interactAction != null)
    {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteractPerformed;
    }
    if (toggleInventoryAction != null)
    {
        toggleInventoryAction.action.Enable();
        toggleInventoryAction.action.performed += OnToggleInventoryPerformed;
    }
}

private void OnDisable()
{
    if (interactAction != null)
    {
        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();
    }
    if (toggleInventoryAction != null)
    {
        toggleInventoryAction.action.performed -= OnToggleInventoryPerformed;
        toggleInventoryAction.action.Disable();
    }
}

private void OnInteractPerformed(InputAction.CallbackContext ctx) => InteractMobile();
private void OnToggleInventoryPerformed(InputAction.CallbackContext ctx) => ToggleInventoryMobile();

    // === MÉTODOS PÚBLICOS (Chamados pelos Botões do Canvas no Android) ===
    
    public void ToggleInventoryMobile()
    {
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("O Player tentou abrir o inventário, mas a UI não foi encontrada!");
        }
    }

    public void InteractMobile()
    {
        if (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
        {
            if (GameManager.Instance.dialogueSystem.IsDialogueActive)
            {
                GameManager.Instance.dialogueSystem.AdvanceDialogue();
                return;
            }
        }

        GameObject closestInteractable = GetClosestInteractable();
        if (closestInteractable != null && closestInteractable.TryGetComponent<IInteractable>(out var interactable))
        {
            interactable.Interact();
        }
    }

    // === LÓGICA DE FÍSICA ===
    private GameObject GetClosestInteractable()
    {
        GameObject closest = null;
        float minDistance = float.MaxValue;
        
        interactablesInRange.RemoveAll(item => item == null || !item.activeInHierarchy);
        foreach (var obj in interactablesInRange)
        {
            float dist = Vector2.SqrMagnitude((Vector2)(transform.position - obj.transform.position));
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = obj;
            }
        }
        return closest;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (!interactablesInRange.Contains(other.gameObject))
                interactablesInRange.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (interactablesInRange.Contains(other.gameObject))
            interactablesInRange.Remove(other.gameObject);
    }
}