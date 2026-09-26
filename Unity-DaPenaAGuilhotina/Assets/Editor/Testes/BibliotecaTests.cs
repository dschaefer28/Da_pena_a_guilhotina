using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prompt 3: biblioteca, qualidade e documentos de apoio. Usa os assets reais do Champ de Mars para o exemplo
/// numérico e objetos temporários para as regras de compra.
/// </summary>
public class BibliotecaTests
{
    private const string Pasta = "Assets/Scriptableobjects/Campanha/";
    private readonly List<Object> criados = new List<Object>();

    [TearDown]
    public void Limpar()
    {
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private static T Carregar<T>(string caminho) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(caminho);
        Assert.IsNotNull(asset, caminho);
        return asset;
    }

    private static void AssertValores(ResultadoDoPanfleto r, int povo, int estado, int ouro, string msg)
    {
        Assert.AreEqual(povo, r.povo, msg + " (povo)");
        Assert.AreEqual(estado, r.estado, msg + " (estado)");
        Assert.AreEqual(ouro, r.ouro, msg + " (ouro)");
    }

    [Test]
    public void ExemploNumerico_ChampDeMars_ApoioMudaDe20_10_30Para40_5_50()
    {
        CaseData champ = Carregar<CaseData>("Assets/Casos/Caso_ChampDeMars.asset");
        ReceitaDeCaso receita = champ.receitaDoPanfleto;
        Item f1 = Carregar<Item>(Pasta + "Fase3/Pista_Champ_Peticao.asset");
        Item f2 = Carregar<Item>(Pasta + "Fase3/Pista_Champ_LeiMarcial.asset");
        Item ata = Carregar<Item>(Pasta + "Fase3/Apoio_ChampDeMars_Biblioteca.asset");
        Item deOutroCaso = Carregar<Item>(Pasta + "Fase3/Apoio_Padeiro_Biblioteca.asset");

        ResultadoDoPanfleto sem = CalculadoraDePanfleto.Calcular(receita, f1, f2, null);
        Assert.AreEqual(NivelDoPanfleto.Fatos, sem.nivel);
        AssertValores(sem, 20, -10, 30, "sem apoio");

        ResultadoDoPanfleto com = CalculadoraDePanfleto.Calcular(receita, f1, f2, new[] { ata });
        AssertValores(com, 40, -5, 50, "com o apoio exigido");
        CollectionAssert.AreEqual(new[] { "apoio_champdemars" }, com.qualificadores);

        ResultadoDoPanfleto repetido = CalculadoraDePanfleto.Calcular(receita, f1, f2, new[] { ata, ata, ata });
        AssertValores(repetido, 40, -5, 50, "o mesmo bônus não empilha");

        ResultadoDoPanfleto outro = CalculadoraDePanfleto.Calcular(receita, f1, f2, new[] { deOutroCaso });
        AssertValores(outro, 20, -10, 30, "documento de outro caso não vale");

        Assert.AreEqual(20, receita.soFatos.povo, "calcular não altera o asset compartilhado");
        Assert.AreEqual(30, receita.soFatos.ouro);
    }

    [Test]
    public void Qualidade_NaoMudaAVerdade_NemAVersao()
    {
        CaseData champ = Carregar<CaseData>("Assets/Casos/Caso_ChampDeMars.asset");
        Item fato = Carregar<Item>(Pasta + "Fase3/Pista_Champ_Peticao.asset");
        Item boato = Carregar<Item>(Pasta + "Fase3/Pista_Champ_Boato.asset");
        Item ata = Carregar<Item>(Pasta + "Fase3/Apoio_ChampDeMars_Biblioteca.asset");

        Assert.AreEqual(Confiabilidade.NaoEPista, ata.confiabilidade);
        Assert.IsFalse(ata.EhPista, "documento de apoio não é uma das duas pistas");
        ResultadoDoPanfleto sem = CalculadoraDePanfleto.Calcular(champ.receitaDoPanfleto, fato, boato, null);
        ResultadoDoPanfleto com = CalculadoraDePanfleto.Calcular(champ.receitaDoPanfleto, fato, boato, new[] { ata });
        Assert.AreEqual(NivelDoPanfleto.ComBoato, sem.nivel);
        Assert.AreEqual(sem.nivel, com.nivel, "o documento caro não transforma boato em fato");
        Assert.AreEqual(sem.penalidadePovo, com.penalidadePovo, "nem apaga a penalidade da revelação");
    }

    // ===== Compra =====

    private GameManager CriarGameManager(CaseData caso, int capital)
    {
        var go = new GameObject("GM_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        gm.faseAtual = caso.fase;
        gm.casoEscolhido = caso;
        gm.capitalAtual = capital;
        return gm;
    }

    private (CaseData caso, OfertaDaBiblioteca oferta) CriarOferta(int preco)
    {
        CaseData caso = ScriptableObject.CreateInstance<CaseData>();
        caso.name = "CasoTeste"; caso.fase = 3;
        Item doc = ScriptableObject.CreateInstance<Item>();
        doc.itemID = "doc_teste"; doc.caso = caso; doc.documentoDeSuporte = true; doc.qualidade = QualidadeDaEvidencia.Superior;
        OfertaDaBiblioteca oferta = ScriptableObject.CreateInstance<OfertaDaBiblioteca>();
        oferta.id = "oferta_teste"; oferta.caso = caso; oferta.item = doc; oferta.preco = preco; oferta.faseMinima = 3;
        criados.AddRange(new Object[] { caso, doc, oferta });
        return (caso, oferta);
    }

    [Test]
    public void Compra_SaldoExato_UmaVezSo_ERegistraEvidencia()
    {
        var (caso, oferta) = CriarOferta(25);
        GameManager gm = CriarGameManager(caso, 25);
        int entregas = 0;
        System.Func<Item, bool> entregar = i => { entregas++; criados.Add(i); return true; };

        Assert.AreEqual(GameManager.EstadoDaOferta.Disponivel, gm.EstadoDe(oferta));
        Assert.AreEqual(GameManager.ResultadoDaCompra.Comprada, gm.ComprarNaBiblioteca(oferta, entregar));
        Assert.AreEqual(0, gm.capitalAtual);
        Assert.AreEqual(GameManager.ResultadoDaCompra.JaComprada, gm.ComprarNaBiblioteca(oferta, entregar), "clique repetido");
        Assert.AreEqual(1, entregas, "entregue uma única vez");
        Assert.AreEqual(0, gm.capitalAtual, "cobrado uma única vez");
        Assert.AreEqual(GameManager.EstadoDaOferta.Comprada, gm.EstadoDe(oferta));
        CollectionAssert.Contains(gm.evidenciasObtidas, "doc_teste");
    }

    [Test]
    public void Compra_SaldoInsuficienteOuNegativo_NaoCobraNemEntrega()
    {
        var (caso, oferta) = CriarOferta(25);
        int entregas = 0;
        System.Func<Item, bool> entregar = i => { entregas++; return true; };

        GameManager pobre = CriarGameManager(caso, 24);
        Assert.AreEqual(GameManager.ResultadoDaCompra.SaldoInsuficiente, pobre.ComprarNaBiblioteca(oferta, entregar));
        Assert.AreEqual(24, pobre.capitalAtual);

        GameManager endividado = CriarGameManager(caso, -10);
        Assert.AreEqual(GameManager.ResultadoDaCompra.SaldoInsuficiente, endividado.ComprarNaBiblioteca(oferta, entregar));
        Assert.AreEqual(-10, endividado.capitalAtual, "a biblioteca não vende fiado");

        var (casoGratis, gratis) = CriarOferta(0);
        GameManager endividado2 = CriarGameManager(casoGratis, -1);
        Assert.AreEqual(GameManager.ResultadoDaCompra.SaldoInsuficiente, endividado2.ComprarNaBiblioteca(gratis, entregar), "dívida não autoriza compra");
        Assert.AreEqual(0, entregas);
        Assert.IsEmpty(pobre.comprasDaBiblioteca);
    }

    [Test]
    public void Compra_InventarioCheio_NaoCobra_EPermiteTentarDepois()
    {
        var (caso, oferta) = CriarOferta(10);
        GameManager gm = CriarGameManager(caso, 30);
        Assert.AreEqual(GameManager.ResultadoDaCompra.InventarioCheio, gm.ComprarNaBiblioteca(oferta, i => { criados.Add(i); return false; }));
        Assert.AreEqual(30, gm.capitalAtual);
        Assert.IsEmpty(gm.comprasDaBiblioteca);
        Assert.AreEqual(GameManager.ResultadoDaCompra.Comprada, gm.ComprarNaBiblioteca(oferta, i => { criados.Add(i); return true; }));
        Assert.AreEqual(20, gm.capitalAtual);
    }

    [Test]
    public void Compra_SoParaOCasoEmAndamento_EAPartirDaFase3()
    {
        var (caso, oferta) = CriarOferta(10);
        GameManager gm = CriarGameManager(caso, 50);
        gm.casoEscolhido = null;
        Assert.AreEqual(GameManager.EstadoDaOferta.CasoNaoAceito, gm.EstadoDe(oferta));
        Assert.AreEqual(GameManager.ResultadoDaCompra.Indisponivel, gm.ComprarNaBiblioteca(oferta, _ => true));

        gm.casoEscolhido = caso;
        gm.faseAtual = 2;
        Assert.AreEqual(GameManager.EstadoDaOferta.FaseBloqueada, gm.EstadoDe(oferta));
        gm.faseAtual = 3;
        gm.casosConcluidos.Add(caso);
        Assert.AreEqual(GameManager.EstadoDaOferta.CasoConcluido, gm.EstadoDe(oferta), "caso concluído não compra mais");
        Assert.AreEqual(50, gm.capitalAtual);

        CaseData outro = ScriptableObject.CreateInstance<CaseData>();
        outro.name = "OutroCaso"; outro.fase = 3;
        criados.Add(outro);
        gm.casosConcluidos.Clear();
        gm.casoEscolhido = outro;
        Assert.AreEqual(GameManager.EstadoDaOferta.OutroCasoEmAndamento, gm.EstadoDe(oferta));
    }

    [Test]
    public void Oferta_ApoioEquivalenteJaObtido_NaoEhVendida()
    {
        // Varennes: o documento da biblioteca e o recibo do Cocheiro ativam o mesmo reforço.
        CaseData varennes = Carregar<CaseData>("Assets/Casos/Caso_Varennes.asset");
        OfertaDaBiblioteca oferta = Carregar<OfertaDaBiblioteca>(Pasta + "Biblioteca/Oferta_Varennes.asset");
        Item recibo = Carregar<Item>(Pasta + "Fase3/Apoio_Varennes_Recibo.asset");
        GameManager gm = CriarGameManager(varennes, 100);

        Assert.AreEqual(GameManager.EstadoDaOferta.Disponivel, gm.EstadoDe(oferta));
        gm.RegistrarEvidencia(recibo);
        Assert.AreEqual(GameManager.EstadoDaOferta.ApoioEquivalente, gm.EstadoDe(oferta));
        Assert.AreEqual(GameManager.ResultadoDaCompra.Indisponivel, gm.ComprarNaBiblioteca(oferta, i => { criados.Add(i); return true; }));
        Assert.AreEqual(100, gm.capitalAtual, "nada cobrado por um reforço que não mudaria nada");
    }

    [Test]
    public void Qualificadores_NaoDependemDaOrdemDeSelecao_EPreviaContaReforcos()
    {
        CaseData caso = ScriptableObject.CreateInstance<CaseData>();
        ReceitaDeCaso receita = ScriptableObject.CreateInstance<ReceitaDeCaso>();
        receita.caso = caso;
        Item Doc(string id) { var d = ScriptableObject.CreateInstance<Item>(); d.itemID = id; d.caso = caso; d.documentoDeSuporte = true; d.qualidade = QualidadeDaEvidencia.Superior; criados.Add(d); return d; }
        Item Pista(string id) { var p = ScriptableObject.CreateInstance<Item>(); p.itemID = id; p.caso = caso; p.confiabilidade = Confiabilidade.Fato; criados.Add(p); return p; }
        Item a = Doc("a"), b = Doc("b");
        // Q1 aceita A ou B; Q2 só A. Guloso na ordem (A, B) daria 1 reforço; o certo são 2 (Q1←B, Q2←A).
        receita.qualificadores.Add(new ReceitaDeCaso.Qualificador { id = "q1", suportesAceitos = new List<Item> { a, b }, povo = 1 });
        receita.qualificadores.Add(new ReceitaDeCaso.Qualificador { id = "q2", suportesAceitos = new List<Item> { a }, povo = 10 });
        criados.AddRange(new Object[] { caso, receita });

        ResultadoDoPanfleto ab = CalculadoraDePanfleto.Calcular(receita, Pista("p1"), Pista("p2"), new[] { a, b });
        ResultadoDoPanfleto ba = CalculadoraDePanfleto.Calcular(receita, Pista("p3"), Pista("p4"), new[] { b, a });
        Assert.AreEqual(11, ab.povo);
        Assert.AreEqual(ab.povo, ba.povo, "a ordem da seleção não muda o resultado");
        Assert.AreEqual(2, CalculadoraDePanfleto.ReforcosPossiveis(receita, new[] { b, a }));
        Assert.AreEqual(1, CalculadoraDePanfleto.ReforcosPossiveis(receita, new[] { b, b }), "o mesmo documento não conta duas vezes");
    }

    // ===== Save v4 =====

    [Test]
    public void Save_V4_PreservaCompras_Evidencias_EHistoricoDaPublicacao()
    {
        CaseData champ = Carregar<CaseData>("Assets/Casos/Caso_ChampDeMars.asset");
        var catalogo = Carregar<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        GameManager origem = CriarGameManager(champ, -5);
        origem.comprasDaBiblioteca.Add("biblioteca_champ|Caso_ChampDeMars");
        origem.evidenciasObtidas.Add("apoio_biblioteca_champ");
        var resultado = new ResultadoDoPanfleto { nivel = NivelDoPanfleto.Fatos, povo = 40, estado = -5, ouro = 50 };
        resultado.pistas.AddRange(new[] { "pista_champ_1", "pista_champ_2" });
        resultado.suportes.Add("apoio_biblioteca_champ");
        resultado.qualificadores.Add("apoio_champdemars");
        origem.RegistrarPanfletoDeCaso(champ, resultado);

        string json = JsonUtility.ToJson(SistemaDeSave.CapturarDados(origem, "Porao", 14));
        GameManager destino = CriarGameManager(champ, 0);
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(json), destino, catalogo);

        Assert.AreEqual(-5, destino.capitalAtual);
        CollectionAssert.AreEqual(origem.comprasDaBiblioteca, destino.comprasDaBiblioteca);
        CollectionAssert.AreEqual(origem.evidenciasObtidas, destino.evidenciasObtidas);
        GameManager.PanfletoPublicado p = destino.panfletosPublicados[0];
        Assert.IsFalse(p.legado);
        Assert.AreEqual(40, p.povo); Assert.AreEqual(-5, p.estado); Assert.AreEqual(50, p.ouro);
        CollectionAssert.AreEqual(resultado.pistas, p.pistas);
        CollectionAssert.AreEqual(resultado.suportes, p.suportes);
        CollectionAssert.AreEqual(resultado.qualificadores, p.qualificadores);
    }

    [Test]
    public void Save_V3_PublicacoesViramLegado_SemValoresInventados()
    {
        var catalogo = Carregar<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        const string jsonV3 = @"{ ""versao"": 3, ""capital"": 12, ""faseAtual"": 3, ""casoEscolhido"": ""Caso_Joalheiro"",
            ""casosConcluidos"": [""Caso_Joalheiro""], ""casosJaSelecionados"": [""Caso_Joalheiro""],
            ""panfletosPublicados"": [{ ""caso"": ""Caso_Joalheiro"", ""nivel"": 1 }],
            ""inventario"": [{ ""itemID"": ""pista_joias_2"", ""quantidade"": 1 }] }";
        var go = new GameObject("GM"); criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonV3), gm, catalogo);
        criados.AddRange(gm.inventarioSalvo);

        Assert.AreEqual(12, gm.capitalAtual);
        Assert.IsTrue(gm.panfletosPublicados[0].legado);
        Assert.AreEqual(NivelDoPanfleto.ComBoato, gm.panfletosPublicados[0].nivel);
        Assert.IsEmpty(gm.panfletosPublicados[0].pistas);
        Assert.AreEqual(0, gm.panfletosPublicados[0].povo);
        CollectionAssert.AreEqual(new[] { "pista_joias_2" }, gm.evidenciasObtidas, "só o que estava no inventário conta como obtido");
        Assert.IsEmpty(gm.comprasDaBiblioteca);
    }
}
