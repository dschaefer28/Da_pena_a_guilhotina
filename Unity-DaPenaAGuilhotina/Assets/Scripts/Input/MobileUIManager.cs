using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    [Header("UI Dinâmica")]
    [Tooltip("Arraste o JoystickBG aqui para ele sumir durante os diálogos.")]
    public GameObject joystickUI;

#if UNITY_EDITOR
    /// <summary>Chave do ajuste global "simular celular no Editor" (menu Ferramentas > Controles). Antes era um
    /// checkbox por cena (showInEditorForTesting), e cada cena ficava com um valor: o tutorial mostrava "E" no
    /// Jogo e "Interagir" no Porão.</summary>
    public const string ChaveSimularCelularNoEditor = "DaPena_SimularCelularNoEditor";
#endif

    /// <summary>Verdadeiro quando os controles de toque estão em uso. É a fonte da verdade para textos e glifos
    /// do tutorial (DispositivoDeControle) e para mostrar ou não os botões mobile, igual em todas as cenas:
    /// build Android/iOS = celular; build PC = teclado; no Editor = menu Ferramentas > Controles, ou o
    /// Device Simulator (aba Simulator no lugar da Game).</summary>
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
        // Lógica de PC vs Android
        gameObject.SetActive(ControlesMobileAtivos);
    }

    void Start()
    {
        // Encontra o sistema de diálogos ao iniciar a cena (prioriza a referência global já resolvida)
        if (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
            dialogueSystem = GameManager.Instance.dialogueSystem;
        else
            dialogueSystem = FindAnyObjectByType<DialogueSystem>();

        if (dialogueSystem != null)
        {
            // Evita inscrição dupla caso Start rode mais de uma vez
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
        // Se desinscreve para evitar erros caso a cena mude
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
