using System;
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

    private const string CHAVE_TUTORIAL_CONCLUIDO = "tutorial_concluido";

    [Header("Configuração")]
    [Tooltip("Marque para o tutorial rodar de novo mesmo que já tenha sido concluído antes " +
             "(útil para testar no Editor sem precisar apagar o PlayerPrefs).")]
    public bool forcarReiniciar = false;

    [Header("Etapas do Tutorial (em ordem)")]
    [Tooltip("Este roteiro é o padrão para um TutorialManager novo. A cena 'Jogo' já tem sua própria lista " +
             "configurada no Inspector (que sobrescreve este valor) — edite-a por lá para afetar o jogo rodando.")]
    public List<TutorialStep> etapas = new List<TutorialStep>
    {
        // Um roteiro só para Windows e Android: os marcadores {mover} {interagir} {inventario} {pausa}
        // {continuar} {toque}/{Toque} viram "a tecla E" ou "o botão Interagir" conforme o aparelho
        // (DispositivoDeControle). Mensagens curtas: uma ação por etapa, como nos tutoriais contextuais.
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
            mensagem = "Use {mover} para andar pelo escritório. Dê alguns passos e depois {continuar}.",
            controle = ControleTutorial.Mover,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "Personagem",
            mensagem = "Vá até Charles Dupaty, o homem com o contorno destacado. Quando estiver perto dele, {continuar}.",
            controle = ControleTutorial.Mover,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "explica_interacao",
            mensagem = "Perto de alguém, use {interagir} para conversar. Fale com Dupaty até o fim da conversa.",
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
            mensagem = "Pista recebida! Use {inventario} para abrir o inventário.",
            controle = ControleTutorial.Inventario,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_INVENTARIO_ALTERNADO
        },
        new TutorialStep
        {
            etapaId = "ver_item_inventario",
            mensagem = "O inventário guarda suas pistas e panfletos. Quando terminar de olhar, {continuar}.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "fechar_inventario",
            mensagem = "Feche o inventário com {inventario} ou no botão Fechar para voltar ao escritório.",
            controle = ControleTutorial.Inventario,
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_INVENTARIO_ALTERNADO
        },
        new TutorialStep
        {
            etapaId = "PauseButton",
            mensagem = "Use {pausa} para abrir o menu de pausa, com volume e velocidade do texto. Quando quiser, {continuar}.",
            controle = ControleTutorial.Pausa,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "segunda_pista",
            mensagem = "Volte até Charles Dupaty e converse de novo: agora ele tem a segunda pista.",
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
            mensagem = "Este é o porão. Aproxime-se da prensa e use {interagir} para abri-la. Depois, {continuar}.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "explicar_barras_status",
            mensagem = "Repare nas barras no alto: Opinião Pública, Opinião do Estado e o seu capital. Cada panfleto impresso mexe nelas de um jeito diferente.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "colocar_itens_prensa",
            mensagem = "{Toque} numa pista do inventário e depois em uma Entrada da prensa. Faça o mesmo com a outra pista. Então {continuar}.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
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
            etapaId = "explicar_efeito_barras",
            mensagem = "Viu as barras mudarem? Foi o seu panfleto. Uns agradam o povo e irritam o Estado; outros, o contrário. Fique de olho nelas.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "coletar_resultado_prensa",
            mensagem = "{Toque} no panfleto em Resultado e depois em Espaço livre no inventário para guardá-lo. Então {continuar}.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "voltar_escritorio",
            mensagem = "Feche os painéis, vá até a saída do porão e use {interagir} para subir ao escritório.",
            controle = ControleTutorial.Interagir,
            tipoDeAvanco = TipoDeAvanco.CarregamentoDeCena,
            nomeDaCena = "Jogo"
        }
    };

    /// <summary>Disparado sempre que a etapa atual muda (inclusive ao (re)carregar uma cena), com o ID da nova etapa
    /// (ou null quando o tutorial já terminou). O TutorialStepUI de cada cena escuta este evento.</summary>
    public event Action<string> OnEtapaAlterada;

    /// <summary>Disparado uma única vez quando a última etapa é concluída (ou quando o tutorial é pulado).
    /// Ponto de extensão para o Desfecho da Fase 1 (GDD): quem implementar a cutscene final da Fase 1
    /// pode se inscrever aqui em vez de mexer neste script.</summary>
    public event Action OnTutorialConcluido;

    private int indiceAtual = -1;
    private DialogueSystem dialogueSystemAtual;
    private InventoryManager inventoryManagerAtual;
    private CraftingPress craftingPressAtual;

    /// <summary>ID da etapa atual, ou null se o tutorial ainda não começou/já terminou.</summary>
    public string EtapaAtualId => EtapaAtualObjeto()?.etapaId;

    /// <summary>Verdadeiro quando todas as etapas já foram concluídas (ou o tutorial foi pulado).</summary>
    public bool TutorialConcluido => indiceAtual >= etapas.Count;

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

        bool jaConcluido = PlayerPrefs.GetInt(CHAVE_TUTORIAL_CONCLUIDO, 0) == 1;
        indiceAtual = (jaConcluido && !forcarReiniciar) ? etapas.Count : 0;

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

        if (etapa != null && etapa.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena && etapa.nomeDaCena == scene.name)
        {
            Debug.Log($"[TutorialManager] Cena bateu com a etapa '{etapa.etapaId}' — avançando automaticamente.");
            CompletarEtapaAtual();
        }
    }

    private void VincularDependenciasLocais()
    {
        if (dialogueSystemAtual != null) dialogueSystemAtual.OnDialogueEnded -= HandleDialogoFinalizado;
        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled -= HandleInventarioAlternado;
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
        }
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado += HandlePanfletoGerado;
    }

    private void HandleDialogoFinalizado() => NotificarEvento(EVENTO_DIALOGO_FINALIZADO);
    private void HandleItemAdicionado(Item item) => NotificarEvento(EVENTO_ITEM_RECEBIDO);
    private void HandlePanfletoGerado() => NotificarEvento(EVENTO_PANFLETO_GERADO);
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

        indiceAtual++;
        if (indiceAtual >= etapas.Count)
        {
            PlayerPrefs.SetInt(CHAVE_TUTORIAL_CONCLUIDO, 1);
            PlayerPrefs.Save();
            OnTutorialConcluido?.Invoke();
        }
        BroadcastEtapaAtual();
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
