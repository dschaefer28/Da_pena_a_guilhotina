using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolve o InventoryManager em runtime: o botão está no prefab UI_Inventory e o manager fica fora dele.</summary>
[RequireComponent(typeof(Button))]
public class BotaoFecharInventario : MonoBehaviour
{
    void Awake() => GetComponent<Button>().onClick.AddListener(Fechar);

    private void Fechar()
    {
        InventoryManager inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventario == null) inventario = FindAnyObjectByType<InventoryManager>();

        if (inventario != null && inventario.inventoryUI != null && inventario.inventoryUI.activeSelf)
            inventario.ToggleInventory();
    }
}
