using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca de cena com fade preto. Configurado no prefab Assets/Prefabs/SceneTransitonManager (CanvasGroup
/// sobre Image preta em tela cheia, Alpha 0 no Editor).
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    // Acima de todo canvas do jogo (popups 10, biblioteca 30–35, ficha 40, cutscenes 50).
    private const int OrdemAcimaDeTudo = 1000;

    private Coroutine transicaoEmAndamento;

    // Inclui fade parcial; CutsceneLegendas usa para já nascer preta e não piscar o cenário.
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
            // Liga sempre: se a primeira cópia (a que persiste) viesse desligada, o jogo ficaria sem fade.
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

    // Limita o avanço por frame para um frame lento (load, save) não consumir o fade inteiro de uma vez.
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
