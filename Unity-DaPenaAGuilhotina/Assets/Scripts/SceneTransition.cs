using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static bool IsTransitioning { get; private set; }

    public static void Load(string scene)
    {
        if (IsTransitioning) return;
        if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"Cena indisponível: {scene}");
            return;
        }
        IsTransitioning = true;
        var root = new GameObject("Transição de cena", typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneTransition));
        DontDestroyOnLoad(root);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 300;
        root.GetComponent<SceneTransition>().StartCoroutine(root.GetComponent<SceneTransition>().Travel(scene));
    }

    private IEnumerator Travel(string scene)
    {
        var veil = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        veil.transform.SetParent(transform, false);
        var image = veil.GetComponent<Image>();
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        yield return Fade(image, 0, 1);
        var operation = SceneManager.LoadSceneAsync(scene);
        if (operation != null) yield return operation;
        PauseManager.ForceReset();
        yield return Fade(image, 1, 0);
        IsTransitioning = false;
        Destroy(gameObject);
    }

    private IEnumerator Fade(Image image, float from, float to)
    {
        for (float time = 0; time < 0.3f; time += Time.unscaledDeltaTime)
        {
            image.color = new Color(0, 0, 0, Mathf.Lerp(from, to, time / 0.3f));
            yield return null;
        }
        image.color = new Color(0, 0, 0, to);
    }

    private void OnDestroy() { IsTransitioning = false; }
}
