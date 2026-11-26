using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Biblioteca (Fase 3+): catálogo de documentos de apoio à venda, montado por código no primeiro uso. A compra é do
/// GameManager.ComprarNaBiblioteca (sem fiado, sem cobrança parcial, uma vez por oferta+caso).
/// Aberta, pausa o jogo (timeScale 0) e desliga os controles de toque.
/// </summary>
public class BibliotecaUI : MonoBehaviour
{
    [Tooltip("Documentos à venda. A biblioteca mostra os que servem a casos visíveis na mesa da fase atual.")]
    public List<OfertaDaBiblioteca> ofertas = new List<OfertaDaBiblioteca>();
    [Tooltip("O botão da biblioteca aparece a partir desta fase.")]
    [Range(1, GameManager.UltimaFase)] public int faseMinima = 3;
    public TMP_FontAsset fonte;
    [Tooltip("Sprite dos botões: o mesmo \"button\" de Pause, Inventário e Interagir (precisa de borda 9-slice). " +
             "Vazio = retângulo de cor lisa.")]
    public Sprite spriteDoBotao;

    public static bool Aberta { get; private set; }
    private static int frameEmQueFechou = -1;
    /// <summary>O Esc que fechou a biblioteca não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => Aberta || frameEmQueFechou == Time.frameCount;

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);
    // Opaco: em espaço de cor Linear, alpha 0,97 ainda deixava o cenário vazar.
    private static readonly Color CorPainel = new Color(0.11f, 0.08f, 0.07f, 1f);
    private static readonly Color CorLinha = new Color(0.18f, 0.13f, 0.11f, 1f);
    private static readonly Color CorBotao = new Color(0.36f, 0.25f, 0.16f, 1f);

    private RectTransform botaoAbrir;
    private GameObject janela;
    private RectTransform lista;
    private TextMeshProUGUI textoOuro;
    private TextMeshProUGUI textoMensagem;
    private Button botaoFechar;
    private float timeScaleAoAbrir = 1f;
    private bool dicaPedida;

    // Play Mode sem domain reload: o estado estático não pode vazar entre sessões.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ZerarEstado()
    {
        Aberta = false;
        frameEmQueFechou = -1;
    }

    private void Start()
    {
        Montar();
        if (GameManager.Instance != null) GameManager.Instance.OnStatusChanged += AtualizarSeAberta;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStatusChanged -= AtualizarSeAberta;
        if (Aberta && janela != null && janela.activeSelf) Fechar(); // troca de cena com a janela aberta
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        // Some com outro modal aberto: não fica por cima nem oferece um clique que seria recusado.
        bool outroModal = CutsceneLegendas.EmExibicao || CaseSelectionUI.Aberta || QuadroDeDeducaoUI.Aberto ||
                          (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) ||
                          (gm != null && gm.dialogueSystem != null && gm.dialogueSystem.IsDialogueActive) ||
                          (gm != null && gm.inventoryManager != null && gm.inventoryManager.inventoryUI != null && gm.inventoryManager.inventoryUI.activeSelf);
        bool visivel = gm != null && gm.faseAtual >= faseMinima && !outroModal;
        if (botaoAbrir != null && botaoAbrir.gameObject.activeSelf != (visivel && !Aberta))
            botaoAbrir.gameObject.SetActive(visivel && !Aberta);

        // A dica explica a Biblioteca na primeira vez que o botão aparece.
        if (visivel && !dicaPedida && TutorialManager.Instance != null)
        {
            dicaPedida = true;
            TutorialManager.Instance.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_BIBLIOTECA, TutorialManager.Instance.dicaBiblioteca);
        }

        if (Aberta && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Fechar();
    }

    // ===== Abrir / fechar =====

    public void Abrir()
    {
        GameManager gm = GameManager.Instance;
        if (Aberta || gm == null || gm.faseAtual < faseMinima) return;
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;
        if (CutsceneLegendas.EmExibicao || CaseSelectionUI.Aberta || QuadroDeDeducaoUI.Aberto) return;
        if (gm.dialogueSystem != null && gm.dialogueSystem.IsDialogueActive) return;
        if (gm.inventoryManager != null && gm.inventoryManager.inventoryUI != null && gm.inventoryManager.inventoryUI.activeSelf)
        {
            AvisoNaTela.Mostrar("Feche o inventário antes de ir à biblioteca.");
            return;
        }

        Aberta = true;
        timeScaleAoAbrir = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(false);
        janela.SetActive(true);
        janela.transform.SetAsLastSibling();
        textoMensagem.text = string.Empty;
        Reconstruir();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoFechar.gameObject);
    }

    public void Fechar()
    {
        if (!Aberta) return;
        Aberta = false;
        frameEmQueFechou = Time.frameCount;
        if (janela != null) janela.SetActive(false);
        Time.timeScale = timeScaleAoAbrir;
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void AtualizarSeAberta()
    {
        if (Aberta) Reconstruir();
    }

    // ===== Catálogo =====

    private void Reconstruir()
    {
        GameManager gm = GameManager.Instance;
        textoOuro.text = $"Seu ouro: {gm.capitalAtual}" + (gm.capitalAtual < 0 ? "  <color=#E57373>(em dívida: a biblioteca não vende fiado)</color>" : string.Empty);

        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            GameObject velho = lista.GetChild(i).gameObject;
            velho.SetActive(false);
            Destroy(velho);
        }

        int mostradas = 0;
        foreach (OfertaDaBiblioteca oferta in ofertas)
        {
            if (oferta == null || oferta.caso == null || gm.faseAtual < oferta.faseMinima) continue;
            if (!CaseSelectionUI.CasoVisivel(oferta.caso, gm)) continue; // só casos da fase/rota atual
            CriarLinha(oferta, gm);
            mostradas++;
        }
        if (mostradas == 0)
            Texto("Vazio", lista, 30f, "Nenhum documento à venda nesta fase.", TextAlignmentOptions.Center).GetComponent<LayoutElement>().minHeight = 120f;
    }

    private void CriarLinha(OfertaDaBiblioteca oferta, GameManager gm)
    {
        GameManager.EstadoDaOferta estado = gm.EstadoDe(oferta);
        bool podeComprar = estado == GameManager.EstadoDaOferta.Disponivel && gm.capitalAtual >= 0 && gm.capitalAtual >= oferta.preco;

        var linha = new GameObject("Oferta_" + oferta.id, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.transform.SetParent(lista, false);
        linha.GetComponent<Image>().color = CorLinha;
        linha.GetComponent<LayoutElement>().minHeight = 170f;
        var h = linha.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(24, 24, 16, 16); h.spacing = 24f;
        h.childControlWidth = h.childControlHeight = true; h.childForceExpandHeight = true; h.childForceExpandWidth = false;

        string titulo = string.IsNullOrWhiteSpace(oferta.titulo) ? oferta.item.NomeExibicao : oferta.titulo;
        string casoTitulo = string.IsNullOrWhiteSpace(oferta.caso.caseTitle) ? oferta.caso.name : oferta.caso.caseTitle.Trim();
        // EB Garamond: nada abaixo de ~22 na referência de 1080.
        var textos = Texto("Textos", linha.transform, 28f,
            $"<b>{titulo}</b>  <size=80%><color=#C9A94E>qualidade superior</color></size>\n" +
            $"<size=90%>{oferta.descricao}</size>\n<size=80%><color=#B0A48C>Serve ao caso: {casoTitulo}</color></size>",
            TextAlignmentOptions.MidlineLeft);
        textos.GetComponent<LayoutElement>().flexibleWidth = 1f;

        string situacao;
        switch (estado)
        {
            case GameManager.EstadoDaOferta.Comprada: situacao = "<color=#81C784>Comprado</color>"; break;
            case GameManager.EstadoDaOferta.JaObtida: situacao = "<color=#81C784>Você já tem</color>"; break;
            case GameManager.EstadoDaOferta.ApoioEquivalente: situacao = "<color=#81C784>Você já tem um apoio equivalente</color>"; break;
            case GameManager.EstadoDaOferta.CasoNaoAceito: situacao = "<color=#9E9E9E>Aceite o caso na mesa para comprar</color>"; break;
            case GameManager.EstadoDaOferta.OutroCasoEmAndamento: situacao = "<color=#9E9E9E>Termine o caso atual primeiro</color>"; break;
            case GameManager.EstadoDaOferta.CasoConcluido: situacao = "<color=#9E9E9E>Caso já concluído</color>"; break;
            case GameManager.EstadoDaOferta.Disponivel:
                situacao = podeComprar ? "Disponível" : "<color=#E57373>Ouro insuficiente</color>"; break;
            default: situacao = "<color=#9E9E9E>Indisponível</color>"; break;
        }
        var preco = Texto("Preco", linha.transform, 28f, $"<b>{oferta.preco} de ouro</b>\n<size=80%>{situacao}</size>", TextAlignmentOptions.Center);
        // Largura mínima: senão a descrição espreme preço e botão até quebrar palavras no meio.
        preco.GetComponent<LayoutElement>().minWidth = 240f;
        preco.GetComponent<LayoutElement>().preferredWidth = 260f;

        Button comprar = Botao("Comprar", linha.transform, estado == GameManager.EstadoDaOferta.Comprada ? "Comprado" : "Comprar");
        comprar.GetComponent<LayoutElement>().minWidth = 200f;
        comprar.GetComponent<LayoutElement>().preferredWidth = 220f;
        comprar.interactable = podeComprar;
        comprar.onClick.AddListener(() => Comprar(oferta, comprar));
    }

    private void Comprar(OfertaDaBiblioteca oferta, Button botao)
    {
        botao.interactable = false; // contra duplo clique
        GameManager gm = GameManager.Instance;
        GameManager.ResultadoDaCompra resultado = gm.ComprarNaBiblioteca(oferta);
        switch (resultado)
        {
            case GameManager.ResultadoDaCompra.Comprada:
                textoMensagem.text = $"Documento comprado: {oferta.item.NomeExibicao}. Selecione-o no campo Suporte da prensa.";
                break;
            case GameManager.ResultadoDaCompra.SaldoInsuficiente: textoMensagem.text = "Ouro insuficiente. Nada foi cobrado."; break;
            case GameManager.ResultadoDaCompra.InventarioCheio: textoMensagem.text = "Inventário cheio. Nada foi cobrado: libere espaço e volte."; break;
            case GameManager.ResultadoDaCompra.JaComprada: textoMensagem.text = "Você já comprou este documento."; break;
            default: textoMensagem.text = "Este documento não está disponível agora."; break;
        }
        Reconstruir();
    }

    // ===== Montagem =====

    private void Montar()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform raiz = canvas != null ? canvas.rootCanvas.transform : transform;
        if (fonte == null)
        {
            var modelo = raiz.GetComponentInChildren<TextMeshProUGUI>(true);
            if (modelo != null) fonte = modelo.font;
        }

        Button abrir = Botao("BotaoBiblioteca", raiz, "Biblioteca");
        // Canvas próprio acima do dos controles de toque, para o toque chegar ao botão.
        var canvasBotao = abrir.gameObject.AddComponent<Canvas>();
        canvasBotao.overrideSorting = true;
        canvasBotao.sortingOrder = 30;
        abrir.gameObject.AddComponent<GraphicRaycaster>();
        botaoAbrir = (RectTransform)abrir.transform;
        abrir.gameObject.AddComponent<BotaoDoCantoDaHud>().Vaga = BotaoDoCantoDaHud.VagaAbaixoDoPause;
        abrir.onClick.AddListener(Abrir);
        botaoAbrir.gameObject.SetActive(false);

        janela = new GameObject("JanelaBiblioteca", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        janela.transform.SetParent(raiz, false);
        var rtJanela = (RectTransform)janela.transform;
        rtJanela.anchorMin = Vector2.zero; rtJanela.anchorMax = Vector2.one; rtJanela.offsetMin = rtJanela.offsetMax = Vector2.zero;
        var canvasJanela = janela.GetComponent<Canvas>();
        canvasJanela.overrideSorting = true;
        canvasJanela.sortingOrder = 35; // acima da HUD/inventário, abaixo da ficha (40) e das cutscenes (50)
        janela.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var painel = new GameObject("Painel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        painel.transform.SetParent(janela.transform, false);
        var rtPainel = (RectTransform)painel.transform;
        rtPainel.anchorMin = new Vector2(0.12f, 0.08f); rtPainel.anchorMax = new Vector2(0.88f, 0.92f);
        rtPainel.offsetMin = rtPainel.offsetMax = Vector2.zero;
        painel.GetComponent<Image>().color = CorPainel;
        var v = painel.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 24, 24); v.spacing = 14f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // Altura fixa, sem esticar filhos: senão o título infla a faixa e o Fechar estica junto.
        var cabecalho = new GameObject("Cabecalho", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        cabecalho.transform.SetParent(painel.transform, false);
        var leCabecalho = cabecalho.GetComponent<LayoutElement>();
        leCabecalho.minHeight = leCabecalho.preferredHeight = 72f;
        leCabecalho.flexibleHeight = 0f;
        var hc = cabecalho.GetComponent<HorizontalLayoutGroup>();
        hc.childControlWidth = hc.childControlHeight = true; hc.childForceExpandWidth = hc.childForceExpandHeight = false; hc.spacing = 16f;
        hc.childAlignment = TextAnchor.MiddleLeft;
        var tituloJanela = Texto("Titulo", cabecalho.transform, 44f, "<b>Biblioteca</b>", TextAlignmentOptions.MidlineLeft);
        tituloJanela.GetComponent<LayoutElement>().flexibleWidth = 1f;
        tituloJanela.textWrappingMode = TextWrappingModes.NoWrap;
        tituloJanela.overflowMode = TextOverflowModes.Ellipsis;
        botaoFechar = Botao("Fechar", cabecalho.transform, "Fechar");
        botaoFechar.GetComponent<LayoutElement>().minWidth = 160f;
        botaoFechar.GetComponent<LayoutElement>().preferredWidth = 200f;
        botaoFechar.onClick.AddListener(Fechar);

        textoOuro = Texto("Ouro", painel.transform, 30f, string.Empty, TextAlignmentOptions.MidlineLeft);
        Texto("Aviso", painel.transform, 25f,
            "<color=#B0A48C>Documentos de apoio não substituem as duas pistas: vão no campo Suporte da prensa, não são gastos e só " +
            "reforçam o panfleto do caso a que servem. Um documento caro não garante que as suas pistas sejam verdadeiras.</color>",
            TextAlignmentOptions.MidlineLeft);

        var area = new GameObject("Lista", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement));
        area.transform.SetParent(painel.transform, false);
        area.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        var leArea = area.GetComponent<LayoutElement>(); leArea.flexibleHeight = 1f; leArea.minHeight = 200f;
        var conteudo = new GameObject("Conteudo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        conteudo.transform.SetParent(area.transform, false);
        lista = (RectTransform)conteudo.transform;
        lista.anchorMin = new Vector2(0f, 1f); lista.anchorMax = new Vector2(1f, 1f); lista.pivot = new Vector2(0.5f, 1f);
        lista.offsetMin = lista.offsetMax = Vector2.zero;
        var vl = conteudo.GetComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(12, 12, 12, 12); vl.spacing = 12f;
        vl.childControlWidth = vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        conteudo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var rolagem = area.GetComponent<ScrollRect>();
        rolagem.content = lista; rolagem.horizontal = false; rolagem.movementType = ScrollRect.MovementType.Clamped;
        rolagem.scrollSensitivity = 30f;

        textoMensagem = Texto("Mensagem", painel.transform, 28f, string.Empty, TextAlignmentOptions.Center);
        textoMensagem.GetComponent<LayoutElement>().minHeight = 44f;

        janela.SetActive(false);
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, float tamanho, string conteudo, TextAlignmentOptions alinhamento)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (fonte != null) tmp.font = fonte;
        tmp.text = conteudo;
        tmp.fontSize = tamanho;
        tmp.color = CorTexto;
        tmp.richText = true;
        tmp.alignment = alinhamento;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Iguala a espessura da moldura à dos botões de Pause/Inventário/Interagir (60 de altura para 43 px de arte).
    private const float EscalaDaMoldura = 0.7f;

    private Button Botao(string nome, Transform pai, string rotulo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        var fundo = go.GetComponent<Image>();
        go.GetComponent<LayoutElement>().minHeight = 64f;
        var botao = go.GetComponent<Button>();
        bool comSprite = spriteDoBotao != null;
        if (comSprite)
        {
            fundo.sprite = spriteDoBotao;
            fundo.type = Image.Type.Sliced;
            fundo.pixelsPerUnitMultiplier = EscalaDaMoldura;
            // A tinta não clareia além da cor do sprite: realce e foco escurecem um pouco.
            ColorBlock cores = botao.colors;
            cores.highlightedColor = cores.selectedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            botao.colors = cores;
        }
        else
        {
            fundo.color = CorBotao;
            ColorBlock cores = botao.colors;
            cores.highlightedColor = new Color(1.2f, 1.15f, 1.05f);
            cores.selectedColor = new Color(1.25f, 1.2f, 1.1f);
            cores.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            botao.colors = cores;
        }
        var texto = Texto("Rotulo", go.transform, 28f, rotulo, TextAlignmentOptions.Center);
        var rt = (RectTransform)texto.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        if (comSprite)
        {
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            EstiloDeBotao.AjustarRotulo(texto);
        }
        SomDeCliqueDoBotao.Garantir(botao);
        return botao;
    }
}
