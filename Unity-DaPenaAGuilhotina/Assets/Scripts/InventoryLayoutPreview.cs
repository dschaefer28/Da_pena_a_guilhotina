using UnityEngine;

/// <summary>
/// Ferramenta de desenvolvimento: com "Show Preview" marcado, enche o inventário com itens de
/// exemplo ao entrar no Play Mode, para testar o layout da lista com nomes longos e pilhas.
/// Só existe no Editor e nunca entra no save (os itens têm o prefixo "__inventory_preview_").
/// Desmarque para testar o jogo de verdade.
/// </summary>
[DisallowMultipleComponent]
public class InventoryLayoutPreview : MonoBehaviour
{
    public bool showPreview;
    public Transform contentContainer;
    public Sprite documentIcon;
    public Sprite letterIcon;

#if UNITY_EDITOR
    private bool runtimeSeeded;
    private readonly string[] names =
    {
        "Carta", "Decreto de 1780", "Relato Bradier", "Panfleto",
        "Anotações do joalheiro", "Recibo de compra",
        "Depoimento completo das testemunhas do caso", "Documento selado"
    };
    private readonly int[] amounts = { 1, 2, 1, 12, 3, 5, 1, 99 };

    // LateUpdate roda depois do Start/RestaurarInventario do InventoryManager.
    private void LateUpdate()
    {
        if (!showPreview || runtimeSeeded || contentContainer == null) return;
        SeedRuntimeItems();
    }

    private void SeedRuntimeItems()
    {
        var slots = contentContainer.GetComponentsInChildren<UISlotHandler>(true);
        var manager = System.Array.Find(slots, slot => slot.inventoryManager != null)?.inventoryManager;
        if (manager == null) return;

        int sample = 0;
        foreach (var slot in slots)
        {
            if (slot.item != null || slot.slotImg == null || slot.itemCount == null) continue;
            if (sample >= names.Length) break;

            var item = ScriptableObject.CreateInstance<Item>();
            item.hideFlags = HideFlags.DontSave;
            item.name = names[sample];
            item.itemID = "__inventory_preview_" + sample;
            item.itemName = names[sample];
            item.itemAmt = amounts[sample];
            item.itemImg = sample % 2 == 0 ? documentIcon : letterIcon;
            slot.inventoryManager = manager;
            manager.PlaceInInventory(slot, item);
            sample++;
        }
        runtimeSeeded = true;
    }
#endif
}
