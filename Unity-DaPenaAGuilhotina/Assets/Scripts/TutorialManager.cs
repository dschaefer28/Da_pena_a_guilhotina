using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Como o jogador avança de uma etapa do tutorial para a próxima.</summary>
public enum TipoDeAvanco
{
    // O popup mostra um botão "Continuar" e a etapa só avança quando o jogador clica.
    CliqueDoJogador,
    // A etapa avança sozinha quando um dos eventos do jogo abaixo acontece
    // (configure "Nome Do Evento" com uma das constantes EVENTO_* do TutorialManager).
    EventoDeJogo,
    // A etapa avança sozinha assim que a cena configurada em "Nome Da Cena" termina de carregar.
    CarregamentoDeCena
}

[Serializable]
public class TutorialStep
{
    [Tooltip("Identificador único desta etapa (sem espaços/acentos, ex: 'botao_inventario'). É usado para " +
             "conectar esta etapa aos objetos revelados na cena através do TutorialStepUI.")]
    public string etapaId;

    [TextArea(2, 4)]
    [Tooltip("Texto explicativo mostrado no popup do tutorial. Pode ser editado livremente aqui.")]
    public string mensagem;

    [Tooltip("Imagem opcional mostrada ao lado do texto no popup (deixe vazio para não mostrar nenhuma imagem).")]
    public Sprite icone;

    [Tooltip("Controle destacado no popup como uma 'tecla' desenhada (E / Interagir, I / Inventário...). " +
             "O texto da tecla muda sozinho entre Windows e Android. 'Principais' mostra o cartão com os três controles básicos.")]
    public ControleTutorial controle = ControleTutorial.Nenhum;

    [Tooltip("Como o jogador avança para a próxima etapa.")]
    public TipoDeAvanco tipoDeAvanco = TipoDeAvanco.CliqueDoJogador;

    [Tooltip("Só é usado quando 'Tipo De Avanco' = Evento Do Jogo. Use uma das constantes EVENTO_* do TutorialManager " +
             "(ex: TutorialManager.EVENTO_DIALOGO_FINALIZADO).")]
    public string nomeDoEvento;

    [Tooltip("Só é usado quando 'Tipo De Avanco' = Carregamento De Cena. Nome exato da cena (ex: Porao).")]
    public string nomeDaCena;

    [Tooltip("Ponto de save: ao concluir esta etapa o jogo é salvo (SistemaDeSave), já com a cena seguinte montada.")]
    public bool salvarAoConcluir;
}

/// <summary>
/// Gerencia o progresso do tutorial guiado (escritório -> porão) como uma sequência linear de etapas.
/// Segue o mesmo padrão "blindado" do GameManager: este objeto é persistente (DontDestroyOnLoad) e
/// guarda apenas DADOS (texto, ícone, índice da etapa atual); ele nunca referencia diretamente botões
/// ou painéis de uma cena específica, porque esses objetos são recriados a cada troca de cena.
///
/// Quem mostra o popup e revela os botões de cada etapa é o script TutorialStepUI, colocado localmente
/// no Canvas de cada cena (Jogo e Porao). O TutorialManager só avisa "a etapa atual agora é X" através
/// do evento OnEtapaAlterada.
///
/// Setup no Editor:
/// 1. Crie um GameObject vazio (pode ser na cena "Jogo", que carrega primeiro) chamado "TutorialManager"
///    e adicione este script nele.
/// 2. Ajuste a lista "Etapas" com o texto/imagem de cada passo do tutorial (já vem com um roteiro de exemplo).
/// 3. Em cada cena (Jogo e Porao), adicione o script TutorialStepUI num objeto do Canvas e configure
///    o popup + quais botões/objetos cada etapa revela (veja os comentários em TutorialStepUI.cs).
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    // Nomes de eventos prontos para usar no campo "Nome Do Evento" das etapas.
    public const string EVENTO_DIALOGO_FINALIZADO = "dialogo_finalizado";
    public const string EVENTO_ITEM_RECEBIDO = "item_recebido";
    public const string EVENTO_PANFLETO_GERADO = "panfleto_gerado";
    public const string EVENTO_INVENTARIO_ALTERNADO = "inventario_alternado";
    // Gatilhos que verificam a ação de verdade, em vez de confiar no "Continuar" (aprender fazendo).
    public const string EVENTO_JOGADOR_ANDOU = "jogador_andou";
    public const string EVENTO_PRENSA_ABERTA = "prensa_aberta";
    public const string EVENTO_ENTRADAS_PRENSA_CHEIAS = "entradas_prensa_cheias";
    public const string EVENTO_PANFLETO_GUARDADO = "panfleto_guardado";
    public const string EVENTO_CASO_ESCOLHIDO = "caso_escolhido";

    public const string CHAVE_TUTORIAL_CONCLUIDO = "tutorial_concluido";
    public const string CHAVE_DICA_FATO_BOATO = "dica_fato_boato_vista";
    public const string CHAVE_DICA_TEMPO = "dica_tempo_vista";

    [Header("Configuração")]
    [Tooltip("Marque para o tutorial rodar de novo mesmo que já tenha sido concluído antes " +
             "(útil para testar no Editor sem precisar apagar o PlayerPrefs). Deixe DESMARCADO no build. " +
             "Para zerar o progresso sem isso, use o menu Ferramentas > Resetar Tutorial e Dicas.")]
    public bool forcarReiniciar = false;

    [Tooltip("Distância (em unidades do mundo) que o jogador precisa andar para a etapa de movimento avançar.")]
    [Min(0.1f)] public float distanciaParaAndou = 1.5f;

    [Header("Dicas contextuais (aparecem uma vez, fora do roteiro)")]
    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira vez que o jogador recebe uma pista de caso (mecânica Fato x Boato, Fase 2).")]
    public string dicaFatoBoato =
        "Nem toda pista é verdade. No inventário, {apontar} uma pista para ler quem contou. " +
        "Pistas \"não verificadas\" podem ser boatos: procure outra fonte que confirme antes de imprimir.";

    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira investigação com o relógio ativo (RelogioDeInvestigacao).")]
    public string dicaTempo =
        "O dia é curto. Cada pessoa com quem você conversa e cada lugar que vasculha gasta horas (veja no alto da tela). " +
        "Falar de novo com quem já ouviu não custa nada. Escolha bem: não dá tempo de investigar tudo.";

    [Header("Etapas do Tutorial (em ordem)")]
    [Tooltip("Este roteiro é o padrão para um TutorialManager novo. A cena 'Jogo' já tem sua própria lista " +
             "configurada no Inspector (que sobrescreve este valor) — edite-a por lá para afetar o jogo rodando.")]
    public List<TutorialStep> etapas = new List<TutorialStep>
    {
        // Um roteiro só para Windows e Android: os marcadores {mover} {interagir} {inventario} {pausa}
        // {continuar} {toque}/{Toque} viram "a tecla E" ou "o botão Interagir" conforme o aparelho
        // (DispositivoDeControle). Uma ação por etapa, e toda etapa que pede uma ação só avança quando
        // o jogo detecta a ação (EventoDeJogo) — "Continuar" fica só para as mensagens de leitura.
        new TutorialStep
        {
            etapaId = "boas_vindas",
            mensagem = "Bem-vindo ao seu primeiro dia na tipografia! Estes são os controles principais. Quando estiver pronto, {continuar}.",
            controle = ControleTutorial.Principais,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "Joystick",
            mensagem = "Use {mover} para andar pelo escritório.",
            controle = ControleTutorial.Mover,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_JOGADOR_ANDOU
        },
        new TutorialStep
        {
            // Junto dos outros controles, antes da história começar (não no meio da busca pelas pistas).
            etapaId = "PauseButton",
            mensagem = "A qualquer momento, use {pausa} para abrir o menu de pausa: lá você ajusta o volume e a velocidade do texto, ou volta ao menu principal. Quando quiser, {continuar}.",
            controle = ControleTutorial.Pausa,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "explica_interacao",
            mensagem = "Vá até Charles Dupaty, o homem com o contorno destacado, e use {interagir} para conversar. Fale com ele até o fim.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_DIALOGO_FINALIZADO
        },
        new TutorialStep
        {
            etapaId = "falar_segunda_personagem",
            mensagem = "Agora fale com Marie Bradier do mesmo jeito. No fim da conversa você recebe a primeira pista.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_ITEM_RECEBIDO
        },
        new TutorialStep
        {
            etapaId = "InventoryButton",
            mensagem = "Pista recebida! Use {inventario} para abrir o inventário: é lá que ficam suas pistas e panfletos.",
            controle = ControleTutorial.Inventario,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_INVENTARIO_ALTERNADO
        },
        new TutorialStep
        {
            etapaId = "segunda_pista",
            mensagem = "Feche o inventário e volte até Charles Dupaty: agora ele tem a segunda pista.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_ITEM_RECEBIDO
        },
        new TutorialStep
        {
            etapaId = "ir_para_porao",
            mensagem = "Com as duas pistas, siga a seta até o alçapão e use {interagir} para descer ao porão.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.CarregamentoDeCena,
            nomeDaCena = "Porao"
        },
        new TutorialStep
        {
            etapaId = "explicar_prensa",
            mensagem = "Este é o porão. Aproxime-se da prensa e use {interagir} para abri-la.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_PRENSA_ABERTA
        },
        new TutorialStep
        {
            // Documento (Fase 1, "A Prensa"): ao abrir a prensa, o tutorial de status. O HUD de status pisca
            // enquanto esta etapa está na tela (DestaqueDeEtapaTutorial no prefab UI).
            etapaId = "explicar_efeito_barras",
            mensagem = "Antes de imprimir, veja o status no alto: a barra de cima é a Opinião do Povo, a de baixo a do Estado, e embaixo fica o seu ouro. " +
                       "Um panfleto só com fatos move as barras como o caso promete; com boato ou calúnia rende mais agora, " +
                       "mas perde apoio quando a mentira é descoberta, no fim do período. Quando terminar de ler, {continuar}.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "colocar_itens_prensa",
            mensagem = "{Toque} numa pista do inventário e depois numa Entrada da prensa. Faça o mesmo com a outra pista.",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_ENTRADAS_PRENSA_CHEIAS
        },
        new TutorialStep
        {
            etapaId = "misturar_itens",
            mensagem = "Com as duas entradas cheias, {toque} em Misturar para imprimir o panfleto.",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_PANFLETO_GERADO
        },
        new TutorialStep
        {
            etapaId = "coletar_resultado_prensa",
            mensagem = "Panfleto impresso! Repare como as barras e o ouro mudaram: cada panfleto agrada uns e irrita outros, e o ouro paga " +
                       "aluguel, papel e tinta no fim do período. {Toque} no panfleto em Resultado e depois em Espaço livre no inventário para guardá-lo.",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_PANFLETO_GUARDADO
        },
        new TutorialStep
        {
            etapaId = "voltar_escritorio",
            mensagem = "Feche os painéis, vá até a saída do porão e use {interagir} para subir ao escritório.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.CarregamentoDeCena,
            nomeDaCena = "Jogo",
            // Primeiro panfleto impresso e de volta ao escritório: fim do aprendizado, ponto de save.
            salvarAoConcluir = true
        },
        new TutorialStep
        {
            etapaId = "mesa_de_casos",
            mensagem = "Novos clientes deixaram cartas na mesa. Siga a seta, use {interagir} na mesa e escolha o seu próximo caso.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_CASO_ESCOLHIDO
        }
    };

    /// <summary>Disparado sempre que a etapa atual muda (inclusive ao (re)carregar uma cena), com o ID da nova etapa
    /// (ou null quando o tutorial já terminou). O TutorialStepUI de cada cena escuta este evento.</summary>
    public event Action<string> OnEtapaAlterada;

    /// <summary>Disparado uma única vez quando a última etapa é concluída (ou quando o tutorial é pulado).
    /// Ponto de extensão para o Desfecho da Fase 1 (GDD): quem implementar a cutscene final da Fase 1
    /// pode se inscrever aqui em vez de mexer neste script.</summary>
    public event Action OnTutorialConcluido;

    /// <summary>Pedido de dica contextual (texto já com marcadores). O TutorialStepUI da cena mostra no popup.</summary>
    public event Action<string> OnDicaSolicitada;

    private int indiceAtual = -1;
    private DialogueSystem dialogueSystemAtual;
    private InventoryManager inventoryManagerAtual;
    private CraftingPress craftingPressAtual;
    private Transform jogador;
    private Vector3 ultimaPosicaoJogador;
    private float distanciaAndada;

    /// <summary>ID da etapa atual, ou null se o tutorial ainda não começou/já terminou.</summary>
    public string EtapaAtualId => EtapaAtualObjeto()?.etapaId;

    /// <summary>Verdadeiro quando todas as etapas já foram concluídas (ou o tutorial foi pulado).</summary>
    public bool TutorialConcluido => indiceAtual >= etapas.Count;

    /// <summary>Posição no roteiro (etapas.Count = concluído). Guardada pelo SistemaDeSave.</summary>
    public int IndiceAtual => indiceAtual;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        if (etapas == null || etapas.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] A lista 'Etapas' está vazia — nenhum tutorial será exibido.", this);
        }

        ValidarEtapaIdsUnicos();

        if (SistemaDeSave.ConsumirEtapaDoTutorial(out int etapaSalva))
        {
            // Continuar do menu: retoma exatamente a etapa em que o jogo foi salvo.
            indiceAtual = Mathf.Clamp(etapaSalva, 0, etapas.Count);
        }
        else
        {
            bool jaConcluido = PlayerPrefs.GetInt(CHAVE_TUTORIAL_CONCLUIDO, 0) == 1;
            indiceAtual = (jaConcluido && !forcarReiniciar) ? etapas.Count : 0;
        }

        VincularDependenciasLocais();
        BroadcastEtapaAtual();

        Debug.Log($"[TutorialManager] Iniciado. Etapa atual: {(EtapaAtualId ?? "(nenhuma — tutorial concluído/pulado)")}. " +
                  $"Se 'forcarReiniciar' estiver marcado e você já tiver terminado antes, isso é esperado.");
    }

    // Disparado a cada troca de cena (ex: Jogo -> Porao). Reencontra os sistemas locais da cena nova
    // (mesma arquitetura do GameManager.OnSceneLoaded) e, se a etapa atual estiver esperando esta cena
    // carregar, avança automaticamente.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        VincularDependenciasLocais();
        BroadcastEtapaAtual();

        TutorialStep etapa = EtapaAtualObjeto();
        Debug.Log($"[TutorialManager] Cena '{scene.name}' carregada. Etapa atual: {(etapa?.etapaId ?? "(nenhuma)")}, " +
                  $"tipoDeAvanco: {(etapa != null ? etapa.tipoDeAvanco.ToString() : "-")}, nomeDaCena esperado: '{etapa?.nomeDaCena}'.");

        // O jogador pode sair de uma cena antes de terminar as últimas etapas dela (ex: subir do porão sem
        // guardar o panfleto — o save já leva o que ficou na prensa). Se só faltavam etapas "dispensáveis"
        // até a etapa que espera esta cena, o tutorial pula até ela em vez de ficar preso numa instrução
        // de outra cena.
        int alvo = IndiceDaProximaEtapaDeCena(scene.name);
        if (alvo > indiceAtual)
        {
            Debug.Log($"[TutorialManager] Saiu antes do fim das etapas de '{etapa?.etapaId}': pulando para '{etapas[alvo].etapaId}'.");
            indiceAtual = alvo;
            etapa = etapas[alvo];
        }

        if (etapa != null && etapa.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena && etapa.nomeDaCena == scene.name)
        {
            Debug.Log($"[TutorialManager] Cena bateu com a etapa '{etapa.etapaId}' — avançando automaticamente.");
            CompletarEtapaAtual();
        }
    }

    /// <summary>Índice da próxima etapa que espera a cena <paramref name="cena"/> carregar, desde que todas as
    /// etapas até lá possam ser dispensadas (leitura, ou guardar o panfleto). -1 se não houver.</summary>
    private int IndiceDaProximaEtapaDeCena(string cena)
    {
        for (int i = Mathf.Max(indiceAtual, 0); i < etapas.Count; i++)
        {
            TutorialStep e = etapas[i];
            if (e.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena) return e.nomeDaCena == cena ? i : -1;
            bool dispensavel = e.tipoDeAvanco == TipoDeAvanco.CliqueDoJogador ||
                               (e.tipoDeAvanco == TipoDeAvanco.EventoDeJogo && e.nomeDoEvento == EVENTO_PANFLETO_GUARDADO);
            if (!dispensavel) return -1;
        }
        return -1;
    }

    private void VincularDependenciasLocais()
    {
        if (dialogueSystemAtual != null) dialogueSystemAtual.OnDialogueEnded -= HandleDialogoFinalizado;
        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled -= HandleInventarioAlternado;
            inventoryManagerAtual.OnSlotAlterado -= HandleSlotAlterado;
        }
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado -= HandlePanfletoGerado;

        dialogueSystemAtual = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : FindAnyObjectByType<DialogueSystem>(FindObjectsInactive.Include);
        inventoryManagerAtual = GameManager.Instance != null ? GameManager.Instance.inventoryManager : FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        // O painel da prensa começa desativado na cena: sem "Include" a busca voltava null, o tutorial
        // nunca ouvia OnPanfletoGerado e travava na etapa "misturar_itens".
        craftingPressAtual = FindAnyObjectByType<CraftingPress>(FindObjectsInactive.Include);

        if (dialogueSystemAtual != null) dialogueSystemAtual.OnDialogueEnded += HandleDialogoFinalizado;
        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado += HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled += HandleInventarioAlternado;
            inventoryManagerAtual.OnSlotAlterado += HandleSlotAlterado;
        }
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado += HandlePanfletoGerado;

        PlayerMove player = FindAnyObjectByType<PlayerMove>();
        jogador = player != null ? player.transform : null;
        if (jogador != null) ultimaPosicaoJogador = jogador.position;
        distanciaAndada = 0f;
    }

    void Update()
    {
        // Etapa de movimento: só avança depois de o jogador andar de verdade (não basta clicar Continuar).
        TutorialStep etapa = EtapaAtualObjeto();
        if (jogador == null || etapa == null || etapa.tipoDeAvanco != TipoDeAvanco.EventoDeJogo ||
            etapa.nomeDoEvento != EVENTO_JOGADOR_ANDOU) return;

        Vector3 posicao = jogador.position;
        distanciaAndada += Vector2.Distance(posicao, ultimaPosicaoJogador);
        ultimaPosicaoJogador = posicao;
        if (distanciaAndada >= distanciaParaAndou)
        {
            distanciaAndada = 0f;
            NotificarEvento(EVENTO_JOGADOR_ANDOU);
        }
    }

    private void HandleDialogoFinalizado() => NotificarEvento(EVENTO_DIALOGO_FINALIZADO);
    private void HandlePanfletoGerado() => NotificarEvento(EVENTO_PANFLETO_GERADO);

    private void HandleItemAdicionado(Item item)
    {
        NotificarEvento(EVENTO_ITEM_RECEBIDO);
        if (item != null && item.EhPista) SolicitarDicaUmaVez(CHAVE_DICA_FATO_BOATO, dicaFatoBoato);
    }

    // Entradas da prensa cheias / panfleto guardado no inventário: os dois só existem como mudança de slot.
    private void HandleSlotAlterado(UISlotHandler slot)
    {
        if (craftingPressAtual == null || slot == null) return;

        if ((slot == craftingPressAtual.slotInput1 || slot == craftingPressAtual.slotInput2) &&
            craftingPressAtual.slotInput1 != null && craftingPressAtual.slotInput1.item != null &&
            craftingPressAtual.slotInput2 != null && craftingPressAtual.slotInput2.item != null)
        {
            NotificarEvento(EVENTO_ENTRADAS_PRENSA_CHEIAS);
        }

        Item panfleto = craftingPressAtual.UltimoPanfleto;
        bool slotDaPrensa = slot == craftingPressAtual.slotInput1 || slot == craftingPressAtual.slotInput2 || slot == craftingPressAtual.slotOutput;
        if (!slotDaPrensa && panfleto != null && slot.item != null && slot.item.itemID == panfleto.itemID)
            NotificarEvento(EVENTO_PANFLETO_GUARDADO);
    }

    /// <summary>Dica contextual fora do roteiro (ex: Fato x Boato na Fase 2): mostrada uma única vez por save.</summary>
    public void SolicitarDicaUmaVez(string chave, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || PlayerPrefs.GetInt(chave, 0) == 1) return;
        PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();
        OnDicaSolicitada?.Invoke(texto);
    }
    // Dispara tanto ao abrir quanto ao fechar o inventário — o mesmo comportamento que o botão de
    // inventário já disparava manualmente (só na cena Jogo). Fazer isso aqui, ouvindo o InventoryManager
    // diretamente, garante que funcione em qualquer cena (inclusive Porao), mesmo sem um botão configurado
    // manualmente no Inspector daquela cena para chamar TutorialManager.
    private void HandleInventarioAlternado(bool aberto) => NotificarEvento(EVENTO_INVENTARIO_ALTERNADO);

    /// <summary>Avança a etapa atual se ela estiver esperando exatamente este evento. Pode ser chamado
    /// também a partir de UnityEvents (botões, animações, etc.) além dos gatilhos automáticos acima.</summary>
    public void NotificarEvento(string nomeDoEvento)
    {
        TutorialStep etapa = EtapaAtualObjeto();
        if (etapa == null) return;

        if (etapa.tipoDeAvanco == TipoDeAvanco.EventoDeJogo && etapa.nomeDoEvento == nomeDoEvento)
        {
            CompletarEtapaAtual();
        }
    }

    public TutorialStep ObterEtapa(string etapaId)
    {
        if (string.IsNullOrEmpty(etapaId) || etapas == null) return null;
        return etapas.Find(e => e.etapaId == etapaId);
    }

    /// <summary>Verdadeiro se a etapa já foi concluída ou é a etapa atual (usado pelo TutorialStepUI para
    /// manter botões já ensinados visíveis mesmo depois de o jogador sair e voltar para a cena).</summary>
    public bool EtapaJaFoiAlcancada(string etapaId)
    {
        if (TutorialConcluido) return true;

        int indiceDaEtapa = etapas.FindIndex(e => e.etapaId == etapaId);
        return indiceDaEtapa >= 0 && indiceDaEtapa <= indiceAtual;
    }

    // Detecta o erro mais comum ao adicionar etapas pelo "+" do Inspector: a Unity duplica os valores
    // da última etapa (inclusive o Etapa Id), então duas etapas acabam com o mesmo ID e ObterEtapa()
    // sempre acha a primeira, fazendo o texto novo nunca aparecer.
    private void ValidarEtapaIdsUnicos()
    {
        if (etapas == null) return;
        var vistos = new HashSet<string>();
        for (int i = 0; i < etapas.Count; i++)
        {
            string id = etapas[i]?.etapaId;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[TutorialManager] A etapa no índice {i} está com 'Etapa Id' vazio.", this);
                continue;
            }
            if (!vistos.Add(id))
            {
                Debug.LogWarning($"[TutorialManager] 'Etapa Id' \"{id}\" está repetido em mais de uma etapa (índice {i}). " +
                                  "Etapas com o mesmo ID fazem o sistema sempre mostrar o texto da primeira. Dê um ID único a cada etapa.", this);
            }
        }
    }

    private TutorialStep EtapaAtualObjeto()
    {
        return (indiceAtual >= 0 && indiceAtual < etapas.Count) ? etapas[indiceAtual] : null;
    }

    /// <summary>Chamado pelo botão "Continuar" do popup (etapas de clique) ou pelos gatilhos automáticos acima.</summary>
    public void CompletarEtapaAtual()
    {
        if (indiceAtual < 0 || indiceAtual >= etapas.Count) return;

        if (etapas[indiceAtual].salvarAoConcluir) StartCoroutine(SalvarComACenaPronta());
        indiceAtual++;
        // A ação pedida pela próxima etapa pode já ter sido feita (ex.: o jogador encheu a prensa enquanto lia o
        // texto do status): ela conta como concluída, senão o tutorial esperaria um evento que não vai se repetir.
        while (indiceAtual < etapas.Count && AcaoJaFeita(etapas[indiceAtual]))
        {
            if (etapas[indiceAtual].salvarAoConcluir) StartCoroutine(SalvarComACenaPronta());
            indiceAtual++;
        }
        if (indiceAtual >= etapas.Count)
        {
            PlayerPrefs.SetInt(CHAVE_TUTORIAL_CONCLUIDO, 1);
            PlayerPrefs.Save();
            OnTutorialConcluido?.Invoke();
        }
        BroadcastEtapaAtual();
    }

    private bool AcaoJaFeita(TutorialStep etapa)
    {
        if (etapa == null || etapa.tipoDeAvanco != TipoDeAvanco.EventoDeJogo || craftingPressAtual == null) return false;
        Item panfleto = craftingPressAtual.UltimoPanfleto;
        switch (etapa.nomeDoEvento)
        {
            case EVENTO_ENTRADAS_PRENSA_CHEIAS:
                return panfleto != null ||
                       (craftingPressAtual.slotInput1 != null && craftingPressAtual.slotInput1.item != null &&
                        craftingPressAtual.slotInput2 != null && craftingPressAtual.slotInput2.item != null);
            case EVENTO_PANFLETO_GERADO:
                return panfleto != null;
            case EVENTO_PANFLETO_GUARDADO:
                return panfleto != null && inventoryManagerAtual != null && inventoryManagerAtual.HasItem(panfleto.itemID);
            default:
                return false;
        }
    }

    // Etapas de troca de cena concluem no sceneLoaded, antes do Start da cena nova: o inventário ainda está vazio
    // (restaura no Start) e a cutscene da Fase 1 ainda não marcou que tocou. Um frame depois tudo já está montado.
    private IEnumerator SalvarComACenaPronta()
    {
        yield return null;
        SistemaDeSave.Salvar();
    }

    /// <summary>Pula o tutorial inteiro (ex: para um botão "Pular Tutorial" no menu de opções).</summary>
    public void PularTutorial()
    {
        bool jaEstavaConcluido = TutorialConcluido;
        indiceAtual = etapas.Count;
        PlayerPrefs.SetInt(CHAVE_TUTORIAL_CONCLUIDO, 1);
        PlayerPrefs.Save();
        BroadcastEtapaAtual();
        if (!jaEstavaConcluido) OnTutorialConcluido?.Invoke();
    }

    private void BroadcastEtapaAtual()
    {
        string id = EtapaAtualId;
        int ouvintes = OnEtapaAlterada?.GetInvocationList().Length ?? 0;
        Debug.Log($"[TutorialManager] BroadcastEtapaAtual: '{id ?? "(nenhuma)"}' (indiceAtual={indiceAtual}/{etapas.Count}, ouvintes={ouvintes}).");
        OnEtapaAlterada?.Invoke(id);
    }
}
