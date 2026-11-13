using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Correções da verificação de 26/09/2026: os boatos publicados são expostos no fim da fase ANTES da rota ser
/// travada; o Tribunal usa as provas do caso julgado (inclusive as pistas gastas na prensa) e o destino do réu
/// vem do panfleto desse caso; os três finais têm textos para os dois destinos do réu.
/// </summary>
public class FinaisEBoatosTests
{
    private readonly List<Object> criados = new List<Object>();

    private T Criar<T>(string nome) where T : ScriptableObject
    {
        T objeto = ScriptableObject.CreateInstance<T>();
        objeto.name = nome;
        criados.Add(objeto);
        return objeto;
    }

    private CaseData CriarCaso(string nome, int fase)
    {
        CaseData caso = Criar<CaseData>(nome);
        caso.caseTitle = nome;
        caso.fase = fase;
        return caso;
    }

    private Item CriarItem(string id, CaseData caso, Confiabilidade confiabilidade)
    {
        Item item = Criar<Item>(id);
        item.itemID = id;
        item.caso = caso;
        item.confiabilidade = confiabilidade;
        item.itemAmt = 1;
        return item;
    }

    private GameManager CriarGameManager(int fase)
    {
        var go = new GameObject("GameManager_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>(); // em Edit Mode o Awake não roda
        gm.faseAtual = fase;
        return gm;
    }

    [TearDown]
    public void Limpar()
    {
        foreach (Object objeto in criados)
            if (objeto != null) Object.DestroyImmediate(objeto);
        criados.Clear();
    }

    // ===== Boatos expostos no fim da fase =====

    [Test]
    public void EncerrarFase_AplicaPenalidadesAntesDeTravarARota()
    {
        GameManager gm = CriarGameManager(3);
        CaseData caso = CriarCaso("Caso_Boato", 3);
        gm.opiniaoPublicaAtual = 60;
        gm.opiniaoEstadoAtual = 45; // desnível 15 (margem 12): sem a penalidade, a rota seria A
        Assert.AreEqual(RotaFinal.A_Guilhotina, gm.CalcularRota());
        gm.revelacoesPendentes.Add(new GameManager.RevelacaoPendente { caso = caso, texto = "Mentira exposta.", povo = -10, estado = 0 });

        GameManager.EncerramentoDeFase fim = gm.EncerrarFase();

        Assert.AreEqual(3, fim.fase);
        Assert.AreEqual(1, fim.revelacoes.Count);
        Assert.AreEqual(0, gm.revelacoesPendentes.Count, "a revelação é consumida uma única vez");
        Assert.AreEqual(50, gm.opiniaoPublicaAtual);
        Assert.AreEqual(4, gm.faseAtual);
        Assert.AreEqual(RotaFinal.C_Equilibrio, gm.rotaFinal, "a mentira descoberta pesa na Definição de Rota");
        Assert.AreEqual(50, fim.despesa);
        Assert.AreEqual(-50, gm.capitalAtual);
    }

    // 13/11/2026: a penalidade do desmentido entra nas barras na impressão; o fim da fase só conta a história.
    [Test]
    public void PanfletoComMentira_PenalidadeEntraNaImpressao_ENaoERepetidaNoFimDaFase()
    {
        GameManager gm = CriarGameManager(2);
        CaseData caso = CriarCaso("Caso_Calunia", 2);
        gm.opiniaoPublicaAtual = 90; // já com o ganho do panfleto, como a prensa deixa antes de registrar
        gm.opiniaoEstadoAtual = 30;

        gm.RegistrarPanfletoDeCaso(caso, new ResultadoDoPanfleto
        {
            nivel = NivelDoPanfleto.Calunia, povo = 40, estado = -20, penalidadePovo = -45, penalidadeEstado = -5,
            textoRevelacao = "Calúnia exposta."
        });

        Assert.AreEqual(45, gm.opiniaoPublicaAtual, "o desmentido é cobrado na hora");
        Assert.AreEqual(25, gm.opiniaoEstadoAtual);
        Assert.AreEqual(1, gm.revelacoesPendentes.Count, "a revelação fica para a cutscene de fim de fase");
        Assert.IsTrue(gm.revelacoesPendentes[0].aplicada);

        // O save guarda que já foi cobrada: o Continuar não cobra de novo.
        SistemaDeSave.DadosDeSave dados = SistemaDeSave.DesserializarDados(JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Jogo", 0)));
        Assert.IsTrue(dados.revelacoesPendentes[0].aplicada);

        GameManager.EncerramentoDeFase fim = gm.EncerrarFase();

        Assert.AreEqual(1, fim.revelacoes.Count, "a cutscene ainda conta o desmentido");
        Assert.AreEqual(45, gm.opiniaoPublicaAtual, "sem cobrança dupla");
        Assert.AreEqual(25, gm.opiniaoEstadoAtual);
    }

    // 13/11/2026: ganho que bate no teto não faz o desmentido punir mais (80 + 40 - 45 = 75, não 100 - 45 = 55).
    [Test]
    public void PanfletoComMentira_GanhoNoTeto_TerminaNoSaldoDoPanfleto()
    {
        GameManager gm = CriarGameManager(2);
        CaseData caso = CriarCaso("Caso_Teto", 2);
        gm.opiniaoPublicaAtual = 80;
        gm.opiniaoEstadoAtual = 10;
        var resultado = new ResultadoDoPanfleto
        {
            nivel = NivelDoPanfleto.Calunia, povo = 40, estado = -20, penalidadePovo = -45, penalidadeEstado = -5,
            textoRevelacao = "Calúnia exposta."
        };

        Vector3Int aplicado = gm.AplicarImpactoPanfleto(resultado.povo, resultado.estado, resultado.ouro);
        Assert.AreEqual(100, gm.opiniaoPublicaAtual, "a barra ainda sobe até o teto");
        gm.RegistrarPanfletoDeCaso(caso, resultado, aplicado);

        Assert.AreEqual(75, gm.opiniaoPublicaAtual, "80 + 40 - 45");
        Assert.AreEqual(0, gm.opiniaoEstadoAtual, "10 - 20 - 5, limitado a 0");
    }

    [Test]
    public void EncerrarFase_SemBoatos_SoCobraEAvanca()
    {
        GameManager gm = CriarGameManager(2);
        gm.capitalAtual = 40;

        GameManager.EncerramentoDeFase fim = gm.EncerrarFase();

        Assert.AreEqual(0, fim.revelacoes.Count);
        Assert.AreEqual(25, fim.despesa);
        Assert.AreEqual(15, gm.capitalAtual);
        Assert.AreEqual(3, gm.faseAtual);
        Assert.AreEqual(50, gm.opiniaoPublicaAtual);
        Assert.AreEqual(50, gm.opiniaoEstadoAtual);
    }

    // ===== Tribunal =====

    [Test]
    public void Tribunal_ReuAbsolvidoSoComPanfletoDeFatos()
    {
        GameManager gm = CriarGameManager(4);
        CaseData caso = CriarCaso("Caso_Julgado", 4);
        Assert.IsNull(TribunalManager.ReuAbsolvido(gm, caso), "sem panfleto publicado não há decisão");

        gm.panfletosPublicados.Add(new GameManager.PanfletoPublicado { caso = caso, nivel = NivelDoPanfleto.Fatos });
        Assert.AreEqual(true, TribunalManager.ReuAbsolvido(gm, caso));

        foreach (NivelDoPanfleto fraco in new[] { NivelDoPanfleto.ComBoato, NivelDoPanfleto.Calunia, NivelDoPanfleto.Alegacoes })
        {
            gm.panfletosPublicados[0].nivel = fraco;
            Assert.AreEqual(false, TribunalManager.ReuAbsolvido(gm, caso), fraco.ToString());
        }
    }

    [Test]
    public void Tribunal_ProvasSaoDoCasoJulgado_IncluemPistasGastasEOPanfleto()
    {
        GameManager gm = CriarGameManager(4);
        CaseData julgado = CriarCaso("Caso_Julgado", 4);
        CaseData anterior = CriarCaso("Caso_Anterior", 3);

        Item fato = CriarItem("fato_julgado", julgado, Confiabilidade.Fato);
        Item boato = CriarItem("boato_julgado", julgado, Confiabilidade.Boato);
        Item apoio = CriarItem("apoio_julgado", julgado, Confiabilidade.NaoEPista);
        apoio.documentoDeSuporte = true;
        Item deOutroCaso = CriarItem("fato_anterior", anterior, Confiabilidade.Fato);
        Item panfletoFatos = CriarItem("panfleto_julgado", null, Confiabilidade.NaoEPista);
        Item panfletoAnterior = CriarItem("panfleto_anterior", null, Confiabilidade.NaoEPista);

        ReceitaDeCaso receita = Criar<ReceitaDeCaso>("Receita_Julgado");
        receita.caso = julgado;
        receita.panfletoPadrao = panfletoFatos;
        julgado.receitaDoPanfleto = receita;

        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        catalogo.itens = new List<Item> { fato, boato, apoio, deOutroCaso, panfletoFatos, panfletoAnterior };

        // As duas pistas impressas já saíram do inventário; o histórico continua com elas.
        gm.evidenciasObtidas.AddRange(new[] { "fato_anterior", "fato_julgado", "boato_julgado", "apoio_julgado" });
        gm.inventarioSalvo.Add(panfletoAnterior);
        gm.panfletosPublicados.Add(new GameManager.PanfletoPublicado { caso = julgado, nivel = NivelDoPanfleto.Fatos });

        List<Item> provas = TribunalManager.ProvasDoCaso(gm, julgado, catalogo);

        CollectionAssert.AreEquivalent(new[] { fato, boato, apoio, panfletoFatos }, provas);
        Assert.AreEqual(0, TribunalManager.ProvasDoCaso(gm, null, catalogo).Count);
    }

    [Test]
    public void Tribunal_DesfechosPadrao_TemAsTresRotasComOsDoisDestinosDoReu()
    {
        List<TribunalManager.DesfechoDaRota> desfechos = TribunalManager.DesfechosPadrao();
        foreach (RotaFinal rota in new[] { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio })
        {
            TribunalManager.DesfechoDaRota d = desfechos.Find(x => x.rota == rota);
            Assert.IsNotNull(d, rota.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.titulo), rota + " título");
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.vereditoReuAbsolvido), rota + " veredito absolvido");
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.vereditoReuCondenado), rota + " veredito condenado");
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.legendaReuAbsolvido), rota + " legenda absolvido");
            Assert.IsFalse(string.IsNullOrWhiteSpace(d.legendaReuCondenado), rota + " legenda condenado");
            Assert.Greater(d.legendas.Count, 0, rota + " cutscene");
        }
        StringAssert.Contains("Esquecido", desfechos.Find(x => x.rota == RotaFinal.C_Equilibrio).titulo);
    }
}
