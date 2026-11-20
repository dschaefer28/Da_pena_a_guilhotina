using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using FMODUnity;

public enum AbaDoInventario
{
    Pistas,
    Panfletos
}

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryGrid;
    public bool messyInventory;
    public GameObject inventoryUI;

    [Header("Áudio (FMOD)")]
    public EventReference somToggleInventario;

    public event Action<bool> OnInventoryToggled;

    // Só itens novos de verdade (não dispara na restauração ao trocar de cena).
    public event Action<Item> OnItemAdicionado;

    public event Action<UISlotHandler> OnSlotAlterado;

    // A grade guarda tudo junto; a aba só decide quais linhas aparecem.
    public AbaDoInventario AbaAtual { get; private set; } = AbaDoInventario.Pistas;
    public event Action<AbaDoInventario> OnAbaAlterada;

    // Suprime o popup de "item recebido" ao repor itens que o jogador já tinha.
    private bool restaurandoInventario = false;

    // Para quem põe item num slot sem passar por AddItem (ex.: saída da prensa).
    public void NotificarItemRecebido(Item item)
    {
        if (item != null && !restaurandoInventario) OnItemAdicionado?.Invoke(item);
    }

   private void Awake()
    {
        // O GameManager persistente aponta sempre para o inventário da cena atual.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.inventoryManager = this;
        }

        ConfigureInventory();
        if(inventoryUI != null) inventoryUI.SetActive(false);

        // Não herdar timeScale 0 de um inventário aberto na cena anterior.
        Time.timeScale = 1f;
    }

    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            bool vaiAbrir = !inventoryUI.activeSelf;

            if (vaiAbrir && PauseMenu.Instance != null && PauseMenu.Instance.IsOpen)
            {
                Debug.Log("Não é possível abrir o inventário com o jogo pausado.");
                return;
            }

            // Aberto sobre um diálogo, os cliques de fechar caíam no texto e os dois dessincronizavam.
            var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : null;
            if (vaiAbrir && dialogo != null && dialogo.IsDialogueActive)
            {
                Debug.Log("Não é possível abrir o inventário durante um diálogo.");
                return;
            }
            if (vaiAbrir && JanelasModais.AlgumaAberta) return; // um modal por vez (biblioteca, quadro de dedução)

            // Sempre abre nas pistas; antes do SetActive para as abas já nascerem certas no OnEnable.
            if (vaiAbrir) DefinirAba(AbaDoInventario.Pistas);
            else DevolverItemDaMao();

            inventoryUI.SetActive(vaiAbrir);
            if (vaiAbrir) ConfigureInventory();

                AudioSeguro.TocarUmaVez(somToggleInventario);

            OnInventoryToggled?.Invoke(vaiAbrir);

            // O toque vai para os slots, não para o joystick/botões mobile.
            if (MobileControlsManager.Instance != null)
                MobileControlsManager.Instance.SetControlsInteractable(!vaiAbrir);

            Time.timeScale = vaiAbrir ? 0f : 1f;
        }
    }

    /// <summary>
    /// Devolve à grade o item "na mão" ao fechar: fora da grade o HasItem não o via e o tutorial travava em
    /// "volte até Dupaty" (no celular, ler a ficha já põe a pista na mão). Sem popup: não é item novo.
    /// </summary>
    private void DevolverItemDaMao()
    {
        MouseManager mao = MouseManager.instance;
        if (mao == null || mao.heldItem == null || mao.heldItem.itemAmt <= 0) return;

        restaurandoInventario = true;
        bool guardou = AddItem(mao.heldItem);
        restaurandoInventario = false;
        if (guardou) mao.heldItem = null; // grade cheia: continua na mão
    }

    public void PlaceInInventory(UISlotHandler activeSlot, Item item)
    {
        if (activeSlot == null || item == null) return;
        if (activeSlot.slotImg == null || activeSlot.itemCount == null) return;

        activeSlot.item = item;
        activeSlot.slotImg.sprite = item.itemImg;
        activeSlot.itemCount.text = item.itemAmt.ToString();
        activeSlot.slotImg.gameObject.SetActive(true);
        if (activeSlot.itemNameText != null) activeSlot.itemNameText.text = TextoDaLinha(item);
        activeSlot.AtualizarObjetoVazio();
        // Solto na aba errada (ex.: panfleto com Pistas aberta): troca para a aba do item, senão ele some da vista.
        if (!restaurandoInventario && EhLinhaDaLista(activeSlot) && inventoryUI != null && inventoryUI.activeSelf)
            DefinirAba(AbaDo(item));
        ConfigureInventory();
        OnSlotAlterado?.Invoke(activeSlot);
    }

    // ===== Abas (Pistas / Panfletos) =====

    public static AbaDoInventario AbaDo(Item item) =>
        item != null && item.EhPanfleto ? AbaDoInventario.Panfletos : AbaDoInventario.Pistas;

    public void MostrarAba(AbaDoInventario aba)
    {
        if (aba == AbaAtual) return;
        DefinirAba(aba);
        ConfigureInventory();
    }

    private void DefinirAba(AbaDoInventario aba)
    {
        if (aba == AbaAtual) return;
        AbaAtual = aba;
        OnAbaAlterada?.Invoke(aba);
    }

    public int QuantidadeNaAba(AbaDoInventario aba)
    {
        int quantidade = 0;
        foreach (Item item in ItensNaGrade())
            if (AbaDo(item) == aba) quantidade++;
        return quantidade;
    }

    private bool EhLinhaDaLista(UISlotHandler slot) =>
        slot != null && inventoryGrid != null && slot.transform.parent == inventoryGrid.transform;

    public void StackInInventory(UISlotHandler activeSlot, Item item)
    {
        if (activeSlot == null || activeSlot.item == null || item == null) { return; }
        if(activeSlot.item.itemID != item.itemID) { return; }

        activeSlot.item.itemAmt += item.itemAmt;
        if (activeSlot.itemCount != null)
            activeSlot.itemCount.text = activeSlot.item.itemAmt.ToString();
        if (activeSlot.itemNameText != null)
            activeSlot.itemNameText.text = TextoDaLinha(activeSlot.item);
        ConfigureInventory();
        OnSlotAlterado?.Invoke(activeSlot);
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
        if (activeSlot.itemNameText != null)
            activeSlot.itemNameText.text = string.Empty;
        activeSlot.item = null;
        activeSlot.AtualizarObjetoVazio();
        OnSlotAlterado?.Invoke(activeSlot);
    }

    public void ConfigureInventory()
    {
        if (inventoryGrid == null) { Debug.LogError("[InventoryManager] inventoryGrid não atribuído.", this); return; }

        List<Transform> uiSlots = new List<Transform>();
        for(int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            uiSlots.Add(inventoryGrid.transform.GetChild(i));
        }

        if (!messyInventory)
        {
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

        AtualizarLinhasVisiveis(uiSlots);
        ResetInventoryScroll();
    }

    // Só as linhas da aba atual, mais UMA vazia: o alvo para guardar o item que o jogador está segurando.
    private void AtualizarLinhasVisiveis(List<Transform> uiSlots)
    {
        bool vagaJaVisivel = false;
        foreach (Transform t in uiSlots)
        {
            UISlotHandler slot = t.GetComponent<UISlotHandler>();
            if (slot == null) continue;

            // Linhas do UI_Inventory.prefab não conseguem referenciar este manager (fora do prefab).
            if (slot.inventoryManager == null) slot.inventoryManager = this;

            bool visivel = slot.item != null ? AbaDo(slot.item) == AbaAtual : !vagaJaVisivel;
            if (slot.item == null && visivel) vagaJaVisivel = true;
            slot.DefinirVisivelNaLista(visivel);

            // Uma pista pode mudar de "não verificada" para "confirmada" quando OUTRO item chega.
            if (slot.item != null && slot.itemNameText != null) slot.itemNameText.text = TextoDaLinha(slot.item);
        }
    }

    // ===== Fato x Boato =====

    public string TextoDaLinha(Item item)
    {
        if (item == null) return string.Empty;
        if (item.EhSuporte) return $"{item.NomeExibicao} <size=75%>({item.RotuloDeApoio})</size>";
        if (item.EhLeitura) return $"{item.NomeExibicao} <size=75%>(<color=#9E9E9E>só leitura</color>)</size>";
        if (!item.EhPista) return item.NomeExibicao;
        return $"{item.NomeExibicao} <size=75%>({RotuloDaPista(item)})</size>";
    }

    /// <summary>Com dedução ativa mostra só a escolha do jogador no quadro, nunca a verdade; senão, regra do Verificada Por.</summary>
    public string RotuloDaPista(Item pista)
    {
        if (pista == null || !pista.EhPista) return string.Empty;
        if (Deducao.Aderente(pista.caso)) return Deducao.Rotulo(GameManager.Instance, pista);
        return pista.RotuloSituacao(PistaVerificada(pista));
    }

    /// <summary>Dedução ativa: nunca (a verdade não é revelada). Outros casos: tem ou já teve um item de "Verificada Por".</summary>
    public bool PistaVerificada(Item pista)
    {
        if (pista == null || !pista.EhPista) return false;
        var gm = GameManager.Instance;
        if (Deducao.Aderente(pista.caso)) return false;
        if (gm != null && gm.pistasVerificadas.Contains(pista.itemID)) return true;
        if (pista.verificadaPor == null) return false;
        foreach (Item prova in pista.verificadaPor)
            if (prova != null && HasItem(prova.itemID)) return true;
        return false;
    }

    private IEnumerable<Item> ItensNaGrade()
    {
        if (inventoryGrid == null) yield break;
        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null) yield return slot.item;
        }
    }

    private void RegistrarVerificacoes(bool avisar)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        foreach (Item item in ItensNaGrade())
        {
            // Dedução ativa: a verdade só vem do quadro.
            if (!item.EhPista || Deducao.Aderente(item.caso)) continue;
            if (gm.pistasVerificadas.Contains(item.itemID) || !PistaVerificada(item)) continue;

            gm.pistasVerificadas.Add(item.itemID);
            if (!avisar) continue;
            string resultado = item.confiabilidade == Confiabilidade.Fato ? "Pista confirmada" : "Boato desmentido";
            AvisoNaTela.Mostrar($"<size=75%><color=#FAD98C>{resultado}</color></size>\n{item.NomeExibicao}", item.itemImg);
        }
    }

    private void ResetInventoryScroll()
    {
        if (inventoryGrid == null || !inventoryGrid.activeInHierarchy) return;
        var content = inventoryGrid.transform as RectTransform;
        var scroll = inventoryGrid.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (content == null || scroll == null || scroll.content != content) return;

        // Rebuild antes de posicionar: o fitter pode mudar a altura do content.
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
    }

    public bool AddItem(Item itemToAdd)
    {
        if (itemToAdd == null) { Debug.LogWarning("[InventoryManager] AddItem recebeu item nulo."); return false; }
        if (inventoryGrid == null) { Debug.LogError("[InventoryManager] inventoryGrid não atribuído.", this); return false; }

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemID == itemToAdd.itemID)
            {
                StackInInventory(slot, itemToAdd);
                if (!restaurandoInventario) AoReceberItemNovo(itemToAdd);
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
                if (!restaurandoInventario) AoReceberItemNovo(itemToAdd);
                return true;
            }
        }


        Debug.Log("Inventário cheio! Não foi possível pegar o item.");
        return false;
    }

    private void AoReceberItemNovo(Item item)
    {
        if (GameManager.Instance != null) GameManager.Instance.RegistrarEvidencia(item);
        OnItemAdicionado?.Invoke(item);
        RegistrarVerificacoes(avisar: true);
    }

    public Item ItemNaGrade(string itemID)
    {
        foreach (Item item in ItensNaGrade())
            if (item.itemID == itemID) return item;
        return null;
    }

    public List<Item> ItensDaGrade() => new List<Item>(ItensNaGrade());

    public bool HasItem(string searchItemID)
    {
        if (string.IsNullOrEmpty(searchItemID) || inventoryGrid == null) return false;

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemID == searchItemID)
            {
                return true;
            }
        }
        return false;
    }


   private void Start()
    {
        // Descarta itens deixados no prefab antes de restaurar os da cena anterior.
        LimparInventarioVisual();

        RestaurarInventario();
    }

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

    public void SalvarEstadoAtual()
    {
        if (GameManager.Instance == null || inventoryGrid == null) return;

        List<Item> itensParaSalvar = new List<Item>();

        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && slot.item != null && slot.item.itemAmt > 0)
            {
#if UNITY_EDITOR
                // Itens do InventoryLayoutPreview não entram no save.
                if (slot.item.itemID != null && slot.item.itemID.StartsWith("__inventory_preview_", StringComparison.Ordinal)) continue;
#endif
                Item clone = slot.item.Clone(); // a UI de origem será destruída
                itensParaSalvar.Add(clone);
            }
        }

        // Também salva prensa e item na mão: perder o panfleto da prensa travava a Fase 1 (Marie, cutscene, porta).
        CraftingPress prensa = FindAnyObjectByType<CraftingPress>(FindObjectsInactive.Include);
        if (prensa != null)
            foreach (UISlotHandler slot in new[] { prensa.slotInput1, prensa.slotInput2, prensa.slotOutput })
                if (slot != null && slot.item != null && slot.item.itemAmt > 0) itensParaSalvar.Add(slot.item.Clone());
        if (MouseManager.instance != null && MouseManager.instance.heldItem != null && MouseManager.instance.heldItem.itemAmt > 0)
            itensParaSalvar.Add(MouseManager.instance.heldItem.Clone());
        // E o que não coube na grade: nunca some ao trocar de cena ou salvar.
        foreach (Item foraDaGrade in GameManager.Instance.itensForaDaGrade)
            if (foraDaGrade != null && foraDaGrade.itemAmt > 0) itensParaSalvar.Add(foraDaGrade.Clone());

        GameManager.Instance.inventarioSalvo = itensParaSalvar;
        Debug.Log($"[SISTEMA] Inventário Salvo: {itensParaSalvar.Count} itens.");
    }

   public void RestaurarInventario()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.inventarioSalvo == null) return;

        // Os itens fora da grade já estão no inventarioSalvo (SalvarEstadoAtual) e tentam entrar de novo.
        gm.itensForaDaGrade.Clear();
        restaurandoInventario = true;
        foreach (Item itemSalvo in gm.inventarioSalvo)
        {
            if (itemSalvo == null) continue;

            Item clone = itemSalvo.Clone();
            if (!AddItem(clone)) gm.itensForaDaGrade.Add(clone); // grade cheia: guardado, não perdido
        }
        restaurandoInventario = false;
        RegistrarVerificacoes(avisar: false); // sem repetir o popup de verificações de outra cena
        Debug.Log("[SISTEMA] Inventário Restaurado com segurança.");
        AvisarItensForaDaGrade();

        // Alegações iniciais que não couberam ao aceitar o caso (inventário cheio) entram assim que houver espaço.
        gm.EntregarAlegacoesPendentes();
    }

    private void AvisarItensForaDaGrade()
    {
        int quantos = GameManager.Instance != null ? GameManager.Instance.itensForaDaGrade.Count : 0;
        if (quantos == 0) return;
        Debug.LogWarning($"[SISTEMA] Inventário cheio: {quantos} item(ns) guardado(s) fora da grade até haver espaço.");
        AvisoNaTela.Mostrar(quantos == 1
            ? "Inventário cheio: um item ficou guardado na tipografia. Ele volta quando houver espaço."
            : $"Inventário cheio: {quantos} itens ficaram guardados na tipografia. Eles voltam quando houver espaço.");
    }

    /// <summary>Chamado quando a grade ganha espaço por arquivamento; sem popup, não é item novo.</summary>
    public void GuardarItensForaDaGrade()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.itensForaDaGrade.Count == 0) return;
        restaurandoInventario = true;
        gm.itensForaDaGrade.RemoveAll(item => item == null || AddItem(item));
        restaurandoInventario = false;
    }

    /// <summary>Remove da grade, das entradas da prensa e de fora da grade. Devolve quantos removeu.</summary>
    public int RemoverItens(Predicate<Item> condicao)
    {
        if (condicao == null || inventoryGrid == null) return 0;
        int removidos = 0;
        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot == null || slot.item == null || !condicao(slot.item)) continue;
            ClearItemSlot(slot);
            removidos++;
        }
        CraftingPress prensa = FindAnyObjectByType<CraftingPress>(FindObjectsInactive.Include);
        if (prensa != null)
            foreach (UISlotHandler slot in new[] { prensa.slotInput1, prensa.slotInput2 })
                if (slot != null && slot.item != null && condicao(slot.item)) { ClearItemSlot(slot); removidos++; }
        if (GameManager.Instance != null)
            removidos += GameManager.Instance.itensForaDaGrade.RemoveAll(item => item != null && condicao(item));
        if (removidos > 0)
        {
            ConfigureInventory();
            GuardarItensForaDaGrade();
        }
        return removidos;
    }

    /// <summary>Empilha num igual ou há slot vazio.</summary>
    public bool TemEspacoPara(Item item)
    {
        if (item == null || inventoryGrid == null) return false;
        for (int i = 0; i < inventoryGrid.transform.childCount; i++)
        {
            UISlotHandler slot = inventoryGrid.transform.GetChild(i).GetComponent<UISlotHandler>();
            if (slot != null && (slot.item == null || slot.item.itemID == item.itemID)) return true;
        }
        return false;
    }

    /// <summary>Grade, prensa, mão ou guardado fora da grade.</summary>
    public bool PossuiEmQualquerLugar(string itemID)
    {
        if (HasItem(itemID)) return true;
        CraftingPress prensa = FindAnyObjectByType<CraftingPress>(FindObjectsInactive.Include);
        if (prensa != null)
            foreach (UISlotHandler slot in new[] { prensa.slotInput1, prensa.slotInput2, prensa.slotOutput })
                if (slot != null && slot.item != null && slot.item.itemID == itemID) return true;
        if (GameManager.Instance != null && GameManager.Instance.itensForaDaGrade.Exists(i => i != null && i.itemID == itemID)) return true;
        return MouseManager.instance != null && MouseManager.instance.heldItem != null && MouseManager.instance.heldItem.itemID == itemID;
    }
}
