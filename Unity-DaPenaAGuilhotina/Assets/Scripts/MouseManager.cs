using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Adicionado para controlar a Image da UI
using UnityEngine.InputSystem; // Adicionado para o Novo Input System

public class MouseManager : MonoBehaviour
{
    public static MouseManager instance;

    public Item heldItem;
    public Item GetHeldItem { get { return heldItem; } }
    [Header("Visual do Item Arrasto")]
    public Image dragIcon;

    private void Awake()
    {
        instance = this;
        if (dragIcon != null) dragIcon.gameObject.SetActive(false);
    }

    private void Update()
    {
       
        if (heldItem != null && heldItem.itemAmt > 0)
        {
            if (dragIcon != null)
            {
                if (!dragIcon.gameObject.activeSelf) dragIcon.gameObject.SetActive(true);

                dragIcon.sprite = heldItem.itemImg;
              
                dragIcon.transform.position = Mouse.current.position.ReadValue();
            }
        }
        else
        {

            if (dragIcon != null && dragIcon.gameObject.activeSelf)
                dragIcon.gameObject.SetActive(false);
        }
    }

    public void UpdateHeldItem(UISlotHandler activeSlot)
    {
        var activeItem = activeSlot.item;

        if (heldItem != null && activeItem != null && heldItem.itemID == activeItem.itemID)
        {
            activeSlot.inventoryManager.StackInInventory(activeSlot, heldItem);
            heldItem = null;
            return;
        }

        if (activeSlot.item != null)
        {
            activeSlot.inventoryManager.ClearItemSlot(activeSlot);
        }
        if (heldItem != null)
            activeSlot.inventoryManager.PlaceInInventory(activeSlot, heldItem);

        heldItem = activeItem;
    }

    public void PickupFromStack(UISlotHandler activeSlot)
    {
        if (heldItem != null && heldItem.itemID != activeSlot.item.itemID) { return; }

        if (heldItem == null)
        {
            heldItem = activeSlot.item.Clone();
            heldItem.itemAmt = default;
        }
        heldItem.itemAmt++;

        activeSlot.item.itemAmt--;
        activeSlot.itemCount.text = activeSlot.item.itemAmt.ToString();

        if (activeSlot.item.itemAmt <= 0)
        {
            activeSlot.inventoryManager.ClearItemSlot(activeSlot);
        }
    }

}