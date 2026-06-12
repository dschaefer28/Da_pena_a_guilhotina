using UnityEngine;

public class PressInteractable : MonoBehaviour
{
    [Header("Paineis de Interface (UI) - Opcional para testes")]
    public GameObject pressUIPanel; 
    public InventoryManager inventoryManager;

   public void Interact()
    {
        if (pressUIPanel != null)
        {
            bool vaiAbrir = !pressUIPanel.activeSelf;
            pressUIPanel.SetActive(vaiAbrir);

            if (inventoryManager != null && inventoryManager.inventoryUI != null)
            {
                inventoryManager.inventoryUI.SetActive(vaiAbrir);
            }

            
            Time.timeScale = vaiAbrir ? 0f : 1f;
        }
    }

    public void ClosePressUI()
    {
        if(pressUIPanel != null) pressUIPanel.SetActive(false);
        if(inventoryManager != null && inventoryManager.inventoryUI != null)
        {
            inventoryManager.inventoryUI.SetActive(false);
        }

        
        Time.timeScale = 1f;
    }
}