using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Feedback shared by inventory, case selection and the printing press.</summary>
public class GameFeedback : MonoBehaviour
{
    private static GameFeedback instance;
    private readonly Queue<string> messages = new Queue<string>();
    private TextMeshProUGUI label;
    private GameObject panel;
    private Coroutine display;

    public static void Show(string message)
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var root = new GameObject("Avisos do jogo", typeof(Canvas), typeof(CanvasScaler), typeof(GameFeedback));
            instance = root.GetComponent<GameFeedback>();
            DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            instance.BuildPanel(root.transform);
        }
        instance.messages.Enqueue(message);
        if (instance.display == null) instance.display = instance.StartCoroutine(instance.Display());
    }

    private void BuildPanel(Transform parent)
    {
        panel = new GameObject("Aviso", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -115);
        rect.sizeDelta = new Vector2(820, 100);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0.13f, 0.09f, 0.06f, 0.96f);
        background.raycastTarget = false;
        var textObject = new GameObject("Mensagem", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        label = textObject.GetComponent<TextMeshProUGUI>();
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24, 12);
        label.rectTransform.offsetMax = new Vector2(-24, -12);
        label.fontSize = 27;
        label.color = new Color(1f, 0.9f, 0.69f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private IEnumerator Display()
    {
        while (messages.Count > 0)
        {
            panel.SetActive(true);
            label.text = messages.Dequeue();
            yield return new WaitForSecondsRealtime(3.5f);
        }
        panel.SetActive(false);
        display = null;
    }

    public static void PlaySound(string path, float volume = 0.65f)
    {
        if (!Application.isPlaying) return;
        try
        {
            var sound = FMODUnity.RuntimeManager.CreateInstance(path);
            sound.setVolume(volume);
            sound.start();
            sound.release();
        }
        catch (FMODUnity.EventNotFoundException exception)
        {
            Debug.LogWarning($"[GameFeedback] {exception.Message}");
        }
    }

    private void OnDestroy() { if (instance == this) instance = null; }
}
