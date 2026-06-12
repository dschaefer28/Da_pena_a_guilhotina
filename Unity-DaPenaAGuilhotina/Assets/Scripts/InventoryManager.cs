using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryGrid;
    public bool messyInventory;

    public GameObject inventoryUI;

    private void Awake()
    {
        ConfigureInventory();

        if(inventoryUI != null) inventoryUI.SetActive(false);
    }



  public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            // Inverte o estado atual
            bool vaiAbrir = !inventoryUI.activeSelf;
            inventoryUI.SetActive(vaiAbrir);

          
            Time.timeScale = vaiAbrir ? 0f : 1f;
        }
    }
    public void PlaceInInventory(UISlotHandler activeSlot, Item item)
    {
        activeSlot.item = item;
        activeSlot.slotImg.sprite = item.itemImg;
        activeSlot.itemCount.text = item.itemAmt.ToString();
        activeSlot.slotImg.gameObject.SetActive(true);
        ConfigureInventory();
    }

    public void StackInInventory(UISlotHandler activeSlot, Item item)
    {
        if(activeSlot.item.itemID != item.itemID) { return; }

        activeSlot.item.itemAmt += item.itemAmt;
        activeSlot.itemCount.text = activeSlot.item.itemAmt.ToString();
        ConfigureInventory();
    }

    public void ClearItemSlot(UISlotHandler activeSlot)
    {
        activeSlot.slotImg.sprite = null;
        activeSlot.slotImg.gameObject.SetActive(false);
        activeSlot.itemCount.text = string.Empty;
        activeSlot.item = null;
    }

    public void ConfigureInventory()
    {
        if (messyInventory) { return; }

        //Loop through each child of inventory grid
        //Rearrange by populated items

        List<Transform> uiSlots = new List<Transform>();
        for(int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            uiSlots.Add(inventoryGrid.transform.GetChild(i));
        }

        uiSlots.Sort((a, b) =>
        {
            UISlotHandler itemA = a.GetComponent<UISlotHandler>();
            UISlotHandler itemB = b.GetComponent<UISlotHandler>();

            bool hasItemA = itemA.item != null;
            bool hasItemB = itemB.item != null;

            return hasItemB.CompareTo(hasItemA);
        });

        for(int i = 0; i < uiSlots.Count; i++)
        {
            uiSlots[i].SetSiblingIndex(i);
        }
    }

    public bool AddItem(Item itemToAdd)
    {
       
        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot.item != null && slot.item.itemID == itemToAdd.itemID)
            {
                StackInInventory(slot, itemToAdd);
                return true; 
            }
        }

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot.item == null)
            {
                Item newItem = itemToAdd.Clone(); 
                PlaceInInventory(slot, newItem);
                return true; 
            }
        }

        
        Debug.Log("Inventário cheio! Não foi possível pegar o item.");
        return false; 
    }
}