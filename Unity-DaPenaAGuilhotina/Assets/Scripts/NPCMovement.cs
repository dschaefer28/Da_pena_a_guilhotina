using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Estrutura que define como o NPC deve agir dependendo de qual caso o jogador escolheu
[System.Serializable]
public struct CasoReacao
{
    public CaseData caso;
    
    [Header("Fase 1: Investigação (Opcional)")]
    [Tooltip("Fala do NPC após o caso ser aceito, mas ANTES de achar a pista.")]
    public DialogueData dialogoInicialDoCaso;
    
    [Header("Fase 2: Resolução")]
    [Tooltip("O jogador precisa ter este item (Pista) para o NPC avançar a conversa.")]
    public Item pistaNecessaria;
    
    [Tooltip("Fala do NPC APÓS o jogador encontrar a pista acima.")]
    public DialogueData dialogoComPista;
    
    [Tooltip("Item entregue ao jogador ao fim do diálogo com a pista (Ex: Decreto/Papel).")]
    public Item recompensaDoDialogo;
}

public class NPCMovement : MonoBehaviour, IInteractable
{
    [Header("Configurações Base")]
    public bool canInteract = true;
    [Tooltip("Desmarque isso para NPCs Mentores (como Dupaty) para permitir falar com eles várias vezes.")]
    public bool disableAfterDialogue = true;

    [Header("Diálogo de Fallback (Padrão)")]
    [Tooltip("Diálogo padrão quando nenhum caso foi escolhido ainda (Ex: 'Vá até a mesa pegar um caso').")]
    public DialogueData dialogoPadrao;
    
    [Header("Restrição Estrita (Opcional)")]
    [Tooltip("Preencha APENAS se este NPC for figurante exclusivo de um caso (Ex: Marie). Deixe VAZIO para o Dupaty.")]
    public CaseData casoObrigatorio;

    [Header("Reações por Caso (GDD)")]
    [Tooltip("Configure aqui como o NPC reage a cada caso diferente.")]
    public List<CasoReacao> reacoesDeCaso;

    [Header("Feedback e Eventos")]
    public GameObject visualIndicator;
    public UnityEvent OnDialogueComplete;

    // Guarda temporariamente qual item o NPC vai dar ao fim da conversa atual
    private Item recompensaPendente;

    void Start() { UpdateVisualFeedback(); }

    public void Interact()
    {
        if (!canInteract) return;

        // 1. Trava estrita para NPCs figurantes (Ignora se for vazio)
        if (casoObrigatorio != null && GameManager.Instance.casoEscolhido != casoObrigatorio)
        {
            Debug.Log($"{gameObject.name} ignora você.");
            return;
        }

        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogueSystem == null) return;

        // 2. Define o roteiro assumindo o Padrão inicialmente
        DialogueData dialogoParaTocar = dialogoPadrao; 
        recompensaPendente = null;

        CaseData casoAtual = GameManager.Instance.casoEscolhido;

        // 3. Procura a reação do NPC baseada no caso da mesa
        if (casoAtual != null && reacoesDeCaso != null)
        {
            foreach (var reacao in reacoesDeCaso)
            {
                if (reacao.caso == casoAtual)
                {
                    // Checa se o jogador tem a pista usando o InventoryManager centralizado
                    bool temAPista = false;
                    if (reacao.pistaNecessaria != null && GameManager.Instance.inventoryManager != null)
                    {
                        temAPista = GameManager.Instance.inventoryManager.HasItem(reacao.pistaNecessaria.itemID);
                    }

                    // Se tem a pista, toca a fala de resolução
                    if (temAPista && reacao.dialogoComPista != null)
                    {
                        dialogoParaTocar = reacao.dialogoComPista;
                        
                        // Garante que o jogador não ganhe o Decreto duplicado se conversar de novo
                        bool jaTemRecompensa = false;
                        if (reacao.recompensaDoDialogo != null)
                        {
                            jaTemRecompensa = GameManager.Instance.inventoryManager.HasItem(reacao.recompensaDoDialogo.itemID);
                        }

                        if (reacao.recompensaDoDialogo != null && !jaTemRecompensa)
                        {
                            recompensaPendente = reacao.recompensaDoDialogo;
                        }
                    }
                    // Se não tem a pista, toca a dica inicial do caso
                    else if (reacao.dialogoInicialDoCaso != null)
                    {
                        dialogoParaTocar = reacao.dialogoInicialDoCaso;
                    }
                    
                    break; // Achou o caso correspondente, não precisa olhar a lista inteira
                }
            }
        }

        // 4. Inicia o Sistema de Diálogos
        if (dialogoParaTocar != null)
        {
            dialogueSystem.dialogueData = dialogoParaTocar;
            dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
            dialogueSystem.Next();
        }
        else
        {
            Debug.LogWarning("Nenhum diálogo configurado para este estado do jogo.");
        }
    }

    private void HandleDialogueEnded()
    {
        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogueSystem != null) dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;

        // Entrega o item silenciosamente e de forma natural
        if (recompensaPendente != null && GameManager.Instance.inventoryManager != null)
        {
            Item recompensa = recompensaPendente.Clone();
            recompensa.itemAmt = 1;
            GameManager.Instance.inventoryManager.AddItem(recompensa);
            
            Debug.Log($"[SISTEMA] O NPC {gameObject.name} te entregou: {recompensa.name}");
            recompensaPendente = null; 
        }

        // Trava o NPC apenas se a opção estiver marcada no Inspector
        if (disableAfterDialogue) DisableInteraction();
        
        OnDialogueComplete?.Invoke();
    }

    public void EnableInteraction() { canInteract = true; UpdateVisualFeedback(); }
    public void DisableInteraction() { canInteract = false; UpdateVisualFeedback(); }

    private void UpdateVisualFeedback()
    {
        if (visualIndicator != null) visualIndicator.SetActive(canInteract);
    }
}