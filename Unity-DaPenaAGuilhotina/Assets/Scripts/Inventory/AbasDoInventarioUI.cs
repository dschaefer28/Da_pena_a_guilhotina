using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Abas "Pistas" (pistas, apoio, cartas) e "Panfletos". Só pinta a aba ativa e a contagem; o filtro fica no
/// InventoryManager, buscado em runtime por viver fora do UI_Inventory.prefab.
/// </summary>
public class AbasDoInventarioUI : MonoBehaviour
{
    [Header("Aba Pistas")]
    [SerializeField] private Image fundoPistas;
    [SerializeField] private TMP_Text rotuloPistas;
    [SerializeField] private string nomePistas = "Pistas";

    [Header("Aba Panfletos")]
    [SerializeField] private Image fundoPanfletos;
    [SerializeField] private TMP_Text rotuloPanfletos;
    [SerializeField] private string nomePanfletos = "Panfletos";

    [Header("Visual")]
    [SerializeField] private Color corAbaAtiva = Color.white;
    [SerializeField] private Color corAbaInativa = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color corTextoInativo = new Color(1f, 1f, 1f, 0.7f);

    private InventoryManager inventario;

    private void OnEnable()
    {
        inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventario == null) inventario = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
        if (inventario != null)
        {
            inventario.OnAbaAlterada += AoMudarAba;
            inventario.OnSlotAlterado += AoMudarSlot;
        }
        Atualizar();
    }

    private void OnDisable()
    {
        if (inventario == null) return;
        inventario.OnAbaAlterada -= AoMudarAba;
        inventario.OnSlotAlterado -= AoMudarSlot;
        inventario = null;
    }

    public void MostrarPistas() => Mostrar(AbaDoInventario.Pistas);
    public void MostrarPanfletos() => Mostrar(AbaDoInventario.Panfletos);

    private void Mostrar(AbaDoInventario aba)
    {
        if (inventario != null) inventario.MostrarAba(aba);
    }

    private void AoMudarAba(AbaDoInventario _) => Atualizar();
    private void AoMudarSlot(UISlotHandler _) => Atualizar();

    private void Atualizar()
    {
        AbaDoInventario atual = inventario != null ? inventario.AbaAtual : AbaDoInventario.Pistas;
        Pintar(fundoPistas, rotuloPistas, nomePistas, AbaDoInventario.Pistas, atual);
        Pintar(fundoPanfletos, rotuloPanfletos, nomePanfletos, AbaDoInventario.Panfletos, atual);
    }

    private void Pintar(Image fundo, TMP_Text rotulo, string nome, AbaDoInventario aba, AbaDoInventario atual)
    {
        bool ativa = aba == atual;
        if (fundo != null) fundo.color = ativa ? corAbaAtiva : corAbaInativa;
        if (rotulo == null) return;
        int quantidade = inventario != null ? inventario.QuantidadeNaAba(aba) : 0;
        rotulo.text = quantidade > 0 ? $"{nome} <size=80%>({quantidade})</size>" : nome;
        rotulo.color = ativa ? Color.white : corTextoInativo;
    }
}
