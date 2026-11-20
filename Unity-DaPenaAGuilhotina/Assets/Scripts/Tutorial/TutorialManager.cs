using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TipoDeAvanco
{
    CliqueDoJogador,
    EventoDeJogo,       // nomeDoEvento = uma das constantes EVENTO_*
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
/// Progresso linear do tutorial (escritório -> porão). Persistente (DontDestroyOnLoad) e só com dados: nunca
/// referencia UI de cena, que é recriada a cada troca. O TutorialStepUI de cada cena escuta OnEtapaAlterada.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public const string EVENTO_DIALOGO_FINALIZADO = "dialogo_finalizado"; // só conversa com NPC, não pensamentos
    public const string EVENTO_ITEM_RECEBIDO = "item_recebido";
    public const string EVENTO_PANFLETO_GERADO = "panfleto_gerado";
    public const string EVENTO_INVENTARIO_ALTERNADO = "inventario_alternado";
    public const string EVENTO_JOGADOR_ANDOU = "jogador_andou";
    public const string EVENTO_PRENSA_ABERTA = "prensa_aberta";
    public const string EVENTO_ENTRADAS_PRENSA_CHEIAS = "entradas_prensa_cheias";
    public const string EVENTO_PANFLETO_GUARDADO = "panfleto_guardado";
    public const string EVENTO_CASO_ESCOLHIDO = "caso_escolhido";

    public const string CHAVE_TUTORIAL_CONCLUIDO = "tutorial_concluido";
    public const string CHAVE_DICA_FATO_BOATO = "dica_fato_boato_vista";
    public const string CHAVE_DICA_TEMPO = "dica_tempo_vista";
    // Dicas de primeira vez: uma vez por partida, zeradas no Novo Jogo, levadas no save.
    public const string CHAVE_DICA_DESPESAS = "dica_despesas_vista";
    public const string CHAVE_DICA_BIBLIOTECA = "dica_biblioteca_vista";
    public const string CHAVE_DICA_DEDUCAO = "dica_deducao_vista";
    public const string CHAVE_DICA_LINHA_EDITORIAL = "dica_linha_editorial_vista";

    [Header("Configuração")]
    [Tooltip("Marque para o tutorial rodar de novo mesmo que já tenha sido concluído antes " +
             "(útil para testar no Editor sem precisar apagar o PlayerPrefs). Deixe DESMARCADO no build. " +
             "Para zerar o progresso sem isso, use o menu Ferramentas > Resetar Tutorial e Dicas.")]
    public bool forcarReiniciar = false;

    [Tooltip("Distância (em unidades do mundo) que o jogador precisa andar para a etapa de movimento avançar.")]
    [Min(0.1f)] public float distanciaParaAndou = 1.5f;

    public const string DicaFatoBoatoPadrao =
        "Nem toda pista é verdade. No inventário, {apontar} uma pista para ler o que ela afirma e quem contou. " +
        "Uma pista \"não verificada\" pode ser boato: compare-a com a afirmação contrária no Quadro de pistas antes de imprimir.";

    [Header("Dicas contextuais (aparecem uma vez, fora do roteiro)")]
    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira vez que o jogador recebe uma pista de caso (mecânica Fato x Boato, Fase 2). " +
             "Não revela a verdade de nenhuma pista: só diz como descobrir.")]
    public string dicaFatoBoato = DicaFatoBoatoPadrao;

    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira investigação com o relógio ativo (RelogioDeInvestigacao).")]
    public string dicaTempo =
        "O dia é curto. Cada pessoa com quem você conversa e cada lugar que vasculha gasta horas (veja no alto da tela). " +
        "Falar de novo com quem já ouviu não custa nada. Escolha bem: não dá tempo de investigar tudo.";

    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira pista que entra num quadro de dedução (Fases 2 a 4).")]
    public string dicaDeducao =
        "Esta pista entrou no Quadro de pistas (botão no alto, à direita). Cada afirmação do caso tem uma contrária, e só " +
        "uma das duas é verdadeira. Quando achar as duas, escolha no quadro em qual você acredita: a outra sai do inventário " +
        "de vez. O quadro nunca diz qual é a verdadeira.";

    [TextArea(3, 5)]
    [Tooltip("Mostrada depois da primeira cobrança de despesas no fim de uma fase (FimDeFase).")]
    public string dicaDespesas =
        "Todo fim de período a tipografia paga aluguel, papel e tinta com o seu ouro. Se não bastar, você fica devendo: " +
        "enquanto houver dívida, cada caso novo começa com menos horas de investigação.";

    [TextArea(3, 5)]
    [Tooltip("Mostrada quando o botão da Biblioteca aparece pela primeira vez (Fase 3).")]
    public string dicaBiblioteca =
        "A Biblioteca abriu (botão no alto, à direita). Com ouro, você compra documentos de apoio para o caso aceito: eles vão " +
        "no campo Suporte da prensa e reforçam o panfleto. Não substituem as duas pistas, não dizem qual é verdadeira e não são vendidos fiado.";

    [TextArea(3, 5)]
    [Tooltip("Mostrada na primeira vez que a prensa abre com um caso que pede linha editorial (Fase 2 em diante).")]
    public string dicaLinhaEditorial =
        "Agora a prensa pede uma linha editorial: ao apertar Misturar, você escolhe o tom do panfleto. Neutro dá o efeito " +
        "normal do caso. Sensacionalista rende mais ouro, mas o lado que o caso defende ganha menos. O tom nunca muda a " +
        "verdade das pistas.";

    [Header("Etapas do Tutorial (em ordem)")]
    [Tooltip("Este roteiro é o padrão para um TutorialManager novo. A cena 'Jogo' já tem sua própria lista " +
             "configurada no Inspector (que sobrescreve este valor) — edite-a por lá para afetar o jogo rodando.")]
    public List<TutorialStep> etapas = new List<TutorialStep>
    {
        // Marcadores {mover} {interagir} {inventario} {pausa} {continuar} {toque} viram o texto do aparelho
        // (DispositivoDeControle). Etapa que pede ação avança pelo evento; "Continuar" só nas de leitura.
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
            // O HUD de status pulsa nesta etapa (DestaqueDeEtapaTutorial no prefab UI).
            etapaId = "explicar_efeito_barras",
            mensagem = "Antes de imprimir, veja o status no alto: a barra de cima é a Opinião do Povo, a de baixo a do Estado, e embaixo fica o seu ouro. " +
                       "Um panfleto só com fatos move as barras como o caso promete; com boato ou calúnia rende mais, " +
                       "mas perde apoio assim que a mentira é descoberta. Quando terminar de ler, {continuar}.",
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
            // O panfleto vai sozinho ao inventário e esta etapa é pulada (AcaoJaFeita); só aparece com inventário cheio.
            mensagem = "Panfleto impresso! Repare como as barras e o ouro mudaram: cada panfleto agrada uns e irrita outros, e o ouro paga " +
                       "aluguel, papel e tinta no fim do período. {Toque} no panfleto em Resultado e depois em Espaço livre no inventário para guardá-lo.",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_PANFLETO_GUARDADO
        },
        new TutorialStep
        {
            etapaId = "voltar_escritorio",
            mensagem = "Panfleto impresso e guardado no inventário! Repare como as barras e o ouro mudaram: cada panfleto agrada uns e " +
                       "irrita outros. Feche os painéis, vá até a saída do porão e use {interagir} para subir ao escritório.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.CarregamentoDeCena,
            nomeDaCena = "Jogo",
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

    /// <summary>Também dispara ao (re)carregar uma cena; null quando o tutorial terminou.</summary>
    public event Action<string> OnEtapaAlterada;

    /// <summary>Dispara uma única vez, ao concluir a última etapa ou pular o tutorial.</summary>
    public event Action OnTutorialConcluido;

    public event Action<string> OnDicaSolicitada;

    // Fila no objeto persistente: dicas pedidas juntas aparecem em sequência, e uma não fechada volta na cena seguinte.
    private readonly List<string> dicasNaFila = new List<string>();

    public string DicaPendente => dicasNaFila.Count > 0 ? dicasNaFila[0] : null;

    public void ConcluirDica()
    {
        if (dicasNaFila.Count > 0) dicasNaFila.RemoveAt(0);
    }

    private int indiceAtual = -1;
    // Eventos não repetíveis ocorridos antes da etapa que os espera; zerados a cada troca de cena.
    private readonly Dictionary<string, int> eventosAdiantados = new Dictionary<string, int>();
    private InventoryManager inventoryManagerAtual;
    private CraftingPress craftingPressAtual;
    private Transform jogador;
    private Vector3 ultimaPosicaoJogador;
    private float distanciaAndada;

    /// <summary>Null se o tutorial ainda não começou ou já terminou.</summary>
    public string EtapaAtualId => EtapaAtualObjeto()?.etapaId;

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
            // Continuar: o save já foi aplicado ao GameManager no Awake dele.
            GameManager gm = GameManager.Instance;
            indiceAtual = EtapaAoCarregar(etapaSalva, etapas.Count, IndiceDaEtapa(EtapaMesaDeCasos), gm != null && gm.TutorialConcluido);
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

    public const string EtapaMesaDeCasos = "mesa_de_casos";

    /// <summary>
    /// Com o tutorial concluído no GameManager, vai ao menos até a mesa de casos: saves v6 podem ter parado numa
    /// etapa anterior (ex.: "desça ao porão") que não tem mais como ser cumprida.
    /// </summary>
    public static int EtapaAoCarregar(int etapaSalva, int totalDeEtapas, int indiceDaMesa, bool tutorialConcluido)
    {
        int etapa = Mathf.Clamp(etapaSalva, 0, totalDeEtapas);
        if (tutorialConcluido && indiceDaMesa >= 0) etapa = Mathf.Max(etapa, Mathf.Min(indiceDaMesa, totalDeEtapas));
        return etapa;
    }

    private int IndiceDaEtapa(string etapaId)
    {
        if (etapas == null) return -1;
        return etapas.FindIndex(e => e != null && e.etapaId == etapaId);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        VincularDependenciasLocais();
        SincronizarComACena(scene.name);
    }

    private void SincronizarComACena(string cena)
    {
        eventosAdiantados.Clear();

        TutorialStep etapa = EtapaAtualObjeto();
        Debug.Log($"[TutorialManager] Cena '{cena}' carregada. Etapa atual: {(etapa?.etapaId ?? "(nenhuma)")}, " +
                  $"tipoDeAvanco: {(etapa != null ? etapa.tipoDeAvanco.ToString() : "-")}, nomeDaCena esperado: '{etapa?.nomeDaCena}'.");

        // Antes do Start ou com o tutorial terminado.
        if (etapa == null)
        {
            BroadcastEtapaAtual();
            return;
        }

        // Saiu da cena com etapas pendentes que só guiavam: pula até a etapa que espera esta cena.
        int alvo = IndiceDaProximaEtapaDeCena(cena);
        if (alvo >= 0)
        {
            if (alvo > indiceAtual)
                Debug.Log($"[TutorialManager] Saiu antes do fim das etapas de '{etapa.etapaId}': pulando para '{etapas[alvo].etapaId}'.");
            indiceAtual = alvo;
            Debug.Log($"[TutorialManager] Cena bateu com a etapa '{etapas[alvo].etapaId}' — avançando automaticamente.");
            CompletarEtapaAtual();
            return;
        }

        // Saiu no meio de uma etapa obrigatória: volta para a etapa que leva de novo à cena dela.
        int volta = IndiceDaEtapaQueLevaDeVolta(cena);
        if (volta >= 0)
        {
            Debug.Log($"[TutorialManager] '{etapa.etapaId}' é de outra cena: voltando para '{etapas[volta].etapaId}'.");
            indiceAtual = volta;
        }
        BroadcastEtapaAtual();
    }

    // Etapas que só guiam: quem chegou à cena seguinte já fez o que importava (o alçapão exige as duas pistas).
    // As da prensa e a da mesa continuam obrigatórias.
    private static readonly HashSet<string> EventosDispensaveisAoTrocarDeCena = new HashSet<string>
    {
        EVENTO_JOGADOR_ANDOU, EVENTO_DIALOGO_FINALIZADO, EVENTO_ITEM_RECEBIDO, EVENTO_INVENTARIO_ALTERNADO, EVENTO_PANFLETO_GUARDADO
    };

    /// <summary>Próxima etapa que espera <paramref name="cena"/>, se todas até lá forem dispensáveis; senão -1.</summary>
    private int IndiceDaProximaEtapaDeCena(string cena)
    {
        for (int i = Mathf.Max(indiceAtual, 0); i < etapas.Count; i++)
        {
            TutorialStep e = etapas[i];
            if (e.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena) return e.nomeDaCena == cena ? i : -1;
            bool dispensavel = e.tipoDeAvanco == TipoDeAvanco.CliqueDoJogador ||
                               (e.tipoDeAvanco == TipoDeAvanco.EventoDeJogo && EventosDispensaveisAoTrocarDeCena.Contains(e.nomeDoEvento));
            if (!dispensavel) return -1;
        }
        return -1;
    }

    /// <summary>Destino da última troca de cena antes da etapa (null = cena inicial do tutorial).</summary>
    private string CenaDaEtapa(int indice)
    {
        for (int i = Mathf.Min(indice, etapas.Count) - 1; i >= 0; i--)
            if (etapas[i].tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena) return etapas[i].nomeDaCena;
        return null;
    }

    /// <summary>Etapa mostrada em <paramref name="cena"/> que leva de volta à cena da etapa atual; -1 se já está nela.</summary>
    private int IndiceDaEtapaQueLevaDeVolta(string cena)
    {
        string cenaDaEtapa = CenaDaEtapa(indiceAtual);
        if (string.IsNullOrEmpty(cenaDaEtapa) || cenaDaEtapa == cena) return -1;
        for (int i = indiceAtual - 1; i >= 0; i--)
        {
            TutorialStep e = etapas[i];
            if (e.tipoDeAvanco != TipoDeAvanco.CarregamentoDeCena || e.nomeDaCena != cenaDaEtapa) continue;
            // Origem null = cena inicial; o menu (saída pelo pause) não é cena do tutorial.
            string origem = CenaDaEtapa(i);
            bool mostradaNestaCena = origem == cena ||
                (origem == null && etapas.Exists(x => x.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena && x.nomeDaCena == cena));
            return mostradaNestaCena ? i : -1;
        }
        return -1;
    }

    private void VincularDependenciasLocais()
    {
        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled -= HandleInventarioAlternado;
            inventoryManagerAtual.OnSlotAlterado -= HandleSlotAlterado;
        }
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado -= HandlePanfletoGerado;

        inventoryManagerAtual = GameManager.Instance != null ? GameManager.Instance.inventoryManager : FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        // O painel da prensa começa desativado: sem Include a busca volta null.
        craftingPressAtual = FindAnyObjectByType<CraftingPress>(FindObjectsInactive.Include);

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
        if (jogador == null) return;
        // Referência atualizada todo frame: só conta o que se andou com a etapa de movimento na tela.
        Vector3 posicao = jogador.position;
        float passo = Vector2.Distance(posicao, ultimaPosicaoJogador);
        ultimaPosicaoJogador = posicao;

        TutorialStep etapa = EtapaAtualObjeto();
        if (etapa == null || etapa.tipoDeAvanco != TipoDeAvanco.EventoDeJogo || etapa.nomeDoEvento != EVENTO_JOGADOR_ANDOU)
        {
            distanciaAndada = 0f;
            return;
        }

        distanciaAndada += passo;
        if (distanciaAndada >= distanciaParaAndou)
        {
            distanciaAndada = 0f;
            NotificarEvento(EVENTO_JOGADOR_ANDOU);
        }
    }

    /// <summary>Chamado pelo NPCMovement: pensamentos do protagonista não contam como conversa.</summary>
    public void NotificarConversaComNpcTerminada() => NotificarEvento(EVENTO_DIALOGO_FINALIZADO);
    private void HandlePanfletoGerado() => NotificarEvento(EVENTO_PANFLETO_GERADO);

    private void HandleItemAdicionado(Item item)
    {
        NotificarEvento(EVENTO_ITEM_RECEBIDO);
        if (item != null && item.EhPista) SolicitarDicaUmaVez(CHAVE_DICA_FATO_BOATO, dicaFatoBoato);
        if (Deducao.NoConjunto(item)) SolicitarDicaUmaVez(CHAVE_DICA_DEDUCAO, dicaDeducao);
    }

    // Entradas cheias e panfleto guardado só são observáveis como mudança de slot.
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

    /// <summary>Uma vez por partida. A chave é marcada já no pedido (vai no próximo save); o popup mostra quando a tela
    /// estiver livre.</summary>
    public void SolicitarDicaUmaVez(string chave, string texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || string.IsNullOrEmpty(chave) || PlayerPrefs.GetInt(chave, 0) == 1) return;
        PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.Save();
        dicasNaFila.Add(texto);
        OnDicaSolicitada?.Invoke(texto);
    }
    // Ao abrir e ao fechar, em qualquer cena.
    private void HandleInventarioAlternado(bool aberto) => NotificarEvento(EVENTO_INVENTARIO_ALTERNADO);

    /// <summary>Também pode ser chamado por UnityEvents.</summary>
    public void NotificarEvento(string nomeDoEvento)
    {
        TutorialStep etapa = EtapaAtualObjeto();
        if (etapa == null) return;

        if (etapa.tipoDeAvanco == TipoDeAvanco.EventoDeJogo && etapa.nomeDoEvento == nomeDoEvento)
        {
            CompletarEtapaAtual();
        }
        else if (EtapaAdianteNestaCenaEspera(nomeDoEvento))
        {
            eventosAdiantados.TryGetValue(nomeDoEvento, out int vezes);
            eventosAdiantados[nomeDoEvento] = vezes + 1;
        }
    }

    // Só eventos que não se repetem sob demanda; os da prensa são conferidos pelo estado real (AcaoJaFeita).
    private static readonly HashSet<string> EventosLembradosAntesDaHora = new HashSet<string>
    {
        EVENTO_DIALOGO_FINALIZADO, EVENTO_ITEM_RECEBIDO
    };

    private bool EtapaAdianteNestaCenaEspera(string nomeDoEvento)
    {
        if (string.IsNullOrEmpty(nomeDoEvento) || !EventosLembradosAntesDaHora.Contains(nomeDoEvento)) return false;
        for (int i = Mathf.Max(indiceAtual, 0); i < etapas.Count; i++)
        {
            TutorialStep e = etapas[i];
            if (e.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena) return false;
            if (e.tipoDeAvanco == TipoDeAvanco.EventoDeJogo && e.nomeDoEvento == nomeDoEvento) return true;
        }
        return false;
    }

    private bool ConsumirEventoAdiantado(TutorialStep etapa)
    {
        if (etapa == null || etapa.tipoDeAvanco != TipoDeAvanco.EventoDeJogo || string.IsNullOrEmpty(etapa.nomeDoEvento) ||
            !eventosAdiantados.TryGetValue(etapa.nomeDoEvento, out int vezes)) return false;
        if (vezes > 1) eventosAdiantados[etapa.nomeDoEvento] = vezes - 1;
        else eventosAdiantados.Remove(etapa.nomeDoEvento);
        return true;
    }

    public TutorialStep ObterEtapa(string etapaId)
    {
        if (string.IsNullOrEmpty(etapaId) || etapas == null) return null;
        return etapas.Find(e => e.etapaId == etapaId);
    }

    /// <summary>Concluída ou atual.</summary>
    public bool EtapaJaFoiAlcancada(string etapaId)
    {
        if (TutorialConcluido) return true;

        int indiceDaEtapa = etapas.FindIndex(e => e.etapaId == etapaId);
        return indiceDaEtapa >= 0 && indiceDaEtapa <= indiceAtual;
    }

    // O "+" do Inspector duplica a última etapa, inclusive o Etapa Id.
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

    public void CompletarEtapaAtual()
    {
        if (indiceAtual < 0 || indiceAtual >= etapas.Count) return;

        if (etapas[indiceAtual].salvarAoConcluir) StartCoroutine(SalvarComACenaPronta());
        indiceAtual++;
        // Pula etapas cuja ação já foi feita, senão esperariam um evento que não se repete.
        while (indiceAtual < etapas.Count && (AcaoJaFeita(etapas[indiceAtual]) || ConsumirEventoAdiantado(etapas[indiceAtual])))
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

    // Etapas de troca de cena concluem no sceneLoaded, antes do Start da cena nova (inventário ainda não restaurado).
    private IEnumerator SalvarComACenaPronta()
    {
        yield return null;
        SistemaDeSave.Salvar();
    }

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
