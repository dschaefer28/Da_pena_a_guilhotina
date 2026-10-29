using NUnit.Framework;

/// <summary>
/// Legendas de transição (abertura e passagem entre fases, 29/10/2026): tempo de leitura de cada bloco pelo número de
/// palavras (1 s de margem + 1 s a cada 3 palavras, no mínimo 5 s), contado com o texto já inteiro na tela.
/// </summary>
public class CutscenesDeTransicaoTests
{
    private const float Margem = 1f;
    private const float PalavrasPorSegundo = 3f;
    private const float Minimo = 5f;

    [Test]
    public void ContarPalavras_IgnoraTagsESinaisSoltos()
    {
        Assert.AreEqual(5, CutsceneLegendas.ContarPalavras("Paris, 1786. — <b>Sob</b> o poder…"));
        Assert.AreEqual(0, CutsceneLegendas.ContarPalavras("  "));
        Assert.AreEqual(0, CutsceneLegendas.ContarPalavras(null));
    }

    [Test]
    public void TempoDeLeitura_MargemMaisUmSegundoACadaTresPalavras()
    {
        // 17 palavras: 1 + 17/3.
        const string texto = "Paris, 1786. Sob o poder da Coroa, a justiça pesa de forma diferente sobre ricos e pobres.";
        Assert.AreEqual(1f + 17f / 3f, CutsceneLegendas.TempoDeLeitura(texto, 0f, Margem, PalavrasPorSegundo, Minimo), 0.001f);
    }

    [Test]
    public void TempoDeLeitura_BlocoCurtoFicaNoMinimo()
    {
        Assert.AreEqual(Minimo, CutsceneLegendas.TempoDeLeitura("A Bastilha cai.", 0f, Margem, PalavrasPorSegundo, Minimo), 0.001f);
    }

    [Test]
    public void TempoDeLeitura_DuracaoDaLinhaSoAumenta()
    {
        const string texto = "O pão fica mais caro. Para muitas famílias, sobreviver se torna uma luta diária.";
        float calculado = CutsceneLegendas.TempoDeLeitura(texto, 0f, Margem, PalavrasPorSegundo, Minimo);
        Assert.AreEqual(calculado, CutsceneLegendas.TempoDeLeitura(texto, 4f, Margem, PalavrasPorSegundo, Minimo), 0.001f);
        Assert.AreEqual(12f, CutsceneLegendas.TempoDeLeitura(texto, 12f, Margem, PalavrasPorSegundo, Minimo), 0.001f);
    }
}
