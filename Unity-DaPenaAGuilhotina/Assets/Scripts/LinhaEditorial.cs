/// <summary>
/// Linha editorial do panfleto (Prompt 5, proposta D do documento de dificuldade), escolhida na prensa depois das duas
/// pistas, a partir da Fase 2. Muda os ganhos e, no sensacionalista, o tamanho da consequência de um boato — nunca a
/// verdade das pistas nem a versão impressa (ReceitaDeCaso.Classificar).
/// </summary>
public enum LinhaEditorial
{
    /// <summary>Compatibilidade, nunca oferecida ao jogador: tutorial (receita exata), receitas sem linha editorial
    /// configurada e publicações de saves anteriores à versão 6. Não aplica modificador nenhum.</summary>
    Neutra = 0,
    DefesaDoPovo = 1,
    /// <summary>"Agradar a Coroa" ou "Agradar o Comitê": o rótulo muda com a fase, a regra não.</summary>
    AgradarOPoder = 2,
    Sensacionalista = 3,
}

public static class LinhasEditoriais
{
    /// <summary>As três opções que o jogador pode escolher, na ordem em que aparecem na prensa.</summary>
    public static readonly LinhaEditorial[] Opcoes = { LinhaEditorial.DefesaDoPovo, LinhaEditorial.AgradarOPoder, LinhaEditorial.Sensacionalista };

    public static bool Escolhivel(LinhaEditorial linha) =>
        linha == LinhaEditorial.DefesaDoPovo || linha == LinhaEditorial.AgradarOPoder || linha == LinhaEditorial.Sensacionalista;

    /// <summary>Rótulo exibido. A opção 2 usa o rótulo da receita, se houver; senão o padrão da fase: até a Fase 3
    /// (1789–1792) ainda há rei, na Fase 4 (1793) quem manda é o Comitê.</summary>
    public static string Rotulo(LinhaEditorial linha, ReceitaDeCaso receita)
    {
        switch (linha)
        {
            case LinhaEditorial.DefesaDoPovo: return "Defesa do povo";
            case LinhaEditorial.AgradarOPoder:
                if (receita != null && receita.linhaEditorial != null && !string.IsNullOrWhiteSpace(receita.linhaEditorial.rotuloAgradarOPoder))
                    return receita.linhaEditorial.rotuloAgradarOPoder.Trim();
                int fase = receita != null && receita.caso != null ? receita.caso.fase : 2;
                return fase >= GameManager.UltimaFase ? "Agradar o Comitê" : "Agradar a Coroa";
            case LinhaEditorial.Sensacionalista: return "Sensacionalista";
            default: return "Neutra";
        }
    }
}
