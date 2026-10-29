using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cutscene provisória do documento de tarefas: tela preta com legendas, uma linha por vez, até as
/// artes ficarem prontas. Vive no prefab UI (existe em todas as cenas) e é chamada por quem precisa:
/// IntroCutscene, Fase1Desfecho e FimDeFase pelo TocarTransicao (abertura e passagem entre fases) e o
/// tribunal pelo Tocar (abertura do julgamento e finais).
/// Enquanto toca, congela o jogo (timeScale 0) e esconde o popup do tutorial (EmExibicao).
/// Toque/clique/Enter pula para a próxima linha.
/// </summary>
public class CutsceneLegendas : MonoBehaviour
{
    [Serializable]
    public class Linha
    {
        [TextArea(2, 4)] public string texto;
        [Tooltip("Segundos que a linha fica na tela antes de passar sozinha. 0 = só avança com toque/clique. " +
                 "Nas legendas de transição (abertura e entre fases), o tempo vem do número de palavras e este valor só o aumenta.")]
        public float duracao = 4f;
    }

    public static CutsceneLegendas Instance { get; private set; }
    /// <summary>Verdadeiro enquanto alguma cutscene estiver na tela (o tutorial usa para se esconder).</summary>
    public static bool EmExibicao { get; private set; }
    public static event Action OnTerminou;

    [Header("Visual (montado automaticamente se vazio)")]
    public CanvasGroup fundo;
    public TextMeshProUGUI legenda;
    public TMP_FontAsset fonte;
    public float duracaoFade = 0.6f;

    [Header("Legendas de transição (abertura e passagem entre fases)")]
    [Tooltip("Segundos somados a todo bloco, além do tempo pelas palavras.")]
    [Min(0f)] public float margemDeLeitura = 1f;
    [Tooltip("Ritmo de leitura: 3 = 1 segundo a cada 3 palavras.")]
    [Min(0.1f)] public float palavrasPorSegundo = 3f;
    [Tooltip("Nenhum bloco fica menos que isto com o texto inteiro na tela.")]
    [Min(0f)] public float duracaoMinima = 5f;
    [Tooltip("Segundos para o texto aparecer e, de novo, para sumir. Não contam no tempo de leitura.")]
    [Min(0f)] public float fadeDoTexto = 0.6f;
    [Tooltip("Segundos de tela preta vazia entre um bloco e o próximo.")]
    [Min(0f)] public float intervaloEntreBlocos = 0.4f;
    [Tooltip("Tamanho do texto (referência 1920×1080), o mesmo em todos os blocos.")]
    [Min(22f)] public float tamanhoDoTexto = 44f;
    [Tooltip("Largura máxima da coluna de texto (referência 1920×1080). Em telas estreitas, sobram 10% de margem de cada lado.")]
    [Min(400f)] public float larguraMaxima = 1300f;

    private Coroutine emAndamento;
    private bool pularLinha;
    // Só o texto montado por código troca de layout entre os dois modos; um texto ligado no Inspector fica como está.
    private bool layoutAutomatico;

    // Cada frame avança no máximo isto do fade do texto: um frame lento (salvar, carregar) não o pula de uma vez.
    private const float PassoMaximoDoFade = 1f / 30f;
    // A cutscene que nasce com a tela coberta pela troca de cena espera o preto dela sair, no máximo por isto.
    private const float EsperaMaximaPelaTrocaDeCena = 3f;

    // Frame em que a última cena terminou de carregar: cutscene tocada no Start da cena nova nasce preta.
    private static int frameDaUltimaCena = -100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegistrarCarregamentoDeCena()
    {
        frameDaUltimaCena = -100;
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo) => frameDaUltimaCena = Time.frameCount;

    private static bool InicioDeCena => Time.frameCount - frameDaUltimaCena <= 2;

    private void Awake()
    {
        // Uma cópia existe em cada cena (prefab UI). Na troca de cena as duas coexistem por um instante:
        // a mais nova vence, porque a antiga está a caminho de ser destruída.
        Instance = this;
        if (fundo == null || legenda == null) Montar();
        fundo.alpha = 0f;
        fundo.blocksRaycasts = false;
        fundo.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Se esta cópia morrer no meio de uma cutscene (troca de cena), não pode deixar o jogo congelado.
        if (emAndamento != null)
        {
            EmExibicao = false;
            Time.timeScale = 1f;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!EmExibicao) return;
        bool toque = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
        bool tecla = Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);
        if (toque || tecla) pularLinha = true;
    }

    /// <param name="fadeDeEntradaInstantaneo">Verdadeiro pula o fade de entrada (tela já nasce preta).
    /// Use para uma cutscene que é a primeira coisa exibida na cena (não há cenário de jogo visível
    /// ainda para esmaecer) — senão o cenário "pisca" por trás do fade antes de escurecer de vez.</param>
    public void Tocar(List<Linha> linhas, Action aoTerminar = null, bool fadeDeEntradaInstantaneo = false) =>
        Iniciar(linhas, aoTerminar, fadeDeEntradaInstantaneo, false);

    /// <summary>
    /// Legendas de transição (abertura e passagem entre fases): o fundo fica preto do começo ao fim e só o texto entra
    /// e sai em fade, um bloco por vez, com um intervalo vazio entre eles. Cada bloco fica legível pelo tempo de
    /// TempoDeLeitura, contado depois de o texto aparecer por inteiro. Toque/clique/Enter durante a entrada só a
    /// completa; durante a leitura, passa ao próximo bloco; durante a saída e o intervalo, é ignorado (nunca pula um
    /// bloco que ainda não apareceu).
    /// </summary>
    public void TocarTransicao(List<Linha> linhas, Action aoTerminar = null, bool fadeDeEntradaInstantaneo = false) =>
        Iniciar(linhas, aoTerminar, fadeDeEntradaInstantaneo, true);

    private void Iniciar(List<Linha> linhas, Action aoTerminar, bool fadeDeEntradaInstantaneo, bool transicao)
    {
        if (linhas == null || linhas.Count == 0) { aoTerminar?.Invoke(); return; }
        if (emAndamento != null) StopCoroutine(emAndamento);
        emAndamento = StartCoroutine(Rotina(linhas, aoTerminar, fadeDeEntradaInstantaneo, transicao));
    }

    /// <summary>Segundos de leitura de um bloco de transição: margem + 1 s a cada N palavras, nunca menos que o
    /// mínimo nem que a duração da própria linha.</summary>
    public static float TempoDeLeitura(string texto, float duracaoDaLinha, float margem, float palavrasPorSegundo, float minimo)
    {
        float pelasPalavras = margem + ContarPalavras(texto) / Mathf.Max(0.1f, palavrasPorSegundo);
        return Mathf.Max(minimo, pelasPalavras, duracaoDaLinha);
    }

    /// <summary>Palavras visíveis: ignora tags de rich text e sinais soltos (travessão, reticências).</summary>
    public static int ContarPalavras(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 0;
        int total = 0;
        foreach (string parte in Regex.Replace(texto, "<[^>]*>", " ").Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (char c in parte)
                if (char.IsLetterOrDigit(c)) { total++; break; }
        }
        return total;
    }

    private IEnumerator Rotina(List<Linha> linhas, Action aoTerminar, bool fadeDeEntradaInstantaneo, bool transicao)
    {
        EmExibicao = true;
        float escalaAnterior = Time.timeScale;
        Time.timeScale = 0f;
        if (layoutAutomatico) AplicarLayout(transicao);
        fundo.gameObject.SetActive(true);
        fundo.blocksRaycasts = true;
        legenda.text = string.Empty;
        legenda.alpha = 1f;
        // Começando junto com a cena (Start) ou com a tela ainda coberta pela troca de cena, a cutscene já nasce
        // preta: com fade de entrada, o preto da troca sumia enquanto o da cutscene surgia e o cenário piscava.
        if (fadeDeEntradaInstantaneo || InicioDeCena || SceneTransitionManager.TelaCoberta) fundo.alpha = 1f;
        else yield return Fade(0f, 1f);

        if (transicao) yield return BlocosDeTransicao(linhas);
        else
        {
            foreach (var linha in linhas)
            {
                legenda.text = linha.texto;
                pularLinha = false;
                float t = 0f;
                while (!pularLinha && (linha.duracao <= 0f || t < linha.duracao))
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return null; // evita que o mesmo toque pule duas linhas
            }
        }

        legenda.text = string.Empty;
        legenda.alpha = 1f;
        yield return Fade(1f, 0f);
        fundo.blocksRaycasts = false;
        fundo.gameObject.SetActive(false);
        Time.timeScale = escalaAnterior == 0f ? 1f : escalaAnterior;
        EmExibicao = false;
        emAndamento = null;
        aoTerminar?.Invoke();
        OnTerminou?.Invoke();
    }

    private IEnumerator BlocosDeTransicao(List<Linha> linhas)
    {
        // Nascendo sob o preto da troca de cena, o texto só começa a aparecer quando ele sai: senão a entrada (e parte
        // da leitura do primeiro bloco) acontecia por baixo dele.
        float espera = 0f;
        while (SceneTransitionManager.TelaCoberta && espera < EsperaMaximaPelaTrocaDeCena)
        {
            espera += Time.unscaledDeltaTime;
            yield return null;
        }

        bool primeiro = true;
        foreach (Linha linha in linhas)
        {
            if (linha == null || string.IsNullOrWhiteSpace(linha.texto)) continue;
            if (!primeiro) yield return EsperarTempoReal(intervaloEntreBlocos);
            primeiro = false;

            if (layoutAutomatico) AjustarLargura();
            legenda.text = linha.texto;
            pularLinha = false; // toque na saída do bloco anterior ou no intervalo não vale para este
            yield return FadeDoTexto(0f, 1f, true);
            pularLinha = false; // o toque que completou a entrada não encurta a leitura

            float leitura = TempoDeLeitura(linha.texto, linha.duracao, margemDeLeitura, palavrasPorSegundo, duracaoMinima);
            float t = 0f;
            while (!pularLinha && t < leitura)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return FadeDoTexto(1f, 0f, false);
            legenda.text = string.Empty;
        }
    }

    private IEnumerator FadeDoTexto(float de, float para, bool toqueCompleta)
    {
        float t = 0f;
        legenda.alpha = de;
        while (t < fadeDoTexto && !(toqueCompleta && pularLinha))
        {
            t += Mathf.Min(Time.unscaledDeltaTime, PassoMaximoDoFade);
            legenda.alpha = Mathf.Lerp(de, para, t / fadeDoTexto);
            yield return null;
        }
        legenda.alpha = para;
    }

    private static IEnumerator EsperarTempoReal(float segundos)
    {
        float t = 0f;
        while (t < segundos)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator Fade(float de, float para)
    {
        float t = 0f;
        fundo.alpha = de;
        while (t < duracaoFade)
        {
            t += Time.unscaledDeltaTime;
            fundo.alpha = Mathf.Lerp(de, para, t / duracaoFade);
            yield return null;
        }
        fundo.alpha = para;
    }

    private void Montar()
    {
        // Este objeto vive dentro do Canvas do prefab UI, então o canvas criado aqui é "aninhado": o modo
        // de render vem do pai e a ordem só vale com overrideSorting. O RectTransform precisa cobrir o pai.
        var go = new GameObject("CutsceneLegendas_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var rGo = (RectTransform)go.transform;
        rGo.anchorMin = Vector2.zero; rGo.anchorMax = Vector2.one; rGo.offsetMin = rGo.offsetMax = Vector2.zero;
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50; // acima dos popups (10) e dos controles mobile (2); abaixo do fade de cena (1000)
        fundo = go.GetComponent<CanvasGroup>();

        // O próprio "Cutscenes" também precisa cobrir a tela (é um RectTransform vazio dentro do canvas).
        var rEste = transform as RectTransform;
        if (rEste != null) { rEste.anchorMin = Vector2.zero; rEste.anchorMax = Vector2.one; rEste.offsetMin = rEste.offsetMax = Vector2.zero; }

        var preto = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
        preto.transform.SetParent(go.transform, false);
        var rPreto = (RectTransform)preto.transform;
        rPreto.anchorMin = Vector2.zero; rPreto.anchorMax = Vector2.one; rPreto.offsetMin = rPreto.offsetMax = Vector2.zero;
        preto.GetComponent<Image>().color = Color.black;

        var t = new GameObject("Legenda", typeof(RectTransform));
        t.transform.SetParent(go.transform, false);
        legenda = t.AddComponent<TextMeshProUGUI>();
        if (fonte != null) legenda.font = fonte;
        legenda.alignment = TextAlignmentOptions.Center;
        legenda.color = Color.white;
        legenda.textWrappingMode = TextWrappingModes.Normal;
        layoutAutomatico = true;
        AplicarLayout(false);
    }

    // Tocar: caixa de 80% × 70% da tela, com auto size. Transição: tamanho fixo em todos os blocos e coluna centralizada
    // de no máximo larguraMaxima; o bloco fica centrado na vertical, tenha uma, duas ou três linhas.
    private void AplicarLayout(bool transicao)
    {
        RectTransform r = legenda.rectTransform;
        if (transicao)
        {
            r.anchorMin = new Vector2(0.5f, 0.15f); r.anchorMax = new Vector2(0.5f, 0.85f);
            r.anchoredPosition = Vector2.zero;
            AjustarLargura();
            legenda.enableAutoSizing = false;
            legenda.fontSize = tamanhoDoTexto;
        }
        else
        {
            r.anchorMin = new Vector2(0.1f, 0.15f); r.anchorMax = new Vector2(0.9f, 0.85f); r.offsetMin = r.offsetMax = Vector2.zero;
            legenda.fontSize = 40f;
            legenda.enableAutoSizing = true; legenda.fontSizeMin = 24f; legenda.fontSizeMax = 44f;
        }
    }

    // Recalculada a cada bloco, para acompanhar a tela se ela mudar de tamanho no meio da cutscene.
    private void AjustarLargura()
    {
        var pai = legenda.rectTransform.parent as RectTransform;
        float largura = pai != null && pai.rect.width > 0f ? Mathf.Min(larguraMaxima, pai.rect.width * 0.8f) : larguraMaxima;
        legenda.rectTransform.sizeDelta = new Vector2(largura, 0f);
    }
}
