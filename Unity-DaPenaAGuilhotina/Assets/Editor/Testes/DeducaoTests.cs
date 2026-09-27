using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prompt 4: dedução ativa. Regras do quadro (Deducao) sobre um caso montado aqui, o inventário real para a revelação
/// automática, o save v7 e os seis casos reais das Fases 3 e 4.
/// </summary>
public class DeducaoTests
{
    private readonly List<Object> criados = new List<Object>();
    private GameManager gm;
    private CaseData caso;
    private Item f1, boato, f2, calunia, alegacao1, alegacao2;

    private T Criar<T>(string nome) where T : ScriptableObject
    {
        T o = ScriptableObject.CreateInstance<T>();
        o.name = nome;
        criados.Add(o);
        return o;
    }

    private GameObject Go(string nome, Transform pai = null)
    {
        var go = new GameObject(nome);
        if (pai != null) go.transform.SetParent(pai, false);
        else criados.Add(go);
        return go;
    }

    private Item Pista(string id, CaseData dono, Confiabilidade conf, string nome = null)
    {
        Item i = Criar<Item>(id);
        i.itemID = id; i.caso = dono; i.confiabilidade = conf; i.itemAmt = 1; i.itemName = nome ?? id;
        return i;
    }

    private CaseData CasoComDeducao(string nome, out Item fato1, out Item b, out Item fato2, out Item c)
    {
        CaseData novo = Criar<CaseData>(nome);
        novo.caseTitle = nome; novo.fase = 3;
        fato1 = Pista(nome + "_f1", novo, Confiabilidade.Fato);
        b = Pista(nome + "_boato", novo, Confiabilidade.Boato);
        fato2 = Pista(nome + "_f2", novo, Confiabilidade.Fato);
        c = Pista(nome + "_calunia", novo, Confiabilidade.Calunia);
        novo.deducao = new ConfiguracaoDeDeducao
        {
            ativa = true,
            pares = new List<ParContraditorio> { new ParContraditorio { a = fato1, b = b }, new ParContraditorio { a = fato2, b = c } }
        };
        return novo;
    }

    private static void DefinirInstancia(GameManager valor) =>
        typeof(GameManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new object[] { valor });

    [SetUp]
    public void Montar()
    {
        gm = Go("GM").AddComponent<GameManager>();
        DefinirInstancia(gm);
        gm.faseAtual = 3;
        gm.casoEscolhido = null;
        gm.casosPorFase = new[] { 1, 1, 3, 1 };

        caso = CasoComDeducao("CasoDeducao", out f1, out boato, out f2, out calunia);
        alegacao1 = Pista("aleg1", caso, Confiabilidade.Boato);
        alegacao2 = Pista("aleg2", caso, Confiabilidade.Boato);
        caso.alegacoesIniciais = new List<Item> { alegacao1, alegacao2 };
    }

    [TearDown]
    public void Desmontar()
    {
        DefinirInstancia(null);
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private void Descobrir(params Item[] pistas)
    {
        foreach (Item p in pistas) gm.RegistrarEvidencia(p);
    }

    private void Marcar(Item pista, MarcacaoDaPista marca) => Assert.IsTrue(Deducao.Marcar(gm, caso, pista, marca), pista.name);

    private void MarcarTudoCerto()
    {
        Marcar(f1, MarcacaoDaPista.Confiavel);
        Marcar(boato, MarcacaoDaPista.Duvidosa);
        Marcar(f2, MarcacaoDaPista.Confiavel);
        Marcar(calunia, MarcacaoDaPista.Duvidosa);
    }

    private InventoryManager InventarioReal()
    {
        GameObject grade = Go("Grade");
        for (int i = 0; i < 10; i++)
        {
            GameObject s = Go("Slot" + i, grade.transform);
            UISlotHandler slot = s.AddComponent<UISlotHandler>();
            slot.slotImg = Go("Img", s.transform).AddComponent<Image>();
            slot.itemCount = Go("Qtd", s.transform).AddComponent<TextMeshProUGUI>();
        }
        InventoryManager inv = Go("Inventario").AddComponent<InventoryManager>();
        inv.inventoryGrid = grade;
        gm.inventoryManager = inv;
        return inv;
    }

    // ===== Configuração =====

    [Test]
    public void Configuracao_ParesCoerentes_SemProblemas_EIncoerenciasSaoAcusadas()
    {
        CollectionAssert.IsEmpty(Deducao.Problemas(caso));
        Assert.IsTrue(Deducao.Aderente(caso));
        CollectionAssert.AreEquivalent(new[] { f1, boato, f2, calunia }, caso.deducao.Conjunto(), "alegações ficam fora do conjunto");

        CaseData outro = CasoComDeducao("Outro", out Item of1, out Item ob, out _, out _);
        caso.deducao.pares.Add(new ParContraditorio { a = f1, b = f2 });            // dois fatos, pistas repetidas
        caso.deducao.pares.Add(new ParContraditorio { a = alegacao1, b = of1 });    // alegação e pista de outro caso
        caso.deducao.pares.Add(new ParContraditorio { a = ob, b = null });          // incompleto
        List<string> problemas = Deducao.Problemas(caso);
        Assert.IsTrue(problemas.Exists(p => p.Contains("exatamente um Fato")), string.Join("\n", problemas));
        Assert.IsTrue(problemas.Exists(p => p.Contains("mais de um par")), string.Join("\n", problemas));
        Assert.IsTrue(problemas.Exists(p => p.Contains("alegação")), string.Join("\n", problemas));
        Assert.IsTrue(problemas.Exists(p => p.Contains("outro caso")), string.Join("\n", problemas));
        Assert.IsTrue(problemas.Exists(p => p.Contains("incompleto")), string.Join("\n", problemas));
        Assert.IsNotNull(outro);
    }

    // ===== Aquisição e revelação automática =====

    [Test]
    public void Aquisicao_NaoRevelaOGabarito_VerificadaPorEVerificacoesAntigasNaoContam_LegadoContinua()
    {
        InventoryManager inv = InventarioReal();
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        boato.verificadaPor = new List<Item> { f1 }; // na regra antiga, ter f1 revelaria o boato
        gm.pistasVerificadas.Add(f2.itemID);       // verificação antiga (save anterior) de uma pista do caso

        inv.AddItem(boato);
        inv.AddItem(f1);
        inv.AddItem(f2);

        Assert.IsFalse(inv.PistaVerificada(boato), "com dedução ativa, Verificada Por não revela nada");
        Assert.IsFalse(inv.PistaVerificada(f2), "verificação antiga não vira dedução confirmada");
        CollectionAssert.DoesNotContain(gm.pistasVerificadas, boato.itemID, "nada é registrado automaticamente");
        StringAssert.Contains("não verificada", inv.RotuloDaPista(boato));
        StringAssert.DoesNotContain("boato", inv.RotuloDaPista(boato).Replace("não verificada", string.Empty));

        // Caso sem dedução configurada (desde o Prompt 7 nenhum caso da campanha, mas a regra continua para conteúdo
        // antigo ou novo ainda sem pares): vale a regra antiga.
        CaseData antigo = Criar<CaseData>("CasoAntigo");
        antigo.fase = 2;
        Item fatoAntigo = Pista("fato_antigo", antigo, Confiabilidade.Fato);
        Item boatoAntigo = Pista("boato_antigo", antigo, Confiabilidade.Boato);
        boatoAntigo.verificadaPor = new List<Item> { fatoAntigo };
        inv.AddItem(boatoAntigo);
        inv.AddItem(fatoAntigo);
        Assert.IsTrue(inv.PistaVerificada(boatoAntigo), "caso não aderente: comportamento legado preservado");
        StringAssert.Contains("boato", inv.RotuloDaPista(boatoAntigo));
    }

    // ===== Conferência =====

    [Test]
    public void Conferir_Incompleta_Parcial_OuErrada_RecebemAMesmaRespostaGenerica()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2);
        Marcar(f1, MarcacaoDaPista.Confiavel);
        Marcar(boato, MarcacaoDaPista.Duvidosa);
        Marcar(f2, MarcacaoDaPista.Confiavel);
        Assert.AreEqual(Deducao.Resultado.NaoSeSustenta, Deducao.Conferir(gm, caso), "falta descobrir uma pista");

        Descobrir(calunia);
        Assert.AreEqual(Deducao.Resultado.NaoSeSustenta, Deducao.Conferir(gm, caso), "falta uma marcação");

        Marcar(calunia, MarcacaoDaPista.Confiavel);
        Assert.AreEqual(Deducao.Resultado.NaoSeSustenta, Deducao.Conferir(gm, caso), "uma marcação errada");

        Marcar(calunia, MarcacaoDaPista.Duvidosa);
        Marcar(f1, MarcacaoDaPista.Duvidosa);
        Assert.AreEqual(Deducao.Resultado.NaoSeSustenta, Deducao.Conferir(gm, caso), "outra marcação errada: mesma resposta");
        Assert.IsFalse(Deducao.Confirmada(gm, caso));
        CollectionAssert.IsEmpty(gm.deducoesConfirmadas);
    }

    [Test]
    public void Conferir_ConjuntoCorreto_Confirma_MostraAVerdade_ETravaAsMarcacoes()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        MarcarTudoCerto();
        StringAssert.Contains("hipótese: confiável", Deducao.Rotulo(gm, f1), "antes de conferir, só a hipótese");

        Assert.AreEqual(Deducao.Resultado.Confirmada, Deducao.Conferir(gm, caso));
        Assert.IsTrue(Deducao.Confirmada(gm, caso));
        StringAssert.Contains("confirmada", Deducao.Rotulo(gm, f1));
        StringAssert.Contains("desmentida", Deducao.Rotulo(gm, boato));
        StringAssert.Contains("desmentida", Deducao.Rotulo(gm, calunia));
        Assert.IsFalse(Deducao.Marcar(gm, caso, f1, MarcacaoDaPista.Duvidosa), "confirmada, o quadro não muda mais");
        Assert.AreEqual(Deducao.Resultado.JaConfirmada, Deducao.Conferir(gm, caso));
    }

    [Test]
    public void Marcar_NaoMudaAVerdade_NemAVersaoImpressa()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        Marcar(f1, MarcacaoDaPista.Duvidosa);
        Marcar(boato, MarcacaoDaPista.Confiavel);

        Assert.AreEqual(Confiabilidade.Fato, f1.confiabilidade);
        Assert.AreEqual(Confiabilidade.Boato, boato.confiabilidade);
        Assert.AreEqual(NivelDoPanfleto.Fatos, ReceitaDeCaso.Classificar(f1, f2), "a versão sai da verdade, não da hipótese");
        Assert.AreEqual(NivelDoPanfleto.ComBoato, ReceitaDeCaso.Classificar(boato, f2));
    }

    [Test]
    public void Marcar_SoNoCasoEmAndamento_ESoPistasDescobertasDoConjunto()
    {
        Descobrir(f1);
        Assert.IsFalse(Deducao.Marcar(gm, caso, f1, MarcacaoDaPista.Confiavel), "sem caso em andamento");
        Assert.AreEqual(Deducao.Resultado.Indisponivel, Deducao.Conferir(gm, caso));

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Assert.IsFalse(Deducao.Marcar(gm, caso, boato, MarcacaoDaPista.Duvidosa), "pista ainda não descoberta");
        Descobrir(alegacao1);
        Assert.IsFalse(Deducao.Marcar(gm, caso, alegacao1, MarcacaoDaPista.Duvidosa), "alegação fica fora do quadro");
        Assert.IsTrue(Deducao.Marcar(gm, caso, f1, MarcacaoDaPista.Confiavel));
        Assert.IsTrue(Deducao.Marcar(gm, caso, f1, MarcacaoDaPista.NaoMarcada), "tirar a marca");
        Assert.AreEqual(MarcacaoDaPista.NaoMarcada, Deducao.MarcacaoDe(gm, caso, f1));

        gm.ConcluirCaso(caso);
        Assert.IsFalse(Deducao.Marcar(gm, caso, f1, MarcacaoDaPista.Confiavel), "caso concluído");
        Assert.AreEqual(Deducao.Resultado.Indisponivel, Deducao.Conferir(gm, caso));
    }

    [Test]
    public void Quadro_ContradicaoSoComAsDuasPistas_EOrdemNaoDenunciaAVerdade()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(boato);
        Assert.IsNull(Deducao.Contradicao(gm, boato), "a outra afirmação ainda não foi encontrada");
        Assert.AreEqual(0, Deducao.ContradicoesEncontradas(gm, caso));
        Assert.AreEqual(2, Deducao.TotalDeContradicoes(caso));
        CollectionAssert.AreEqual(new[] { boato }, Deducao.PistasConhecidas(gm, caso), "só o que foi descoberto aparece");

        Descobrir(calunia, f1);
        Assert.AreSame(boato, Deducao.Contradicao(gm, f1));
        Assert.AreSame(f1, Deducao.Contradicao(gm, boato));
        Assert.IsNull(Deducao.Contradicao(gm, calunia));
        Assert.AreEqual(1, Deducao.ContradicoesEncontradas(gm, caso));
        // No par, f1 é "a" e o boato é "b"; o quadro segue a ordem em que o jogador achou as pistas.
        CollectionAssert.AreEqual(new[] { boato, f1, calunia }, Deducao.PistasConhecidas(gm, caso),
            "agrupadas por par, na ordem de descoberta, nunca pela posição no par");
    }

    [Test]
    public void NovoCaso_NaoHerdaMarcacoes()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        MarcarTudoCerto();
        Assert.AreEqual(Deducao.Resultado.Confirmada, Deducao.Conferir(gm, caso));
        gm.ConcluirCaso(caso);

        CaseData outro = CasoComDeducao("Outro", out Item of1, out Item ob, out _, out _);
        Assert.IsTrue(gm.ConfirmarCaso(outro));
        Descobrir(of1, ob);
        Assert.AreEqual(MarcacaoDaPista.NaoMarcada, Deducao.MarcacaoDe(gm, outro, of1));
        Assert.IsFalse(Deducao.Confirmada(gm, outro));
        StringAssert.Contains("não verificada", Deducao.Rotulo(gm, of1));
        Assert.IsTrue(gm.marcacoesDeDeducao.TrueForAll(m => m.caso == caso.name), "as hipóteses ficam no caso em que foram feitas");
    }

    // ===== Save =====

    [Test]
    public void Save_V7_PreservaQuadro_ESaveAntigoNaoInventaHipoteses()
    {
        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        catalogo.casos.Add(caso);
        catalogo.itens.AddRange(new[] { f1, boato, f2, calunia });

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        MarcarTudoCerto();
        Assert.AreEqual(Deducao.Resultado.Confirmada, Deducao.Conferir(gm, caso));

        string json = JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Fase3", 14));
        GameManager lido = Go("GM_Lido").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(json), lido, catalogo);
        Assert.IsTrue(Deducao.Confirmada(lido, caso), "confirmação preservada");
        Assert.AreEqual(MarcacaoDaPista.Duvidosa, Deducao.MarcacaoDe(lido, caso, boato), "hipóteses preservadas");
        Assert.AreEqual(4, lido.marcacoesDeDeducao.Count);

        // Save v6 (antes da dedução), mesmo que o JSON traga os campos: nada é inventado.
        const string jsonV6 = @"{ ""versao"": 6, ""faseAtual"": 3, ""casoEscolhido"": ""CasoDeducao"", ""casosJaSelecionados"": [""CasoDeducao""],
            ""pistasVerificadas"": [""CasoDeducao_f1""],
            ""marcacoesDeDeducao"": [{ ""caso"": ""CasoDeducao"", ""pista"": ""CasoDeducao_f1"", ""marca"": 1 }],
            ""deducoesConfirmadas"": [""CasoDeducao""] }";
        GameManager antigo = Go("GM_Antigo").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonV6), antigo, catalogo);
        CollectionAssert.IsEmpty(antigo.marcacoesDeDeducao);
        CollectionAssert.IsEmpty(antigo.deducoesConfirmadas);
        CollectionAssert.Contains(antigo.pistasVerificadas, "CasoDeducao_f1", "a verificação antiga fica para os casos sem dedução");
        Assert.IsFalse(Deducao.PistaConfirmada(antigo, f1), "e não conta como dedução confirmada");
    }

    // ===== Conteúdo real =====

    [Test]
    public void CasosReais_FasesDoisATres_TemDoisParesCoerentes_EQuadroDesdeAFaseDois()
    {
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI.prefab");
        Assert.AreEqual(2, ui.GetComponentInChildren<QuadroDeDeducaoUI>(true).faseMinima, "o botão do quadro aparece já na Fase 2");
        int daFase2 = 0;
        foreach (CaseData real in ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases)
        {
            if (real.fase < 2) continue; // o tutorial fica com a receita simples
            if (real.fase == 2) daFase2++;
            Assert.IsTrue(Deducao.Aderente(real), real.name);
            CollectionAssert.IsEmpty(Deducao.Problemas(real), real.name);
            Assert.AreEqual(2, real.deducao.pares.Count, real.name);
            foreach (Item pista in real.deducao.Conjunto())
                Assert.IsFalse(real.EhAlegacao(pista), $"{real.name}: {pista.name}");
        }
        Assert.AreEqual(3, daFase2, "os três casos da Fase 2 entram na dedução");
    }

    [Test]
    public void CasosReais_FaseDois_CadaParFalaDoMesmoAssunto_EAVerdadeNaoApareceNoNome()
    {
        // Os pares da Fase 2 (Prompt 7): o boato contradiz o fato do cliente e a calúnia contradiz o fato do objeto.
        // Uma palavra-chave comum confirma que as duas afirmações tratam do mesmo assunto.
        var esperado = new Dictionary<string, string[]>
        {
            { "Caso_Joalheiro", new[] { "compareceu", "encomenda" } },
            { "Caso_Reveillon", new[] { "assembleia", "inverno" } },
            { "Caso_Operario", new[] { "pedia pão", "ordem" } },
        };
        foreach (var par in esperado)
        {
            CaseData real = AssetDatabase.LoadAssetAtPath<CaseData>($"Assets/Casos/{par.Key}.asset");
            Assert.IsTrue(Deducao.Aderente(real), par.Key);
            for (int i = 0; i < 2; i++)
            {
                ParContraditorio p = real.deducao.pares[i];
                Item fato = p.a.confiabilidade == Confiabilidade.Fato ? p.a : p.b;
                Item contraria = p.OutraDe(fato);
                Assert.AreNotEqual(Confiabilidade.Fato, contraria.confiabilidade, $"{par.Key} par {i + 1}");
                string chave = par.Value[i];
                bool comum = fato.descricao.ToLowerInvariant().Contains(chave) && contraria.descricao.ToLowerInvariant().Contains(chave);
                Assert.IsTrue(comum, $"{par.Key} par {i + 1}: '{fato.NomeExibicao}' × '{contraria.NomeExibicao}' deveriam falar do mesmo assunto ('{chave}')");
                foreach (string revela in new[] { "Boato", "Calúnia", "Rumor" })
                    StringAssert.DoesNotStartWith(revela, contraria.NomeExibicao);
            }
        }
    }
}
