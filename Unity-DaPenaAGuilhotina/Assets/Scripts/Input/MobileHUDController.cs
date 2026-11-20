using UnityEngine;

public class MobileHUDController : MonoBehaviour
{
    [Header("Interface Mobile")]
    [Tooltip("Arraste o objeto pai que contém todos os botões do Android aqui.")]
    public GameObject painelHUD;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
        {
            GameManager.Instance.dialogueSystem.OnDialogueStarted += EsconderHUD;
            GameManager.Instance.dialogueSystem.OnDialogueEnded += MostrarHUD;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && GameManager.Instance.dialogueSystem != null)
        {
            GameManager.Instance.dialogueSystem.OnDialogueStarted -= EsconderHUD;
            GameManager.Instance.dialogueSystem.OnDialogueEnded -= MostrarHUD;
        }
    }

    private void EsconderHUD()
    {
        if (painelHUD != null) painelHUD.SetActive(false);
    }

    private void MostrarHUD()
    {
        if (painelHUD != null) painelHUD.SetActive(true);
    }
}