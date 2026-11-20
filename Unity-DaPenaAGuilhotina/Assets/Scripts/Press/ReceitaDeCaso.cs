using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Qual versão do panfleto sai da prensa, conforme a confiabilidade das duas pistas usadas.</summary>
public enum NivelDoPanfleto
{
    Fatos,    // Fato + Fato
    ComBoato, // Fato + Boato
    Calunia,  // Boato + Boato, ou qualquer pista Calúnia
    Alegacoes // legado: saves antigos em que cartas do cliente iam para a prensa
}

/// <summary>
/// Mecânica "Fato x Boato": ao contrário de Recipe (par exato), qualquer par de pistas do caso vira panfleto e só a
/// versão muda. Boatos rendem mais na hora e cobram quando a mentira é exposta no fim da fase (GameManager.EncerrarFase).
/// Valores finais compostos pela CalculadoraDePanfleto.
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
    /// Reforço por documento de apoio: um dos suportes aceitos, com qualidade mínima, soma os valores à versão.
    /// Cada qualificador vale uma vez por publicação e cada documento serve a um qualificador só.
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
    [Tooltip("Legado: versão das impressões com carta do cliente, de antes de as cartas virarem só leitura. A prensa não " +
             "a gera mais; fica para os panfletos já publicados em saves antigos (tribunal e histórico).")]
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
    /// Somada depois da versão e do apoio, igual em todas as versões, para nunca depender nem revelar a verdade das
    /// pistas. Desligada = publicação sem linha (receitas antigas).
    /// </summary>
    [Serializable]
    public class ConfiguracaoEditorial
    {
        [Tooltip("Liga a escolha obrigatória da linha editorial neste caso (Fase 2 em diante). Desligada, o panfleto sai " +
                 "sem linha, sem modificador — só para dados antigos; o validador da campanha acusa casos novos sem ela.")]
        public bool ativa;

        [Tooltip("Sensacionalista, somado ao efeito do Neutro: ouro positivo; o lado que o caso favorece negativo (ganha " +
                 "menos); o outro lado zero ou negativo (perde o mesmo ou mais). Ferramentas > Campanha > 4b preenche pelo lado do caso.")]
        public ModificadorEditorial sensacionalista = new ModificadorEditorial();
        [Tooltip("Sensacionalista: quanto a PERDA da revelação cresce, em %, quando a versão impressa tem boato ou calúnia. " +
                 "Determinístico (sem sorteio). Na versão Fatos nada é cobrado. 0 = desligado (padrão desde 08/11/2026).")]
        [Min(0)] public int agravamentoPercentual;

        /// <summary>Neutro = zero; Sensacionalista = o configurado; legadas ou nenhuma = nulo.</summary>
        public ModificadorEditorial De(LinhaEditorial linha)
        {
            switch (linha)
            {
                case LinhaEditorial.Neutro: return new ModificadorEditorial();
                case LinhaEditorial.Sensacionalista: return sensacionalista;
                default: return null;
            }
        }

        /// <summary>Nunca preenchida: a ferramenta de setup só configura estas.</summary>
        public bool Vazia => !ativa && (sensacionalista == null || sensacionalista.Zerado) && agravamentoPercentual == 0;
    }

    [Header("Reforço por documentos de apoio (biblioteca / pistas complementares)")]
    public List<Qualificador> qualificadores = new List<Qualificador>();

    [Header("Linha editorial (Fase 2 em diante)")]
    public ConfiguracaoEditorial linhaEditorial = new ConfiguracaoEditorial();

    public bool ExigeLinhaEditorial => linhaEditorial != null && linhaEditorial.ativa;

    /// <summary>Não depende da confiabilidade das pistas (seguro para a prévia).</summary>
    public bool AceitaComoSuporte(Item suporte) =>
        suporte != null && suporte.caso == caso && qualificadores != null && qualificadores.Exists(q => q != null && q.Aceita(suporte));

    /// <summary>
    /// Pela verdade real das pistas: qualquer Calúnia → Calúnia; senão pelo número de boatos (0 Fatos, 1 ComBoato, 2 Calúnia).
    /// </summary>
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
