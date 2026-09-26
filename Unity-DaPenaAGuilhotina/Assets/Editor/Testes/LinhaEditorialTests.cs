using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prompt 5: linha editorial. Cálculo único versão → apoio → linha, sensacionalista agravando só a consequência que
/// já existe, compatibilidade neutra, histórico/revelação e save v6. A prensa com a janela de escolha está em
/// ComponentesTests.
/// </summary>
public class LinhaEditorialTests
{
    private readonly List<Object> criados = new List<Object>();

    [TearDown]
    public void Limpar()
    {
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private T Criar<T>(string nome) where T : ScriptableObject
    {
        T o = ScriptableObject.CreateInstance<T>();
        o.name = nome;
        criados.Add(o);
        return o;
    }

    private CaseData caso;
    private ReceitaDeCaso receita;
    private Item fato1, fato2, boato, calunia, alegacao1, alegacao2;

    private Item Pista(string id, Confiabilidade c)
    {
        Item i = Criar<Item>(id);
        i.itemID = id; i.caso = caso; i.confiabilidade = c; i.itemAmt = 1;
        return i;
    }

    [SetUp]
    public void Montar()
    {
        caso = Criar<CaseData>("Caso_Linha");
        caso.caseTitle = "Caso da Linha"; caso.fase = 2;
        receita = Criar<ReceitaDeCaso>("Receita_Linha");
        receita.caso = caso;
        receita.panfletoPadrao = Pista("panfleto_linha", Confiabilidade.NaoEPista);
        receita.panfletoPadrao.caso = null;
        receita.soFatos = new ReceitaDeCaso.Versao { povo = 20, estado = -10, ouro = 30 };
        receita.comBoato = new ReceitaDeCaso.Versao { povo = 30, estado = -15, ouro = 40, penalidadePovo = -10, penalidadeEstado = -5, textoRevelacao = "Boato exposto." };
        receita.calunia = new ReceitaDeCaso.Versao { povo = 40, estado = -20, ouro = 55, penalidadePovo = -20, penalidadeEstado = -10, textoRevelacao = "Calúnia exposta." };
        receita.soAlegacoes = new ReceitaDeCaso.Versao { povo = 8, estado = -4, ouro = 10, penalidadePovo = -5, textoRevelacao = "Alegações desmentidas." };
        receita.linhaEditorial = new ReceitaDeCaso.ConfiguracaoEditorial
        {
            ativa = true,
            defesaDoPovo = new ReceitaDeCaso.ModificadorEditorial { povo = 10, estado = -5 },
            agradarOPoder = new ReceitaDeCaso.ModificadorEditorial { povo = -5, estado = 10 },
            sensacionalista = new ReceitaDeCaso.ModificadorEditorial { ouro = 15 },
            agravamentoPercentual = 50
        };
        caso.receitaDoPanfleto = receita;

        fato1 = Pista("fato1", Confiabilidade.Fato);
        fato2 = Pista("fato2", Confiabilidade.Fato);
        boato = Pista("boato", Confiabilidade.Boato);
        calunia = Pista("calunia", Confiabilidade.Calunia);
        alegacao1 = Pista("alegacao1", Confiabilidade.Boato);
        alegacao2 = Pista("alegacao2", Confiabilidade.Boato);
        caso.alegacoesIniciais = new List<Item> { alegacao1, alegacao2 };
    }

    private ResultadoDoPanfleto Calcular(Item a, Item b, LinhaEditorial linha) =>
        CalculadoraDePanfleto.Calcular(receita, a, b, null, linha);

    // ===== Cálculo =====

    [Test]
    public void TresTons_NasQuatroVersoes_SomamOModificador_ENaoMudamAVersao()
    {
        var pares = new[]
        {
            (a: fato1, b: fato2, nivel: NivelDoPanfleto.Fatos),
            (a: fato1, b: boato, nivel: NivelDoPanfleto.ComBoato),
            (a: fato1, b: calunia, nivel: NivelDoPanfleto.Calunia),
            (a: alegacao1, b: alegacao2, nivel: NivelDoPanfleto.Alegacoes),
        };
        foreach (var par in pares)
        {
            ReceitaDeCaso.Versao versao = receita.VersaoDe(par.nivel);
            foreach (LinhaEditorial linha in LinhasEditoriais.Opcoes)
            {
                ReceitaDeCaso.ModificadorEditorial mod = receita.linhaEditorial.De(linha);
                ResultadoDoPanfleto r = Calcular(par.a, par.b, linha);
                string msg = $"{par.nivel} {linha}";
                Assert.AreEqual(par.nivel, r.nivel, msg + ": o tom não muda a verdade");
                Assert.AreEqual(linha, r.linha, msg);
                Assert.AreEqual(versao.povo + mod.povo, r.povo, msg + " (povo)");
                Assert.AreEqual(versao.estado + mod.estado, r.estado, msg + " (estado)");
                Assert.AreEqual(versao.ouro + mod.ouro, r.ouro, msg + " (ouro)");
                Assert.AreEqual(mod.povo, r.editorialPovo, msg);
                Assert.AreEqual(mod.estado, r.editorialEstado, msg);
                Assert.AreEqual(mod.ouro, r.editorialOuro, msg);
                Assert.AreSame(receita.PanfletoDe(versao), r.panfleto, msg);
            }
        }
    }

    [Test]
    public void Sensacionalista_AgravaSoAConsequenciaExistente_FatoNaoGanhaPenalidadeInventada()
    {
        ResultadoDoPanfleto fatos = Calcular(fato1, fato2, LinhaEditorial.Sensacionalista);
        Assert.AreEqual(0, fatos.penalidadePovo);
        Assert.AreEqual(0, fatos.penalidadeEstado);
        Assert.AreEqual(0, fatos.agravamentoPovo);
        Assert.IsFalse(fatos.TemRevelacao, "fato sensacionalista não vira mentira");

        ResultadoDoPanfleto comBoato = Calcular(fato1, boato, LinhaEditorial.Sensacionalista);
        Assert.AreEqual(-15, comBoato.penalidadePovo, "-10 + 50%");
        Assert.AreEqual(-8, comBoato.penalidadeEstado, "-5 + 50% = -7,5, arredondado para longe de zero");
        Assert.AreEqual(-5, comBoato.agravamentoPovo);
        Assert.AreEqual(-3, comBoato.agravamentoEstado);

        ResultadoDoPanfleto cal = Calcular(fato1, calunia, LinhaEditorial.Sensacionalista);
        Assert.AreEqual(-30, cal.penalidadePovo);
        Assert.AreEqual(-15, cal.penalidadeEstado);

        ResultadoDoPanfleto alegacoes = Calcular(alegacao1, alegacao2, LinhaEditorial.Sensacionalista);
        Assert.AreEqual(-8, alegacoes.penalidadePovo, "alegação não verificada também é boato");
        Assert.AreEqual(0, alegacoes.penalidadeEstado, "o que não tinha perda continua sem perda");

        foreach (LinhaEditorial outra in new[] { LinhaEditorial.DefesaDoPovo, LinhaEditorial.AgradarOPoder })
        {
            ResultadoDoPanfleto r = Calcular(fato1, boato, outra);
            Assert.AreEqual(-10, r.penalidadePovo, outra + ": só o sensacionalista agrava");
            Assert.AreEqual(-5, r.penalidadeEstado, outra.ToString());
            Assert.AreEqual(0, r.agravamentoPovo, outra.ToString());
        }

        Assert.AreEqual(-10, receita.comBoato.penalidadePovo, "o asset Versao compartilhado não é alterado");
        Assert.AreEqual(40, receita.comBoato.ouro);
    }

    [Test]
    public void CasoNovoExigeEscolha_ReceitaAntigaPublicaNeutra()
    {
        Assert.IsNull(Calcular(fato1, fato2, LinhaEditorial.Neutra), "caso com linha editorial não publica sem a escolha");

        receita.linhaEditorial = new ReceitaDeCaso.ConfiguracaoEditorial(); // como um asset anterior ao Prompt 5
        Assert.IsTrue(receita.linhaEditorial.Vazia);
        ResultadoDoPanfleto neutro = Calcular(fato1, boato, LinhaEditorial.Sensacionalista);
        Assert.AreEqual(LinhaEditorial.Neutra, neutro.linha, "sem linha configurada a escolha é ignorada");
        Assert.AreEqual(30, neutro.povo);
        Assert.AreEqual(40, neutro.ouro);
        Assert.AreEqual(-10, neutro.penalidadePovo);
        Assert.IsNotNull(Calcular(fato1, fato2, LinhaEditorial.Neutra));
    }

    [Test]
    public void RotuloDaOpcao2_AcompanhaAFase_EARegraNaoMuda()
    {
        caso.fase = 2;
        Assert.AreEqual("Agradar a Coroa", LinhasEditoriais.Rotulo(LinhaEditorial.AgradarOPoder, receita));
        caso.fase = 4;
        Assert.AreEqual("Agradar o Comitê", LinhasEditoriais.Rotulo(LinhaEditorial.AgradarOPoder, receita));
        receita.linhaEditorial.rotuloAgradarOPoder = "Agradar a Assembleia";
        Assert.AreEqual("Agradar a Assembleia", LinhasEditoriais.Rotulo(LinhaEditorial.AgradarOPoder, receita));
        Assert.AreEqual(-5, Calcular(fato1, fato2, LinhaEditorial.AgradarOPoder).editorialPovo, "o rótulo não muda o modificador");
    }

    [Test]
    public void ComSuporteSuperior_ChampDeMars_VersaoDepoisApoioDepoisLinha()
    {
        const string pasta = "Assets/Scriptableobjects/Campanha/Fase3/";
        CaseData champ = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_ChampDeMars.asset");
        ReceitaDeCaso r = champ.receitaDoPanfleto;
        Item f1 = AssetDatabase.LoadAssetAtPath<Item>(pasta + "Pista_Champ_Peticao.asset");
        Item f2 = AssetDatabase.LoadAssetAtPath<Item>(pasta + "Pista_Champ_LeiMarcial.asset");
        Item b = AssetDatabase.LoadAssetAtPath<Item>(pasta + "Pista_Champ_Boato.asset");
        Item ata = AssetDatabase.LoadAssetAtPath<Item>(pasta + "Apoio_ChampDeMars_Biblioteca.asset");
        Assert.IsTrue(r.ExigeLinhaEditorial, "rode Ferramentas > Campanha > 4 - Aplicar linha editorial");

        foreach (LinhaEditorial linha in LinhasEditoriais.Opcoes)
        {
            ReceitaDeCaso.ModificadorEditorial mod = r.linhaEditorial.De(linha);
            ResultadoDoPanfleto com = CalculadoraDePanfleto.Calcular(r, f1, f2, new[] { ata, ata }, linha);
            Assert.AreEqual(40 + mod.povo, com.povo, linha + ": exemplo do Prompt 3 (40/-5/50) + linha");
            Assert.AreEqual(-5 + mod.estado, com.estado, linha.ToString());
            Assert.AreEqual(50 + mod.ouro, com.ouro, linha.ToString());
            CollectionAssert.AreEqual(new[] { "apoio_champdemars" }, com.qualificadores, "o apoio vale uma vez");

            ResultadoDoPanfleto comBoato = CalculadoraDePanfleto.Calcular(r, f1, b, new[] { ata }, linha);
            Assert.AreEqual(NivelDoPanfleto.ComBoato, comBoato.nivel, linha + ": nem apoio caro nem tom transformam boato em fato");
        }
    }

    // ===== Histórico, revelação e fim de fase =====

    private GameManager CriarGameManager(int fase)
    {
        var go = new GameObject("GM_Linha");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        gm.faseAtual = fase;
        return gm;
    }

    [Test]
    public void Publicacao_GuardaTomEAgravamento_RevelacaoAgravadaAplicadaUmaVezNoFimDaFase()
    {
        GameManager gm = CriarGameManager(2);
        gm.capitalAtual = 40;
        ResultadoDoPanfleto r = Calcular(fato1, boato, LinhaEditorial.Sensacionalista);
        gm.RegistrarPanfletoDeCaso(caso, r, gm.AplicarImpactoPanfleto(r.povo, r.estado, r.ouro));

        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.AreEqual(LinhaEditorial.Sensacionalista, p.linha);
        Assert.AreEqual(NivelDoPanfleto.ComBoato, p.nivel);
        CollectionAssert.AreEqual(new[] { "fato1", "boato" }, p.pistas);
        Assert.AreEqual(55, p.ouro); Assert.AreEqual(15, p.editorialOuro);
        Assert.AreEqual(-15, p.penalidadePovo); Assert.AreEqual(-5, p.agravamentoPovo);
        Assert.AreEqual(-8, p.penalidadeEstado); Assert.AreEqual(-3, p.agravamentoEstado);
        Assert.AreEqual(30, p.povoAplicado, "50 → 80");
        Assert.AreEqual(95, gm.capitalAtual);

        Assert.AreEqual(1, gm.revelacoesPendentes.Count);
        Assert.AreEqual(-15, gm.revelacoesPendentes[0].povo);
        Assert.AreEqual(LinhaEditorial.Sensacionalista, gm.revelacoesPendentes[0].linha);

        // Fato sensacionalista de outro caso: sem revelação.
        CaseData outro = Criar<CaseData>("Caso_Outro"); outro.fase = 2;
        gm.RegistrarPanfletoDeCaso(outro, Calcular(fato1, fato2, LinhaEditorial.Sensacionalista));
        Assert.AreEqual(1, gm.revelacoesPendentes.Count, "fato sensacionalista não agenda penalidade de mentira");

        GameManager.EncerramentoDeFase fim = gm.EncerrarFase();
        Assert.AreEqual(1, fim.revelacoes.Count);
        Assert.AreEqual(65, gm.opiniaoPublicaAtual, "80 - 15 (penalidade agravada)");
        Assert.AreEqual(27, gm.opiniaoEstadoAtual, "35 - 8");
        Assert.AreEqual(0, gm.EncerrarFase().revelacoes.Count, "a revelação não se repete");
        Assert.AreEqual(65, gm.opiniaoPublicaAtual);
    }

    // ===== Save v6 =====

    [Test]
    public void Save_V6_PreservaTomDaPublicacaoEDaRevelacao()
    {
        CatalogoDeSave catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        CaseData joias = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_Joalheiro.asset");
        GameManager origem = CriarGameManager(2);
        // Valores da receita de teste; o save guarda o caso pelo nome do asset, então a publicação vai em um caso do catálogo.
        ResultadoDoPanfleto r = Calcular(fato1, calunia, LinhaEditorial.Sensacionalista);
        origem.RegistrarPanfletoDeCaso(joias, r, new Vector3Int(40, -20, 70));

        string json = JsonUtility.ToJson(SistemaDeSave.CapturarDados(origem, "Porao", 14));
        GameManager destino = CriarGameManager(2);
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(json), destino, catalogo);

        GameManager.PanfletoPublicado p = destino.panfletosPublicados[0];
        Assert.AreEqual(joias, p.caso);
        Assert.AreEqual(LinhaEditorial.Sensacionalista, p.linha);
        Assert.AreEqual(15, p.editorialOuro);
        Assert.AreEqual(-30, p.penalidadePovo);
        Assert.AreEqual(-10, p.agravamentoPovo);
        Assert.AreEqual(-5, p.agravamentoEstado);
        Assert.AreEqual(LinhaEditorial.Sensacionalista, destino.revelacoesPendentes[0].linha);
        Assert.AreEqual(-30, destino.revelacoesPendentes[0].povo);
    }

    [Test]
    public void Save_V5_SemTom_VirouPublicacaoLegadaNeutra()
    {
        CatalogoDeSave catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        // Um v5 não tem "linha"; o campo estranho abaixo prova que nada de tom é aceito de um save anterior ao 6.
        const string jsonV5 = @"{ ""versao"": 5, ""capital"": 20, ""faseAtual"": 3, ""opiniaoPublica"": 70, ""opiniaoEstado"": 40,
            ""casosConcluidos"": [""Caso_Joalheiro""], ""casosJaSelecionados"": [""Caso_Joalheiro""],
            ""panfletosPublicados"": [{ ""caso"": ""Caso_Joalheiro"", ""nivel"": 1, ""pistas"": [""pista_joias_1"", ""pista_joias_boato""],
                ""povo"": 70, ""estado"": -20, ""ouro"": 35, ""penalidadePovo"": -30, ""penalidadeEstado"": -5, ""linha"": 3, ""agravamentoPovo"": -9,
                ""aplicadoConhecido"": true, ""povoAplicado"": 20, ""estadoAplicado"": -20, ""ouroAplicado"": 35 }],
            ""revelacoesPendentes"": [{ ""caso"": ""Caso_Joalheiro"", ""texto"": ""x"", ""povo"": -30, ""estado"": -5 }] }";
        GameManager gm = CriarGameManager(1);
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonV5), gm, catalogo);

        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.IsFalse(p.legado, "v5 tem os valores da publicação");
        Assert.AreEqual(LinhaEditorial.Neutra, p.linha, "sem tom: publicação neutra");
        Assert.AreEqual(0, p.agravamentoPovo);
        Assert.AreEqual(-30, p.penalidadePovo, "nada recalculado");
        Assert.IsTrue(p.aplicadoConhecido);
        Assert.AreEqual(LinhaEditorial.Neutra, gm.revelacoesPendentes[0].linha);
        Assert.AreEqual(-30, gm.revelacoesPendentes[0].povo);
        Assert.AreEqual(70, gm.opiniaoPublicaAtual);
    }
}
