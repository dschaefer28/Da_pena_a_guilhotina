using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MouseManager : MonoBehaviour
{
    public static MouseManager instance;
    public Item heldItem;
    public Item GetHeldItem { get { return heldItem; } }

    [Header("Visual do Item Arrasto")]
    public Image dragIcon;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (dragIcon == null) return;

        if (heldItem != null && heldItem.itemAmt > 0)
        {
            if (!dragIcon.gameObject.activeSelf) dragIcon.gameObject.SetActive(true);
            if (dragIcon.sprite != heldItem.itemImg) dragIcon.sprite = heldItem.itemImg;

            // ARQUITETURA UNIVERSAL: Pointer.current capta Mouse (Windows) e Toque (Android)
            if (Pointer.current != null)
            {
                dragIcon.transform.position = Pointer.current.position.ReadValue();
            }
        }
        else
        {
            if (heldItem != null && heldItem.itemAmt <= 0) heldItem = null; // stack zerado: solta o item fantasma
            if (dragIcon.gameObject.activeSelf) dragIcon.gameObject.SetActive(false);
        }
    }

    public void UpdateHeldItem(UISlotHandler activeSlot)
    {
        if (activeSlot == null || activeSlot.inventoryManager == null)
        {
            Debug.LogWarning("[MouseManager] Slot ou InventoryManager nulo em UpdateHeldItem.");
            return;
        }

        var inventory = activeSlot.inventoryManager;
        var activeItem = activeSlot.item;

        if (heldItem != null && activeItem != null && heldItem.itemID == activeItem.itemID)
        {
            inventory.StackInInventory(activeSlot, heldItem);
            heldItem = null;
            return;
        }

        if (activeSlot.item != null)
        {
            inventory.ClearItemSlot(activeSlot);
        }

        if (heldItem != null)
            inventory.PlaceInInventory(activeSlot, heldItem);

        heldItem = activeItem;
    }

    public void PickupFromStack(UISlotHandler activeSlot)
    {
        if (activeSlot == null || activeSlot.item == null || activeSlot.inventoryManager == null) { return; }
        if (heldItem != null && heldItem.itemID != activeSlot.item.itemID) { return; }
        if (heldItem == null)
        {
            heldItem = activeSlot.item.Clone();
            heldItem.itemAmt = 0;
        }

        heldItem.itemAmt++;
        activeSlot.item.itemAmt--;
        if (activeSlot.itemCount != null)
            activeSlot.itemCount.text = activeSlot.item.itemAmt.ToString();

        if (activeSlot.item.itemAmt <= 0)
        {
            activeSlot.inventoryManager.ClearItemSlot(activeSlot);
        }
    }
}