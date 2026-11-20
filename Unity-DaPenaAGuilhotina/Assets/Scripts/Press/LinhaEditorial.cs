/// <summary>
/// Tom da publicação, escolhido na prensa depois das duas pistas, a partir da Fase 2. Muda os ganhos, nunca a
/// verdade das pistas nem a versão impressa. Valores salvos como número: não renumere.
/// </summary>
public enum LinhaEditorial
{
    /// <summary>Nunca oferecida: tutorial, receitas sem linha configurada e saves anteriores à versão 6. Sem modificador.</summary>
    Nenhuma = 0,
    /// <summary>Legado: só em publicações de saves antigos.</summary>
    DefesaDoPovo = 1,
    /// <summary>Legado: só em publicações de saves antigos.</summary>
    AgradarOPoder = 2,
    /// <summary>Ouro extra, mas o lado que o caso favorece ganha menos que no Neutro.</summary>
    Sensacionalista = 3,
    /// <summary>Efeito normal do caso, sem modificador.</summary>
    Neutro = 4,
}

public static class LinhasEditoriais
{
    /// <summary>Na ordem em que aparecem na prensa.</summary>
    public static readonly LinhaEditorial[] Opcoes = { LinhaEditorial.Neutro, LinhaEditorial.Sensacionalista };

    public static bool Escolhivel(LinhaEditorial linha) =>
        linha == LinhaEditorial.Neutro || linha == LinhaEditorial.Sensacionalista;

    /// <summary>Legadas mantêm rótulo para o histórico. "Agradar o poder" depende da fase: rei até a Fase 3, Comitê na 4.</summary>
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
