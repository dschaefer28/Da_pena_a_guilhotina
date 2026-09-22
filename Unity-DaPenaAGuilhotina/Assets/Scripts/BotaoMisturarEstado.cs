using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Habilita o botão "Misturar" da prensa só quando as duas entradas têm item. Em vez de checar todo
/// frame, escuta o evento OnSlotAlterado do InventoryManager (único lugar onde um slot muda de item).
/// Vive no prefab UI_Inventory, então resolve o manager em runtime.
/// </summary>
[RequireComponent(typeof(Button))]
public class BotaoMisturarEstado : MonoBehaviour
{
    [Range(0f, 1f)] public float alphaDesabilitado = 0.55f;

    private Button botao;
    private CanvasGroup grupo;
    private CraftingPress prensa;
    private InventoryManager inventario;

    private void Awake()
    {
        botao = GetComponent<Button>();
        grupo = GetComponent<CanvasGroup>();
        prensa = GetComponentInParent<CraftingPress>();
    }

    private void OnEnable()
    {
        inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventario == null) inventario = FindAnyObjectByType<InventoryManager>();
        if (inventario != null) inventario.OnSlotAlterado += HandleSlotAlterado;
        Atualizar();
    }

    private void OnDisable()
    {
        if (inventario != null) inventario.OnSlotAlterado -= HandleSlotAlterado;
    }

    private void HandleSlotAlterado(UISlotHandler slot)
    {
        if (prensa == null || slot == prensa.slotInput1 || slot == prensa.slotInput2) Atualizar();
    }

    private void Atualizar()
    {
        bool pronto = prensa != null && prensa.slotInput1 != null && prensa.slotInput2 != null
                      && prensa.slotInput1.item != null && prensa.slotInput2.item != null;
        botao.interactable = pronto;
        if (grupo != null) grupo.alpha = pronto ? 1f : alphaDesabilitado;
    }
}
