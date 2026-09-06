using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;

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

    [Header("Áudio (FMOD)")]
    public EventReference somAlcapao;

    public void Interact()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TrapdoorInteractable] GameManager ausente.");
            return;
        }

        CaseData casoAtual = GameManager.Instance.casoEscolhido;
        if (casoAtual == null)
        {
            TocarPensamento(pensamentoSemCaso);
            return;
        }

        InventoryManager inventario = GameManager.Instance.inventoryManager;

        bool temPermissaoParaDescer = false;
        bool casoEncontradoNaLista = false;

        if (requisitosDeCaso != null)
        {
            foreach (var req in requisitosDeCaso)
            {
                if (req.caso != casoAtual) continue;

                casoEncontradoNaLista = true;

                // O pulo do gato: Se o slot da pista estiver vazio no Inspector, ele pula a checagem (considera true)
                bool temPista1 = (req.pista1 == null) || (inventario != null && inventario.HasItem(req.pista1.itemID));
                bool temPista2 = (req.pista2 == null) || (inventario != null && inventario.HasItem(req.pista2.itemID));

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
            if (string.IsNullOrEmpty(nomeCenaPorao) || !Application.CanStreamedLevelBeLoaded(nomeCenaPorao))
            {
                Debug.LogError($"[TrapdoorInteractable] Cena '{nomeCenaPorao}' não está nas Build Settings.", this);
                return;
            }

            if (!somAlcapao.IsNull)
                RuntimeManager.PlayOneShot(somAlcapao, transform.position);

            Debug.Log("Acessando o porão...");
            if (inventario != null)
            {
                inventario.SalvarEstadoAtual();
            }

            PauseManager.ForceReset();
            SceneManager.LoadScene(nomeCenaPorao);
        }
        else
        {
            TocarPensamento(pensamentoFaltamPistas);
        }
    }

    private void TocarPensamento(DialogueData pensamento)
    {
        if (pensamento == null) return;
        if (GameManager.Instance == null || GameManager.Instance.dialogueSystem == null)
        {
            Debug.LogWarning("[TrapdoorInteractable] DialogueSystem ausente; não foi possível tocar o pensamento.");
            return;
        }

        GameManager.Instance.dialogueSystem.dialogueData = pensamento;
        GameManager.Instance.dialogueSystem.Next();
    }
}
