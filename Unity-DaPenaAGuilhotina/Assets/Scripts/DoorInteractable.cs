using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações")]
    public string nomeDaCenaFase2 = "Fase2"; // O nome exato da sua cena da fase 2

    public void Interact()
    {
        // Verifica no GameManager imortal se o jogador já escolheu um caso
        if (GameManager.Instance != null && GameManager.Instance.casoEscolhido != null)
        {
            Debug.Log("O detetive sai de casa com um caso em mãos. Carregando Fase 2...");
            SceneManager.LoadScene(nomeDaCenaFase2);
        }
        else
        {
            // O jogador apertou E na porta sem olhar a mesa
            Debug.Log("Não posso sair ainda. Preciso escolher um caso na mesa primeiro!");
            
            // Futuramente, você pode ligar isso ao seu sistema de diálogo para o 
            // personagem "pensar" isso na tela.
        }
    }
}