using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Setup necessário no Editor (não é feito por código):
/// 1. Criar um GameObject persistente (pode ser o mesmo que já carrega o GameManager/PauseManager,
///    ou um novo objeto na primeira cena) e adicionar este script nele.
/// 2. Nesse mesmo objeto (ou em um filho), criar um Canvas com:
///    - Render Mode "Screen Space - Overlay" e Sort Order acima de tudo (ex: 999);
///    - uma Image preta cobrindo a tela inteira (anchors stretch, cor preta);
///    - um CanvasGroup no mesmo GameObject da Image (ou no Canvas).
/// 3. Arrastar esse CanvasGroup para o campo "Canvas Group" deste script no Inspector.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    // Acima de todo canvas do jogo (popups 10, biblioteca 30–35, ficha 40, cutscenes 50): o preto da troca de cena
    // cobre tudo, senão botões e popups ficavam boiando sobre a tela preta.
    private const int OrdemAcimaDeTudo = 1000;

    private Coroutine transicaoEmAndamento;

    /// <summary>Verdadeiro enquanto o preto da troca de cena cobre a tela, mesmo em parte (fade em andamento).
    /// Uma cutscene que começa nesse momento já nasce preta (CutsceneLegendas), para o cenário não piscar.</summary>
    public static bool TelaCoberta => Instance != null && Instance.canvasGroup != null && Instance.canvasGroup.alpha > 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            Canvas canvas = canvasGroup.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.rootCanvas.sortingOrder = Mathf.Max(canvas.rootCanvas.sortingOrder, OrdemAcimaDeTudo);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (transicaoEmAndamento != null)
        {
            StopCoroutine(transicaoEmAndamento);
        }
        transicaoEmAndamento = StartCoroutine(TransicaoDeCena(sceneName));
    }

    private IEnumerator TransicaoDeCena(string sceneName)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        AsyncOperation operacao = SceneManager.LoadSceneAsync(sceneName);
        while (operacao != null && !operacao.isDone)
        {
            yield return null;
        }

        if (canvasGroup != null)
        {
            yield return Fade(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        transicaoEmAndamento = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float tempoDecorrido = 0f;
        canvasGroup.alpha = from;

        while (tempoDecorrido < fadeDuration)
        {
            tempoDecorrido += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, tempoDecorrido / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
