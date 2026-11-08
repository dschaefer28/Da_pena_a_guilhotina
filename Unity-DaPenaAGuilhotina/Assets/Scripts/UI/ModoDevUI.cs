using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Painel do modo dev (ModoDev), para a apresentação: F1 abre e fecha em qualquer cena, inclusive no menu principal; no
/// celular, cinco toques seguidos no canto superior esquerdo da tela. No Editor também há Ferramentas > Modo dev.
///
/// Montado por código num objeto que sobrevive às trocas de cena: nenhuma cena ou prefab precisa dele.
/// Seções: os três finais direto no Tribunal (com o destino do réu), o início de cada fase e, numa partida em andamento,
/// atalhos (barras, horas, pistas, concluir o caso, viajar). Tudo o que sai daqui vira partida de demonstração: não grava.
/// </summary>
public class ModoDevUI : MonoBehaviour
{
    public const Key Tecla = Key.F1;
    private const int ToquesParaAbrir = 5;
    private const float JanelaDosToques = 3f;

    // O projeto usa espaço de cor Linear: com alpha < 1, um painel escuro ainda deixa ver bem o que está atrás. O painel é
    // opaco; o fundo escurece a tela sem esconder em que cena o jogador está.
    private static readonly Color CorFundo = new Color(0f, 0f, 0f, 0.85f);
    private static readonly Color CorPainel = new Color(0.10f, 0.07f, 0.06f, 1f);
    private static readonly Color CorTexto = new Color(0.91f, 0.85f, 0.71f);
    private static readonly Color CorTitulo = new Color(0.98f, 0.85f, 0.55f);
    private static readonly Color CorSuave = new Color(0.78f, 0.72f, 0.60f);
    private static readonly Color CorBotaoSemSprite = new Color(0.45f, 0.20f, 0.15f);
    private static readonly Color CorDesmarcado = new Color(0.55f, 0.55f, 0.55f);

    private static ModoDevUI instancia;

    private RectTransform raiz;
    private GameObject secaoPartida;
    private TextMeshProUGUI textoResumo;
    private TextMeshProUGUI textoAviso;
    private Button botaoAbsolvido;
    private Button botaoCondenado;
    private bool reuAbsolvido = true;
    private float proximaAtualizacao;
    private int toques;
    private float inicioDosToques;

    public static bool Aberto => instancia != null && instancia.raiz != null && instancia.raiz.gameObject.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Criar()
    {
        if (instancia != null) return;
        var go = new GameObject("[ModoDev]");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<ModoDevUI>();
    }

    /// <summary>Abre ou fecha o painel (a tecla, o gesto e o menu do Editor chamam isto).</summary>
    public static void Alternar()
    {
        if (instancia == null) Criar();
        instancia.DefinirAberto(!Aberto);
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        if ((Keyboard.current != null && Keyboard.current[Tecla].wasPressedThisFrame) || GestoNoCanto()) Alternar();
        if (Aberto && Time.unscaledTime >= proximaAtualizacao) Atualizar();
    }

    // Cinco toques no canto superior esquerdo (15% da largura e da altura) em até 3 segundos.
    private bool GestoNoCanto()
    {
        Touchscreen tela = Touchscreen.current;
        if (tela == null || !tela.primaryTouch.press.wasPressedThisFrame) return false;
        Vector2 p = tela.primaryTouch.position.ReadValue();
        if (p.x > Screen.width * 0.15f || p.y < Screen.height * 0.85f) { toques = 0; return false; }

        if (toques == 0 || Time.unscaledTime - inicioDosToques > JanelaDosToques)
        {
            toques = 0;
            inicioDosToques = Time.unscaledTime;
        }
        if (++toques < ToquesParaAbrir) return false;
        toques = 0;
        return true;
    }

    private void DefinirAberto(bool aberto)
    {
        if (raiz == null) Montar();
        raiz.gameObject.SetActive(aberto);
        if (!aberto) return;
        textoAviso.text = string.Empty;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        Atualizar();
    }

    private void Atualizar()
    {
        proximaAtualizacao = Time.unscaledTime + 0.25f;
        GameManager gm = GameManager.Instance;
        bool emPartida = gm != null && SceneManager.GetActiveScene().name != PauseMenu.CenaDoMenu;
        if (secaoPartida.activeSelf != emPartida) secaoPartida.SetActive(emPartida);
        if (emPartida) textoResumo.text = ModoDev.Resumo(gm);
        MarcarReu();
    }

    // ===== Ações =====

    private void IrPara(ModoDev.Destino destino, RotaFinal rota = RotaFinal.Nenhuma)
    {
        DefinirAberto(false);
        ModoDev.IrPara(destino, rota, reuAbsolvido);
    }

    private void Avisar(string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;
        textoAviso.text = mensagem;
        AvisoNaTela.Mostrar(mensagem);
        Atualizar();
    }

    private void EscolherReu(bool absolvido)
    {
        reuAbsolvido = absolvido;
        MarcarReu();
    }

    private void MarcarReu()
    {
        Pintar(botaoAbsolvido, reuAbsolvido, "Réu absolvido (panfleto só com fatos)");
        Pintar(botaoCondenado, !reuAbsolvido, "Réu condenado (panfleto com boato)");
    }

    private static void Pintar(Button botao, bool marcado, string texto)
    {
        var fundo = botao.GetComponent<Image>();
        bool comSprite = fundo.sprite != null;
        fundo.color = marcado ? (comSprite ? Color.white : CorBotaoSemSprite) : (comSprite ? CorDesmarcado : CorBotaoSemSprite * CorDesmarcado);
        var rotulo = botao.GetComponentInChildren<TextMeshProUGUI>();
        rotulo.text = marcado ? $"<b>» {texto} «</b>" : texto;
    }

    // ===== Montagem =====

    private void Montar()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000; // acima de toda a HUD, das cutscenes e do fade de troca de cena
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo escuro em tela cheia: bloqueia os cliques no jogo enquanto o painel está aberto.
        raiz = Retangulo("Fundo", canvasGo.transform);
        Esticar(raiz);
        raiz.gameObject.AddComponent<Image>().color = CorFundo;

        RectTransform painel = Retangulo("Painel", raiz);
        painel.anchorMin = painel.anchorMax = painel.pivot = new Vector2(0.5f, 0.5f);
        painel.sizeDelta = new Vector2(1340f, 0f);
        painel.gameObject.AddComponent<Image>().color = CorPainel;
        var vertical = painel.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(32, 32, 24, 24);
        vertical.spacing = 8f;
        vertical.childControlWidth = vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        painel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform cabecalho = Linha(painel, 56f);
        TextMeshProUGUI titulo = Texto(cabecalho, "MODO DEV  <size=60%>apresentação</size>", 40f, CorTitulo, TextAlignmentOptions.MidlineLeft);
        titulo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3f;
        Botao(cabecalho, "Fechar (F1)", () => DefinirAberto(false));

        Texto(painel, "Partidas abertas ou alteradas por aqui são de demonstração: não gravam nos slots de save nem entram em " +
                      "\"Casos resolvidos\". O próximo Novo Jogo ou Carregar volta ao normal. Cutscenes: Enter ou Espaço pulam a legenda.",
              22f, CorSuave, TextAlignmentOptions.TopLeft);

        Secao(painel, "TRIBUNAL: OS TRÊS FINAIS  <size=70%>(o caso da Fase 4 de cada rota, já publicado)</size>");
        RectTransform reu = Linha(painel, 58f);
        botaoAbsolvido = Botao(reu, string.Empty, () => EscolherReu(true));
        botaoCondenado = Botao(reu, string.Empty, () => EscolherReu(false));
        RectTransform finais = Linha(painel, 62f);
        Botao(finais, "Final A: A Guilhotina", () => IrPara(ModoDev.Destino.Tribunal, RotaFinal.A_Guilhotina));
        Botao(finais, "Final B: O Tirano", () => IrPara(ModoDev.Destino.Tribunal, RotaFinal.B_Tirano));
        Botao(finais, "Final C: O Esquecido", () => IrPara(ModoDev.Destino.Tribunal, RotaFinal.C_Equilibrio));

        Secao(painel, "COMEÇAR EM  <size=70%>(escritório, com a mesa de casos da fase)</size>");
        RectTransform fases = Linha(painel, 58f);
        Botao(fases, "Fase 1 (tutorial)", () => IrPara(ModoDev.Destino.Fase1));
        Botao(fases, "Fase 2", () => IrPara(ModoDev.Destino.Fase2));
        Botao(fases, "Fase 3", () => IrPara(ModoDev.Destino.Fase3));
        RectTransform fase4 = Linha(painel, 58f);
        Botao(fase4, "Fase 4, rota A", () => IrPara(ModoDev.Destino.Fase4, RotaFinal.A_Guilhotina));
        Botao(fase4, "Fase 4, rota B", () => IrPara(ModoDev.Destino.Fase4, RotaFinal.B_Tirano));
        Botao(fase4, "Fase 4, rota C", () => IrPara(ModoDev.Destino.Fase4, RotaFinal.C_Equilibrio));

        RectTransform partida = Retangulo("PartidaAtual", painel);
        var vPartida = partida.gameObject.AddComponent<VerticalLayoutGroup>();
        vPartida.spacing = 8f;
        vPartida.childControlWidth = vPartida.childControlHeight = true;
        vPartida.childForceExpandWidth = true;
        vPartida.childForceExpandHeight = false;
        secaoPartida = partida.gameObject;

        Secao(partida, "PARTIDA ATUAL");
        textoResumo = Texto(partida, string.Empty, 24f, CorTexto, TextAlignmentOptions.TopLeft);
        RectTransform barras = Linha(partida, 56f);
        Botao(barras, "Povo −10", () => ModoDev.MudarBarras(-10, 0, 0));
        Botao(barras, "Povo +10", () => ModoDev.MudarBarras(10, 0, 0));
        Botao(barras, "Estado −10", () => ModoDev.MudarBarras(0, -10, 0));
        Botao(barras, "Estado +10", () => ModoDev.MudarBarras(0, 10, 0));
        Botao(barras, "Ouro +50", () => ModoDev.MudarBarras(0, 0, 50));
        Botao(barras, "Repor horas", () => Avisar(ModoDev.ReporHoras()));
        RectTransform caso = Linha(partida, 56f);
        Botao(caso, "Receber pistas do caso", () => Avisar(ModoDev.ReceberPistas()));
        Botao(caso, "Concluir caso: só fatos", () => Avisar(ModoDev.ConcluirCasoAtual(true)));
        Botao(caso, "Concluir caso: com boato", () => Avisar(ModoDev.ConcluirCasoAtual(false)));
        RectTransform viagem = Linha(partida, 56f);
        Botao(viagem, "Voltar ao escritório (fecha a fase)", () => { DefinirAberto(false); ModoDev.VoltarAoEscritorio(); });
        Botao(viagem, "Ir ao Tribunal (Fase 4)", IrAoTribunal);

        textoAviso = Texto(painel, string.Empty, 24f, CorTitulo, TextAlignmentOptions.TopLeft);

        MarcarReu();
        raiz.gameObject.SetActive(false);
    }

    private void IrAoTribunal()
    {
        string erro = ModoDev.IrAoTribunal();
        if (erro == null) DefinirAberto(false);
        else Avisar(erro);
    }

    private void Secao(Transform pai, string texto)
    {
        TextMeshProUGUI t = Texto(pai, texto, 28f, CorTitulo, TextAlignmentOptions.BottomLeft);
        t.fontStyle = FontStyles.Bold;
        t.margin = new Vector4(0f, 10f, 0f, 0f);
    }

    private static RectTransform Retangulo(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    private static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform Linha(Transform pai, float altura)
    {
        RectTransform rt = Retangulo("Linha", pai);
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = true;
        rt.gameObject.AddComponent<LayoutElement>().preferredHeight = altura;
        return rt;
    }

    // Texto dentro de um layout: sem auto-size (o layout lê a altura preferida do TMP).
    private static TextMeshProUGUI Texto(Transform pai, string texto, float tamanho, Color cor, TextAlignmentOptions alinhamento)
    {
        RectTransform rt = Retangulo("Texto", pai);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = tamanho;
        tmp.color = cor;
        tmp.alignment = alinhamento;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button Botao(Transform linha, string rotulo, UnityAction acao)
    {
        RectTransform rt = Retangulo("Botao", linha);
        var fundo = rt.gameObject.AddComponent<Image>();
        var botao = rt.gameObject.AddComponent<Button>();
        rt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        bool comSprite = EstiloDeBotao.Aplicar(fundo, botao);
        if (!comSprite) fundo.color = CorBotaoSemSprite;

        RectTransform rotuloRt = Retangulo("Rotulo", rt);
        Esticar(rotuloRt);
        var texto = rotuloRt.gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = rotulo;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        if (comSprite) EstiloDeBotao.AjustarRotulo(texto, 24f);
        else
        {
            texto.color = Color.white;
            EstiloDeBotao.ConfigurarTexto(texto, 24f);
            rotuloRt.offsetMin = new Vector2(10f, 4f);
            rotuloRt.offsetMax = new Vector2(-10f, -4f);
        }
        botao.onClick.AddListener(acao);
        return botao;
    }
}
