using UnityEngine;

public class TableInteractable : MonoBehaviour, IInteractable
{
    [Header("Interface")]
    [Tooltip("Arraste o GameObject do painel de Casos (UI) aqui")]
    public GameObject caseSelectionUI;

    [Header("Trava de Progressão (Opcional)")]
    [Tooltip("Arraste o ScriptableObject do item necessário para liberar a mesa.")]
    public Item itemObrigatorio;
    
    [Tooltip("O que ele pensa se tentar mexer na mesa antes da hora?")]
    public DialogueData pensamentoBloqueado;

    public void Interact()
    {
        if (itemObrigatorio != null && GameManager.Instance != null && GameManager.Instance.inventoryManager != null)
        {
            if (!GameManager.Instance.inventoryManager.HasItem(itemObrigatorio.itemID))
            {
                if (pensamentoBloqueado != null && GameManager.Instance.dialogueSystem != null)
                {
                    GameManager.Instance.dialogueSystem.dialogueData = pensamentoBloqueado;
                    GameManager.Instance.dialogueSystem.Next();
                }
                return; // Impede que o painel abra
            }
        }

        if (caseSelectionUI.activeSelf) return;
        caseSelectionUI.SetActive(true);
        Debug.Log("O jogador abriu as cartas sobre a mesa.");
    }
}