using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    public GameObject opções;

    void Update()
    {
        // NOVA FORMA: Lê a tecla Escape usando o Novo Input System
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

    // Criei esse método para organizar a ação de pausar
    private void Pausar() 
    {
        container.SetActive(true);
        Time.timeScale = 0;
    }

    public void ResumirButton()
    {
        container.SetActive(false);
        Time.timeScale = 1;
    }

    public void ConfigButton()
    {
        opções.SetActive(true);
        // CORREÇÃO: Removi o Time.timeScale = 1 daqui. 
        // Se você despausar o jogo enquanto o jogador apenas abriu as opções, 
        // o jogo continuará rodando no fundo com a tela coberta!
    }

    public void MainMenuButton()
    {
        // CORREÇÃO: É fundamental voltar o tempo ao normal antes de mudar de cena, 
        // senão o Menu Principal vai carregar "congelado".
        Time.timeScale = 1; 
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu principal");
    }
}