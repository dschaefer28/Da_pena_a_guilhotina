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

    private const string CHAVE_TUTORIAL_CONCLUIDO = "tutorial_concluido";

    [Header("Configuração")]
    [Tooltip("Marque para o tutorial rodar de novo mesmo que já tenha sido concluído antes " +
             "(útil para testar no Editor sem precisar apagar o PlayerPrefs).")]
    public bool forcarReiniciar = false;

    [Header("Etapas do Tutorial (em ordem)")]
    [Tooltip("Roteiro de exemplo — edite o texto, a imagem e o tipo de avanço de cada etapa à vontade.")]
    public List<TutorialStep> etapas = new List<TutorialStep>
    {
        new TutorialStep
        {
            etapaId = "boas_vindas",
            mensagem = "Bem-vindo ao seu primeiro dia no escritório! Vamos te mostrar rapidinho como tudo funciona.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "falar_com_npc",
            mensagem = "Toque no personagem e use o botão de interação para conversar com ele.",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_DIALOGO_FINALIZADO
        },
        new TutorialStep
        {
            etapaId = "botao_inventario",
            mensagem = "Este é o seu inventário. Nele ficam guardadas as pistas e os itens que você encontrar.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "botao_pause",
            mensagem = "Este botão pausa o jogo e abre as opções, caso precise.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "ir_para_porao",
            mensagem = "Agora desça até o porão para usar a prensa. Siga a seta até o alçapão.",
            tipoDeAvanco = TipoDeAvanco.CarregamentoDeCena,
            nomeDaCena = "Porao"
        },
        new TutorialStep
        {
            etapaId = "explicar_prensa",
            mensagem = "Esta é a prensa. Coloque dois itens do inventário nos slots de entrada para criar um panfleto.",
            tipoDeAvanco = TipoDeAvanco.CliqueDoJogador
        },
        new TutorialStep
        {
            etapaId = "explicar_opiniao",
            mensagem = "Estas barras mostram a opinião pública e a do estado sobre o seu trabalho. " +
                       "Elas mudam a cada panfleto que você cria!",
            tipoDeAvanco = TipoDeAvanco.EventoDeJogo,
            nomeDoEvento = EVENTO_PANFLETO_GERADO
        }
    };

    /// <summary>Disparado sempre que a etapa atual muda (inclusive ao (re)carregar uma cena), com o ID da nova etapa
    /// (ou null quando o tutorial já terminou). O TutorialStepUI de cada cena escuta este evento.</summary>
    public event Action<string> OnEtapaAlterada;

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
        if (etapa != null && etapa.tipoDeAvanco == TipoDeAvanco.CarregamentoDeCena && etapa.nomeDaCena == scene.name)
        {
            CompletarEtapaAtual();
        }
    }

    private void VincularDependenciasLocais()
    {
        if (dialogueSystemAtual != null) dialogueSystemAtual.OnDialogueEnded -= HandleDialogoFinalizado;
        if (inventoryManagerAtual != null) inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado -= HandlePanfletoGerado;

        dialogueSystemAtual = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : FindAnyObjectByType<DialogueSystem>();
        inventoryManagerAtual = GameManager.Instance != null ? GameManager.Instance.inventoryManager : FindAnyObjectByType<InventoryManager>();
        craftingPressAtual = FindAnyObjectByType<CraftingPress>();

        if (dialogueSystemAtual != null) dialogueSystemAtual.OnDialogueEnded += HandleDialogoFinalizado;
        if (inventoryManagerAtual != null) inventoryManagerAtual.OnItemAdicionado += HandleItemAdicionado;
        if (craftingPressAtual != null) craftingPressAtual.OnPanfletoGerado += HandlePanfletoGerado;
    }

    private void HandleDialogoFinalizado() => NotificarEvento(EVENTO_DIALOGO_FINALIZADO);
    private void HandleItemAdicionado(Item item) => NotificarEvento(EVENTO_ITEM_RECEBIDO);
    private void HandlePanfletoGerado() => NotificarEvento(EVENTO_PANFLETO_GERADO);

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
        }
        BroadcastEtapaAtual();
    }

    /// <summary>Pula o tutorial inteiro (ex: para um botão "Pular Tutorial" no menu de opções).</summary>
    public void PularTutorial()
    {
        indiceAtual = etapas.Count;
        PlayerPrefs.SetInt(CHAVE_TUTORIAL_CONCLUIDO, 1);
        PlayerPrefs.Save();
        BroadcastEtapaAtual();
    }

    private void BroadcastEtapaAtual()
    {
        OnEtapaAlterada?.Invoke(EtapaAtualId);
    }
}
