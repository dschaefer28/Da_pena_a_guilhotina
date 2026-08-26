using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    private List<GameObject> interactablesInRange = new List<GameObject>();

    // ARQUITETURA BLINDADA: Fazemos a assinatura segura dos inputs no OnEnable
    private void OnEnable()
    {
        if (interactAction != null) 
        {
            interactAction.action.Enable();
            // Vincula o teclado à função real (sem usar '=>')
            interactAction.action.performed += OnInteractPerformed;
        }

        if (toggleInventoryAction != null) 
        {
            toggleInventoryAction.action.Enable();
            toggleInventoryAction.action.performed += OnToggleInventoryPerformed;
        }
    }

    // ARQUITETURA BLINDADA: Cancelamos a assinatura no OnDisable para evitar "Delegate Leaks"
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

    // O Start agora fica limpo, pois os Inputs são gerenciados pelo ciclo de vida do objeto
    private void Start()
    {
        // Mantido limpo intencionalmente
    }

    // Método exigido pela arquitetura do Novo Input System (recebe o contexto da tecla)
    private void OnToggleInventoryPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("O Player tentou abrir o inventário, mas o GameManager não encontrou a UI nesta cena!");
        }
    }

    // Método exigido pela arquitetura do Novo Input System
    private void OnInteractPerformed(InputAction.CallbackContext context)
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

        if (closestInteractable != null)
        {
            IInteractable interactable = closestInteractable.GetComponent<IInteractable>();
            if (interactable != null) interactable.Interact();
        }
        else
        {
            Debug.Log("Não há nada próximo para interagir.");
        }
    }

    private GameObject GetClosestInteractable()
    {
        GameObject closest = null;
        float minDistance = float.MaxValue;
        
        interactablesInRange.RemoveAll(item => item == null);

        foreach (var obj in interactablesInRange)
        {
            float dist = Vector2.Distance(transform.position, obj.transform.position);
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
            {
                interactablesInRange.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (interactablesInRange.Contains(other.gameObject))
        {
            interactablesInRange.Remove(other.gameObject);
        }
    }
}