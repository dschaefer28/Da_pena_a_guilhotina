using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Mecânica "Fato x Boato" (Fase 2 em diante): o quanto uma pista é verdadeira. O jogador não vê
/// isso direto — só descobre quando junta um item de "Verificada Por" (ver InventoryManager.PistaVerificada).</summary>
public enum Confiabilidade
{
    NaoEPista, // itens do tutorial, panfletos, etc.: a prensa usa só as receitas exatas (Recipe)
    Fato,
    Boato,
    Calunia
}

/// <summary>Qualidade da evidência (Prompt 3). INDEPENDENTE da Confiabilidade: um documento caro pode ser boato, e o
/// preço nunca muda a verdade. Só entra no cálculo por regras explícitas da receita (ReceitaDeCaso.qualificadores).</summary>
public enum QualidadeDaEvidencia
{
    Comum,
    Superior
}

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Items")]
public class Item : ScriptableObject
{
    public string itemID;
    public Sprite itemImg;
    [Min(0)] public int itemAmt;

    [Header("Popup de Coleta (opcional)")]
    [Tooltip("Nome mostrado ao jogador no popup de 'item recebido'. Se deixar vazio, usa o nome do arquivo do ScriptableObject.")]
    public string itemName;

    [Header("Ficha da Pista (aparece ao passar o mouse no inventário)")]
    [TextArea(3, 6)]
    [Tooltip("O que a pista diz. É o que o jogador lê para decidir o que imprimir.")]
    public string descricao;
    [Tooltip("Quem contou ou onde foi achada (ex: 'Criado de Réveillon', 'Livro-caixa da fábrica'). " +
             "É a principal dica de confiabilidade para o jogador.")]
    public string fonte;

    [Header("Fato x Boato (pistas de caso)")]
    [Tooltip("Caso ao qual a pista pertence. A prensa só junta pistas do mesmo caso.")]
    public CaseData caso;
    [Tooltip("Verdade escondida da pista. Define qual versão do panfleto sai na prensa (ver ReceitaDeCaso).")]
    public Confiabilidade confiabilidade = Confiabilidade.NaoEPista;
    [Tooltip("Itens que confirmam ou desmentem esta pista. Se o jogador tiver QUALQUER um deles no inventário, " +
             "a verdade é revelada na ficha (\"confirmada\" / \"boato\" / \"calúnia\").")]
    public List<Item> verificadaPor = new List<Item>();

    [Header("Documento de apoio (biblioteca, Prompt 3)")]
    [Tooltip("Documento complementar do caso: não vai nos dois slots da prensa, e sim no campo Suporte, e não é gasto " +
             "na impressão. Só reforça o panfleto se a receita do caso tiver um qualificador que o aceite.")]
    public bool documentoDeSuporte;
    [Tooltip("Qualidade da evidência. Não altera a confiabilidade.")]
    public QualidadeDaEvidencia qualidade = QualidadeDaEvidencia.Comum;

    /// <summary>Nome pronto para exibição em UI (usa itemName se configurado, senão cai no nome do asset).</summary>
    public string NomeExibicao => string.IsNullOrWhiteSpace(itemName) ? name : itemName;

    /// <summary>Documento de apoio de um caso (vai no campo Suporte da prensa).</summary>
    public bool EhSuporte => documentoDeSuporte && caso != null;

    /// <summary>Etiqueta do documento de apoio (informação conhecida: qualidade, não verdade).</summary>
    public string RotuloDeApoio =>
        !EhSuporte ? string.Empty
        : qualidade == QualidadeDaEvidencia.Superior ? "<color=#C9A94E>apoio · qualidade superior</color>" : "<color=#9E9E9E>apoio</color>";

    /// <summary>Verdadeiro para pistas que entram na regra Fato x Boato da prensa.</summary>
    public bool EhPista => caso != null && confiabilidade != Confiabilidade.NaoEPista;

    /// <summary>Etiqueta colorida (rich text) da situação da pista para o inventário e a ficha.</summary>
    public string RotuloSituacao(bool verificada)
    {
        if (!EhPista) return string.Empty;
        if (!verificada) return "<color=#9E9E9E>não verificada</color>";
        switch (confiabilidade)
        {
            case Confiabilidade.Fato: return "<color=#4CAF50>confirmada</color>";
            case Confiabilidade.Boato: return "<color=#E0A030>boato</color>";
            default: return "<color=#E53935>calúnia</color>";
        }
    }

    /// <summary>Verdadeiro se os dois itens representam o mesmo tipo (mesmo itemID não-vazio).</summary>
    public bool Matches(Item other)
    {
        return other != null
            && !string.IsNullOrEmpty(itemID)
            && itemID == other.itemID;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemID))
            Debug.LogWarning($"[Item] '{name}' está sem itemID. O empilhamento e as buscas de pista podem falhar.", this);

        if (itemAmt < 0) itemAmt = 0;

        if (confiabilidade != Confiabilidade.NaoEPista && caso == null)
            Debug.LogWarning($"[Item] '{name}' tem confiabilidade {confiabilidade} mas nenhum caso: a prensa vai tratá-lo como item comum.", this);
        if (caso != null && confiabilidade == Confiabilidade.NaoEPista && !documentoDeSuporte)
            Debug.LogWarning($"[Item] '{name}' tem caso mas está como NaoEPista: escolha Fato, Boato ou Calunia (ou marque Documento De Suporte).", this);
        if (documentoDeSuporte && confiabilidade != Confiabilidade.NaoEPista)
            Debug.LogWarning($"[Item] '{name}' é documento de suporte: deixe a confiabilidade em NaoEPista (ele não é uma das duas pistas).", this);
    }
#endif
}

public static class ScriptableObjectExtension
{
    /// <summary>
    /// Creates and returns a clone of any given scriptable object.
    /// </summary>
    public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
    {
        if (scriptableObject == null)
        {
            Debug.LogError($"ScriptableObject was null. Returning default {typeof(T)} object.");
            return (T)ScriptableObject.CreateInstance(typeof(T));
        }

        T instance = UnityEngine.Object.Instantiate(scriptableObject);
        instance.name = scriptableObject.name; // remove (Clone) from name
        instance.hideFlags = HideFlags.DontSave; // clone é só de runtime, nunca deve ser serializado
        return instance;
    }
}