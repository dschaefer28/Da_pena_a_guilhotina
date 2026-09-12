using UnityEngine;

public class PressInteractable : MonoBehaviour, IInteractable
{
    [Header("Paineis de Interface (UI)")]
    public GameObject pressUIPanel;
    public InventoryManager inventoryManager;

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
    }

    public void Interact()
    {
        if (pressUIPanel == null)
        {
            Debug.LogWarning("[PressInteractable] pressUIPanel não atribuído no Inspector.", this);
            return;
        }

        bool vaiAbrir = !pressUIPanel.activeSelf;

        // Não deixa abrir a prensa com o jogo pausado (mesma regra do inventário).
        if (vaiAbrir && PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
        {
            Debug.Log("Não é possível abrir a prensa com o jogo pausado.");
            return;
        }

        pressUIPanel.SetActive(vaiAbrir);

        // Mesmo tratamento do PauseMenu: desliga o raycast dos controles mobile enquanto
        // a prensa está aberta, para o toque ir pros botões da UI em vez do joystick/botões.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(!vaiAbrir);

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

        // Garante que os controles mobile voltam a receber toque mesmo se a prensa for
        // fechada por este botão (e não pelo atalho de inventário).
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);

        // Se a prensa foi fechada pelo próprio botão "X" dela, fecha o inventário junto
        if(inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }
    }
}