using TMPro;
using UnityEngine;

/// <summary>
/// Coluna de botões do canto superior direito da HUD (Pause, Biblioteca, Quadro de pistas), dentro da área segura.
/// Medidas em unidades da HUD (1920x1080, Expand). O Pause vive no canvas de toque, que escala pela largura: a escala
/// local compensa. O pai do botão precisa cobrir o canvas inteiro.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BotaoDoCantoDaHud : MonoBehaviour
{
    // Cabe "Quadro de pistas" (o rótulo mais longo) em 28.
    public const float Largura = 240f;
    // Menor altura em que o rótulo de 28 não encolhe.
    public const float Altura = 64f;
    public const float TamanhoDaFonte = 28f;
    // A coluna precisa terminar acima do limite do popup do tutorial (270 do topo).
    public const float Margem = 24f;
    public const float Espacamento = 12f;
    // Precisa bater com o CanvasScaler do prefab UI (há teste).
    public static readonly Vector2 ReferenciaDaHud = new Vector2(1920f, 1080f);

    /// <summary>Pixels de tela por unidade da HUD, como o CanvasScaler em Expand calcula.</summary>
    public static float EscalaDaHud(Vector2 tela) => Mathf.Min(tela.x / ReferenciaDaHud.x, tela.y / ReferenciaDaHud.y);

    [Tooltip("Posição na coluna, de cima para baixo (0 = topo). A Biblioteca e o Quadro de pistas definem a sua por código.")]
    [SerializeField, Min(0)] private int vaga;

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

    /// <summary>No PC não há botão de Pause, então a coluna começa no topo.</summary>
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

    // Área segura e escala mudam ao girar o aparelho ou redimensionar a janela.
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

    /// <summary>Estático para os testes. Âncora e pivô no canto superior direito.</summary>
    public static void Calcular(int vaga, float escalaDoCanvas, Vector2 tela, Rect areaSegura, out float escalaLocal, out Vector2 posicao)
    {
        // 1 no canvas da HUD; no de toque, < 1 em telas mais largas que 16:9.
        escalaLocal = EscalaDaHud(tela) / escalaDoCanvas;
        float direita = (tela.x - areaSegura.xMax) / escalaDoCanvas;
        float topo = (tela.y - areaSegura.yMax) / escalaDoCanvas;
        float desdeOTopo = Margem + vaga * (Altura + Espacamento);
        posicao = new Vector2(-(Margem * escalaLocal + direita), -(desdeOTopo * escalaLocal + topo));
    }
}
