using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Hipótese do jogador sobre uma pista do quadro de dedução (Prompt 4). Fica separada da verdade interna
/// (Item.confiabilidade): marcar nunca muda a pista, a versão impressa nem a receita.</summary>
public enum MarcacaoDaPista
{
    NaoMarcada = 0,
    Confiavel = 1,
    Duvidosa = 2,
}

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
/// de pistas de dedução é a união dos pares; alegações do cliente e documentos de apoio ficam de fora.</summary>
[Serializable]
public class ConfiguracaoDeDeducao
{
    [Tooltip("Liga o quadro de dedução neste caso. Ligada, a verdade das pistas do caso não é revelada sozinha " +
             "(o campo Verificada Por é ignorado): só a conferência do quadro confirma.")]
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

/// <summary>Uma hipótese guardada: caso (CaseData.name) + pista (itemID). Vai para o save como está.</summary>
[Serializable]
public class MarcacaoDeDeducao
{
    public string caso;
    public string pista;
    public MarcacaoDaPista marca;
}

/// <summary>
/// Regras da dedução ativa (Prompt 4). O estado do jogador fica no GameManager (marcacoesDeDeducao,
/// deducoesConfirmadas) e as pistas descobertas vêm do histórico de evidências (GameManager.evidenciasObtidas),
/// que guarda também as pistas já gastas na prensa.
///
/// "Conferir" confirma só quando TODO o conjunto do caso foi descoberto e marcado certo (Fato = Confiável;
/// Boato/Calúnia = Duvidosa). Qualquer outra situação — pista faltando, marcação faltando ou errada — dá o mesmo
/// resultado genérico, sem dizer o que acertou nem quantas acertou. Isso reduz a informação por tentativa, mas não
/// impede uma busca exaustiva: com dois pares há só quatro combinações coerentes, e conferir não custa nada.
/// </summary>
public static class Deducao
{
    public enum Resultado
    {
        Confirmada,
        NaoSeSustenta, // resposta única para incompleta, parcial ou errada
        JaConfirmada,
        Indisponivel,  // caso sem dedução, ou não é o caso em andamento
    }

    /// <summary>O caso usa o quadro de dedução (e deixa de revelar a verdade sozinho).</summary>
    public static bool Aderente(CaseData caso) =>
        caso != null && caso.deducao != null && caso.deducao.ativa && caso.deducao.pares != null && caso.deducao.pares.Count > 0;

    public static bool NoConjunto(Item pista) =>
        pista != null && pista.EhPista && Aderente(pista.caso) && pista.caso.deducao.Contem(pista);

    public static bool Descoberta(GameManager gm, Item pista) => gm != null && gm.EvidenciaObtida(pista);

    /// <summary>Pistas do conjunto que o jogador já descobriu, agrupadas por par, na ordem em que ele as descobriu
    /// (GameManager.evidenciasObtidas). A ordem vem só das escolhas do jogador: nunca da posição no par nem de outra
    /// coisa que possa denunciar qual afirmação é a verdadeira.</summary>
    public static List<Item> PistasConhecidas(GameManager gm, CaseData caso)
    {
        var lista = new List<Item>();
        if (!Aderente(caso) || gm == null) return lista;

        var vistas = new HashSet<string>();
        var grupos = new List<List<Item>>();
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
        foreach (List<Item> grupo in grupos) lista.AddRange(grupo);
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

    public static bool Confirmada(GameManager gm, CaseData caso) =>
        gm != null && caso != null && gm.deducoesConfirmadas.Contains(caso.name);

    /// <summary>Pista do conjunto de um caso cuja dedução foi confirmada.</summary>
    public static bool PistaConfirmada(GameManager gm, Item pista) => NoConjunto(pista) && Confirmada(gm, pista.caso);

    public static MarcacaoDaPista MarcacaoDe(GameManager gm, CaseData caso, Item pista)
    {
        MarcacaoDeDeducao m = Encontrar(gm, caso, pista);
        return m != null ? m.marca : MarcacaoDaPista.NaoMarcada;
    }

    private static MarcacaoDeDeducao Encontrar(GameManager gm, CaseData caso, Item pista)
    {
        if (gm == null || caso == null || pista == null || string.IsNullOrEmpty(pista.itemID)) return null;
        return gm.marcacoesDeDeducao.Find(m => m != null && m.caso == caso.name && m.pista == pista.itemID);
    }

    /// <summary>Só o caso em andamento, com dedução não confirmada, e só pistas do conjunto já descobertas.</summary>
    public static bool PodeMarcar(GameManager gm, CaseData caso, Item pista) =>
        EmAndamento(gm, caso) && !Confirmada(gm, caso) && caso.deducao.Contem(pista) && Descoberta(gm, pista);

    private static bool EmAndamento(GameManager gm, CaseData caso) =>
        gm != null && Aderente(caso) && gm.casoEscolhido == caso && gm.CasoAtualEmAndamento;

    /// <summary>Guarda a hipótese do jogador (NaoMarcada apaga). Falso se a marcação não é permitida agora.</summary>
    public static bool Marcar(GameManager gm, CaseData caso, Item pista, MarcacaoDaPista marca)
    {
        if (!PodeMarcar(gm, caso, pista)) return false;
        MarcacaoDeDeducao existente = Encontrar(gm, caso, pista);
        if (marca == MarcacaoDaPista.NaoMarcada)
        {
            if (existente != null) gm.marcacoesDeDeducao.Remove(existente);
            return true;
        }
        if (existente != null) existente.marca = marca;
        else gm.marcacoesDeDeducao.Add(new MarcacaoDeDeducao { caso = caso.name, pista = pista.itemID, marca = marca });
        return true;
    }

    /// <summary>Confere o quadro do caso em andamento. Todas as falhas devolvem NaoSeSustenta.</summary>
    public static Resultado Conferir(GameManager gm, CaseData caso)
    {
        if (!EmAndamento(gm, caso)) return Resultado.Indisponivel;
        if (Confirmada(gm, caso)) return Resultado.JaConfirmada;

        bool certo = true;
        foreach (Item pista in caso.deducao.Conjunto())
        {
            MarcacaoDaPista esperada = pista.confiabilidade == Confiabilidade.Fato ? MarcacaoDaPista.Confiavel : MarcacaoDaPista.Duvidosa;
            if (!Descoberta(gm, pista) || MarcacaoDe(gm, caso, pista) != esperada) certo = false; // sem sair antes: nada muda com o motivo
        }
        if (!certo) return Resultado.NaoSeSustenta;

        gm.deducoesConfirmadas.Add(caso.name);
        return Resultado.Confirmada;
    }

    /// <summary>Etiqueta da situação de uma pista de caso aderente (inventário e ficha). Antes da confirmação mostra só
    /// a hipótese do jogador; a verdade aparece depois que o quadro inteiro foi confirmado.</summary>
    public static string Rotulo(GameManager gm, Item pista)
    {
        if (PistaConfirmada(gm, pista))
            return pista.confiabilidade == Confiabilidade.Fato ? "<color=#4CAF50>confirmada</color>" : "<color=#E0A030>desmentida</color>";

        MarcacaoDaPista marca = NoConjunto(pista) ? MarcacaoDe(gm, pista.caso, pista) : MarcacaoDaPista.NaoMarcada;
        switch (marca)
        {
            case MarcacaoDaPista.Confiavel: return "<color=#9E9E9E>não verificada · hipótese: confiável</color>";
            case MarcacaoDaPista.Duvidosa: return "<color=#9E9E9E>não verificada · hipótese: duvidosa</color>";
            default: return "<color=#9E9E9E>não verificada</color>";
        }
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
                else if (!pista.EhPista || pista.EhSuporte) problemas.Add($"{onde}: '{pista.name}' não é pista (documento de apoio ou item comum).");
                else if (caso.EhAlegacao(pista)) problemas.Add($"{onde}: '{pista.name}' é alegação do cliente (fica fora da dedução).");
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
