using UnityEngine;

public class PressInteractable : MonoBehaviour, IInteractable
{
    [Header("Paineis de Interface (UI)")]
    public GameObject pressUIPanel;
    public InventoryManager inventoryManager;
    private bool ownsPauseRequest;

    private void Start()
    {
        // Fallback: usa o inventário global se não foi ligado no Inspector
        if (inventoryManager == null && GameManager.Instance != null)
            inventoryManager = GameManager.Instance.inventoryManager;

        // Padrão Observer: A Prensa começa a "escutar" o inventário
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryToggled -= HandleInventoryToggled; // evita inscrição dupla
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
        SetFallbackPause(false);
    }

    public void Interact()
    {
        if (pressUIPanel == null)
        {
            Debug.LogWarning("[PressInteractable] pressUIPanel não atribuído no Inspector.", this);
            return;
        }

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
            SetFallbackPause(vaiAbrir);
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
        else
        {
            SetFallbackPause(false);
        }
    }

    private void SetFallbackPause(bool shouldPause)
    {
        if (ownsPauseRequest == shouldPause) return;
        ownsPauseRequest = shouldPause;
        PauseManager.RequestPause(shouldPause);
    }
}
