using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estrutura Data-Driven para mapear o caso atual às pistas que liberam o porão
[System.Serializable]
public struct RequisitosDoPorao
{
    public CaseData caso;
    public Item pista1;
    public Item pista2;
}

public class TrapdoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações de Transição")]
    [Tooltip("Nome exato da cena do porão conforme está no Build Settings.")]
    public string nomeCenaPorao = "Porao";

    [Header("Gatilhos de Missão (Quest Gates)")]
    [Tooltip("Lista dos casos e quais são as 2 pistas obrigatórias para descer.")]
    public List<RequisitosDoPorao> requisitosDeCaso;

    [Header("Feedback Narrativo (Inner Thoughts)")]
    [Tooltip("O que o personagem pensa se tentar descer sem pegar um caso na mesa?")]
    public DialogueData pensamentoSemCaso;
    
    [Tooltip("O que o personagem pensa se tentar descer sem todas as evidências?")]
    public DialogueData pensamentoFaltamPistas;

    public void Interact()
    {
        // 1. Verifica se o jogador já aceitou um caso na mesa
        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual == null)
        {
            TocarPensamento(pensamentoSemCaso);
            return;
        }

        // 2. Busca na nossa lista de requisitos quais são as pistas deste caso específico
        RequisitosDoPorao requisitosAtuais = new RequisitosDoPorao();
        bool casoConfigurado = false;

        foreach (var req in requisitosDeCaso)
        {
            if (req.caso == casoAtual)
            {
                requisitosAtuais = req;
                casoConfigurado = true;
                break;
            }
        }

        if (!casoConfigurado)
        {
            Debug.LogWarning($"O caso '{casoAtual.caseTitle}' não tem requisitos configurados no Alçapão!");
            return;
        }

        // 3. Verifica no InventoryManager otimizado (O(n)) se os itens estão na mochila
        bool temPista1 = GameManager.Instance.inventoryManager.HasItem(requisitosAtuais.pista1.itemID);
        bool temPista2 = GameManager.Instance.inventoryManager.HasItem(requisitosAtuais.pista2.itemID);

        // 4. Libera a passagem ou barra o jogador com feedback narrativo
        if (temPista1 && temPista2)
        {
            Debug.Log("Provas suficientes coletadas. Salvando inventário e acessando a tipografia...");
            
            // NOVO: Obriga o inventário a guardar as provas no GameManager
            if (GameManager.Instance.inventoryManager != null)
            {
                GameManager.Instance.inventoryManager.SalvarEstadoAtual();
            }
            
            SceneManager.LoadScene(nomeCenaPorao);
        }
        else
        {
            TocarPensamento(pensamentoFaltamPistas);
        }
    }

    // Método auxiliar para usar o sistema de diálogo já existente como "Voz da Consciência"
    private void TocarPensamento(DialogueData pensamento)
    {
        if (pensamento != null && GameManager.Instance.dialogueSystem != null)
        {
            GameManager.Instance.dialogueSystem.dialogueData = pensamento;
            GameManager.Instance.dialogueSystem.Next();
        }
        else
        {
            Debug.Log("O alçapão está trancado. Faltam evidências (Sem diálogo configurado).");
        }
    }

    
}