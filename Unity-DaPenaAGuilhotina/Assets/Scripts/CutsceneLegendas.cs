using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cutscene provisória do documento de tarefas: tela preta com legendas, uma linha por vez, até as
/// artes ficarem prontas. Vive no prefab UI (existe em todas as cenas) e é chamada por quem precisa:
/// Fase1Desfecho (após o primeiro panfleto) ou qualquer outro gatilho via Tocar(linhas).
/// Enquanto toca, congela o jogo (timeScale 0) e esconde o popup do tutorial (EmExibicao).
/// Toque/clique/Enter pula para a próxima linha.
/// </summary>
public class CutsceneLegendas : MonoBehaviour
{
    [Serializable]
    public class Linha
    {
        [TextArea(2, 4)] public string texto;
        [Tooltip("Segundos que a linha fica na tela antes de passar sozinha. 0 = só avança com toque/clique.")]
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

    private Coroutine emAndamento;
    private bool pularLinha;

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
    public void Tocar(List<Linha> linhas, Action aoTerminar = null, bool fadeDeEntradaInstantaneo = false)
    {
        if (linhas == null || linhas.Count == 0) { aoTerminar?.Invoke(); return; }
        if (emAndamento != null) StopCoroutine(emAndamento);
        emAndamento = StartCoroutine(Rotina(linhas, aoTerminar, fadeDeEntradaInstantaneo));
    }

    private IEnumerator Rotina(List<Linha> linhas, Action aoTerminar, bool fadeDeEntradaInstantaneo)
    {
        EmExibicao = true;
        float escalaAnterior = Time.timeScale;
        Time.timeScale = 0f;
        fundo.gameObject.SetActive(true);
        fundo.blocksRaycasts = true;
        legenda.text = string.Empty;
        // Começando junto com a cena (Start) ou com a tela ainda coberta pela troca de cena, a cutscene já nasce
        // preta: com fade de entrada, o preto da troca sumia enquanto o da cutscene surgia e o cenário piscava.
        if (fadeDeEntradaInstantaneo || InicioDeCena || SceneTransitionManager.TelaCoberta) fundo.alpha = 1f;
        else yield return Fade(0f, 1f);

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

        legenda.text = string.Empty;
        yield return Fade(1f, 0f);
        fundo.blocksRaycasts = false;
        fundo.gameObject.SetActive(false);
        Time.timeScale = escalaAnterior == 0f ? 1f : escalaAnterior;
        EmExibicao = false;
        emAndamento = null;
        aoTerminar?.Invoke();
        OnTerminou?.Invoke();
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
        var rT = (RectTransform)t.transform;
        rT.anchorMin = new Vector2(0.1f, 0.15f); rT.anchorMax = new Vector2(0.9f, 0.85f); rT.offsetMin = rT.offsetMax = Vector2.zero;
        legenda = t.AddComponent<TextMeshProUGUI>();
        if (fonte != null) legenda.font = fonte;
        legenda.fontSize = 40f;
        legenda.enableAutoSizing = true; legenda.fontSizeMin = 24f; legenda.fontSizeMax = 44f;
        legenda.alignment = TextAlignmentOptions.Center;
        legenda.color = Color.white;
        legenda.textWrappingMode = TextWrappingModes.Normal;
    }
}
