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

    [Header("Etapas complementares (Prompt 3)")]
    [Tooltip("Conversas posteriores que entregam pistas/documentos complementares quando os pré-requisitos já foram " +
             "obtidos (mesmo que gastos depois). Têm prioridade sobre a fala normal enquanto houver algo a entregar.")]
    public List<EtapaComplementar> etapasComplementares;
}

[System.Serializable]
public struct EtapaComplementar
{
    [Tooltip("ID estável da etapa (vai para o registro de recompensas). Não troque depois que houver saves.")]
    public string id;
    [Tooltip("Evidências que o jogador precisa ter obtido alguma vez (GameManager.evidenciasObtidas).")]
    public List<Item> requisitos;
    public DialogueData dialogo;
    public List<Item> recompensas;
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
        string etapa = RegistroDaInvestigacao.EtapaReacao;
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

                    // Etapa complementar só depois que a fala normal não tem mais nada a entregar.
                    if (!TemRecompensaPendente(recompensasDaEtapa, casoAtual, etapa) &&
                        EtapaComplementarPendente(reacao, casoAtual, out EtapaComplementar complementar))
                    {
                        dialogoParaTocar = complementar.dialogo;
                        recompensasDaEtapa = complementar.recompensas;
                        etapa = complementar.id;
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

        casoDaConversa = casoAtual;
        etapaDaConversa = etapa;
        // Recompensas só para o caso em andamento: um caso concluído (ou nenhum) só conversa.
        PrepararRecompensas(GameManager.Instance.CasoAtualEmAndamento ? recompensasDaEtapa : null);

        // Inventário cheio (não há descarte): se nada do que o NPC entregaria cabe, a conversa não cobra horas.
        InventoryManager inventario = GameManager.Instance.inventoryManager;
        if (recompensasPendentes.Count > 0 && inventario != null && !recompensasPendentes.Exists(inventario.TemEspacoPara))
        {
            recompensasPendentes.Clear();
            AvisoNaTela.Mostrar("Inventário cheio: não há onde guardar o que ele tem para você. Libere espaço antes de conversar.");
            return;
        }

        if (!RelogioDeInvestigacao.TentarGastar(this, idDaInteracao, custoEmHoras))
        {
            recompensasPendentes.Clear();
            return;
        }

        dialogueSystem.dialogueData = dialogoParaTocar;
        dialogueSystem.OnDialogueEnded -= HandleDialogueEnded; // nunca inscrever duas vezes
        dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
        aguardandoFimDoDialogo = true;
        dialogueSystem.Next();
    }

    private bool TemRecompensaPendente(List<Item> recompensas, CaseData caso, string etapa)
    {
        if (recompensas == null) return false;
        foreach (Item r in recompensas)
            if (AindaPendente(r, caso, etapa)) return true;
        return false;
    }

    /// <summary>Mesmo critério para escolher a etapa e para entregar: não registrado como entregue neste
    /// caso/interação/etapa e, num caso vindo de save v2 (sem registro), o jogador ainda não tem o item.</summary>
    private bool AindaPendente(Item item, CaseData caso, string etapa)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID)) return false;
        GameManager gm = GameManager.Instance;
        if (gm.registroDaInvestigacao.RecompensaEntregue(caso, ChaveDaInteracao, etapa, item.itemID)) return false;
        return !(gm.registroDaInvestigacao.CasoComEstadoLegado(caso) && gm.inventoryManager != null &&
                 gm.inventoryManager.PossuiEmQualquerLugar(item.itemID));
    }

    /// <summary>Primeira etapa complementar com todos os pré-requisitos já obtidos e alguma recompensa ainda não entregue.</summary>
    private bool EtapaComplementarPendente(CasoReacao reacao, CaseData caso, out EtapaComplementar pendente)
    {
        pendente = default;
        if (reacao.etapasComplementares == null) return false;
        GameManager gm = GameManager.Instance;
        foreach (EtapaComplementar e in reacao.etapasComplementares)
        {
            if (string.IsNullOrWhiteSpace(e.id) || e.id == RegistroDaInvestigacao.EtapaReacao || e.dialogo == null || e.recompensas == null)
            {
                Debug.LogWarning($"[NPC] {name}: etapa complementar ignorada (id vazio, id reservado \"{RegistroDaInvestigacao.EtapaReacao}\", sem fala ou sem recompensas).", this);
                continue;
            }
            bool requisitosOk = true;
            if (e.requisitos != null)
                foreach (Item r in e.requisitos)
                    if (r != null && !gm.EvidenciaObtida(r)) { requisitosOk = false; break; }
            if (!requisitosOk) continue;

            if (TemRecompensaPendente(e.recompensas, caso, e.id))
            {
                pendente = e;
                return true;
            }
        }
        return false;
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
            if (AindaPendente(item, casoDaConversa, etapaDaConversa)) { recompensasPendentes.Add(item); continue; }

            // Save v2 (sem registro de entregas): quem já tem a pista a recebeu pela regra antiga — registra agora.
            if (registro.CasoComEstadoLegado(casoDaConversa) && !registro.RecompensaEntregue(casoDaConversa, interacao, etapaDaConversa, item.itemID))
                registro.RegistrarRecompensa(casoDaConversa, interacao, etapaDaConversa, item.itemID);
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