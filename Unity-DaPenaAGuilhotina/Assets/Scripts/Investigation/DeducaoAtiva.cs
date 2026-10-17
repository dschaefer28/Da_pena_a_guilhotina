using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Duas afirmações do mesmo caso que não podem ser verdadeiras juntas: exatamente uma delas é Fato.
/// A ordem (a/b) não significa nada: o quadro lista as pistas pelo nome, nunca pela posição no par.</summary>
[Serializable]
public class ParContraditorio
{
    public Item a;
    public Item b;

    public bool Contem(Item pista) => Mesmo(a, pista) || Mesmo(b, pista);

    /// <summary>A outra afirmação do par (ou null se a pista não está nele).</summary>
    public Item OutraDe(Item pista) => Mesmo(a, pista) ? b : Mesmo(b, pista) ? a : null;

    internal static bool Mesmo(Item x, Item y) =>
        x != null && y != null && !string.IsNullOrEmpty(x.itemID) && x.itemID == y.itemID;
}

/// <summary>Dedução ativa de um caso (proposta C do documento de dificuldade; Fases 2, 3 e 4 desde o Prompt 7). O conjunto
/// de pistas de dedução é a união dos pares; cartas do cliente e documentos de apoio ficam de fora.</summary>
[Serializable]
public class ConfiguracaoDeDeducao
{
    [Tooltip("Liga o quadro de dedução neste caso. Ligada, a verdade das pistas do caso nunca é revelada pelo jogo " +
             "(o campo Verificada Por é ignorado): em cada par, o jogador escolhe em qual afirmação acredita.")]
    public bool ativa;

    [Tooltip("Pares de afirmações contraditórias do caso. Em cada par, exatamente uma pista é Fato (a outra é Boato " +
             "ou Calúnia). Cada pista entra em um par só. Ferramentas > Campanha > 2 - Validar campanha confere.")]
    public List<ParContraditorio> pares = new List<ParContraditorio>();

    /// <summary>Nunca foi configurada (a ferramenta de setup só preenche estas).</summary>
    public bool Vazia => !ativa && (pares == null || pares.Count == 0);

    public bool Contem(Item pista)
    {
        if (pista == null || pares == null) return false;
        foreach (ParContraditorio par in pares)
            if (par != null && par.Contem(pista)) return true;
        return false;
    }

    /// <summary>Pistas do conjunto, sem repetir (itemID).</summary>
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

/// <summary>Escolha do jogador num par contraditório: caso (CaseData.name), a pista com que ele ficou e a que foi
/// descartada (itemID). Não tem volta e vai para o save como está.</summary>
[Serializable]
public class EscolhaDeDeducao
{
    public string caso;
    public string escolhida;
    public string descartada;
}

/// <summary>
/// Regras da dedução ativa. O estado do jogador fica no GameManager (escolhasDeDeducao) e as pistas descobertas vêm do
/// histórico de evidências (GameManager.evidenciasObtidas), que guarda também as pistas já gastas na prensa ou descartadas.
///
/// Cada par é decidido à parte: quando o jogador já achou as duas afirmações, ele escolhe em qual acredita, e a outra sai
/// do inventário de vez (não vai mais para a prensa nem para o tribunal). Qualquer escolha é aceita: o jogo nunca diz qual
/// afirmação é a verdadeira. A verdade (Item.confiabilidade) só decide a versão do panfleto impresso (ReceitaDeCaso).
/// </summary>
public static class Deducao
{
    /// <summary>O caso usa o quadro de dedução (e deixa de revelar a verdade sozinho).</summary>
    public static bool Aderente(CaseData caso) =>
        caso != null && caso.deducao != null && caso.deducao.ativa && caso.deducao.pares != null && caso.deducao.pares.Count > 0;

    public static bool NoConjunto(Item pista) =>
        pista != null && pista.EhPista && Aderente(pista.caso) && pista.caso.deducao.Contem(pista);

    public static bool Descoberta(GameManager gm, Item pista) => gm != null && gm.EvidenciaObtida(pista);

    /// <summary>Pares com alguma pista já descoberta, cada um com as pistas descobertas, na ordem em que o jogador as
    /// descobriu (GameManager.evidenciasObtidas). A ordem vem só das escolhas do jogador: nunca da posição no par nem de
    /// outra coisa que possa denunciar qual afirmação é a verdadeira.</summary>
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

    /// <summary>As pistas de ParesConhecidos, em sequência (agrupadas por par).</summary>
    public static List<Item> PistasConhecidas(GameManager gm, CaseData caso)
    {
        var lista = new List<Item>();
        foreach (List<Item> grupo in ParesConhecidos(gm, caso)) lista.AddRange(grupo);
        return lista;
    }

    // Ordem de descoberta; o que veio de um save antigo sem esse histórico (achado só no inventário) fica no fim, pelo nome.
    private static int PorDescoberta(GameManager gm, Item x, Item y)
    {
        int ix = gm.evidenciasObtidas.IndexOf(x.itemID), iy = gm.evidenciasObtidas.IndexOf(y.itemID);
        if (ix < 0) ix = int.MaxValue;
        if (iy < 0) iy = int.MaxValue;
        int porOrdem = ix.CompareTo(iy);
        return porOrdem != 0 ? porOrdem : string.Compare(x.NomeExibicao, y.NomeExibicao, StringComparison.CurrentCulture);
    }

    /// <summary>A afirmação contrária desta pista, se o jogador já descobriu as duas. Senão, null.</summary>
    public static Item Contradicao(GameManager gm, Item pista)
    {
        if (!NoConjunto(pista) || !Descoberta(gm, pista)) return null;
        Item outra = pista.caso.deducao.ParDe(pista)?.OutraDe(pista);
        return outra != null && Descoberta(gm, outra) ? outra : null;
    }

    public static int TotalDeContradicoes(CaseData caso) => Aderente(caso) ? caso.deducao.pares.FindAll(p => p != null).Count : 0;

    /// <summary>Pares com as duas afirmações já descobertas.</summary>
    public static int ContradicoesEncontradas(GameManager gm, CaseData caso)
    {
        if (!Aderente(caso) || gm == null) return 0;
        int total = 0;
        foreach (ParContraditorio par in caso.deducao.pares)
            if (par != null && Descoberta(gm, par.a) && Descoberta(gm, par.b)) total++;
        return total;
    }

    // ===== Escolhas =====

    /// <summary>A escolha já feita no par desta pista (ou null).</summary>
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

    /// <summary>O jogador ficou com esta pista no par dela.</summary>
    public static bool Escolhida(GameManager gm, Item pista)
    {
        EscolhaDeDeducao e = EscolhaDoPar(gm, pista);
        return e != null && e.escolhida == pista.itemID;
    }

    /// <summary>O jogador descartou esta pista ao escolher a contrária: ela não volta ao inventário.</summary>
    public static bool Descartada(GameManager gm, Item pista)
    {
        EscolhaDeDeducao e = EscolhaDoPar(gm, pista);
        return e != null && e.descartada == pista.itemID;
    }

    /// <summary>Pares do caso já decididos.</summary>
    public static int ParesDecididos(GameManager gm, CaseData caso)
    {
        if (!Aderente(caso) || gm == null) return 0;
        int total = 0;
        foreach (ParContraditorio par in caso.deducao.pares)
            if (par != null && par.a != null && ParDecidido(gm, par.a)) total++;
        return total;
    }

    /// <summary>A pista tem uma contrária já descoberta e o par ainda não foi decidido: só uma das duas pode ir para a
    /// prensa, e quem escolhe é o jogador, no quadro.</summary>
    public static bool AguardandoEscolha(GameManager gm, Item pista) =>
        Contradicao(gm, pista) != null && !ParDecidido(gm, pista);

    /// <summary>Só o caso em andamento, com as duas afirmações do par já descobertas e o par ainda sem escolha.</summary>
    public static bool PodeEscolher(GameManager gm, CaseData caso, Item pista) =>
        EmAndamento(gm, caso) && pista != null && pista.caso == caso && AguardandoEscolha(gm, pista);

    private static bool EmAndamento(GameManager gm, CaseData caso) =>
        gm != null && Aderente(caso) && gm.casoEscolhido == caso && gm.CasoAtualEmAndamento;

    /// <summary>Fica com esta pista e descarta a contrária (sai do inventário, das entradas da prensa e do que está
    /// guardado fora da grade). Falso, sem mudar nada, se a escolha não é permitida agora.</summary>
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

    /// <summary>Etiqueta da situação de uma pista de caso aderente (inventário, ficha e quadro). Mostra só a decisão do
    /// jogador, nunca a verdade.</summary>
    public static string Rotulo(GameManager gm, Item pista)
    {
        if (Escolhida(gm, pista)) return "<color=#E0C080>sua escolha</color>";
        if (Descartada(gm, pista)) return "<color=#9E9E9E>descartada</color>";
        return "<color=#9E9E9E>não verificada</color>";
    }

    /// <summary>Problemas de configuração da dedução de um caso (vazio = coerente). Usado pelo validador da campanha.</summary>
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
