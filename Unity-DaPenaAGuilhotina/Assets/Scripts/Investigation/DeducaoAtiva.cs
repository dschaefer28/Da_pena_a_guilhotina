using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Exatamente uma das duas é Fato. A ordem a/b não tem significado e nunca é exibida.</summary>
[Serializable]
public class ParContraditorio
{
    public Item a;
    public Item b;

    public bool Contem(Item pista) => Mesmo(a, pista) || Mesmo(b, pista);

    public Item OutraDe(Item pista) => Mesmo(a, pista) ? b : Mesmo(b, pista) ? a : null;

    internal static bool Mesmo(Item x, Item y) =>
        x != null && y != null && !string.IsNullOrEmpty(x.itemID) && x.itemID == y.itemID;
}

/// <summary>O conjunto de dedução é a união dos pares; cartas do cliente e documentos de apoio ficam de fora.</summary>
[Serializable]
public class ConfiguracaoDeDeducao
{
    [Tooltip("Liga o quadro de dedução neste caso. Ligada, a verdade das pistas do caso nunca é revelada pelo jogo " +
             "(o campo Verificada Por é ignorado): em cada par, o jogador escolhe em qual afirmação acredita.")]
    public bool ativa;

    [Tooltip("Pares de afirmações contraditórias do caso. Em cada par, exatamente uma pista é Fato (a outra é Boato " +
             "ou Calúnia). Cada pista entra em um par só. Ferramentas > Campanha > 2 - Validar campanha confere.")]
    public List<ParContraditorio> pares = new List<ParContraditorio>();

    /// <summary>Nunca configurada: a ferramenta de setup só preenche estas.</summary>
    public bool Vazia => !ativa && (pares == null || pares.Count == 0);

    public bool Contem(Item pista)
    {
        if (pista == null || pares == null) return false;
        foreach (ParContraditorio par in pares)
            if (par != null && par.Contem(pista)) return true;
        return false;
    }

    public List<Item> Conjunto()
    {
        var lista = new List<Item>();
        if (pares == null) return lista;
        foreach (ParContraditorio par in pares)
        {
            if (par == null) continue;
            foreach (Item pista in new[] { par.a, par.b })
                if (pista != null && !lista.Exists(p => ParContraditorio.Mesmo(p, pista))) lista.Add(pista);
        }
        return lista;
    }

    public ParContraditorio ParDe(Item pista)
    {
        if (pista == null || pares == null) return null;
        foreach (ParContraditorio par in pares)
            if (par != null && par.Contem(pista)) return par;
        return null;
    }
}

/// <summary>caso = CaseData.name; escolhida/descartada = itemID. Irreversível; vai para o save como está.</summary>
[Serializable]
public class EscolhaDeDeducao
{
    public string caso;
    public string escolhida;
    public string descartada;
}

/// <summary>
/// Regras da dedução ativa. Escolhas em GameManager.escolhasDeDeducao; descobertas em GameManager.evidenciasObtidas
/// (inclui pistas já gastas ou descartadas). Com as duas afirmações de um par achadas, o jogador escolhe uma e a outra
/// sai de vez. O jogo nunca revela a verdade: ela só decide a versão do panfleto (ReceitaDeCaso).
/// </summary>
public static class Deducao
{
    /// <summary>O caso usa o quadro e deixa de revelar a verdade sozinho.</summary>
    public static bool Aderente(CaseData caso) =>
        caso != null && caso.deducao != null && caso.deducao.ativa && caso.deducao.pares != null && caso.deducao.pares.Count > 0;

    public static bool NoConjunto(Item pista) =>
        pista != null && pista.EhPista && Aderente(pista.caso) && pista.caso.deducao.Contem(pista);

    public static bool Descoberta(GameManager gm, Item pista) => gm != null && gm.EvidenciaObtida(pista);

    /// <summary>Pares com pistas descobertas, em ordem de descoberta: nunca pela posição no par, que poderia denunciar a
    /// verdadeira.</summary>
    public static List<List<Item>> ParesConhecidos(GameManager gm, CaseData caso)
    {
        var grupos = new List<List<Item>>();
        if (!Aderente(caso) || gm == null) return grupos;

        var vistas = new HashSet<string>();
        foreach (ParContraditorio par in caso.deducao.pares)
        {
            if (par == null) continue;
            var grupo = new List<Item>();
            foreach (Item pista in new[] { par.a, par.b })
                if (pista != null && !string.IsNullOrEmpty(pista.itemID) && Descoberta(gm, pista) && vistas.Add(pista.itemID))
                    grupo.Add(pista);
            if (grupo.Count == 0) continue;
            grupo.Sort((x, y) => PorDescoberta(gm, x, y));
            grupos.Add(grupo);
        }
        grupos.Sort((x, y) => PorDescoberta(gm, x[0], y[0]));
        return grupos;
    }

    public static List<Item> PistasConhecidas(GameManager gm, CaseData caso)
    {
        var lista = new List<Item>();
        foreach (List<Item> grupo in ParesConhecidos(gm, caso)) lista.AddRange(grupo);
        return lista;
    }

    // Pistas sem histórico (save antigo) vão para o fim, pelo nome.
    private static int PorDescoberta(GameManager gm, Item x, Item y)
    {
        int ix = gm.evidenciasObtidas.IndexOf(x.itemID), iy = gm.evidenciasObtidas.IndexOf(y.itemID);
        if (ix < 0) ix = int.MaxValue;
        if (iy < 0) iy = int.MaxValue;
        int porOrdem = ix.CompareTo(iy);
        return porOrdem != 0 ? porOrdem : string.Compare(x.NomeExibicao, y.NomeExibicao, StringComparison.CurrentCulture);
    }

    /// <summary>Null até o jogador descobrir as duas.</summary>
    public static Item Contradicao(GameManager gm, Item pista)
    {
        if (!NoConjunto(pista) || !Descoberta(gm, pista)) return null;
        Item outra = pista.caso.deducao.ParDe(pista)?.OutraDe(pista);
        return outra != null && Descoberta(gm, outra) ? outra : null;
    }

    public static int TotalDeContradicoes(CaseData caso) => Aderente(caso) ? caso.deducao.pares.FindAll(p => p != null).Count : 0;

    public static int ContradicoesEncontradas(GameManager gm, CaseData caso)
    {
        if (!Aderente(caso) || gm == null) return 0;
        int total = 0;
        foreach (ParContraditorio par in caso.deducao.pares)
            if (par != null && Descoberta(gm, par.a) && Descoberta(gm, par.b)) total++;
        return total;
    }

    // ===== Escolhas =====

    public static EscolhaDeDeducao EscolhaDoPar(GameManager gm, Item pista)
    {
        if (gm == null || !NoConjunto(pista)) return null;
        ParContraditorio par = pista.caso.deducao.ParDe(pista);
        string caso = pista.caso.name;
        return gm.escolhasDeDeducao.Find(e => e != null && e.caso == caso && DoPar(par, e.escolhida));
    }

    private static bool DoPar(ParContraditorio par, string itemID) =>
        !string.IsNullOrEmpty(itemID) && ((par.a != null && par.a.itemID == itemID) || (par.b != null && par.b.itemID == itemID));

    public static bool ParDecidido(GameManager gm, Item pista) => EscolhaDoPar(gm, pista) != null;

    public static bool Escolhida(GameManager gm, Item pista)
    {
        EscolhaDeDeducao e = EscolhaDoPar(gm, pista);
        return e != null && e.escolhida == pista.itemID;
    }

    /// <summary>Descartada não volta ao inventário.</summary>
    public static bool Descartada(GameManager gm, Item pista)
    {
        EscolhaDeDeducao e = EscolhaDoPar(gm, pista);
        return e != null && e.descartada == pista.itemID;
    }

    public static int ParesDecididos(GameManager gm, CaseData caso)
    {
        if (!Aderente(caso) || gm == null) return 0;
        int total = 0;
        foreach (ParContraditorio par in caso.deducao.pares)
            if (par != null && par.a != null && ParDecidido(gm, par.a)) total++;
        return total;
    }

    /// <summary>Contrária descoberta e par sem decisão: só uma das duas pode ir para a prensa, escolhida no quadro.</summary>
    public static bool AguardandoEscolha(GameManager gm, Item pista) =>
        Contradicao(gm, pista) != null && !ParDecidido(gm, pista);

    public static bool PodeEscolher(GameManager gm, CaseData caso, Item pista) =>
        EmAndamento(gm, caso) && pista != null && pista.caso == caso && AguardandoEscolha(gm, pista);

    private static bool EmAndamento(GameManager gm, CaseData caso) =>
        gm != null && Aderente(caso) && gm.casoEscolhido == caso && gm.CasoAtualEmAndamento;

    /// <summary>Descarta a contrária de todo lugar (grade, prensa, mão, fora da grade). Falso, sem efeito, se não permitido.</summary>
    public static bool Escolher(GameManager gm, CaseData caso, Item pista, out Item descartada)
    {
        descartada = null;
        if (!PodeEscolher(gm, caso, pista)) return false;

        descartada = Contradicao(gm, pista);
        gm.escolhasDeDeducao.Add(new EscolhaDeDeducao { caso = caso.name, escolhida = pista.itemID, descartada = descartada.itemID });

        string id = descartada.itemID;
        if (gm.inventoryManager != null) gm.inventoryManager.RemoverItens(item => item != null && item.itemID == id);
        else if (gm.inventarioSalvo != null) gm.inventarioSalvo.RemoveAll(item => item != null && item.itemID == id);
        if (MouseManager.instance != null && MouseManager.instance.heldItem != null && MouseManager.instance.heldItem.itemID == id)
            MouseManager.instance.heldItem = null;
        return true;
    }

    /// <summary>Mostra só a decisão do jogador, nunca a verdade.</summary>
    public static string Rotulo(GameManager gm, Item pista)
    {
        if (Escolhida(gm, pista)) return "<color=#E0C080>sua escolha</color>";
        if (Descartada(gm, pista)) return "<color=#9E9E9E>descartada</color>";
        return "<color=#9E9E9E>não verificada</color>";
    }

    /// <summary>Vazio = coerente. Usado pelo validador da campanha.</summary>
    public static List<string> Problemas(CaseData caso)
    {
        var problemas = new List<string>();
        if (caso == null || caso.deducao == null || !caso.deducao.ativa) return problemas;
        List<ParContraditorio> pares = caso.deducao.pares;
        if (pares == null || pares.Count == 0) { problemas.Add("dedução ligada sem nenhum par contraditório."); return problemas; }

        var vistas = new HashSet<string>();
        for (int i = 0; i < pares.Count; i++)
        {
            ParContraditorio par = pares[i];
            string onde = $"par {i + 1}";
            if (par == null || par.a == null || par.b == null) { problemas.Add($"{onde} incompleto."); continue; }
            if (ParContraditorio.Mesmo(par.a, par.b)) { problemas.Add($"{onde} repete a mesma pista ('{par.a.name}')."); continue; }

            foreach (Item pista in new[] { par.a, par.b })
            {
                if (pista.caso != caso) problemas.Add($"{onde}: '{pista.name}' pertence a outro caso ({(pista.caso != null ? pista.caso.name : "nenhum")}).");
                else if (pista.EhLeitura) problemas.Add($"{onde}: '{pista.name}' é carta do cliente (só leitura, fica fora da dedução).");
                else if (!pista.EhPista || pista.EhSuporte) problemas.Add($"{onde}: '{pista.name}' não é pista (documento de apoio ou item comum).");
                if (!string.IsNullOrEmpty(pista.itemID) && !vistas.Add(pista.itemID))
                    problemas.Add($"'{pista.name}' aparece em mais de um par.");
            }

            int fatos = (par.a.confiabilidade == Confiabilidade.Fato ? 1 : 0) + (par.b.confiabilidade == Confiabilidade.Fato ? 1 : 0);
            if (fatos != 1)
                problemas.Add($"{onde} ('{par.a.name}' × '{par.b.name}') sem solução coerente: precisa de exatamente um Fato, tem {fatos}.");
        }
        return problemas;
    }
}
