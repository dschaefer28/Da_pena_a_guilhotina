using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configurações de Input")]
    public InputActionReference interactAction;
    public InputActionReference toggleInventoryAction;

    [Header("Alcance de interação")]
    [Tooltip("Quanto a área de interação passa da borda do corpo do player, para cada lado (unidades do mundo). " +
             "0 = só interage encostando no objeto. Só cresce na horizontal, para não pegar objetos de outro andar.")]
    [Min(0f)] public float margemDeAlcance = 0.4f;

    private Collider2D corpo;
    private ContactFilter2D filtroComTriggers;
    private readonly List<Collider2D> contatos = new List<Collider2D>();

    /// <summary>Alimenta o PromptDeInteracao.</summary>
    public GameObject InteragivelMaisProximo { get; private set; }
    public event Action<GameObject> OnInteragivelMaisProximoAlterado;

    public bool PodeInteragirAgora
    {
        get
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return false;
            if (CutsceneLegendas.EmExibicao) return false;
            if (JanelasModais.AlgumaAberta) return false;
            var inventoryManager = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
            if (inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf) return false;
            return true;
        }
    }

    private void Awake()
    {
        corpo = GetComponent<Collider2D>();
        filtroComTriggers = new ContactFilter2D { useTriggers = true };
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
        // Poucos contatos: recalcular todo frame é barato e mantém o aviso sempre certo.
        GameObject atual = GetClosestInteractable();
        if (atual != InteragivelMaisProximo)
        {
            InteragivelMaisProximo = atual;
            OnInteragivelMaisProximoAlterado?.Invoke(atual);
        }
    }

    // Chamados pelos botões do Canvas (mobile) e pelas teclas.

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
        // Antes do diálogo: com o pause aberto, nem a conversa por trás dele avança.
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;

        var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : null;
        if (dialogo != null && dialogo.IsDialogueActive)
        {
            dialogo.AdvanceDialogue();
            return;
        }

        if (!PodeInteragirAgora) return;

        GameObject closestInteractable = GetClosestInteractable();
        if (closestInteractable != null && closestInteractable.TryGetComponent<IInteractable>(out var interactable))
        {
            interactable.Interact();
        }
    }

    // Corpo alargado só na horizontal, para não pegar objetos de outro andar.
    private Bounds AreaDeInteracao()
    {
        Bounds area = corpo.bounds;
        area.Expand(new Vector3(2f * margemDeAlcance, 0f, 0f));
        return area;
    }

    private GameObject GetClosestInteractable()
    {
        if (corpo == null) return null;

        GameObject closest = null;
        float minDistance = float.MaxValue;

        Bounds area = AreaDeInteracao();
        Physics2D.OverlapBox(area.center, area.size, 0f, filtroComTriggers, contatos);
        foreach (var col in contatos)
        {
            // Só o trigger define a área do objeto, não o colisor sólido.
            if (!col.isTrigger) continue;
            GameObject obj = col.gameObject;

            // Quem não responde agora não pode "roubar" o Interagir de um objeto útil ao lado.
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

    private void OnDrawGizmosSelected()
    {
        if (corpo == null) corpo = GetComponent<Collider2D>();
        if (corpo == null) return;
        Bounds area = AreaDeInteracao();
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
