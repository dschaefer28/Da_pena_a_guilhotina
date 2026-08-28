using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct RequisitosDoPorao
{
    public CaseData caso;
    [Tooltip("Deixe vazio (None) se não houver exigência para este slot.")]
    public Item pista1;
    [Tooltip("Deixe vazio (None) se não houver exigência para este slot.")]
    public Item pista2;
}

public class TrapdoorInteractable : MonoBehaviour, IInteractable
{
    public string nomeCenaPorao = "Porao";
    public List<RequisitosDoPorao> requisitosDeCaso;
    
    public DialogueData pensamentoSemCaso;
    public DialogueData pensamentoFaltamPistas;

    public void Interact()
    {
        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual == null)
        {
            TocarPensamento(pensamentoSemCaso);
            return;
        }

        bool temPermissaoParaDescer = false;
        bool casoEncontradoNaLista = false;

        foreach (var req in requisitosDeCaso)
        {
            if (req.caso == casoAtual)
            {
                casoEncontradoNaLista = true;

                // O pulo do gato: Se o slot da pista estiver vazio no Inspector, ele pula a checagem (considera true)
                bool temPista1 = (req.pista1 == null) || GameManager.Instance.inventoryManager.HasItem(req.pista1.itemID);
                bool temPista2 = (req.pista2 == null) || GameManager.Instance.inventoryManager.HasItem(req.pista2.itemID);

                if (temPista1 && temPista2)
                {
                    temPermissaoParaDescer = true;
                }
                break; 
            }
        }

        // Se o caso atual for da Fase 2 e nem estiver configurado nesta lista, o alçapão abre direto!
        if (!casoEncontradoNaLista)
        {
            Debug.Log($"O caso {casoAtual.name} não exige fiscalização do alçapão. Acesso livre!");
            temPermissaoParaDescer = true;
        }

        if (temPermissaoParaDescer)
        {
            Debug.Log("Acessando o porão...");
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

    private void TocarPensamento(DialogueData pensamento)
    {
        if (pensamento != null && GameManager.Instance.dialogueSystem != null)
        {
            GameManager.Instance.dialogueSystem.dialogueData = pensamento;
            GameManager.Instance.dialogueSystem.Next();
        }
    }
}