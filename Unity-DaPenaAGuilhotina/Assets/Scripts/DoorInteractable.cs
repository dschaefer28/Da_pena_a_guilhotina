using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações de Transição")]
    [Tooltip("O nome exato da cena de destino (Ex: Fase2)")]
    public string cenaDestino = "Fase2";

    public void Interact()
    {
        Debug.Log($"Salvando inventário e retornando para a cena: {cenaDestino}...");

        // ARQUITETURA: Salva o inventário atual no GameManager antes de destruir a cena
        // Assim, o panfleto que você imprimiu na prensa não será perdido.
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.SalvarEstadoAtual();
        }

        // Carrega a nova cena
        SceneManager.LoadScene(cenaDestino);
    }
}