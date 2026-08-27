using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryGrid;
    public bool messyInventory;
    public GameObject inventoryUI;

    // NOVO: Evento Observer para avisar outras UIs (como a Prensa)
    public event Action<bool> OnInventoryToggled;

   private void Awake()
    {
        // ARQUITETURA BLINDADA: Injeção de Dependência Reversa
        // Garante que o GameManager imortal sempre aponte para o inventário vivo da cena atual
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inventoryManager = this;
        }

        ConfigureInventory();
        if(inventoryUI != null) inventoryUI.SetActive(false);
    }

    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            bool vaiAbrir = !inventoryUI.activeSelf;
            inventoryUI.SetActive(vaiAbrir);

            // Dispara o evento avisando a Prensa se o inventário abriu(true) ou fechou(false)
            OnInventoryToggled?.Invoke(vaiAbrir);

            Time.timeScale = vaiAbrir ? 0f : 1f;

            // Mesma lógica já usada no diálogo e no menu de pause: desliga o raycast
            // dos controles mobile enquanto o inventário/prensa está aberto, para que
            // os slots e o botão de combinar recebam o toque em vez de serem
            // interceptados pelo joystick/botões de fundo.
            if (MobileControlsManager.Instance != null)
                MobileControlsManager.Instance.SetControlsInteractable(!vaiAbrir);
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

    // NOVO MÉTODO: Retorna verdadeiro se a pista/item já estiver no inventário
    public bool HasItem(string searchItemID)
    {
        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot.item != null && slot.item.itemID == searchItemID)
            {
                return true;
            }
        }
        return false;
    }


   private void Start()
    {
        // Limpa qualquer lixo que tenha ficado no Prefab no Unity Editor
        LimparInventarioVisual();

        // Em seguida, puxa os itens reais que vieram da cena anterior
        RestaurarInventario();
    }

    // ARQUITETURA BLINDADA: Esvazia todos os slots antes de injetar os dados salvos
    public void LimparInventarioVisual()
    {
        if (inventoryGrid == null) return;

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null)
            {
                ClearItemSlot(slot);
            }
        }
    }

    // ARQUITETURA: Salva o estado físico da UI nos dados imortais do GameManager
    public void SalvarEstadoAtual()
    {
        if (GameManager.Instance == null) return;

        List<Item> itensParaSalvar = new List<Item>();

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot.item != null && slot.item.itemAmt > 0)
            {
                // Cria um clone limpo para não referenciar um item de uma UI que será destruída
                Item clone = slot.item.Clone();
                itensParaSalvar.Add(clone);
            }
        }

        GameManager.Instance.inventarioSalvo = itensParaSalvar;
        Debug.Log($"[SISTEMA] Inventário Salvo: {itensParaSalvar.Count} itens.");
    }

    // ARQUITETURA: Restaura os itens salvos nos slots vazios da nova cena
   public void RestaurarInventario()
    {
        if (GameManager.Instance == null || GameManager.Instance.inventarioSalvo == null) return;

        foreach (Item itemSalvo in GameManager.Instance.inventarioSalvo)
        {
            // Proteção contra o NullReferenceException: Ignora itens vazios no cofre
            if (itemSalvo == null) continue;

            Item clone = itemSalvo.Clone();
            AddItem(clone);
        }
        Debug.Log("[SISTEMA] Inventário Restaurado com segurança.");
    }
}