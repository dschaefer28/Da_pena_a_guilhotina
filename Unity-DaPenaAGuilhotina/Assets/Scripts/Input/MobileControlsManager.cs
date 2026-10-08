using UnityEngine;

/// <summary>
/// Controla se os controles mobile (joystick virtual, botões de ação, etc.)
/// estão recebendo toque ou não. Deve ser colocado no mesmo GameObject
/// que tem o Canvas "MobileControlsUI" e o CanvasGroup atribuído no Inspector.
/// </summary>
public class MobileControlsManager : MonoBehaviour
{
    public static MobileControlsManager Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;

    // NOVO: o "TouchZone" (área de toque em tela cheia usada pelo joystick) é irmã do
    // JoystickBG, então o CanvasGroup acima (só no JoystickBG) não cobre ela. Sem isso,
    // o TouchZone continuava sempre recebendo raycast e "roubava" o toque de qualquer
    // UI que abrisse por cima (pause, inventário, prensa) mesmo com o joystick desligado.
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

    /// <summary>
    /// true  -> controles mobile ativos e recebendo toque normalmente (estado padrão).
    /// false -> controles mobile "desligados" para raycast, para que UI modal
    ///          (diálogo, pause, etc.) por cima receba o toque em vez deles.
    /// </summary>
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

        // Desliga também o TouchZone (senão ele continua roubando o toque da UI aberta por cima).
        if (touchZoneCanvasGroup != null)
        {
            touchZoneCanvasGroup.interactable = isInteractable;
            touchZoneCanvasGroup.blocksRaycasts = isInteractable;
        }
    }
}