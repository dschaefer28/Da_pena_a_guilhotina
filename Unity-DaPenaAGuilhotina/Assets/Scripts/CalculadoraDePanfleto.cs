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

    public bool TemRevelacao => penalidadePovo != 0 || penalidadeEstado != 0 || !string.IsNullOrWhiteSpace(textoRevelacao);
}

/// <summary>
/// Cálculo único do panfleto de caso, em etapas que podem ser compostas:
///   1. versão pela confiabilidade REAL das duas pistas (ReceitaDeCaso.Classificar);
///   2. qualificação por documentos de apoio válidos (ReceitaDeCaso.qualificadores);
///   3. (Prompt 5) modificadores da linha editorial entram aqui, depois da qualificação.
/// O clamp de Povo/Estado fica no ponto de aplicação (GameManager.AplicarImpactoPanfleto).
/// </summary>
public static class CalculadoraDePanfleto
{
    public static ResultadoDoPanfleto Calcular(ReceitaDeCaso receita, Item a, Item b, IEnumerable<Item> suportesSelecionados)
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
