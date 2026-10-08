using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Qual versão do panfleto sai da prensa, conforme a confiabilidade das duas pistas usadas.</summary>
public enum NivelDoPanfleto
{
    Fatos,    // Fato + Fato
    ComBoato, // Fato + Boato
    Calunia,  // Boato + Boato, ou qualquer pista Calúnia
    Alegacoes // alguma alegação inicial do cliente, não verificada (CaseData.alegacoesIniciais)
}

/// <summary>
/// Mecânica "Fato x Boato" (documento, Fase 2: "risco de coletar informações erradas e prejudicar a receita
/// do panfleto"). Diferente de Recipe (par exato de itens), aqui QUALQUER par de pistas do caso vira panfleto:
/// a prensa nunca recusa, mas a versão muda. Boatos rendem mais na hora e cobram depois — quando a mentira é
/// exposta no fim da fase (FimDeFase / GameManager.EncerrarFase). Ligada ao caso pelo campo CaseData.receitaDoPanfleto.
/// Os valores finais são compostos pela CalculadoraDePanfleto: versão → apoio → linha editorial.
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

    /// <summary>
    /// Regra explícita de reforço por documento de apoio (Prompt 3): se o jogador selecionar na prensa um dos
    /// "Suportes Aceitos", com qualidade mínima, os valores abaixo são SOMADOS à versão impressa. Cada qualificador
    /// vale uma vez por publicação, e cada documento serve a um qualificador só (sem empilhar o mesmo bônus).
    /// Não é multiplicador universal: cada receita define os seus.
    /// </summary>
    [Serializable]
    public class Qualificador
    {
        [Tooltip("ID estável (vai para o histórico da publicação). Não troque depois que houver saves.")]
        public string id;
        [TextArea(1, 3)] public string descricao;
        [Tooltip("Qualquer um destes documentos (comparados pelo itemID) ativa o reforço.")]
        public List<Item> suportesAceitos = new List<Item>();
        public QualidadeDaEvidencia qualidadeMinima = QualidadeDaEvidencia.Superior;
        [Tooltip("Versões em que o reforço vale. Vazio = todas. Restringir a Fatos torna o reforço um indício da " +
                 "verdade: a prévia da prensa nunca mostra se ele foi aplicado.")]
        public List<NivelDoPanfleto> niveis = new List<NivelDoPanfleto>();

        [Header("Somado à versão impressa")]
        public int povo;
        public int estado;
        public int ouro;

        public bool Aceita(Item suporte) =>
            suporte != null && suporte.EhSuporte && suporte.qualidade >= qualidadeMinima &&
            suportesAceitos != null && suportesAceitos.Exists(s => s != null && s.itemID == suporte.itemID);

        public bool ValeNo(NivelDoPanfleto nivel) => niveis == null || niveis.Count == 0 || niveis.Contains(nivel);
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
    [Tooltip("Qualquer alegação inicial do cliente entre as duas pistas (sem Calúnia): saída para quem ficou sem tempo. " +
             "Ganho menor que o de Fato + Fato e uma consequência própria.")]
    public Versao soAlegacoes = new Versao();

    [Serializable]
    public class ModificadorEditorial
    {
        public int povo;
        public int estado;
        public int ouro;

        public bool Zerado => povo == 0 && estado == 0 && ouro == 0;
    }

    /// <summary>
    /// Linha editorial (Prompt 5): somada DEPOIS da versão e do apoio, igual em todas as versões — assim a escolha
    /// nunca depende nem revela a verdade das pistas. Desligada = publicação neutra (compatibilidade com receitas
    /// antigas); ligada = a prensa exige uma das três opções antes de publicar.
    /// </summary>
    [Serializable]
    public class ConfiguracaoEditorial
    {
        [Tooltip("Liga a escolha obrigatória da linha editorial neste caso (Fase 2 em diante). Desligada, o panfleto sai " +
                 "neutro, sem modificador — só para dados antigos; o validador da campanha acusa casos novos sem ela.")]
        public bool ativa;

        [Tooltip("Defesa do povo: Povo positivo e Estado negativo.")]
        public ModificadorEditorial defesaDoPovo = new ModificadorEditorial();

        [Tooltip("Agradar a Coroa / o Comitê: Estado positivo e Povo negativo.")]
        public ModificadorEditorial agradarOPoder = new ModificadorEditorial();
        [Tooltip("Rótulo da opção neste caso. Vazio = padrão da fase (\"Agradar a Coroa\" até a Fase 3, \"Agradar o Comitê\" na 4).")]
        public string rotuloAgradarOPoder;

        [Tooltip("Sensacionalista: ouro adicional agora.")]
        public ModificadorEditorial sensacionalista = new ModificadorEditorial();
        [Tooltip("Sensacionalista: quanto a PERDA da revelação cresce, em %, quando a versão impressa tem boato, calúnia ou " +
                 "alegação não verificada. Determinístico (sem sorteio). Na versão Fatos nada é cobrado.")]
        [Min(0)] public int agravamentoPercentual;

        public ModificadorEditorial De(LinhaEditorial linha)
        {
            switch (linha)
            {
                case LinhaEditorial.DefesaDoPovo: return defesaDoPovo;
                case LinhaEditorial.AgradarOPoder: return agradarOPoder;
                case LinhaEditorial.Sensacionalista: return sensacionalista;
                default: return null;
            }
        }

        /// <summary>Nunca foi preenchida (asset anterior ao Prompt 5): a ferramenta de setup só configura estas.</summary>
        public bool Vazia =>
            !ativa && (defesaDoPovo == null || defesaDoPovo.Zerado) && (agradarOPoder == null || agradarOPoder.Zerado) &&
            (sensacionalista == null || sensacionalista.Zerado) && agravamentoPercentual == 0 && string.IsNullOrWhiteSpace(rotuloAgradarOPoder);
    }

    [Header("Reforço por documentos de apoio (biblioteca / pistas complementares)")]
    public List<Qualificador> qualificadores = new List<Qualificador>();

    [Header("Linha editorial (Fase 2 em diante)")]
    public ConfiguracaoEditorial linhaEditorial = new ConfiguracaoEditorial();

    /// <summary>A prensa só publica este caso com uma das três linhas editoriais escolhida pelo jogador.</summary>
    public bool ExigeLinhaEditorial => linhaEditorial != null && linhaEditorial.ativa;

    /// <summary>Verdadeiro se algum qualificador desta receita aceita o documento (informação conhecida: não
    /// depende da confiabilidade das pistas).</summary>
    public bool AceitaComoSuporte(Item suporte) =>
        suporte != null && suporte.caso == caso && qualificadores != null && qualificadores.Exists(q => q != null && q.Aceita(suporte));

    /// <summary>
    /// Versão do panfleto pela verdade REAL das duas pistas:
    ///   qualquer Calúnia → Calúnia;
    ///   qualquer alegação do cliente (não verificada) → Só Alegações (ganho menor, consequência própria) —
    ///   assim "1 pista + 1 alegação" nunca rende mais que a investigação completa;
    ///   senão, pelo número de boatos (0 = Fatos, 1 = Com Boato, 2 = Calúnia).
    /// </summary>
    public static NivelDoPanfleto Classificar(Item a, Item b)
    {
        if (a.confiabilidade == Confiabilidade.Calunia || b.confiabilidade == Confiabilidade.Calunia)
            return NivelDoPanfleto.Calunia;

        if (EhAlegacao(a) || EhAlegacao(b))
            return NivelDoPanfleto.Alegacoes;

        int boatos = (a.confiabilidade == Confiabilidade.Boato ? 1 : 0) + (b.confiabilidade == Confiabilidade.Boato ? 1 : 0);
        if (boatos == 0) return NivelDoPanfleto.Fatos;
        return boatos == 1 ? NivelDoPanfleto.ComBoato : NivelDoPanfleto.Calunia;
    }

    private static bool EhAlegacao(Item item) => item.caso != null && item.caso.EhAlegacao(item);

    public Versao VersaoDe(NivelDoPanfleto nivel)
    {
        switch (nivel)
        {
            case NivelDoPanfleto.Fatos: return soFatos;
            case NivelDoPanfleto.ComBoato: return comBoato;
            case NivelDoPanfleto.Alegacoes: return soAlegacoes;
            default: return calunia;
        }
    }

    public Item PanfletoDe(Versao versao) => versao != null && versao.panfleto != null ? versao.panfleto : panfletoPadrao;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (caso == null)
            Debug.LogWarning($"[ReceitaDeCaso] '{name}' está sem caso.", this);
        if (PanfletoDe(soFatos) == null || PanfletoDe(comBoato) == null || PanfletoDe(calunia) == null || PanfletoDe(soAlegacoes) == null)
            Debug.LogWarning($"[ReceitaDeCaso] '{name}': defina o Panfleto Padrão ou o panfleto de cada versão.", this);
    }
#endif
}
