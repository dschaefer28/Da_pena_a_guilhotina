using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    private readonly List<GameObject> interactablesInRange = new List<GameObject>();

    /// <summary>Objeto interagível mais próximo que responde agora (ou null). Alimenta o PromptDeInteracao.</summary>
    public GameObject InteragivelMaisProximo { get; private set; }
    public event Action<GameObject> OnInteragivelMaisProximoAlterado;

    /// <summary>Falso enquanto pause, inventário/prensa ou cutscene estiverem abertos (o Interagir não age no mundo).</summary>
    public bool PodeInteragirAgora
    {
        get
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return false;
            if (CutsceneLegendas.EmExibicao) return false;
            if (BibliotecaUI.Aberta) return false;
            var inventoryManager = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
            if (inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf) return false;
            return true;
        }
    }

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

    private void Update()
    {
        // Lista minúscula (1-3 objetos): recalcular todo frame é barato e mantém o aviso de interação sempre certo.
        GameObject atual = GetClosestInteractable();
        if (atual != InteragivelMaisProximo)
        {
            InteragivelMaisProximo = atual;
            OnInteragivelMaisProximoAlterado?.Invoke(atual);
        }
    }

    // === MÉTODOS PÚBLICOS (Chamados pelos Botões do Canvas no Android e pelas teclas no Windows) ===

    public void ToggleInventoryMobile()
    {
        if (CutsceneLegendas.EmExibicao) return;

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
        // Diálogo em andamento: o mesmo botão/tecla avança a conversa.
        var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : null;
        if (dialogo != null && dialogo.IsDialogueActive)
        {
            dialogo.AdvanceDialogue();
            return;
        }

        // Mesma trava do pause/inventário: não deixa interagir com o mundo (portas, prensa, NPCs...)
        // enquanto o menu de pause, o inventário/prensa ou uma cutscene estiverem abertos.
        if (!PodeInteragirAgora) return;

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
            // Ignora quem está no alcance mas não responde agora (NPC que já entregou a pista, etc.),
            // senão ele "rouba" o Interagir de um objeto útil logo ao lado.
            if (!obj.TryGetComponent<IInteractable>(out var interactable) || !interactable.PodeInteragir) continue;

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
        if (other.TryGetComponent<IInteractable>(out _))
        {
            if (!interactablesInRange.Contains(other.gameObject))
                interactablesInRange.Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        interactablesInRange.Remove(other.gameObject);
    }
}
