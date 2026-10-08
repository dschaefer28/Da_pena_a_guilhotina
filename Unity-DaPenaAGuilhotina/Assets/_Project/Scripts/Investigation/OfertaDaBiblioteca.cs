using UnityEngine;

/// <summary>
/// Um documento à venda na biblioteca (Prompt 3, a partir da Fase 3). Editável como asset; a lista de ofertas
/// fica no componente BibliotecaUI do prefab UI. A compra é registrada por oferta + caso (GameManager.comprasDaBiblioteca)
/// e entrega o "Item" uma única vez. O preço não diz nada sobre a verdade: documentos da biblioteca não são pistas
/// (não entram nos dois slots) e só reforçam o panfleto por regras explícitas da receita do caso.
/// </summary>
[CreateAssetMenu(fileName = "Oferta", menuName = "ScriptableObject/Oferta da Biblioteca")]
public class OfertaDaBiblioteca : ScriptableObject
{
    [Tooltip("ID estável: vai para o save. Não troque depois que houver saves.")]
    public string id;
    public string titulo;
    [TextArea(2, 5)] public string descricao;
    [Min(0)] public int preco = 20;
    [Tooltip("A biblioteca abre a partir desta fase.")]
    [Range(1, GameManager.UltimaFase)] public int faseMinima = 3;
    [Tooltip("Caso ao qual o documento serve. Só pode ser comprado enquanto este caso estiver em andamento.")]
    public CaseData caso;
    [Tooltip("Documento entregue (Item com Documento De Suporte marcado e o mesmo caso).")]
    public Item item;

    public string ChaveDeCompra => ChaveDe(this, caso);

    public static string ChaveDe(OfertaDaBiblioteca oferta, CaseData caso) =>
        (oferta != null ? oferta.id : string.Empty) + "|" + (caso != null ? caso.name : string.Empty);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) Debug.LogWarning($"[Oferta] '{name}' sem id.", this);
        if (caso == null || item == null) Debug.LogWarning($"[Oferta] '{name}' sem caso ou item.", this);
        else if (item.caso != caso || !item.documentoDeSuporte)
            Debug.LogWarning($"[Oferta] '{name}': o item precisa ser documento de suporte do mesmo caso.", this);
    }
#endif
}
