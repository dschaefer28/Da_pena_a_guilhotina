using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quadro de dedução (Prompt 4, proposta C, Fases 3 e 4): botão na HUD enquanto o caso em andamento tiver dedução ativa.
/// A janela lista só as pistas do conjunto do caso que o jogador já descobriu (mesmo as gastas na prensa), com fonte,
/// descrição, a afirmação contrária (quando as duas do par já foram achadas) e a hipótese do jogador: Confiável,
/// Duvidosa ou sem marca. "Conferir dedução" confirma só se TODO o conjunto estiver descoberto e bem marcado; qualquer
/// falha recebe a mesma resposta, sem dizer o que está certo nem quantas estão certas (regras em Deducao).
///
/// Marcar não muda a verdade das pistas nem a prensa: dá para publicar sem conferir. Enquanto aberto: jogo pausado,
/// movimento, interação, inventário e pause bloqueados, controles de toque desligados; Esc ou Fechar restauram tudo.
/// Fica no objeto "QuadroDeDeducao" do prefab UI e se monta por código no primeiro uso, como a Biblioteca.
/// </summary>
public class QuadroDeDeducaoUI : MonoBehaviour
{
    [Tooltip("O botão do quadro aparece a partir desta fase (e só com um caso de dedução ativa em andamento).")]
    [Range(1, GameManager.UltimaFase)] public int faseMinima = 3;
    public TMP_FontAsset fonte;

    [Header("Textos (provisórios, editáveis)")]
    public string rotuloDoBotao = "Quadro de pistas";
    [TextArea(2, 5)]
    public string regras = "Cada pista deste quadro tem uma afirmação contrária no mesmo caso, e só uma das duas é verdadeira. " +
                           "Marque o que você acha confiável ou duvidoso. A conferência só diz se o quadro inteiro está certo.";
    [TextArea(1, 3)] public string semPistas = "Nenhuma pista deste caso no quadro ainda. Cada pista que você obtiver aparece aqui.";
    [TextArea(1, 3)] public string respostaConfirmada = "A dedução se sustenta: as suas marcações estão certas.";
    [TextArea(1, 3)] public string respostaGenerica = "A dedução ainda não se sustenta. Pode faltar uma pista, ou alguma marcação pode estar errada.";
    [TextArea(1, 3)] public string lembreteDaPrensa = "Marcar não muda as pistas: a prensa imprime o que você puser nela, conferido ou não.";

    /// <summary>Verdadeiro com a janela aberta (PauseMenu, inventário, jogador e interação consultam via JanelasModais).</summary>
    public static bool Aberto { get; private set; }
    private static int frameEmQueFechou = -1;
    /// <summary>O Esc que fechou o quadro não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => Aberto || frameEmQueFechou == Time.frameCount;

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);
    private static readonly Color CorPainel = new Color(0.11f, 0.08f, 0.07f, 0.97f);
    private static readonly Color CorLinha = new Color(0.18f, 0.13f, 0.11f, 1f);
    private static readonly Color CorBotao = new Color(0.36f, 0.25f, 0.16f, 1f);
    private static readonly Color CorMarcado = new Color(0.62f, 0.46f, 0.22f, 1f);

    private RectTransform botaoAbrir;
    private GameObject janela;
    private RectTransform lista;
    private TextMeshProUGUI textoCaso;
    private TextMeshProUGUI textoContador;
    private TextMeshProUGUI textoMensagem;
    private Button botaoConferir;
    private TextMeshProUGUI rotuloConferir;
    private Button botaoFechar;
    private Vector2 posicaoBaseBotao;
    private Rect ultimaArea;
    private float timeScaleAoAbrir = 1f;
    private CaseData casoAberto;

    // Play Mode sem recarregar domínio: o estado estático não pode vazar de uma sessão para a outra.
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

    /// <summary>O caso em andamento usa o quadro (a partir da fase mínima).</summary>
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
        // Some com qualquer outro modal aberto: não fica por cima do pause nem oferece um clique que seria recusado.
        bool outroModal = CutsceneLegendas.EmExibicao || CaseSelectionUI.Aberta || BibliotecaUI.Aberta ||
                          (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen) ||
                          (gm != null && gm.dialogueSystem != null && gm.dialogueSystem.IsDialogueActive) ||
                          (gm != null && gm.inventoryManager != null && gm.inventoryManager.inventoryUI != null && gm.inventoryManager.inventoryUI.activeSelf);
        bool visivel = Disponivel && !outroModal;
        if (botaoAbrir != null && botaoAbrir.gameObject.activeSelf != (visivel && !Aberto))
            botaoAbrir.gameObject.SetActive(visivel && !Aberto);
        if (botaoAbrir != null && Screen.safeArea != ultimaArea) AplicarAreaSegura();

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
        textoMensagem.text = Deducao.Confirmada(gm, casoAberto) ? respostaConfirmada : string.Empty;
        Reconstruir();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoFechar.gameObject);
    }

    public void Fechar()
    {
        if (!Aberto) return;
        Aberto = false;
        frameEmQueFechou = Time.frameCount;
        casoAberto = null;
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
        bool confirmada = Deducao.Confirmada(gm, caso);

        textoCaso.text = $"<b>{Titulo(caso)}</b>";
        textoContador.text = $"Contradições encontradas: {Deducao.ContradicoesEncontradas(gm, caso)} de {Deducao.TotalDeContradicoes(caso)}" +
                             (confirmada ? "   <color=#81C784>Dedução confirmada</color>" : string.Empty);

        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            GameObject velho = lista.GetChild(i).gameObject;
            velho.SetActive(false);
            Destroy(velho);
        }

        List<Item> conhecidas = Deducao.PistasConhecidas(gm, caso);
        foreach (Item pista in conhecidas) CriarLinha(pista, gm, caso, confirmada);
        if (conhecidas.Count == 0)
            Texto("Vazio", lista, 28f, $"<color=#9E9E9E>{semPistas}</color>", TextAlignmentOptions.Center).GetComponent<LayoutElement>().minHeight = 120f;

        botaoConferir.interactable = !confirmada;
        rotuloConferir.text = confirmada ? "Dedução confirmada" : "Conferir dedução";
    }

    private void CriarLinha(Item pista, GameManager gm, CaseData caso, bool confirmada)
    {
        var linha = new GameObject("Pista_" + pista.itemID, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.transform.SetParent(lista, false);
        linha.GetComponent<Image>().color = CorLinha;
        linha.GetComponent<LayoutElement>().minHeight = 150f;
        var h = linha.GetComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(20, 20, 14, 14); h.spacing = 18f;
        h.childControlWidth = h.childControlHeight = true; h.childForceExpandHeight = true; h.childForceExpandWidth = false;

        Item contraria = Deducao.Contradicao(gm, pista);
        string contradicao = contraria != null
            ? $"<color=#E0C080>Contradiz: {contraria.NomeExibicao}</color>"
            : "<color=#9E9E9E>A afirmação contrária ainda não foi encontrada.</color>";
        string fonteDaPista = string.IsNullOrWhiteSpace(pista.fonte) ? string.Empty : $"<size=80%><color=#C8B89A>Fonte: {pista.fonte}</color></size>\n";
        var textos = Texto("Textos", linha.transform, 24f,
            $"<b>{pista.NomeExibicao}</b>  <size=78%>({Deducao.Rotulo(gm, pista)})</size>\n{fonteDaPista}" +
            $"<size=85%>{pista.descricao}</size>\n<size=80%>{contradicao}</size>",
            TextAlignmentOptions.MidlineLeft);
        textos.GetComponent<LayoutElement>().flexibleWidth = 1f;

        MarcacaoDaPista marca = Deducao.MarcacaoDe(gm, caso, pista);
        bool podeMarcar = !confirmada && Deducao.PodeMarcar(gm, caso, pista);
        BotaoDeMarca(linha.transform, pista, caso, MarcacaoDaPista.Confiavel, "Confiável", marca, podeMarcar);
        BotaoDeMarca(linha.transform, pista, caso, MarcacaoDaPista.Duvidosa, "Duvidosa", marca, podeMarcar);
    }

    // Clicar na marcação que já está ativa tira a marca (volta a "não marcada").
    private void BotaoDeMarca(Transform pai, Item pista, CaseData caso, MarcacaoDaPista opcao, string rotulo, MarcacaoDaPista atual, bool podeMarcar)
    {
        bool ativa = atual == opcao;
        Button b = Botao("Marca_" + opcao, pai, (ativa ? "[x] " : "[  ] ") + rotulo);
        LayoutElement le = b.GetComponent<LayoutElement>();
        le.minWidth = 190f; // o texto da pista encolhe primeiro: o botão continua tocável em telas estreitas
        le.preferredWidth = 210f;
        if (ativa) b.GetComponent<Image>().color = CorMarcado;
        b.interactable = podeMarcar;
        b.onClick.AddListener(() => Marcar(pista, caso, ativa ? MarcacaoDaPista.NaoMarcada : opcao));
    }

    private void Marcar(Item pista, CaseData caso, MarcacaoDaPista marca)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !Deducao.Marcar(gm, caso, pista, marca)) return;
        textoMensagem.text = string.Empty; // a resposta anterior não vale para as marcações novas
        Reconstruir();
    }

    private void Conferir()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || casoAberto == null) return;
        switch (Deducao.Conferir(gm, casoAberto))
        {
            case Deducao.Resultado.Confirmada:
            case Deducao.Resultado.JaConfirmada:
                textoMensagem.text = $"<color=#81C784>{respostaConfirmada}</color>";
                break;
            case Deducao.Resultado.NaoSeSustenta:
                textoMensagem.text = $"<color=#E0A040>{respostaGenerica}</color>";
                break;
            default:
                textoMensagem.text = "O quadro não está disponível agora.";
                break;
        }
        Reconstruir();
        if (gm.inventoryManager != null) gm.inventoryManager.ConfigureInventory(); // etiquetas das pistas
    }

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

        // Botão fixo no canto superior direito, logo abaixo do botão da Biblioteca, dentro da área segura.
        Button abrir = Botao("BotaoQuadroDeDeducao", raiz, rotuloDoBotao);
        var canvasBotao = abrir.gameObject.AddComponent<Canvas>(); // acima do canvas dos controles de toque
        canvasBotao.overrideSorting = true;
        canvasBotao.sortingOrder = 30;
        abrir.gameObject.AddComponent<GraphicRaycaster>();
        botaoAbrir = (RectTransform)abrir.transform;
        botaoAbrir.anchorMin = botaoAbrir.anchorMax = botaoAbrir.pivot = new Vector2(1f, 1f);
        botaoAbrir.sizeDelta = new Vector2(260f, 64f);
        posicaoBaseBotao = new Vector2(-24f, -230f);
        abrir.onClick.AddListener(Abrir);
        AplicarAreaSegura();
        botaoAbrir.gameObject.SetActive(false);

        // Janela modal: fundo escurecido que bloqueia cliques no jogo + painel central.
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
        // Sem esticar na altura: senão a faixa vira "flexível" e divide a sobra da janela com a lista.
        hc.childControlWidth = hc.childControlHeight = true; hc.childForceExpandWidth = hc.childForceExpandHeight = false; hc.spacing = 16f;
        hc.childAlignment = TextAnchor.MiddleLeft;
        Texto("Titulo", cabecalho.transform, 44f, "<b>Quadro de dedução</b>", TextAlignmentOptions.MidlineLeft).GetComponent<LayoutElement>().flexibleWidth = 1f;
        botaoFechar = Botao("Fechar", cabecalho.transform, "Fechar");
        botaoFechar.GetComponent<LayoutElement>().minWidth = 160f;
        botaoFechar.GetComponent<LayoutElement>().preferredWidth = 200f;
        botaoFechar.onClick.AddListener(Fechar);

        textoCaso = Texto("Caso", painel.transform, 30f, string.Empty, TextAlignmentOptions.MidlineLeft);
        Texto("Regras", painel.transform, 22f, $"<color=#B0A48C>{regras}</color>", TextAlignmentOptions.MidlineLeft);
        textoContador = Texto("Contador", painel.transform, 24f, string.Empty, TextAlignmentOptions.MidlineLeft);

        // Lista com rolagem (mouse, arrasto e toque).
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
        textoMensagem = Texto("Mensagem", rodape.transform, 24f, string.Empty, TextAlignmentOptions.MidlineLeft);
        textoMensagem.GetComponent<LayoutElement>().flexibleWidth = 1f;
        botaoConferir = Botao("Conferir", rodape.transform, "Conferir dedução");
        botaoConferir.GetComponent<LayoutElement>().minWidth = 260f;
        botaoConferir.GetComponent<LayoutElement>().preferredWidth = 320f;
        rotuloConferir = botaoConferir.GetComponentInChildren<TextMeshProUGUI>(true);
        botaoConferir.onClick.AddListener(Conferir);

        Texto("Lembrete", painel.transform, 20f, $"<color=#9E9E9E>{lembreteDaPrensa}</color>", TextAlignmentOptions.MidlineLeft);

        janela.SetActive(false);
    }

    // Recuo do notch/barras (celular) somado à margem base, como a Biblioteca.
    private void AplicarAreaSegura()
    {
        ultimaArea = Screen.safeArea;
        Canvas canvas = botaoAbrir.GetComponentInParent<Canvas>();
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        if (escala <= 0f) return;
        float direita = (Screen.width - ultimaArea.xMax) / escala;
        float topo = (Screen.height - ultimaArea.yMax) / escala;
        botaoAbrir.anchoredPosition = posicaoBaseBotao + new Vector2(-direita, -topo);
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

    private Button Botao(string nome, Transform pai, string rotulo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        go.GetComponent<Image>().color = CorBotao;
        go.GetComponent<LayoutElement>().minHeight = 64f;
        var botao = go.GetComponent<Button>();
        ColorBlock cores = botao.colors;
        cores.highlightedColor = new Color(1.2f, 1.15f, 1.05f);
        cores.selectedColor = new Color(1.25f, 1.2f, 1.1f);
        cores.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        botao.colors = cores;
        var texto = Texto("Rotulo", go.transform, 26f, rotulo, TextAlignmentOptions.Center);
        var rt = (RectTransform)texto.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return botao;
    }
}
