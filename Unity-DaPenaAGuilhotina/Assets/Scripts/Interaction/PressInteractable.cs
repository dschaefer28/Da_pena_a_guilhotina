using UnityEngine;

public class PressInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Nome que aparece embaixo do \"E\"/\"Interagir\" quando o jogador chega perto. Vazio = nome do GameObject.")]
    public string nomeExibido = "Prensa";
    public string NomeExibido => NomeDoInteragivel.Resolver(nomeExibido, gameObject);

    [Header("Paineis de Interface (UI)")]
    public GameObject pressUIPanel;
    public InventoryManager inventoryManager;

    private void Start()
    {
        if (inventoryManager == null && GameManager.Instance != null)
            inventoryManager = GameManager.Instance.inventoryManager;

        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryToggled -= HandleInventoryToggled;
            inventoryManager.OnInventoryToggled += HandleInventoryToggled;
        }
    }

    private void OnDestroy()
    {
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

        if (vaiAbrir && PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
        {
            Debug.Log("Não é possível abrir a prensa com o jogo pausado.");
            return;
        }

        pressUIPanel.SetActive(vaiAbrir);
        if (vaiAbrir && TutorialManager.Instance != null)
            TutorialManager.Instance.NotificarEvento(TutorialManager.EVENTO_PRENSA_ABERTA);

        // Como no PauseMenu: sem raycast nos controles mobile, o toque vai para a UI da prensa.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(!vaiAbrir);

        if (inventoryManager != null && inventoryManager.inventoryUI != null)
        {
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

    // Fechar o inventário pelo atalho fecha a prensa junto.
    private void HandleInventoryToggled(bool isInventoryOpen)
    {
        if (!isInventoryOpen && pressUIPanel != null && pressUIPanel.activeSelf)
        {
            pressUIPanel.SetActive(false);
        }
    }

    public void ClosePressUI()
    {
        if(pressUIPanel != null) pressUIPanel.SetActive(false);

        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);

        if(inventoryManager != null && inventoryManager.inventoryUI != null && inventoryManager.inventoryUI.activeSelf)
        {
            inventoryManager.ToggleInventory();
        }
    }
}