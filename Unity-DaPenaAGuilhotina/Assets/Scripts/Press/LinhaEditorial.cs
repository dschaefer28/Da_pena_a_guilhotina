/// <summary>
/// Linha editorial (o "tom" ou teor da publicação), escolhida na prensa depois das duas pistas, a partir da Fase 2.
/// Desde 08/11/2026 são só duas opções: Neutro (o efeito normal do caso) e Sensacionalista (mais ouro, mas o lado que o
/// caso favorece ganha menos). Muda os ganhos, nunca a verdade das pistas nem a versão impressa (ReceitaDeCaso.Classificar).
/// Os valores ficam salvos como número: não renumere.
/// </summary>
public enum LinhaEditorial
{
    /// <summary>Nenhuma linha escolhida, nunca oferecida ao jogador: tutorial (receita exata), receitas sem linha
    /// editorial configurada e publicações de saves anteriores à versão 6. Não aplica modificador nenhum.</summary>
    Nenhuma = 0,
    /// <summary>Legado (até 08/11/2026): só aparece em publicações de saves antigos. Não é mais oferecida.</summary>
    DefesaDoPovo = 1,
    /// <summary>Legado (até 08/11/2026): "Agradar a Coroa" / "Agradar o Comitê". Só em publicações de saves antigos.</summary>
    AgradarOPoder = 2,
    /// <summary>Vende mais (ouro extra), mas convence menos: o lado que o caso favorece ganha menos que no Neutro.</summary>
    Sensacionalista = 3,
    /// <summary>Conta o caso sem exagero: o panfleto tem o efeito normal do caso, sem modificador.</summary>
    Neutro = 4,
}

public static class LinhasEditoriais
{
    /// <summary>As opções que o jogador pode escolher, na ordem em que aparecem na prensa.</summary>
    public static readonly LinhaEditorial[] Opcoes = { LinhaEditorial.Neutro, LinhaEditorial.Sensacionalista };

    public static bool Escolhivel(LinhaEditorial linha) =>
        linha == LinhaEditorial.Neutro || linha == LinhaEditorial.Sensacionalista;

    /// <summary>Rótulo exibido. As linhas legadas mantêm o rótulo para o histórico de saves antigos; "Agradar o poder"
    /// usa o da fase: até a Fase 3 (1789–1792) ainda há rei, na Fase 4 (1793) quem manda é o Comitê.</summary>
    public static string Rotulo(LinhaEditorial linha, ReceitaDeCaso receita)
    {
        switch (linha)
        {
            case LinhaEditorial.Neutro: return "Neutro";
            case LinhaEditorial.Sensacionalista: return "Sensacionalista";
            case LinhaEditorial.DefesaDoPovo: return "Defesa do povo";
            case LinhaEditorial.AgradarOPoder:
                int fase = receita != null && receita.caso != null ? receita.caso.fase : 2;
                return fase >= GameManager.UltimaFase ? "Agradar o Comitê" : "Agradar a Coroa";
            default: return "Nenhuma";
        }
    }
}
