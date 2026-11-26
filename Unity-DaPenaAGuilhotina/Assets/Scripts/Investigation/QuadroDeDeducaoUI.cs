using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quadro de dedução: agrupa por par as pistas descobertas do caso em andamento e deixa o jogador escolher em qual
/// acredita (dois toques). Nunca revela a verdade (regras em Deducao). Montado por código no primeiro uso; aberto,
/// pausa o jogo (timeScale 0) e desliga os controles de toque.
/// </summary>
public class QuadroDeDeducaoUI : MonoBehaviour
{
    [Tooltip("O botão do quadro aparece a partir desta fase (e só com um caso de dedução ativa em andamento). " +
             "Decisão do grupo (27/09): a dedução vale desde a Fase 2.")]
    [Range(1, GameManager.UltimaFase)] public int faseMinima = 2;
    public TMP_FontAsset fonte;
    [Tooltip("Sprite dos botões: o mesmo \"button\" de Pause, Inventário e Interagir (precisa de borda 9-slice). " +
             "Vazio = retângulo de cor lisa.")]
    public Sprite spriteDoBotao;

    [Header("Textos (provisórios, editáveis)")]
    public string rotuloDoBotao = "Quadro de pistas";
    [TextArea(2, 5)]
    public string regras = "Cada pista deste quadro tem uma afirmação contrária no mesmo caso, e só uma das duas é verdadeira. " +
                           "Quando encontrar as duas, escolha em qual você acredita. Cada contradição é decidida à parte, " +
                           "e o quadro nunca diz qual é a verdadeira.";
    [TextArea(1, 3)] public string semPistas = "Nenhuma pista deste caso no quadro ainda. Cada pista que você obtiver aparece aqui.";
    [Tooltip("Depois do primeiro toque em \"Escolher esta\". {escolhida} e {descartada} viram os nomes das pistas.")]
    [TextArea(1, 3)] public string pedidoDeConfirmacao = "Toque em Confirmar para ficar com \"{escolhida}\". \"{descartada}\" sai do inventário e não poderá mais ir para a prensa.";
    [Tooltip("Depois da escolha confirmada. {escolhida} e {descartada} viram os nomes das pistas.")]
    [TextArea(1, 3)] public string respostaEscolha = "Você ficou com \"{escolhida}\". \"{descartada}\" foi descartada.";
    [TextArea(1, 3)] public string lembreteDaPrensa = "A escolha não tem volta. De cada contradição, só a afirmação escolhida pode ir para a prensa.";

    public static bool Aberto { get; private set; }
    private static int frameEmQueFechou = -1;
    /// <summary>O Esc que fechou o quadro não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => Aberto || frameEmQueFechou == Time.frameCount;

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);
    // Opaco: em espaço de cor Linear, alpha 0,97 ainda deixava o cenário vazar.
    private static readonly Color CorPainel = new Color(0.11f, 0.08f, 0.07f, 1f);
    private static readonly Color CorLinha = new Color(0.18f, 0.13f, 0.11f, 1f);
    private static readonly Color CorPista = new Color(0.23f, 0.17f, 0.14f, 1f);
    // Âmbar, nunca verde: marca a escolha do jogador, não uma resposta certa.
    private static readonly Color CorEscolhida = new Color(0.36f, 0.27f, 0.14f, 1f);
    private static readonly Color CorBotao = new Color(0.36f, 0.25f, 0.16f, 1f);
    private static readonly Color CorMarcado = new Color(0.62f, 0.46f, 0.22f, 1f);
    private static readonly Color CorMarcaInativa = new Color(0.62f, 0.62f, 0.62f, 1f); // tinta do sprite na opção que recua

    private RectTransform botaoAbrir;
    private BotaoDoCantoDaHud cantoDaHud;
    private BibliotecaUI biblioteca;
    private GameObject janela;
    private RectTransform lista;
    private TextMeshProUGUI textoCaso;
    private TextMeshProUGUI textoContador;
    private TextMeshProUGUI textoMensagem;
    private Button botaoFechar;
    private float timeScaleAoAbrir = 1f;
    private CaseData casoAberto;
    // A escolha só vale no segundo toque: o descarte não tem volta.
    private Item pistaAConfirmar;

    // Play Mode sem domain reload: o estado estático não pode vazar entre sessões.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ZerarEstado()
    {
        Aberto = false;
        frameEmQueFechou = -1;
    }

    private void Start() => Montar();

    private void OnDestroy()
    {
        if (Aberto && janela != null && janela.activeSelf) Fechar(); // troca de cena com a janela aberta
    }

    public bool Disponivel
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm != null && gm.faseAtual >= faseMinima && gm.CasoAtualEmAndamento && Deducao.Aderente(gm.casoEscolhido);
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        // Some com outro modal aberto: não fica por cima nem oferece um clique que seria recusado.
        bool outroModal = CutsceneLegendas.EmExibicao || CaseSelectionUI.Aberta || BibliotecaUI.Aberta ||
                          (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) ||
                          (gm != null && gm.dialogueSystem != null && gm.dialogueSystem.IsDialogueActive) ||
                          (gm != null && gm.inventoryManager != null && gm.inventoryManager.inventoryUI != null && gm.inventoryManager.inventoryUI.activeSelf);
        bool visivel = Disponivel && !outroModal;
        if (botaoAbrir != null && botaoAbrir.gameObject.activeSelf != (visivel && !Aberto))
            botaoAbrir.gameObject.SetActive(visivel && !Aberto);
        if (cantoDaHud != null) cantoDaHud.Vaga = VagaNaColuna();

        if (Aberto && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Fechar();
    }

    // ===== Abrir / fechar =====

    public void Abrir()
    {
        GameManager gm = GameManager.Instance;
        if (Aberto || !Disponivel || BibliotecaUI.Aberta) return;
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) return;
        if (CutsceneLegendas.EmExibicao || CaseSelectionUI.Aberta) return;
        if (gm.dialogueSystem != null && gm.dialogueSystem.IsDialogueActive) return;
        if (gm.inventoryManager != null && gm.inventoryManager.inventoryUI != null && gm.inventoryManager.inventoryUI.activeSelf)
        {
            AvisoNaTela.Mostrar("Feche o inventário antes de abrir o quadro de pistas.");
            return;
        }

        Aberto = true;
        casoAberto = gm.casoEscolhido;
        timeScaleAoAbrir = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(false);
        janela.SetActive(true);
        janela.transform.SetAsLastSibling();
        pistaAConfirmar = null;
        textoMensagem.text = string.Empty;
        Reconstruir();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoFechar.gameObject);
    }

    public void Fechar()
    {
        if (!Aberto) return;
        Aberto = false;
        frameEmQueFechou = Time.frameCount;
        casoAberto = null;
        pistaAConfirmar = null;
        if (janela != null) janela.SetActive(false);
        Time.timeScale = timeScaleAoAbrir;
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    // ===== Conteúdo =====

    private void Reconstruir()
    {
        GameManager gm = GameManager.Instance;
        CaseData caso = casoAberto;
        if (gm == null || caso == null) return;

        int total = Deducao.TotalDeContradicoes(caso);
        textoCaso.text = $"<b>{Titulo(caso)}</b>";
        textoContador.text = $"Contradições encontradas: {Deducao.ContradicoesEncontradas(gm, caso)} de {total}   ·   " +
                             $"decididas: {Deducao.ParesDecididos(gm, caso)} de {total}";

        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            GameObject velho = lista.GetChild(i).gameObject;
            velho.SetActive(false);
            Destroy(velho);
        }

        // Numeração pela ordem de descoberta, nunca pela posição do par no caso.
        List<List<Item>> pares = Deducao.ParesConhecidos(gm, caso);
        for (int i = 0; i < pares.Count; i++) CriarPar(i + 1, pares[i], gm, caso);
        if (pares.Count == 0)
            Texto("Vazio", lista, 30f, $"<color=#9E9E9E>{semPistas}</color>", TextAlignmentOptions.Center).GetComponent<LayoutElement>().minHeight = 120f;
    }

    private void CriarPar(int numero, List<Item> pistas, GameManager gm, CaseData caso)
    {
        var bloco = new GameObject("Contradicao_" + numero, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        bloco.transform.SetParent(lista, false);
        bloco.GetComponent<Image>().color = CorLinha;
        var v = bloco.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 10, 14); v.spacing = 8f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        string situacao = Deducao.ParDecidido(gm, pistas[0]) ? "decidida"
                        : pistas.Count < 2 ? "falta encontrar a afirmação contrária"
                        : "escolha em qual das duas você acredita";
        Texto("Titulo", bloco.transform, 28f, $"<b>Contradição {numero}</b>  <size=85%><color=#B0A48C>({situacao})</color></size>",
              TextAlignmentOptions.MidlineLeft);
        foreach (Item pista in pistas) CriarLinha(pista, bloco.transform, gm, caso);
    }

    private void CriarLinha(Item pista, Transform pai, GameManager gm, CaseData caso)
    {
        bool escolhida = Deducao.Escolhida(gm, pista);
        bool descartada = Deducao.Descartada(gm, pista);
        var linha = new GameObject("Pista_" + pista.itemID, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.transform.SetParent(pai, false);
        linha.GetComponent<Image>().color = escolhida ? CorEscolhida : CorPista;
        linha.GetComponent<LayoutElement>().minHeight = 120f;
        var h = linha.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(16, 16, 10, 10); h.spacing = 18f;
        h.childControlWidth = h.childControlHeight = true; h.childForceExpandHeight = true; h.childForceExpandWidth = false;

        string nome = descartada ? $"<s>{pista.NomeExibicao}</s>" : $"<b>{pista.NomeExibicao}</b>";
        string fonteDaPista = string.IsNullOrWhiteSpace(pista.fonte) ? string.Empty : $"<size=80%><color=#C8B89A>Fonte: {pista.fonte}</color></size>\n";
        // EB Garamond: nada abaixo de ~22 na referência de 1080.
        var textos = Texto("Textos", linha.transform, 28f,
            $"{nome}  <size=80%>({Deducao.Rotulo(gm, pista)})</size>\n{fonteDaPista}<size=90%>{pista.descricao}</size>",
            TextAlignmentOptions.MidlineLeft);
        textos.GetComponent<LayoutElement>().flexibleWidth = 1f;
        if (descartada) textos.alpha = 0.55f;

        if (escolhida) { Selo(linha.transform, "<b>Sua escolha</b>"); return; }
        if (descartada) { Selo(linha.transform, "<color=#9E9E9E>Descartada</color>"); return; }
        if (!Deducao.PodeEscolher(gm, caso, pista)) { Selo(linha.transform, "<size=85%><color=#9E9E9E>Falta a contrária</color></size>"); return; }

        bool aConfirmar = Mesma(pistaAConfirmar, pista);
        bool recua = pistaAConfirmar != null && !aConfirmar && Mesma(Deducao.Contradicao(gm, pista), pistaAConfirmar);
        Button b = Botao("Escolher", linha.transform, aConfirmar ? "Confirmar" : "Escolher esta");
        // Largura fixa: o texto encolhe primeiro e os botões do par ficam alinhados.
        LayoutElement le = b.GetComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = LarguraDaColuna;
        // Cor lisa: o botão a confirmar clareia. Com sprite (não clareia além do branco), a outra opção recua.
        if (spriteDoBotao == null) { if (aConfirmar) b.GetComponent<Image>().color = CorMarcado; }
        else if (recua) b.GetComponent<Image>().color = CorMarcaInativa;
        b.onClick.AddListener(() => Escolher(pista, caso));
    }

    private const float LarguraDaColuna = 220f;

    // Mesma largura do botão: as linhas não mudam de tamanho depois da escolha.
    private void Selo(Transform pai, string texto)
    {
        LayoutElement le = Texto("Situacao", pai, 26f, texto, TextAlignmentOptions.Center).GetComponent<LayoutElement>();
        le.minWidth = le.preferredWidth = LarguraDaColuna;
    }

    private static bool Mesma(Item x, Item y) => x != null && y != null && x.itemID == y.itemID;

    // Primeiro toque só pede confirmação; o segundo na mesma pista decide.
    private void Escolher(Item pista, CaseData caso)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !Deducao.PodeEscolher(gm, caso, pista)) return;
        Item contraria = Deducao.Contradicao(gm, pista);

        if (!Mesma(pistaAConfirmar, pista))
        {
            pistaAConfirmar = pista;
            textoMensagem.text = $"<color=#E0C080>{ComNomes(pedidoDeConfirmacao, pista, contraria)}</color>";
            Reconstruir();
            return;
        }

        pistaAConfirmar = null;
        if (!Deducao.Escolher(gm, caso, pista, out Item descartada)) return;
        textoMensagem.text = ComNomes(respostaEscolha, pista, descartada);
        Reconstruir();
        if (gm.inventoryManager != null) gm.inventoryManager.ConfigureInventory(); // atualiza etiquetas das pistas
    }

    private static string ComNomes(string modelo, Item escolhida, Item descartada) =>
        (modelo ?? string.Empty).Replace("{escolhida}", escolhida != null ? escolhida.NomeExibicao : "?")
                                .Replace("{descartada}", descartada != null ? descartada.NomeExibicao : "?");

    private static string Titulo(CaseData caso) =>
        string.IsNullOrWhiteSpace(caso.caseTitle) ? caso.name : caso.caseTitle.Trim();

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

        biblioteca = raiz.GetComponentInChildren<BibliotecaUI>(true);
        Button abrir = Botao("BotaoQuadroDeDeducao", raiz, rotuloDoBotao);
        var canvasBotao = abrir.gameObject.AddComponent<Canvas>(); // acima do canvas dos controles de toque
        canvasBotao.overrideSorting = true;
        canvasBotao.sortingOrder = 30;
        abrir.gameObject.AddComponent<GraphicRaycaster>();
        botaoAbrir = (RectTransform)abrir.transform;
        cantoDaHud = abrir.gameObject.AddComponent<BotaoDoCantoDaHud>();
        cantoDaHud.Vaga = VagaNaColuna();
        abrir.onClick.AddListener(Abrir);
        botaoAbrir.gameObject.SetActive(false);

        janela = new GameObject("JanelaQuadroDeDeducao", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
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
        rtPainel.anchorMin = new Vector2(0.08f, 0.06f); rtPainel.anchorMax = new Vector2(0.92f, 0.94f);
        rtPainel.offsetMin = rtPainel.offsetMax = Vector2.zero;
        painel.GetComponent<Image>().color = CorPainel;
        var v = painel.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 24, 24); v.spacing = 12f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        var cabecalho = new GameObject("Cabecalho", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        cabecalho.transform.SetParent(painel.transform, false);
        cabecalho.GetComponent<LayoutElement>().minHeight = 72f;
        var hc = cabecalho.GetComponent<HorizontalLayoutGroup>();
        // Sem esticar na altura: senão a faixa divide a sobra da janela com a lista.
        hc.childControlWidth = hc.childControlHeight = true; hc.childForceExpandWidth = hc.childForceExpandHeight = false; hc.spacing = 16f;
        hc.childAlignment = TextAnchor.MiddleLeft;
        Texto("Titulo", cabecalho.transform, 44f, "<b>Quadro de dedução</b>", TextAlignmentOptions.MidlineLeft).GetComponent<LayoutElement>().flexibleWidth = 1f;
        botaoFechar = Botao("Fechar", cabecalho.transform, "Fechar");
        botaoFechar.GetComponent<LayoutElement>().minWidth = 160f;
        botaoFechar.GetComponent<LayoutElement>().preferredWidth = 200f;
        botaoFechar.onClick.AddListener(Fechar);

        textoCaso = Texto("Caso", painel.transform, 32f, string.Empty, TextAlignmentOptions.MidlineLeft);
        Texto("Regras", painel.transform, 25f, $"<color=#B0A48C>{regras}</color>", TextAlignmentOptions.MidlineLeft);
        textoContador = Texto("Contador", painel.transform, 26f, string.Empty, TextAlignmentOptions.MidlineLeft);

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

        var rodape = new GameObject("Rodape", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rodape.transform.SetParent(painel.transform, false);
        rodape.GetComponent<LayoutElement>().minHeight = 72f;
        var hr = rodape.GetComponent<HorizontalLayoutGroup>();
        hr.childControlWidth = hr.childControlHeight = true; hr.childForceExpandWidth = hr.childForceExpandHeight = false; hr.spacing = 16f;
        hr.childAlignment = TextAnchor.MiddleLeft;
        textoMensagem = Texto("Mensagem", rodape.transform, 26f, string.Empty, TextAlignmentOptions.MidlineLeft);
        textoMensagem.GetComponent<LayoutElement>().flexibleWidth = 1f;

        Texto("Lembrete", painel.transform, 23f, $"<color=#9E9E9E>{lembreteDaPrensa}</color>", TextAlignmentOptions.MidlineLeft);

        janela.SetActive(false);
    }

    // Sem Biblioteca (Fase 2) ocupa a vaga dela. Segue a fase, não a visibilidade do botão, para não pular de lugar.
    private int VagaNaColuna()
    {
        GameManager gm = GameManager.Instance;
        bool semBiblioteca = gm != null && (biblioteca == null || gm.faseAtual < biblioteca.faseMinima);
        return BotaoDoCantoDaHud.VagaAbaixoDoPause + (semBiblioteca ? 0 : 1);
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

    // Mesma espessura de moldura da Biblioteca.
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
