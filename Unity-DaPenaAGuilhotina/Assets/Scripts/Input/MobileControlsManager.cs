using UnityEngine;

/// <summary>Liga/desliga o toque dos controles mobile. Vai no GameObject do Canvas "MobileControlsUI".</summary>
public class MobileControlsManager : MonoBehaviour
{
    public static MobileControlsManager Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;

    // TouchZone (tela cheia) é irmã do JoystickBG, fora do canvasGroup acima; sem isto ela rouba o toque da UI modal.
    [SerializeField] private CanvasGroup touchZoneCanvasGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>False desliga o raycast para a UI modal por cima receber o toque.</summary>
    public void SetControlsInteractable(bool isInteractable)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("MobileControlsManager: CanvasGroup não foi atribuído no Inspector.");
        }
        else
        {
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
        }

        if (touchZoneCanvasGroup != null)
        {
            touchZoneCanvasGroup.interactable = isInteractable;
            touchZoneCanvasGroup.blocksRaycasts = isInteractable;
        }
    }
}