using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UISlotHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Item item;
    public Image slotImg;
    public TextMeshProUGUI itemCount;
    public InventoryManager inventoryManager;

    [Header("Toque - Long Press")]
    [Tooltip("Tempo em segundos segurando o slot para contar como 'clique direito' (pegar 1 item por vez). Usado no lugar do clique direito, que não existe no toque.")]
    [SerializeField] private float longPressThreshold = 0.35f;

    // Usamos tempo NÃO escalado porque o inventário pausa o jogo (Time.timeScale = 0)
    // enquanto está aberto; com Time.time normal, a contagem do long press travaria.
    private float pointerDownTime;
    private bool longPressTriggered;

    void Awake()
    {
        if (item != null)
        {
            item = item.Clone();
            slotImg.sprite = item.itemImg;
            itemCount.text = item.itemAmt.ToString();
        }
        else
        {
            itemCount.text = string.Empty;
            slotImg.gameObject.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownTime = Time.unscaledTime;
        longPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float heldDuration = Time.unscaledTime - pointerDownTime;

        if (heldDuration >= longPressThreshold)
        {
            longPressTriggered = true;

            if (item == null) return;
            MouseManager.instance.PickupFromStack(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Clique direito no mouse continua funcionando exatamente como antes (PC).
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (item == null) { return; }

            MouseManager.instance.PickupFromStack(this);
            return;
        }

        // Se esse toque/clique já foi tratado como long press no OnPointerUp,
        // não executa a ação normal de novo (evita disparar as duas ações juntas).
        if (longPressTriggered)
        {
            longPressTriggered = false;
            return;
        }

        MouseManager.instance.UpdateHeldItem(this);
    }
}