using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    public GameObject opções;
    private bool ownsPauseRequest;

    void Update()
    {
        // Lê a tecla Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Lógica de "Toggle": Se já estiver aberto, fecha. Se estiver fechado, abre.
            if (container.activeSelf)
            {
                ResumirButton();
            }
            else
            {
                Pausar();
            }
        }
    }


    private void Pausar()
    {
        if (container == null) return;
        container.SetActive(true);
        SetPauseRequest(true);

        // Desliga o raycast dos controles mobile enquanto o menu de pause está aberto.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    public void ResumirButton()
    {
        if (container == null) return;
        container.SetActive(false);
        SetPauseRequest(false);

        // Devolve o raycast pros controles mobile assim que o menu fecha.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);
    }

    public void ConfigButton()
    {
        opções.SetActive(true);
        // Removi o Time.timeScale = 1 daqui.

    }

    public void MainMenuButton()
    {

        PauseManager.ForceReset();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu principal");
    }

    public void TogglePauseMobile()
    {
        // Se o menu já estiver aberto, fecha. Se estiver fechado, pausa.
        if (container.activeSelf)
        {
            ResumirButton();
        }
        else
        {
            Pausar();
        }
    }

    private void SetPauseRequest(bool shouldPause)
    {
        if (ownsPauseRequest == shouldPause) return;
        ownsPauseRequest = shouldPause;
        PauseManager.RequestPause(shouldPause);
    }

    private void OnDestroy()
    {
        if (ownsPauseRequest)
            SetPauseRequest(false);
    }
}
