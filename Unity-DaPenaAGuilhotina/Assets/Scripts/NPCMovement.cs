using UnityEngine;
using UnityEngine.Events; // Necessário para criar a "fila" de NPCs no Inspector

public class NPCMovement : MonoBehaviour, IInteractable
{
    [Header("Configurações de Interação")]
    [Tooltip("Se falso, o jogador não consegue interagir com este NPC ainda.")]
    public bool canInteract = true;
    
    [Tooltip("O ScriptableObject com as falas específicas deste NPC.")]
    public DialogueData myDialogue; 

    [Header("Feedback Visual")]
    [Tooltip("Arraste o GameObject do contorno ou ícone que indica que o NPC quer falar.")]
    public GameObject visualIndicator; 

    [Header("Eventos (O que acontece quando o diálogo acaba?)")]
    [Tooltip("Arraste o próximo NPC aqui e chame a função EnableInteraction() dele.")]
    public UnityEvent OnDialogueComplete;

    [Header("Referências")]
    [SerializeField] private DialogueSystem dialogueSystem;

    void Awake()
    {
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
        }
    }

    void Start()
    {
        // Atualiza a UI visual logo no início do jogo
        UpdateVisualFeedback();
    }

    public void Interact()
    {
        Debug.Log("PASSO 1: O NPC recebeu o comando de interação!");
        // 1. Verifica se é a vez deste NPC
        if (!canInteract)
        {
            Debug.Log($"Ainda não é o momento de falar com {gameObject.name}.");
            return;
        }

        // 2. Alimenta o sistema com as falas DESTE NPC
        if (dialogueSystem != null && myDialogue != null)
        {
            dialogueSystem.dialogueData = myDialogue;
            
            // Assina o evento para saber quando ESTE diálogo terminar
            dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
            
            dialogueSystem.Next();
        }
        else
        {
            Debug.LogWarning("DialogueSystem ou DialogueData não configurados no NPC!");
        }
    }

    private void HandleDialogueEnded()
    {
        // Desassina o evento para não ouvir os diálogos de outros NPCs
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
        }

        // Desativa este NPC para que o jogador não fale com ele de novo (opcional)
        DisableInteraction();

        // Dispara o evento que vai "acordar" o próximo NPC na fila
        OnDialogueComplete?.Invoke();
    }

    // --- MÉTODOS DE CONTROLE ---

    public void EnableInteraction()
    {
        canInteract = true;
        UpdateVisualFeedback();
    }

    public void DisableInteraction()
    {
        canInteract = false;
        UpdateVisualFeedback();
    }

    private void UpdateVisualFeedback()
    {
        // Liga ou desliga o contorno/ícone com base na variável canInteract
        if (visualIndicator != null)
        {
            visualIndicator.SetActive(canInteract);
        }
    }

    // --- NOVO MÉTODO PARA TROCAR O TEXTO ---
    public void ChangeDialogue(DialogueData newDialogue)
    {
        // Troca o ScriptableObject antigo pelo novo
        myDialogue = newDialogue;
        Debug.Log($"{gameObject.name} agora tem um novo texto para falar!");
    }
}