using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using FMODUnity;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryGrid;
    public bool messyInventory;
    public GameObject inventoryUI;

    [Header("Áudio (FMOD)")]
    public EventReference somToggleInventario;

    // NOVO: Evento Observer para avisar outras UIs (como a Prensa)
    public event Action<bool> OnInventoryToggled; 
    private bool ownsPauseRequest;

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

            if (!vaiAbrir)
                foreach (var press in FindObjectsByType<CraftingPress>(FindObjectsSortMode.None))
                    if (!press.ReturnItemsToInventory()) return;

            if (!vaiAbrir && MouseManager.instance != null && !MouseManager.instance.ReturnHeldItemToInventory(this))
                return;

            inventoryUI.SetActive(vaiAbrir);

            if (!vaiAbrir) GameFeedback.PlaySound("event:/portafechar", 0.3f);
            else if (!somToggleInventario.IsNull)
                RuntimeManager.PlayOneShot(somToggleInventario);

            // Dispara o evento avisando a Prensa se o inventário abriu(true) ou fechou(false)
            OnInventoryToggled?.Invoke(vaiAbrir);

            SetPauseRequest(vaiAbrir);
        }
    }

    private void SetPauseRequest(bool shouldPause)
    {
        if (ownsPauseRequest == shouldPause) return;
        ownsPauseRequest = shouldPause;
        PauseManager.RequestPause(shouldPause);
    }

    private void OnDestroy()
    {
        if (ownsPauseRequest)
            SetPauseRequest(false);
    }
    public void PlaceInInventory(UISlotHandler activeSlot, Item item)
    {
        if (activeSlot == null || item == null) return;
        if (activeSlot.slotImg == null || activeSlot.itemCount == null) return;

        activeSlot.item = item;
        activeSlot.slotImg.sprite = item.itemImg;
        activeSlot.itemCount.text = item.itemAmt.ToString();
        activeSlot.slotImg.gameObject.SetActive(true);
        ConfigureInventory();
    }

    public void StackInInventory(UISlotHandler activeSlot, Item item)
    {
        if (activeSlot == null || activeSlot.item == null || item == null) { return; }
        if(activeSlot.item.itemID != item.itemID) { return; }

        activeSlot.item.itemAmt += item.itemAmt;
        if (activeSlot.itemCount != null)
            activeSlot.itemCount.text = activeSlot.item.itemAmt.ToString();
        ConfigureInventory();
    }

    public void ClearItemSlot(UISlotHandler activeSlot)
    {
        if (activeSlot == null) return;

        if (activeSlot.slotImg != null)
        {
            activeSlot.slotImg.sprite = null;
            activeSlot.slotImg.gameObject.SetActive(false);
        }
        if (activeSlot.itemCount != null)
            activeSlot.itemCount.text = string.Empty;
        activeSlot.item = null;
    }

    public void ConfigureInventory()
    {
        if (messyInventory) { return; }
        if (inventoryGrid == null) { Debug.LogError("[InventoryManager] inventoryGrid não atribuído.", this); return; }

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

            bool hasItemA = itemA != null && itemA.item != null;
            bool hasItemB = itemB != null && itemB.item != null;

            return hasItemB.CompareTo(hasItemA);
        });

        for(int i = 0; i < uiSlots.Count; i++)
        {
            uiSlots[i].SetSiblingIndex(i);
        }
    }

    public bool AddItem(Item itemToAdd) => AddItem(itemToAdd, true);

    public bool AddItem(Item itemToAdd, bool notify)
    {
        if (itemToAdd == null) { Debug.LogWarning("[InventoryManager] AddItem recebeu item nulo."); return false; }
        if (itemToAdd.itemAmt <= 0 || string.IsNullOrWhiteSpace(itemToAdd.itemID)) return false;
        if (inventoryGrid == null) { Debug.LogError("[InventoryManager] inventoryGrid não atribuído.", this); return false; }

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemID == itemToAdd.itemID)
            {
                StackInInventory(slot, itemToAdd);
                if (notify) NotifyReceived(itemToAdd);
                return true;
            }
        }

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item == null)
            {
                Item newItem = itemToAdd.Clone();
                PlaceInInventory(slot, newItem);
                if (notify) NotifyReceived(itemToAdd);
                return true;
            }
        }


        Debug.Log("Inventário cheio! Não foi possível pegar o item.");
        if (notify) GameFeedback.Show("Inventário cheio. Libere um espaço e tente novamente.");
        return false;
    }

    private void NotifyReceived(Item item)
    {
        GameFeedback.Show($"Recebido: {item.DisplayName}\nTAB para abrir o inventário.");
        GameFeedback.PlaySound("event:/pegaritem");
    }

    // NOVO MÉTODO: Retorna verdadeiro se a pista/item já estiver no inventário
    public bool HasItem(string searchItemID)
    {
        if (string.IsNullOrEmpty(searchItemID) || inventoryGrid == null) return false;

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemAmt > 0 && slot.item.itemID == searchItemID)
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
        if (GameManager.Instance == null || inventoryGrid == null) return;

        List<Item> itensParaSalvar = new List<Item>();

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemAmt > 0)
            {
                // Cria um clone limpo para não referenciar um item de uma UI que será destruída
                Item clone = slot.item.Clone();
                itensParaSalvar.Add(clone);
            }
        }

        // Um item arrastado não está em nenhum slot. Incluí-lo aqui impede que desapareça
        // caso a cena mude antes de o jogador soltá-lo de volta no inventário.
        Item itemNaMao = MouseManager.instance != null ? MouseManager.instance.GetHeldItem : null;
        if (itemNaMao != null && itemNaMao.itemAmt > 0)
        {
            Item stackExistente = itensParaSalvar.Find(item => item != null && item.Matches(itemNaMao));
            if (stackExistente != null)
                stackExistente.itemAmt += itemNaMao.itemAmt;
            else
                itensParaSalvar.Add(itemNaMao.Clone());
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
            AddItem(clone, false);
        }
        Debug.Log("[SISTEMA] Inventário Restaurado com segurança.");
    }
}
