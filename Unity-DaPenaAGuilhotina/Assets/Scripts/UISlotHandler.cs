using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using FMODUnity;

public class UISlotHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public Item item;
    public Image slotImg;
    public TextMeshProUGUI itemCount;
    public InventoryManager inventoryManager;

    [Header("Áudio de Hover (FMOD)")]
    public EventReference somHover;
    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    public float hoverPitchMin = 0.92f;
    public float hoverPitchMax = 1.08f;

    void Awake()
    {
        if (inventoryManager == null)
            inventoryManager = GetComponentInParent<InventoryManager>();

        if (slotImg == null || itemCount == null)
        {
            Debug.LogError($"[UISlotHandler] '{name}' está sem slotImg ou itemCount atribuído.", this);
            return;
        }

        if (item != null)
        {
            item = item.Clone();
            slotImg.sprite = item.itemImg;
            slotImg.gameObject.SetActive(true);
            itemCount.text = item.itemAmt.ToString();
        }
        else
        {
            itemCount.text = string.Empty;
            slotImg.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (MouseManager.instance == null)
        {
            Debug.LogWarning("[UISlotHandler] MouseManager ausente na cena.");
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if(item == null) { return; }

            MouseManager.instance.PickupFromStack(this);
            return;
        }

        MouseManager.instance.UpdateHeldItem(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null || somHover.IsNull) return;

        // Instância manual para poder mexer em volume e pitch antes de tocar
        FMOD.Studio.EventInstance hover = RuntimeManager.CreateInstance(somHover);
        hover.setVolume(hoverVolume);
        hover.setPitch(Random.Range(hoverPitchMin, hoverPitchMax));
        hover.start();
        hover.release();
    }
}