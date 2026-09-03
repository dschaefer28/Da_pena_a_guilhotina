using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    [Header("Configurações de Teste")]
    [Tooltip("Marque para ver os botões no Editor da Unity enquanto programa. Eles sumirão no Build final de PC de qualquer forma.")]
    public bool showInEditorForTesting = true;

    [Header("UI Dinâmica")]
    [Tooltip("Arraste o JoystickBG aqui para ele sumir durante os diálogos.")]
    public GameObject joystickUI; 

    private DialogueSystem dialogueSystem;

    void Awake()
    {
        // Lógica de PC vs Android
#if UNITY_EDITOR
        gameObject.SetActive(showInEditorForTesting);
#elif UNITY_ANDROID || UNITY_IOS
        gameObject.SetActive(true);
#else
        gameObject.SetActive(false);
#endif
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