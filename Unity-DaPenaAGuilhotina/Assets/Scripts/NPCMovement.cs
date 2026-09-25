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
    [Tooltip("ID estável e ÚNICO nesta cena. Cobrança de horas e recompensas entregues são guardadas por caso + este ID " +
             "(não pelo nome do GameObject). Vazio = caminho na hierarquia. Preencha com Ferramentas > Investigação > " +
             "Gerar IDs de interação. Não troque depois que houver saves: o progresso guardado aponta para ele.")]
    public string idDaInteracao;
    public bool canInteract = true;
    [Tooltip("Desmarque isso para NPCs Mentores (como Dupaty) para permitir falar com eles várias vezes.")]
    public bool disableAfterDialogue = true;
    [Tooltip("Horas de investigação gastas na primeira conversa com este NPC no caso (RelogioDeInvestigacao). " +
             "Só conta na cena de investigação do caso em andamento.")]
    [Min(0)] public int custoEmHoras = 1;

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

    // Itens que o NPC vai dar ao fim da conversa atual, e de qual caso/etapa eles são
    private List<Item> recompensasPendentes = new List<Item>();
    private CaseData casoDaConversa;
    private string etapaDaConversa;
    private bool aguardandoFimDoDialogo;

    void Start() { UpdateVisualFeedback(); }

    private void OnDestroy()
    {
        if (aguardandoFimDoDialogo && GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            GameManager.Instance.dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
    }

    /// <summary>"cena/id" desta interação (ver IdDeInteracao).</summary>
    public string ChaveDaInteracao => IdDeInteracao.ChaveDaCena(this, idDaInteracao);

    // Usado pelo Player (alvo mais próximo) e pelo aviso de interação: só conta como interagível
    // enquanto ainda responde. Mesma regra dos dois primeiros "return" de Interact().
    public bool PodeInteragir =>
        canInteract && (casoObrigatorio == null || GameManager.Instance == null || GameManager.Instance.casoEscolhido == casoObrigatorio);

    public void Interact()
    {
        if (!canInteract) return;

        if (casoObrigatorio != null && GameManager.Instance.casoEscolhido != casoObrigatorio)
        {
            Debug.Log($"{gameObject.name} ignora você.");
            return;
        }

        DialogueSystem dialogueSystem = GameManager.Instance.dialogueSystem;
        if (dialogueSystem == null || dialogueSystem.IsDialogueActive) return;

        DialogueData dialogoParaTocar = dialogoPadrao;
        List<Item> recompensasDaEtapa = null;
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
                        recompensasDaEtapa = reacao.recompensasDoDialogo;
                    }
                    else if (reacao.dialogoInicialDoCaso != null)
                    {
                        dialogoParaTocar = reacao.dialogoInicialDoCaso;
                        recompensasDaEtapa = reacao.recompensasDoDialogo;
                    }

                    break;
                }
            }
        }

        // Sem fala para tocar a interação não acontece: não cobra horas nem deixa o fim do diálogo pendurado.
        if (dialogoParaTocar == null || dialogoParaTocar.talkScript == null || dialogoParaTocar.talkScript.Count == 0)
        {
            Debug.LogWarning($"[NPC] {gameObject.name}: nenhum diálogo configurado para este estado do jogo.", this);
            return;
        }

        if (!RelogioDeInvestigacao.TentarGastar(this, idDaInteracao, custoEmHoras)) return;

        casoDaConversa = casoAtual;
        etapaDaConversa = RegistroDaInvestigacao.EtapaReacao;
        PrepararRecompensas(recompensasDaEtapa);

        dialogueSystem.dialogueData = dialogoParaTocar;
        dialogueSystem.OnDialogueEnded -= HandleDialogueEnded; // nunca inscrever duas vezes
        dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
        aguardandoFimDoDialogo = true;
        dialogueSystem.Next();
    }

    /// <summary>Lista o que ainda não foi entregue NESTE caso/interação/etapa. Não depende do que está no
    /// inventário agora: uma pista gasta na prensa não volta ao revisitar o NPC.</summary>
    private void PrepararRecompensas(List<Item> itensConfigurados)
    {
        recompensasPendentes.Clear();
        if (itensConfigurados == null || casoDaConversa == null) return;

        GameManager gm = GameManager.Instance;
        RegistroDaInvestigacao registro = gm.registroDaInvestigacao;
        string interacao = ChaveDaInteracao;
        foreach (Item item in itensConfigurados)
        {
            if (item == null || string.IsNullOrEmpty(item.itemID) || recompensasPendentes.Contains(item)) continue;
            if (registro.RecompensaEntregue(casoDaConversa, interacao, etapaDaConversa, item.itemID)) continue;

            // Save v2 (sem registro de entregas): quem já tem a pista a recebeu pela regra antiga.
            if (registro.CasoComEstadoLegado(casoDaConversa) && gm.inventoryManager != null && gm.inventoryManager.HasItem(item.itemID))
            {
                registro.RegistrarRecompensa(casoDaConversa, interacao, etapaDaConversa, item.itemID);
                continue;
            }
            recompensasPendentes.Add(item);
        }
    }

    private void HandleDialogueEnded()
    {
        aguardandoFimDoDialogo = false;
        GameManager gm = GameManager.Instance;
        DialogueSystem dialogueSystem = gm != null ? gm.dialogueSystem : null;
        if (dialogueSystem != null) dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;

        // Entrega item por item e só registra o que entrou de fato. Inventário cheio: o que faltou fica para
        // a próxima conversa (que não cobra horas de novo, já está paga).
        bool faltouEntregar = false;
        if (recompensasPendentes.Count > 0 && gm != null && gm.inventoryManager != null)
        {
            string interacao = ChaveDaInteracao;
            foreach (Item itemPendente in recompensasPendentes)
            {
                Item recompensa = itemPendente.Clone();
                recompensa.itemAmt = 1;
                if (gm.inventoryManager.AddItem(recompensa))
                {
                    gm.registroDaInvestigacao.RegistrarRecompensa(casoDaConversa, interacao, etapaDaConversa, itemPendente.itemID);
                    Debug.Log($"[SISTEMA] O NPC {gameObject.name} te entregou: {recompensa.name}");
                }
                else faltouEntregar = true;
            }
            if (faltouEntregar)
                AvisoNaTela.Mostrar("Inventário cheio. Libere espaço e fale de novo para receber o que faltou.");
        }
        recompensasPendentes.Clear();

        // Só desliga quem foi configurado para isso (disableAfterDialogue); com entrega pendente, continua
        // acessível para o jogador voltar e receber o resto.
        if (disableAfterDialogue && !faltouEntregar) DisableInteraction();

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