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

        // Verificação de plataforma: essa HUD (joystick + botões de ação) só faz sentido
        // em tela de toque. Mesmo critério usado em MobileUIManager.cs.
#if UNITY_EDITOR
        // Mantém ativo no Editor pra dar pra testar sem precisar buildar pro Android.
#elif UNITY_ANDROID || UNITY_IOS
        // Build mobile: mantém ativo.
#else
        // PC/standalone: não tem toque, então desativa a HUD mobile inteira.
        gameObject.SetActive(false);
#endif
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
            return;
        }

        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }
}