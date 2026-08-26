using UnityEngine;

public class PressInteractable : MonoBehaviour, IInteractable
{
    [Header("Paineis de Interface (UI)")]
    public GameObject pressUIPanel;
    public InventoryManager inventoryManager;

    private void Start()
    {
        // Padrão Observer: A Prensa começa a "escutar" o inventário
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryToggled += HandleInventoryToggled;
        }
    }

    private void OnDestroy()
    {
        // Boa Prática: Evita Memory Leaks ao destruir o objeto
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryToggled -= HandleInventoryToggled;
        }
    }

    public void Interact()
    {
        if (pressUIPanel != null)
        {
            bool vaiAbrir = !pressUIPanel.activeSelf;
            pressUIPanel.SetActive(vaiAbrir);

            if (inventoryManager != null && inventoryManager.inventoryUI != null)
            {
                // Abre ou fecha o inventário apenas se ele estiver dessincronizado da prensa
                if (inventoryManager.inventoryUI.activeSelf != vaiAbrir)
                {
                    inventoryManager.ToggleInventory();
                }
            }
            else
            {
                Time.timeScale = vaiAbrir ? 0f : 1f;
            }
        }
    }

    // Método disparado automaticamente sempre que o jogador usar o atalho do inventário
    private void HandleInventoryToggled(bool isInventoryOpen)
    {
        // Se o jogador FECHOU o inventário no atalho, e a prensa estava aberta, força o fechamento.
        if (!isInventoryOpen && pressUIPanel != null && pressUIPanel.activeSelf)
        {
            pressUIPanel.SetActive(false);
        }
    }

    public void ClosePressUI()
    {
        if(pressUIPanel != null) pressUIPanel.SetActive(false);
        
        // Se a prensa foi fechada pelo próprio botão "X" dela, fecha o inventário junto
        if(inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }
    }
}