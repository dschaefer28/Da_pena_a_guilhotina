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
        if (SceneTransition.IsTransitioning) return;
        // Verifica se existe um item exigido e se o jogador não o possui
        if (itemObrigatorio != null)
        {
            InventoryManager inventory = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
            if (inventory == null)
            {
                Debug.LogError("[DoorInteractable] Não foi possível validar o item obrigatório porque o inventário está ausente.", this);
                return;
            }

            if (!inventory.HasItem(itemObrigatorio.itemID))
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

        if (string.IsNullOrWhiteSpace(cenaDestino) || !Application.CanStreamedLevelBeLoaded(cenaDestino))
        {
            Debug.LogError($"[DoorInteractable] Cena '{cenaDestino}' não está nas Build Settings.", this);
            return;
        }

        Debug.Log($"Salvando inventário e retornando para a cena: {cenaDestino}...");
        if (GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            GameManager.Instance.inventoryManager.SalvarEstadoAtual();
        }
        PauseManager.ForceReset();
        GameFeedback.PlaySound("event:/portaabrir");
        SceneTransition.Load(cenaDestino);
    }
}
