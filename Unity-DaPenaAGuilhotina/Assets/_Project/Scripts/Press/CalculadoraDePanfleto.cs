using System;
using System.Collections.Generic;

/// <summary>
/// Resultado de uma impressão de caso: o que a prensa entrega, o que é aplicado agora e o que fica para a
/// revelação. É uma cópia — calcular nunca altera ReceitaDeCaso, Versao ou Item (assets compartilhados).
/// </summary>
[Serializable]
public class ResultadoDoPanfleto
{
    public NivelDoPanfleto nivel;
    [NonSerialized] public Item panfleto;
    public int povo, estado, ouro;
    public int penalidadePovo, penalidadeEstado;
    public string textoRevelacao;
    public List<string> pistas = new List<string>();          // itemIDs das duas pistas impressas
    public List<string> suportes = new List<string>();        // itemIDs dos documentos de apoio que valeram
    public List<string> qualificadores = new List<string>();  // IDs dos qualificadores aplicados

    // Linha editorial (Prompt 5). Os valores acima já incluem estes: aqui fica só a parte de cada um, para o histórico.
    public LinhaEditorial linha;
    public int editorialPovo, editorialEstado, editorialOuro;   // modificador da linha somado a povo/estado/ouro
    public int agravamentoPovo, agravamentoEstado;              // parte da penalidade que veio do sensacionalista

    public bool TemRevelacao => penalidadePovo != 0 || penalidadeEstado != 0 || !string.IsNullOrWhiteSpace(textoRevelacao);
}

/// <summary>
/// Cálculo único do panfleto de caso, em etapas que podem ser compostas:
///   1. versão pela confiabilidade REAL das duas pistas (ReceitaDeCaso.Classificar);
///   2. qualificação por documentos de apoio válidos (ReceitaDeCaso.qualificadores);
///   3. linha editorial (ReceitaDeCaso.linhaEditorial): o mesmo modificador em qualquer versão e, no sensacionalista,
///      a perda da revelação agravada — só se a versão tiver revelação, nunca na versão Fatos;
///   4. o impacto global é o resultado; o clamp de Povo/Estado fica no ponto de aplicação (GameManager.AplicarImpactoPanfleto).
/// A linha editorial não muda a versão: tom, apoio e preço nunca transformam boato em fato.
/// </summary>
public static class CalculadoraDePanfleto
{
    /// <summary>Etapas 1 a 3. Nulo se faltar receita/pista, ou se a receita exige linha editorial e <paramref name="linha"/>
    /// não é uma das três opções (a prensa pergunta antes; publicar "sem tom" um caso novo é recusado). Receita sem linha
    /// editorial configurada: publicação neutra, a linha informada é ignorada.</summary>
    public static ResultadoDoPanfleto Calcular(ReceitaDeCaso receita, Item a, Item b, IEnumerable<Item> suportesSelecionados, LinhaEditorial linha)
    {
        if (receita == null || a == null || b == null) return null;
        if (receita.ExigeLinhaEditorial && !LinhasEditoriais.Escolhivel(linha)) return null;

        ResultadoDoPanfleto resultado = CalcularAntesDaLinha(receita, a, b, suportesSelecionados);
        if (receita.ExigeLinhaEditorial) AplicarLinhaEditorial(resultado, receita.linhaEditorial, linha);
        return resultado;
    }

    /// <summary>Etapas 1 e 2 (versão e apoio), sem a linha editorial. A prensa nunca imprime com isto; serve para
    /// conferir cada etapa isoladamente (ex.: o exemplo numérico do Prompt 3).</summary>
    public static ResultadoDoPanfleto CalcularAntesDaLinha(ReceitaDeCaso receita, Item a, Item b, IEnumerable<Item> suportesSelecionados)
    {
        if (receita == null || a == null || b == null) return null;

        // 1. Versão
        NivelDoPanfleto nivel = ReceitaDeCaso.Classificar(a, b);
        ReceitaDeCaso.Versao versao = receita.VersaoDe(nivel);
        var resultado = new ResultadoDoPanfleto
        {
            nivel = nivel,
            panfleto = receita.PanfletoDe(versao),
            povo = versao.povo,
            estado = versao.estado,
            ouro = versao.ouro,
            penalidadePovo = versao.penalidadePovo,
            penalidadeEstado = versao.penalidadeEstado,
            textoRevelacao = versao.textoRevelacao,
        };
        resultado.pistas.Add(a.itemID);
        resultado.pistas.Add(b.itemID);

        // 2. Qualificação: documentos do próprio caso, sem repetir; cada documento serve a um qualificador só e
        // cada qualificador vale uma vez. A atribuição é um emparelhamento máximo, independente da ordem da seleção.
        List<ReceitaDeCaso.Qualificador> ativos = QualificadoresValidos(receita, q => q.ValeNo(nivel));
        foreach (var par in Emparelhar(ativos, SuportesValidos(receita.caso, suportesSelecionados)))
        {
            ReceitaDeCaso.Qualificador q = par.Key;
            resultado.povo += q.povo;
            resultado.estado += q.estado;
            resultado.ouro += q.ouro;
            resultado.suportes.Add(par.Value.itemID);
            resultado.qualificadores.Add(q.id);
        }
        return resultado;
    }

    // 3. Linha editorial. O agravamento é da consequência que a versão JÁ tem (nada é inventado para Fatos) e só
    // aumenta as perdas; arredonda para longe de zero, sem sorteio.
    private static void AplicarLinhaEditorial(ResultadoDoPanfleto resultado, ReceitaDeCaso.ConfiguracaoEditorial config, LinhaEditorial linha)
    {
        resultado.linha = linha;
        ReceitaDeCaso.ModificadorEditorial mod = config.De(linha);
        if (mod != null)
        {
            resultado.editorialPovo = mod.povo;
            resultado.editorialEstado = mod.estado;
            resultado.editorialOuro = mod.ouro;
            resultado.povo += mod.povo;
            resultado.estado += mod.estado;
            resultado.ouro += mod.ouro;
        }

        if (linha != LinhaEditorial.Sensacionalista || resultado.nivel == NivelDoPanfleto.Fatos || !resultado.TemRevelacao) return;
        resultado.agravamentoPovo = Agravamento(resultado.penalidadePovo, config.agravamentoPercentual);
        resultado.agravamentoEstado = Agravamento(resultado.penalidadeEstado, config.agravamentoPercentual);
        resultado.penalidadePovo += resultado.agravamentoPovo;
        resultado.penalidadeEstado += resultado.agravamentoEstado;
    }

    /// <summary>Parte extra de uma penalidade agravada em <paramref name="percentual"/>%: só perdas (valores negativos) crescem.</summary>
    public static int Agravamento(int penalidade, int percentual) =>
        penalidade < 0 && percentual > 0 ? (int)Math.Round(penalidade * percentual / 100.0, MidpointRounding.AwayFromZero) : 0;

    /// <summary>Quantos reforços os documentos selecionados podem ativar, SEM olhar a versão (verdade das pistas):
    /// é o número que a prévia pode mostrar. Documentos equivalentes (mesmo qualificador) contam uma vez.</summary>
    public static int ReforcosPossiveis(ReceitaDeCaso receita, IEnumerable<Item> suportesSelecionados)
    {
        if (receita == null) return 0;
        return Emparelhar(QualificadoresValidos(receita, _ => true), SuportesValidos(receita.caso, suportesSelecionados)).Count;
    }

    private static List<ReceitaDeCaso.Qualificador> QualificadoresValidos(ReceitaDeCaso receita, Predicate<ReceitaDeCaso.Qualificador> filtro)
    {
        var lista = new List<ReceitaDeCaso.Qualificador>();
        var ids = new HashSet<string>();
        if (receita.qualificadores == null) return lista;
        foreach (ReceitaDeCaso.Qualificador q in receita.qualificadores)
            if (q != null && !string.IsNullOrWhiteSpace(q.id) && ids.Add(q.id) && filtro(q)) lista.Add(q);
        return lista;
    }

    // Emparelhamento máximo qualificador ↔ documento (caminhos aumentantes; listas minúsculas). Documentos em ordem
    // de itemID: o resultado não depende da ordem em que o jogador marcou os apoios.
    private static List<KeyValuePair<ReceitaDeCaso.Qualificador, Item>> Emparelhar(List<ReceitaDeCaso.Qualificador> qualificadores, List<Item> documentos)
    {
        documentos.Sort((x, y) => string.CompareOrdinal(x.itemID, y.itemID));
        var donoDoDocumento = new ReceitaDeCaso.Qualificador[documentos.Count];

        bool Tentar(ReceitaDeCaso.Qualificador q, bool[] visitado)
        {
            for (int d = 0; d < documentos.Count; d++)
            {
                if (visitado[d] || !q.Aceita(documentos[d])) continue;
                visitado[d] = true;
                if (donoDoDocumento[d] == null || Tentar(donoDoDocumento[d], visitado))
                {
                    donoDoDocumento[d] = q;
                    return true;
                }
            }
            return false;
        }

        foreach (ReceitaDeCaso.Qualificador q in qualificadores) Tentar(q, new bool[documentos.Count]);

        var pares = new List<KeyValuePair<ReceitaDeCaso.Qualificador, Item>>();
        foreach (ReceitaDeCaso.Qualificador q in qualificadores) // ordem da receita no histórico
            for (int d = 0; d < documentos.Count; d++)
                if (donoDoDocumento[d] == q) pares.Add(new KeyValuePair<ReceitaDeCaso.Qualificador, Item>(q, documentos[d]));
        return pares;
    }

    /// <summary>Documentos de apoio do caso, sem repetição de itemID (o mesmo documento duas vezes não vale dobrado).</summary>
    public static List<Item> SuportesValidos(CaseData caso, IEnumerable<Item> suportes)
    {
        var validos = new List<Item>();
        if (suportes == null || caso == null) return validos;
        var vistos = new HashSet<string>();
        foreach (Item s in suportes)
            if (s != null && s.EhSuporte && s.caso == caso && !string.IsNullOrEmpty(s.itemID) && vistos.Add(s.itemID))
                validos.Add(s);
        return validos;
    }
}
