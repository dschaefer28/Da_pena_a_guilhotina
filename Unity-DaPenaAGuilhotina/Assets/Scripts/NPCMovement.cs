using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    
    [Tooltip("Lista de itens entregues ao jogador ao fim do diálogo (Ex: Decreto/Papel).")]
    public List<Item> recompensasDoDialogo; // Transformado em Lista
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

    private bool blockEvents = false;

    [Header("Reações por Caso (GDD)")]
    [Tooltip("Configure aqui como o NPC reage a cada caso diferente.")]
    public List<CasoReacao> reacoesDeCaso;

    [Header("Feedback e Eventos")]
    public GameObject visualIndicator;
    public UnityEvent OnDialogueComplete;

    // Guarda temporariamente a lista de itens que o NPC vai dar ao fim da conversa atual
    private List<Item> recompensasPendentes = new List<Item>();

    void Start() { UpdateVisualFeedback(); }

    public void Interact()
    {
        if (!canInteract) return;

        if (casoObrigatorio != null && GameManager.Instance.casoEscolhido != casoObrigatorio)
        {
            Debug.Log($"{gameObject.name} ignora você.");
            return;
        }

        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogueSystem == null) return;

        DialogueData dialogoParaTocar = dialogoPadrao; 
        recompensasPendentes.Clear(); // Limpa a lista antes de cada interação

        CaseData casoAtual = GameManager.Instance.casoEscolhido;

        if (casoAtual != null && reacoesDeCaso != null)
        {
            foreach (var reacao in reacoesDeCaso)
            {
                if (reacao.caso == casoAtual)
                {
                    bool temAPista = false;
                    if (reacao.pistaNecessaria != null && GameManager.Instance.inventoryManager != null)
                    {
                        temAPista = GameManager.Instance.inventoryManager.HasItem(reacao.pistaNecessaria.itemID);
                    }

                    if (temAPista && reacao.dialogoComPista != null)
                    {
                        dialogoParaTocar = reacao.dialogoComPista;
                        PrepararRecompensas(reacao.recompensasDoDialogo);
                    }
                    else if (reacao.dialogoInicialDoCaso != null)
                    {
                        dialogoParaTocar = reacao.dialogoInicialDoCaso;
                        PrepararRecompensas(reacao.recompensasDoDialogo);
                    }
                    
                    break; 
                }
            }
        }

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

    // Rotina auxiliar para verificar e listar os itens que faltam no inventário
    private void PrepararRecompensas(List<Item> itensConfigurados)
    {
        if (itensConfigurados != null && GameManager.Instance.inventoryManager != null)
        {
            foreach (Item item in itensConfigurados)
            {
                if (item != null && !GameManager.Instance.inventoryManager.HasItem(item.itemID))
                {
                    recompensasPendentes.Add(item);
                }
            }
        }
    }

    private void HandleDialogueEnded()
    {
        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogueSystem != null) dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;

        // Entrega todos os itens da lista de uma vez
        if (recompensasPendentes.Count > 0 && GameManager.Instance.inventoryManager != null)
        {
            foreach (Item itemPendente in recompensasPendentes)
            {
                Item recompensa = itemPendente.Clone();
                recompensa.itemAmt = 1;
                GameManager.Instance.inventoryManager.AddItem(recompensa);
                Debug.Log($"[SISTEMA] O NPC {gameObject.name} te entregou: {recompensa.name}");
            }
            
            recompensasPendentes.Clear();
            DisableInteraction();
        }

        if (disableAfterDialogue) DisableInteraction();
        
        if (!blockEvents)
        {
            OnDialogueComplete?.Invoke();
        }
    }

    public void EnableInteraction() { canInteract = true; UpdateVisualFeedback(); }
    public void DisableInteraction() { canInteract = false; UpdateVisualFeedback(); }

    private void UpdateVisualFeedback()
    {
        if (visualIndicator != null) visualIndicator.SetActive(canInteract);
    }

    public void ChangeDefaultDialogue(DialogueData newDialogue)
    {
        dialogoPadrao = newDialogue;
        Debug.Log($"O roteiro padrão de {gameObject.name} foi atualizado para uma nova conversa!");
    }

    public void BlockFutureEvents()
    {
        blockEvents = true;
        Debug.Log($"Os eventos futuros de {gameObject.name} foram bloqueados!");
    }
}