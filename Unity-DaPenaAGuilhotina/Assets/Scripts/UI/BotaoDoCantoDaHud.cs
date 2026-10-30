using TMPro;
using UnityEngine;

/// <summary>
/// Coluna de botões do canto superior direito da HUD, de cima para baixo: Pause (só com os controles de toque),
/// Biblioteca e Quadro de pistas. Todos com a mesma largura, altura, fonte, margem e espaçamento, dentro da área segura
/// (notch e barras do celular), como o HudAreaSegura faz no canto oposto. As medidas estão em unidades da HUD (canvas
/// do prefab UI: 1920 x 1080 no modo Expand, isto é, pela altura em telas largas e pela largura nas mais quadradas).
///
/// O Pause fica no canvas dos controles de toque (MobileControlsUI), que escala sempre pela largura: a escala local
/// compensa a diferença, e ele sai do mesmo tamanho dos outros em qualquer proporção de tela (16:9, 20:9, 4:3). O pai do
/// botão precisa cobrir o canvas inteiro (o próprio canvas, ou o TouchZone no caso do Pause).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BotaoDoCantoDaHud : MonoBehaviour
{
    // "Quadro de pistas", o rótulo mais longo, ocupa 180 de largura em 28: cabe na face do sprite com folga.
    public const float Largura = 240f;
    // Menor altura em que o rótulo de 28 não encolhe (face de 36,9 para uma linha de 36,6). Com 60, o Pause caía para 25.
    public const float Altura = 64f;
    public const float TamanhoDaFonte = 28f;
    // Da borda direita e do topo. A coluna termina a 240 do topo, acima do limite do popup do tutorial (270).
    public const float Margem = 24f;
    public const float Espacamento = 12f;
    // Resolução de referência do canvas da HUD (CanvasScaler em Expand): o teste confere que o prefab UI continua assim.
    public static readonly Vector2 ReferenciaDaHud = new Vector2(1920f, 1080f);

    /// <summary>Pixels de tela por unidade da HUD, como o CanvasScaler do prefab UI calcula no modo Expand.</summary>
    public static float EscalaDaHud(Vector2 tela) => Mathf.Min(tela.x / ReferenciaDaHud.x, tela.y / ReferenciaDaHud.y);

    [Tooltip("Posição na coluna, de cima para baixo (0 = topo). A Biblioteca e o Quadro de pistas definem a sua por código.")]
    [SerializeField, Min(0)] private int vaga;

    /// <summary>Posição na coluna (0 = topo). Mudar reposiciona na hora.</summary>
    public int Vaga
    {
        get => vaga;
        set
        {
            if (vaga == value) return;
            vaga = value;
            Aplicar();
        }
    }

    /// <summary>Primeira vaga abaixo do Pause: no PC (teclado) não há botão de pause, e a coluna começa no topo.</summary>
    public static int VagaAbaixoDoPause => MobileUIManager.ControlesMobileAtivos ? 1 : 0;

    private RectTransform rect;
    private Canvas canvas;
    private Rect ultimaArea;
    private Vector2Int ultimaTela;
    private float ultimaEscala;

    private void Awake()
    {
        rect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.sizeDelta = new Vector2(Largura, Altura);
        EstiloDeBotao.AjustarRotulo(GetComponentInChildren<TMP_Text>(true), TamanhoDaFonte);
    }

    private void OnEnable() => Aplicar();

    // Checagem barata por frame: a área segura e a escala do canvas mudam ao girar o aparelho ou redimensionar a janela.
    private void Update()
    {
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        if (Screen.safeArea != ultimaArea || Screen.width != ultimaTela.x || Screen.height != ultimaTela.y || !Mathf.Approximately(escala, ultimaEscala))
            Aplicar();
    }

    private void Aplicar()
    {
        if (rect == null) return;
        Rect area = Screen.safeArea;
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        ultimaArea = area;
        ultimaTela = new Vector2Int(Screen.width, Screen.height);
        ultimaEscala = escala;
        if (escala <= 0f || Screen.height <= 0) return;

        Calcular(vaga, escala, new Vector2(Screen.width, Screen.height), area, out float escalaLocal, out Vector2 posicao);
        rect.localScale = new Vector3(escalaLocal, escalaLocal, 1f);
        rect.anchoredPosition = posicao;
    }

    /// <summary>
    /// Escala local e posição (âncora e pivô no canto superior direito) do botão na vaga, num canvas com a escala dada
    /// (pixels de tela por unidade do canvas). Separado do componente para os testes.
    /// </summary>
    public static void Calcular(int vaga, float escalaDoCanvas, Vector2 tela, Rect areaSegura, out float escalaLocal, out Vector2 posicao)
    {
        // Unidades da HUD convertidas para as deste canvas: 1 no canvas da HUD; no dos controles de toque, 1 em telas até
        // 16:9 e menos que 1 nas mais largas (lá ele escala pela largura e a HUD pela altura).
        escalaLocal = EscalaDaHud(tela) / escalaDoCanvas;
        // Recuo da área segura em pixels de tela convertido para unidades do canvas.
        float direita = (tela.x - areaSegura.xMax) / escalaDoCanvas;
        float topo = (tela.y - areaSegura.yMax) / escalaDoCanvas;
        float desdeOTopo = Margem + vaga * (Altura + Espacamento);
        posicao = new Vector2(-(Margem * escalaLocal + direita), -(desdeOTopo * escalaLocal + topo));
    }
}
