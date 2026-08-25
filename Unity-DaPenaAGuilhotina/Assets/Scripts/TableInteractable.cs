using UnityEngine;

public class TableInteractable : MonoBehaviour, IInteractable
{
    [Header("Interface")]
    [Tooltip("Arraste o GameObject do painel de Casos (UI) aqui")]
    public GameObject caseSelectionUI;

    public void Interact()
    {
        // Se o painel já estiver aberto, não faz nada
        if (caseSelectionUI.activeSelf) return;

        // Ativa o painel de UI na tela
        caseSelectionUI.SetActive(true);
        
        Debug.Log("O jogador abriu as cartas sobre a mesa.");
        
        // Dica: Aqui você também pode chamar um evento para pausar o movimento do player,
        // igual fizemos no diálogo, para ele não sair andando com a tela aberta.
    }
}