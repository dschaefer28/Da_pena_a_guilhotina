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

    private Coroutine transicaoEmAndamento;

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
