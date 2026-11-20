using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Verdade escondida da pista (Fato x Boato). Com dedução ativa (Fases 2 a 4) só se descobre no Quadro de
/// pistas; nos outros casos, por um item de "Verificada Por".</summary>
public enum Confiabilidade
{
    NaoEPista, // itens do tutorial, panfletos, cartas do cliente etc.: a prensa usa só as receitas exatas (Recipe)
    Fato,
    Boato,
    Calunia
}

/// <summary>Peso da prova no julgamento (Fase 4) pelo que ela DIZ, nunca pela Confiabilidade. Só informativo na ficha:
/// não muda a barra nem o veredito.</summary>
public enum PesoNoJulgamento
{
    NaoDefinido, // a ficha do tribunal não mostra a linha "Pesa"
    AFavorDoAcusado,
    ContraOAcusado,
    DosDoisLados,
    Contexto     // explica a lei ou a situação, sem acusar nem defender
}

/// <summary>Independente da Confiabilidade: o preço nunca muda a verdade. Só pesa via ReceitaDeCaso.qualificadores.</summary>
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

    [Header("Arquivo de casos (menu principal > Casos resolvidos)")]
    [TextArea(2, 4)]
    [Tooltip("O que o panfleto afirma quando esta pista é impressa (uma frase). Aparece em \"O seu panfleto\". " +
             "Vazio = mostra só o nome da pista.")]
    public string afirmacaoNoPanfleto;

    [Header("Julgamento (Fase 4)")]
    [Tooltip("Para que lado a prova pesa no tribunal, pelo que ela diz (não pela verdade). Aparece na ficha ao passar o " +
             "mouse na prova; não muda a barra nem o veredito. Panfletos não precisam: o lado vem das pistas impressas.")]
    public PesoNoJulgamento pesoNoJulgamento = PesoNoJulgamento.NaoDefinido;

    [Header("Inventário")]
    [Tooltip("Panfleto impresso na prensa: fica na aba Panfletos do inventário, separado das pistas e documentos.")]
    public bool ehPanfleto;

    public bool EhPanfleto => ehPanfleto;

    public string NomeExibicao => string.IsNullOrWhiteSpace(itemName) ? name : itemName;

    public bool EhSuporte => documentoDeSuporte && caso != null;

    /// <summary>Mostra a qualidade, nunca a verdade.</summary>
    public string RotuloDeApoio =>
        !EhSuporte ? string.Empty
        : qualidade == QualidadeDaEvidencia.Superior ? "<color=#C9A94E>apoio · qualidade superior</color>" : "<color=#9E9E9E>apoio</color>";

    /// <summary>Carta do cliente (CaseData.alegacoesIniciais): só leitura, entra ao aceitar e sai ao concluir o caso.
    /// Nunca é pista, não vai para a prensa nem para o tribunal.</summary>
    public bool EhLeitura => caso != null && caso.EhAlegacao(this);

    /// <summary>Carta do cliente nunca é pista, mesmo com confiabilidade definida no asset.</summary>
    public bool EhPista => caso != null && confiabilidade != Confiabilidade.NaoEPista && !EhLeitura;

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

    public bool Matches(Item other)
    {
        return other != null
            && !string.IsNullOrEmpty(itemID)
            && itemID == other.itemID;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemAmt < 0) itemAmt = 0;

        // Só assets salvos: clones de runtime e itens de teste repassam por aqui ao dar Play.
        if (!UnityEditor.EditorUtility.IsPersistent(this)) return;

        if (string.IsNullOrWhiteSpace(itemID))
            Debug.LogWarning($"[Item] '{name}' está sem itemID. O empilhamento e as buscas de pista podem falhar.", this);

        if (confiabilidade != Confiabilidade.NaoEPista && caso == null)
            Debug.LogWarning($"[Item] '{name}' tem confiabilidade {confiabilidade} mas nenhum caso: a prensa vai tratá-lo como item comum.", this);
        if (caso != null && confiabilidade == Confiabilidade.NaoEPista && !documentoDeSuporte && !EhLeitura)
            Debug.LogWarning($"[Item] '{name}' tem caso mas está como NaoEPista: escolha Fato, Boato ou Calunia (ou marque Documento De Suporte).", this);
        if (EhLeitura && confiabilidade != Confiabilidade.NaoEPista)
            Debug.LogWarning($"[Item] '{name}' é carta do cliente (só leitura): deixe a confiabilidade em NaoEPista.", this);
        if (documentoDeSuporte && confiabilidade != Confiabilidade.NaoEPista)
            Debug.LogWarning($"[Item] '{name}' é documento de suporte: deixe a confiabilidade em NaoEPista (ele não é uma das duas pistas).", this);
    }
#endif
}

public static class ScriptableObjectExtension
{
    public static T Clone<T>(this T scriptableObject) where T : ScriptableObject
    {
        if (scriptableObject == null)
        {
            Debug.LogError($"ScriptableObject was null. Returning default {typeof(T)} object.");
            return (T)ScriptableObject.CreateInstance(typeof(T));
        }

        T instance = UnityEngine.Object.Instantiate(scriptableObject);
        instance.name = scriptableObject.name; // sem o sufixo "(Clone)"
        instance.hideFlags = HideFlags.DontSave; // só runtime, nunca serializado
        return instance;
    }
}