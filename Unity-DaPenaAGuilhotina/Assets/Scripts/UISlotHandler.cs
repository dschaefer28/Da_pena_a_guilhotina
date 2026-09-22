using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using FMODUnity;

public class UISlotHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private bool draggingItem;
    private bool suppressClick;
    private LayoutElement layoutElement;
    private CanvasGroup canvasGroup;
    public Item item;
    public Image slotImg;
    public TextMeshProUGUI itemCount;
    public TextMeshProUGUI itemNameText;
    public InventoryManager inventoryManager;

    [Header("Lista do Inventário (opcional)")]
    [Tooltip("Objeto mostrado só enquanto este slot está vazio (ex: o texto 'Espaço livre' da linha). Deixe vazio nos slots da prensa.")]
    public GameObject objetoVazio;

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
            if (itemNameText != null) itemNameText.text = item.NomeExibicao;
        }
        else
        {
            itemCount.text = string.Empty;
            slotImg.gameObject.SetActive(false);
            if (itemNameText != null) itemNameText.text = string.Empty;
        }
        AtualizarObjetoVazio();
    }

    public void AtualizarObjetoVazio()
    {
        if (objetoVazio != null) objetoVazio.SetActive(item == null);
    }

    /// <summary>Mostra/esconde esta linha na lista sem desativar o GameObject (um slot inativo no meio
    /// de um arrasto perderia o OnEndDrag). Sem LayoutElement/CanvasGroup (slots da prensa) não faz nada.</summary>
    public void DefinirVisivelNaLista(bool visivel)
    {
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (layoutElement != null) layoutElement.ignoreLayout = !visivel;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visivel ? 1f : 0f;
            canvasGroup.blocksRaycasts = visivel;
            canvasGroup.interactable = visivel;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (suppressClick) return;
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

    public void OnPointerDown(PointerEventData eventData) => suppressClick = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        var mouse = MouseManager.instance;
        if (eventData.button != PointerEventData.InputButton.Left || item == null ||
            inventoryManager == null || mouse == null || mouse.heldItem != null) return;
        suppressClick = true;
        mouse.UpdateHeldItem(this);
        draggingItem = mouse.heldItem != null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // MouseManager moves the held item's icon for both mouse and touch.
    }

    public void OnDrop(PointerEventData eventData)
    {
        var source = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<UISlotHandler>() : null;
        if (source == null || !source.draggingItem || MouseManager.instance == null) return;
        MouseManager.instance.UpdateHeldItem(this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!draggingItem) return;
        draggingItem = false;
        // Cancelled drops, or the other item after a swap, return to the source.
        if (MouseManager.instance != null && MouseManager.instance.heldItem != null)
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
