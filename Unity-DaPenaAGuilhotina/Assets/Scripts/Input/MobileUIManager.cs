using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    [Header("UI Dinâmica")]
    [Tooltip("Arraste o JoystickBG aqui para ele sumir durante os diálogos.")]
    public GameObject joystickUI;

#if UNITY_EDITOR
    /// <summary>EditorPrefs global (Ferramentas > Controles), não por cena, para todas as cenas concordarem.</summary>
    public const string ChaveSimularCelularNoEditor = "DaPena_SimularCelularNoEditor";
#endif

    /// <summary>Fonte da verdade de "é toque?" para textos e botões. No Editor: Ferramentas > Controles ou Device Simulator.</summary>
    public static bool ControlesMobileAtivos
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(ChaveSimularCelularNoEditor, false) ||
                   UnityEngine.Device.Application.isMobilePlatform;
#elif UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }
    }

    private DialogueSystem dialogueSystem;

    void Awake()
    {
        gameObject.SetActive(ControlesMobileAtivos);
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            dialogueSystem = GameManager.Instance.dialogueSystem;
        else
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();

        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted -= EsconderJoystick;
            dialogueSystem.OnDialogueEnded -= MostrarJoystick;

            dialogueSystem.OnDialogueStarted += EsconderJoystick;
            dialogueSystem.OnDialogueEnded += MostrarJoystick;
        }
        else
        {
            Debug.LogWarning("[MobileUIManager] Nenhum DialogueSystem encontrado na cena; o joystick não será ocultado durante diálogos.");
        }
    }

    void OnDestroy()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted -= EsconderJoystick;
            dialogueSystem.OnDialogueEnded -= MostrarJoystick;
        }
    }

    private void EsconderJoystick()
    {
        if (joystickUI != null)
        {
            joystickUI.SetActive(false);
        }
    }

    private void MostrarJoystick()
    {
        if (joystickUI != null)
        {
            joystickUI.SetActive(true);
        }
    }
}
