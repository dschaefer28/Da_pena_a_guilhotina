using System.Collections.Generic;
using FMODUnity;
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
    public List<Item> recompensasDoDialogo;

    [Tooltip("Opcional. Fala curta depois que todas as recompensas desta reação já foram entregues (ex: 'Vá para o porão'). " +
             "Vazio = repete a fala da reação, sem entregar nada de novo.")]
    public DialogueData dialogoDepoisDaEntrega;

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
    [Tooltip("Nome que aparece embaixo do \"E\"/\"Interagir\" quando o jogador chega perto. Vazio = nome do GameObject.")]
    public string nomeExibido;
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

    [Header("Virar para o jogador")]
    [Tooltip("Ao começar a conversa o NPC (e o jogador) se viram um para o outro. Todos os sprites de NPC são desenhados " +
             "olhando para a direita, como o do jogador; desmarque para um NPC que não deve virar (sentado, de perfil fixo...).")]
    public bool virarParaOJogador = true;
    [Tooltip("Ao fim da conversa o NPC volta a olhar para o lado em que estava.")]
    public bool voltarADirecaoOriginal = true;

    [Header("Feedback e Eventos")]
    public GameObject visualIndicator;
    public UnityEvent OnDialogueComplete;

    [Header("Áudio (FMOD)")]
    [Tooltip("Som tocado quando este NPC entrega algum item/pista ao jogador no fim do diálogo (ex.: event:/Pegaritem).")]
    public EventReference somEntregaItem;

    private List<Item> recompensasPendentes = new List<Item>();
    private CaseData casoDaConversa;
    private string etapaDaConversa;
    private bool aguardandoFimDoDialogo;
    private InventoryManager inventarioObservado;
    private bool entregandoRecompensas;

    void Start()
    {
        UpdateVisualFeedback();
        ObservarRequisitosDasEtapas();
        // Volta à cena/save com etapa liberada e não recebida: lembra o jogador.
        if (EtapaLiberadaParaVoltar(null)) AvisarEtapaLiberada();
    }

    private void OnDestroy()
    {
        if (aguardandoFimDoDialogo && GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            GameManager.Instance.dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
        if (inventarioObservado != null) inventarioObservado.OnItemAdicionado -= AoReceberItem;
    }

    /// <summary>"cena/id" desta interação (ver IdDeInteracao).</summary>
    public string ChaveDaInteracao => IdDeInteracao.ChaveDaCena(this, idDaInteracao);

    // Mesma regra dos dois primeiros "return" de Interact().
    public bool PodeInteragir =>
        canInteract && (casoObrigatorio == null || GameManager.Instance == null || GameManager.Instance.casoEscolhido == casoObrigatorio);

    public string NomeExibido => NomeDoInteragivel.Resolver(nomeExibido, gameObject);

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
                    // Tudo entregue: fala curta no lugar de repetir a conversa inteira.
                    else if (reacao.dialogoDepoisDaEntrega != null && recompensasDaEtapa != null &&
                             recompensasDaEtapa.Exists(r => r != null && !string.IsNullOrEmpty(r.itemID)) &&
                             !TemRecompensaPendente(recompensasDaEtapa, casoAtual, etapa))
                    {
                        dialogoParaTocar = reacao.dialogoDepoisDaEntrega;
                        recompensasDaEtapa = null;
                    }

                    break;
                }
            }
        }

        // Sem fala, não cobra horas nem deixa o fim do diálogo pendurado.
        if (dialogoParaTocar == null || dialogoParaTocar.talkScript == null || dialogoParaTocar.talkScript.Count == 0)
        {
            Debug.LogWarning($"[NPC] {gameObject.name}: nenhum diálogo configurado para este estado do jogo.", this);
            return;
        }

        casoDaConversa = casoAtual;
        etapaDaConversa = etapa;
        // Caso concluído (ou nenhum) só conversa.
        PrepararRecompensas(GameManager.Instance.CasoAtualEmAndamento ? recompensasDaEtapa : null);

        // Inventário cheio (não há descarte): se nada cabe, a conversa não cobra horas.
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

        IniciarConversa(dialogueSystem, dialogoParaTocar);
    }

    private void IniciarConversa(DialogueSystem dialogueSystem, DialogueData dialogo)
    {
        dialogueSystem.dialogueData = dialogo;
        dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
        dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
        aguardandoFimDoDialogo = true;
        VirarUmParaOOutro();
        dialogueSystem.Next();
    }

    // ===== Virar para o jogador =====

    private SpriteRenderer sprite;
    private bool virado;          // flipX original guardado e ainda não devolvido
    private bool flipXOriginal;

    // Sprites de NPC e do jogador olham para a direita com flipX = false (PlayerMove usa a mesma convenção).
    private void VirarUmParaOOutro()
    {
        if (!virarParaOJogador) return;
        PlayerMove jogador = FindFirstObjectByType<PlayerMove>();
        if (jogador == null) return;
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) return;

        float dx = jogador.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        if (!virado) { flipXOriginal = sprite.flipX; virado = true; }
        sprite.flipX = dx < 0f;
        jogador.OlharParaX(transform.position.x);
    }

    private void VoltarADirecaoOriginal()
    {
        if (!virado) return;
        virado = false;
        if (voltarADirecaoOriginal && sprite != null) sprite.flipX = flipXOriginal;
    }

    /// <summary>A primeira da lista, como em Interact.</summary>
    private bool ReacaoDoCaso(CaseData caso, out CasoReacao reacao)
    {
        reacao = default;
        if (caso == null || reacoesDeCaso == null) return false;
        foreach (CasoReacao r in reacoesDeCaso)
            if (r.caso == caso) { reacao = r; return true; }
        return false;
    }

    // Etapa já liberada continua na mesma conversa (já paga): senão exigiria uma segunda conversa sem nada que a indicasse.
    private bool ContinuarNaEtapaLiberada(DialogueSystem dialogueSystem)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || dialogueSystem == null || casoDaConversa == null || casoDaConversa != gm.casoEscolhido ||
            !gm.CasoAtualEmAndamento || !canInteract)
            return false;
        if (!ReacaoDoCaso(casoDaConversa, out CasoReacao reacao) ||
            !EtapaComplementarPendente(reacao, casoDaConversa, out EtapaComplementar etapa))
            return false;

        etapaDaConversa = etapa.id;
        PrepararRecompensas(etapa.recompensas);
        InventoryManager inventario = gm.inventoryManager;
        if (recompensasPendentes.Count == 0 || (inventario != null && !recompensasPendentes.Exists(inventario.TemEspacoPara)))
        {
            // Sem espaço: a etapa espera a próxima conversa.
            recompensasPendentes.Clear();
            return false;
        }

        IniciarConversa(dialogueSystem, etapa.dialogo);
        return true;
    }

    // ===== Aviso de etapa liberada =====

    private void ObservarRequisitosDasEtapas()
    {
        bool temEtapas = reacoesDeCaso != null &&
                         reacoesDeCaso.Exists(r => r.etapasComplementares != null && r.etapasComplementares.Count > 0);
        GameManager gm = GameManager.Instance;
        if (!temEtapas || gm == null || gm.inventoryManager == null) return;
        inventarioObservado = gm.inventoryManager;
        inventarioObservado.OnItemAdicionado -= AoReceberItem;
        inventarioObservado.OnItemAdicionado += AoReceberItem;
    }

    // Avisa quem já conversou com o NPC que vale voltar; quem não conversou recebe a etapa na primeira conversa.
    private void AoReceberItem(Item item)
    {
        // Item vindo deste NPC: a conversa já segue para a etapa.
        if (entregandoRecompensas) return;
        if (item != null && EtapaLiberadaParaVoltar(item)) AvisarEtapaLiberada();
    }

    /// <summary>Etapa liberada e não entregue num NPC já pago neste caso. Com <paramref name="requisito"/>, só a que o exige.</summary>
    private bool EtapaLiberadaParaVoltar(Item requisito)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.CasoAtualEmAndamento || !gameObject.activeInHierarchy || !PodeInteragir) return false;
        if (!RelogioDeInvestigacao.JaPaga(this, idDaInteracao)) return false;
        return ReacaoDoCaso(gm.casoEscolhido, out CasoReacao reacao) &&
               EtapaComplementarPendente(reacao, gm.casoEscolhido, out _, requisito);
    }

    private void AvisarEtapaLiberada() =>
        AvisoNaTela.Mostrar($"Talvez {NomeExibido} queira ver o que você encontrou.");

    private bool TemRecompensaPendente(List<Item> recompensas, CaseData caso, string etapa)
    {
        if (recompensas == null) return false;
        foreach (Item r in recompensas)
            if (AindaPendente(r, caso, etapa)) return true;
        return false;
    }

    /// <summary>Mesmo critério para escolher a etapa e para entregar. Em save v2 (sem registro), vale ainda não ter o item.</summary>
    private bool AindaPendente(Item item, CaseData caso, string etapa)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID)) return false;
        GameManager gm = GameManager.Instance;
        if (gm.registroDaInvestigacao.RecompensaEntregue(caso, ChaveDaInteracao, etapa, item.itemID)) return false;
        if (Deducao.Descartada(gm, item)) return false;
        return !(gm.registroDaInvestigacao.CasoComEstadoLegado(caso) && gm.inventoryManager != null &&
                 gm.inventoryManager.PossuiEmQualquerLugar(item.itemID));
    }

    /// <summary>Primeira etapa com requisitos obtidos e recompensa pendente. Com <paramref name="soComRequisito"/>, só as que o exigem.</summary>
    private bool EtapaComplementarPendente(CasoReacao reacao, CaseData caso, out EtapaComplementar pendente, Item soComRequisito = null)
    {
        pendente = default;
        if (reacao.etapasComplementares == null) return false;
        GameManager gm = GameManager.Instance;
        foreach (EtapaComplementar e in reacao.etapasComplementares)
        {
            if (soComRequisito != null &&
                (e.requisitos == null || !e.requisitos.Exists(r => r != null && r.itemID == soComRequisito.itemID)))
                continue;
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

    /// <summary>Não depende do inventário atual: pista gasta na prensa não volta ao revisitar o NPC.</summary>
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

        // Só registra o que entrou; com inventário cheio, o resto fica para a próxima conversa (já paga).
        bool faltouEntregar = false;
        bool entregouAlgum = false;
        if (recompensasPendentes.Count > 0 && gm != null && gm.inventoryManager != null)
        {
            string interacao = ChaveDaInteracao;
            entregandoRecompensas = true;
            try
            {
                foreach (Item itemPendente in recompensasPendentes)
                {
                    Item recompensa = itemPendente.Clone();
                    recompensa.itemAmt = 1;
                    if (gm.inventoryManager.AddItem(recompensa))
                    {
                        entregouAlgum = true;
                        gm.registroDaInvestigacao.RegistrarRecompensa(casoDaConversa, interacao, etapaDaConversa, itemPendente.itemID);
                        Debug.Log($"[SISTEMA] O NPC {gameObject.name} te entregou: {recompensa.name}");
                    }
                    else faltouEntregar = true;
                }
            }
            finally { entregandoRecompensas = false; }
            if (entregouAlgum)
                AudioSeguro.TocarUmaVez(somEntregaItem, transform.position);
            if (faltouEntregar)
                AvisoNaTela.Mostrar("Inventário cheio. Libere espaço e fale de novo para receber o que faltou.");
        }
        recompensasPendentes.Clear();

        // Eventos de fim ficam para quando a conversa inteira terminar (este método roda de novo).
        if (!faltouEntregar && ContinuarNaEtapaLiberada(dialogueSystem)) return;

        VoltarADirecaoOriginal();

        // Com entrega pendente continua acessível para o jogador voltar.
        if (disableAfterDialogue && !faltouEntregar) DisableInteraction();

        if (!blockEvents)
        {
            OnDialogueComplete?.Invoke();
        }

        // Depois dos eventos acima, que liberam o próximo passo do tutorial.
        if (TutorialManager.Instance != null) TutorialManager.Instance.NotificarConversaComNpcTerminada();
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