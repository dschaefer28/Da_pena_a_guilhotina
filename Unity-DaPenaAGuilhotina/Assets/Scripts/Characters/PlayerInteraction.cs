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
        // Pause aberto: nem o mundo nem a conversa por trás dele respondem ao Interagir (antes o E avançava o diálogo
        // escondido pelo menu).
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;

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
    // Antes o alcance era o próprio corpo encostando no trigger do objeto, então o player quase entrava nas coisas.
    // Agora a área é o corpo alargado margemDeAlcance para cada lado, consultada a cada frame.
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
            // Só o trigger do objeto conta como a área dele, como antes (o colisor sólido não).
            if (!col.isTrigger) continue;
            GameObject obj = col.gameObject;

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

    // Retângulo amarelo na Scene view com o Player selecionado: mostra até onde ele alcança.
    private void OnDrawGizmosSelected()
    {
        if (corpo == null) corpo = GetComponent<Collider2D>();
        if (corpo == null) return;
        Bounds area = AreaDeInteracao();
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
