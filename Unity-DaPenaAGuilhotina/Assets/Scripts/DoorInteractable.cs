using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações de Transição")]
    [Tooltip("O nome exato da cena de destino (Ex: Fase2)")]
    public string cenaDestino = "Fase2";

    [Header("Trava de Progressão (Opcional)")]
    [Tooltip("Arraste o ScriptableObject do item necessário para passar. Deixe VAZIO para portas livres.")]
    public Item itemObrigatorio;
    
    [Tooltip("O que o personagem pensa se tentar passar sem o item?")]
    public DialogueData pensamentoBloqueado;

    public void Interact()
    {
        // Verifica se existe um item exigido e se o jogador não o possui
        if (itemObrigatorio != null && GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            if (!GameManager.Instance.inventoryManager.HasItem(itemObrigatorio.itemID))
            {
                Debug.Log("A porta está trancada. Preciso terminar o meu trabalho primeiro.");
                
                if (pensamentoBloqueado != null && GameManager.Instance.dialogueSystem != null)
                {
                    GameManager.Instance.dialogueSystem.dialogueData = pensamentoBloqueado;
                    GameManager.Instance.dialogueSystem.Next();
                }
                return; // Corta a viagem
            }
        }

        Debug.Log($"Salvando inventário e retornando para a cena: {cenaDestino}...");
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.SalvarEstadoAtual();
        }
        SceneManager.LoadScene(cenaDestino);
    }
}