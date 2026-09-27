using UnityEngine;

public class TableInteractable : MonoBehaviour, IInteractable
{
    [Header("Interface")]
    [Tooltip("Arraste o GameObject do painel de Casos (UI) aqui")]
    public GameObject caseSelectionUI;

    [Header("Trava de Progressão")]
    [Tooltip("A mesa só abre depois do tutorial (caso da Fase 1 concluído). Regra de progresso: não depende de o panfleto " +
             "do tutorial ainda estar no inventário (Prompt 6).")]
    public bool exigeTutorialConcluido = true;

    [Tooltip("Opcional: item que também precisa estar no inventário. Deixe vazio; a trava do tutorial é a regra acima.")]
    public Item itemObrigatorio;

    [Tooltip("O que ele pensa se tentar mexer na mesa antes da hora?")]
    public DialogueData pensamentoBloqueado;

    [Tooltip("Aviso quando não há casos cadastrados para a fase atual.")]
    public string avisoSemCasos = "Não há novos casos na mesa por enquanto.";

    /// <summary>Falso antes do fim do tutorial (ou sem o item opcional): o aviso de interação (PromptDeInteracao) só
    /// aparece sobre a mesa quando ela realmente responde.</summary>
    public bool PodeInteragir
    {
        get
        {
            GameManager gm = GameManager.Instance;
            if (exigeTutorialConcluido && (gm == null || !gm.TutorialConcluido)) return false;
            return itemObrigatorio == null ||
                   (gm != null && gm.inventoryManager != null && gm.inventoryManager.HasItem(itemObrigatorio.itemID));
        }
    }

    public void Interact()
    {
        if (!PodeInteragir)
        {
            if (pensamentoBloqueado != null && GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            {
                GameManager.Instance.dialogueSystem.dialogueData = pensamentoBloqueado;
                GameManager.Instance.dialogueSystem.Next();
            }
            return; // Impede que o painel abra
        }

        if (caseSelectionUI.activeSelf) return;

        // Sem nenhum caso cadastrado para a fase atual (CaseData.fase), avisa em vez de abrir o painel vazio.
        CaseSelectionUI mesa = caseSelectionUI.GetComponent<CaseSelectionUI>();
        if (mesa != null && !mesa.TemCasosNaFase())
        {
            int fase = GameManager.Instance != null ? GameManager.Instance.faseAtual : 0;
            Debug.LogWarning($"[Mesa] Nenhum caso com fase {fase} na lista 'Available Cases' do painel da mesa.", mesa);
            AvisoNaTela.Mostrar(avisoSemCasos);
            return;
        }

        caseSelectionUI.SetActive(true);
        Debug.Log("O jogador abriu as cartas sobre a mesa.");
    }
}