using UnityEngine;
using UnityEngine.UI;

/// <summary>Botão "Fechar" do painel do inventário. Resolve o InventoryManager em runtime porque o
/// botão vive dentro do prefab UI_Inventory e o manager fica fora dele (no prefab UI).</summary>
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
