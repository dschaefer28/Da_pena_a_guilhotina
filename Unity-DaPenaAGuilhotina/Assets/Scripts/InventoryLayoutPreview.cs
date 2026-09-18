using UnityEngine;

// Editor samples: visual in edit mode, interactive clones during Play.
[ExecuteAlways]
[DisallowMultipleComponent]
public class InventoryLayoutPreview : MonoBehaviour
{
    public bool showPreview = true;
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
    private readonly System.Collections.Generic.List<UISlotHandler> previewed =
        new System.Collections.Generic.List<UISlotHandler>();

    private void OnEnable()
    {
        if (!Application.isPlaying) LateUpdate();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            if (showPreview && !runtimeSeeded && contentContainer != null)
                SeedRuntimeItems();
            return;
        }
        ClearPreview();
        if (!showPreview || contentContainer == null)
        {
            return;
        }

        int sample = 0;
        foreach (Transform child in contentContainer)
        {
            var slot = child.GetComponent<UISlotHandler>();
            if (slot == null || slot.slotImg == null || slot.itemNameText == null)
                continue;
            if (Application.isPlaying && slot.item != null)
            {
                previewed.Remove(slot);
                continue;
            }
            if (sample >= names.Length) break;

            slot.slotImg.sprite = sample % 2 == 0 ? documentIcon : letterIcon;
            slot.slotImg.gameObject.SetActive(true);
            slot.itemNameText.text = names[sample];
            if (slot.itemCount != null) slot.itemCount.text = amounts[sample].ToString();
            if (!previewed.Contains(slot)) previewed.Add(slot);
            sample++;
        }
    }

    private void SeedRuntimeItems()
    {
        // LateUpdate runs after the inventory's Start/restore routine.
        var slots = contentContainer.GetComponentsInChildren<UISlotHandler>(true);
        var manager = System.Array.Find(slots, slot => slot.inventoryManager != null)?.inventoryManager;
        if (manager == null) return;
        ClearPreview();
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

    private void OnDisable() => ClearPreview();

    private void ClearPreview()
    {
        foreach (var slot in previewed)
        {
            if (slot == null || (Application.isPlaying && slot.item != null)) continue;
            if (slot.slotImg != null)
            {
                slot.slotImg.sprite = null;
                slot.slotImg.gameObject.SetActive(false);
            }
            if (slot.itemNameText != null) slot.itemNameText.text = string.Empty;
            if (slot.itemCount != null) slot.itemCount.text = string.Empty;
        }
        previewed.Clear();
    }
#endif
}
