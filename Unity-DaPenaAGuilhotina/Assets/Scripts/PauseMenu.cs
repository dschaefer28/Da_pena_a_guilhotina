using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    public GameObject opções;

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
        container.SetActive(true);
        Time.timeScale = 0;

        // Desliga o raycast dos controles mobile enquanto o menu de pause está aberto.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    public void ResumirButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;

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

        Time.timeScale = 1;
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
}