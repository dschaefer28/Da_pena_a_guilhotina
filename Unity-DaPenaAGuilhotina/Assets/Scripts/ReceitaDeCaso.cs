using System;
using UnityEngine;

/// <summary>Qual versão do panfleto sai da prensa, conforme a confiabilidade das duas pistas usadas.</summary>
public enum NivelDoPanfleto
{
    Fatos,    // Fato + Fato
    ComBoato, // Fato + Boato
    Calunia   // Boato + Boato, ou qualquer pista Calúnia
}

/// <summary>
/// Mecânica "Fato x Boato" (documento, Fase 2: "risco de coletar informações erradas e prejudicar a receita
/// do panfleto"). Diferente de Recipe (par exato de itens), aqui QUALQUER par de pistas do caso vira panfleto:
/// a prensa nunca recusa, mas a versão muda. Boatos rendem mais na hora e cobram depois — quando a mentira é
/// exposta na fase seguinte (RevelacaoDeBoatos). Ligada ao caso pelo campo CaseData.receitaDoPanfleto.
/// </summary>
[CreateAssetMenu(fileName = "ReceitaDeCaso", menuName = "ScriptableObjects/Receita de Caso (Fato x Boato)")]
public class ReceitaDeCaso : ScriptableObject
{
    [Serializable]
    public class Versao
    {
        [Tooltip("Panfleto gerado nesta versão. Vazio = usa o Panfleto Padrão da receita.")]
        public Item panfleto;

        [Header("Efeito imediato")]
        public int povo;
        public int estado;
        public int ouro;

        [Header("Revelação na fase seguinte (deixe zerado/vazio para versões honestas)")]
        [TextArea(2, 4)]
        [Tooltip("Legenda da cutscene quando a mentira é exposta.")]
        public string textoRevelacao;
        public int penalidadePovo;
        public int penalidadeEstado;

        public bool TemRevelacao =>
            penalidadePovo != 0 || penalidadeEstado != 0 || !string.IsNullOrWhiteSpace(textoRevelacao);
    }

    public CaseData caso;

    [Tooltip("Panfleto usado pelas versões que não definem um próprio.")]
    public Item panfletoPadrao;

    [Tooltip("Fato + Fato: a receita base do caso.")]
    public Versao soFatos = new Versao();
    [Tooltip("Fato + Boato: mais povo e ouro agora, penalidade quando o boato for desmentido.")]
    public Versao comBoato = new Versao();
    [Tooltip("Boato + Boato ou qualquer Calúnia: o maior ganho imediato e a maior penalidade depois.")]
    public Versao calunia = new Versao();

    public static NivelDoPanfleto Classificar(Item a, Item b)
    {
        if (a.confiabilidade == Confiabilidade.Calunia || b.confiabilidade == Confiabilidade.Calunia)
            return NivelDoPanfleto.Calunia;

        int boatos = (a.confiabilidade == Confiabilidade.Boato ? 1 : 0) + (b.confiabilidade == Confiabilidade.Boato ? 1 : 0);
        if (boatos == 0) return NivelDoPanfleto.Fatos;
        return boatos == 1 ? NivelDoPanfleto.ComBoato : NivelDoPanfleto.Calunia;
    }

    public Versao VersaoDe(NivelDoPanfleto nivel)
    {
        switch (nivel)
        {
            case NivelDoPanfleto.Fatos: return soFatos;
            case NivelDoPanfleto.ComBoato: return comBoato;
            default: return calunia;
        }
    }

    public Item PanfletoDe(Versao versao) => versao != null && versao.panfleto != null ? versao.panfleto : panfletoPadrao;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (caso == null)
            Debug.LogWarning($"[ReceitaDeCaso] '{name}' está sem caso.", this);
        if (PanfletoDe(soFatos) == null || PanfletoDe(comBoato) == null || PanfletoDe(calunia) == null)
            Debug.LogWarning($"[ReceitaDeCaso] '{name}': defina o Panfleto Padrão ou o panfleto de cada versão.", this);
    }
#endif
}
