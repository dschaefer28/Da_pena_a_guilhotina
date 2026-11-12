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
    [Tooltip("Nome que aparece embaixo do \"E\"/\"Interagir\" quando o jogador chega perto. Vazio = nome do GameObject.")]
    public string nomeExibido = "Alçapão";
    public string NomeExibido => NomeDoInteragivel.Resolver(nomeExibido, gameObject);

    public string nomeCenaPorao = "Porao";
    public List<RequisitosDoPorao> requisitosDeCaso;

    [Tooltip("Nenhum caso em andamento (nenhum aceito, ou o último já impresso): falta aceitar um caso na mesa.")]
    public DialogueData pensamentoSemCaso;
    [Tooltip("Fase 4 com o panfleto já impresso: o próximo passo é Dupaty e o tribunal, não a mesa. Vazio = pensamento sem caso.")]
    public DialogueData pensamentoPanfletoImpresso;
    [Tooltip("Caso em andamento sem as pistas exigidas em Requisitos De Caso (o tutorial).")]
    public DialogueData pensamentoFaltamPistas;

    [Header("Áudio (FMOD)")]
    public EventReference somAlcapao;

    public void Interact()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[TrapdoorInteractable] GameManager ausente.");
            return;
        }

        // Sem caso em andamento não há o que imprimir. O caso do tutorial continua "escolhido" depois de impresso, e o
        // alçapão dizia na Fase 2 que ainda faltavam o relato de Marie e o decreto de Dupaty.
        if (!gm.CasoAtualEmAndamento)
        {
            bool aguardaTribunal = gm.faseAtual >= GameManager.UltimaFase && gm.FaseConcluida;
            TocarPensamento(aguardaTribunal && pensamentoPanfletoImpresso != null ? pensamentoPanfletoImpresso : pensamentoSemCaso);
            return;
        }

        CaseData casoAtual = gm.casoEscolhido;
        InventoryManager inventario = gm.inventoryManager;

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

            // AudioSeguro ignora eventos vazios e nunca lança exceção se o FMOD falhar.
                AudioSeguro.TocarUmaVez(somAlcapao, transform.position);

            Debug.Log("Acessando o porão...");
            if (inventario != null)
            {
                inventario.SalvarEstadoAtual();
            }

            Time.timeScale = 1f; // garante que a próxima cena não abra congelada
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.LoadScene(nomeCenaPorao);
            else
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