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
/// O prefab (Assets/Prefab/SceneTransitonManager) já vem assim, com o CanvasGroup em Alpha 0 para não cobrir a
/// Game View no Editor. Não desligue o objeto do fade numa cena para enxergá-la: o jogo fica sem transições.
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
            // O objeto do fade já apareceu desligado numa cena (menu principal), e como a primeira cópia é a que
            // sobrevive entre cenas, nenhum fade aparecia no jogo inteiro. Liga sempre; o alpha 0 o deixa invisível.
            canvasGroup.gameObject.SetActive(true);
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
            // Parte do alpha atual: uma transição que interrompe outra não pisca o cenário.
            yield return Fade(canvasGroup.alpha, 1f);
        }

        AsyncOperation operacao = SceneManager.LoadSceneAsync(sceneName);
        while (operacao != null && !operacao.isDone)
        {
            yield return null;
        }

        if (canvasGroup != null)
        {
            // O primeiro frame da cena nova é pesado (Start de UI, NPCs, inventário): clareia só depois dele.
            yield return null;
            yield return Fade(1f, 0f);
            canvasGroup.blocksRaycasts = false;
        }

        transicaoEmAndamento = null;
    }

    // Cada frame avança no máximo este tempo do fade. Sem o limite, um frame lento (carregar a cena, salvar)
    // consumia o fade inteiro e a tela ia de clara a escura (ou o contrário) de uma vez, como se não houvesse fade.
    private const float PassoMaximoPorFrame = 1f / 30f;

    private IEnumerator Fade(float from, float to)
    {
        float tempoDecorrido = 0f;
        canvasGroup.alpha = from;

        while (tempoDecorrido < fadeDuration)
        {
            tempoDecorrido += Mathf.Min(Time.unscaledDeltaTime, PassoMaximoPorFrame);
            canvasGroup.alpha = Mathf.Lerp(from, to, tempoDecorrido / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
