using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedução ativa (Prompt 4; escolha por par desde 17/10). Regras do quadro (Deducao) sobre um caso montado aqui: cada par
/// decidido à parte, qualquer combinação aceita, a pista não escolhida descartada do inventário e do tribunal, nenhuma
/// etiqueta que revele a verdade; o inventário real, o save v9 e os casos reais das Fases 2 a 4.
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

    private Item Escolher(Item pista)
    {
        Assert.IsTrue(Deducao.Escolher(gm, caso, pista, out Item descartada), pista.name);
        return descartada;
    }

    // Etiquetas que denunciariam a verdade: nenhuma pode aparecer antes nem depois da escolha.
    private static void SemVerdade(string rotulo, string onde)
    {
        foreach (string revela in new[] { "confirmada", "desmentida", "boato", "calúnia", "fato" })
            StringAssert.DoesNotContain(revela, rotulo.ToLowerInvariant(), onde);
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
        Assert.IsTrue(problemas.Exists(p => p.Contains("carta do cliente")), string.Join("\n", problemas));
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

    // ===== Escolha por par =====

    [Test]
    public void Escolher_CadaParEIndependente_QualquerCombinacaoEAceita_SemRevelarAVerdade()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);

        // As quatro combinações (fato ou boato no par 1; fato ou calúnia no par 2) são aceitas do mesmo jeito.
        foreach (Item primeiro in new[] { f1, boato })
            foreach (Item segundo in new[] { f2, calunia })
            {
                gm.escolhasDeDeducao.Clear();
                string combinacao = $"{primeiro.name} + {segundo.name}";
                Assert.AreSame(Deducao.Contradicao(gm, primeiro), Escolher(primeiro), combinacao);
                Assert.IsTrue(Deducao.PodeEscolher(gm, caso, segundo), $"{combinacao}: o par 2 continua aberto");
                Assert.AreSame(Deducao.Contradicao(gm, segundo), Escolher(segundo), combinacao);
                Assert.AreEqual(2, Deducao.ParesDecididos(gm, caso), combinacao);

                foreach (Item pista in caso.deducao.Conjunto())
                {
                    bool ficou = pista == primeiro || pista == segundo;
                    Assert.AreEqual(ficou, Deducao.Escolhida(gm, pista), $"{combinacao}: {pista.name}");
                    Assert.AreEqual(!ficou, Deducao.Descartada(gm, pista), $"{combinacao}: {pista.name}");
                    StringAssert.Contains(ficou ? "sua escolha" : "descartada", Deducao.Rotulo(gm, pista));
                    SemVerdade(Deducao.Rotulo(gm, pista), $"{combinacao}: {pista.name}");
                }
            }
        Assert.AreEqual(Confiabilidade.Boato, boato.confiabilidade, "escolher não muda a verdade");
        Assert.AreEqual(Confiabilidade.Fato, f2.confiabilidade);
    }

    [Test]
    public void Escolher_DescartaAContrariaDoInventario_ENaoTemVolta()
    {
        InventoryManager inv = InventarioReal();
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        inv.AddItem(f1);
        inv.AddItem(boato);
        inv.AddItem(f2);
        Assert.IsTrue(Deducao.AguardandoEscolha(gm, f1));
        Assert.IsFalse(Deducao.AguardandoEscolha(gm, f2), "a calúnia ainda não apareceu: nada a escolher no par 2");
        Assert.IsFalse(Deducao.PodeEscolher(gm, caso, f2));
        StringAssert.Contains("não verificada", inv.RotuloDaPista(boato));

        Assert.AreSame(f1, Escolher(boato), "o jogador pode ficar com o boato");
        Assert.IsTrue(inv.HasItem(boato.itemID));
        Assert.IsFalse(inv.HasItem(f1.itemID), "a pista descartada sai do inventário");
        Assert.IsFalse(inv.PossuiEmQualquerLugar(f1.itemID));
        Assert.IsTrue(inv.HasItem(f2.itemID), "o outro par não é tocado");
        Assert.IsTrue(Deducao.Descoberta(gm, f1), "o histórico continua sabendo que ela foi achada");
        Assert.IsFalse(Deducao.AguardandoEscolha(gm, boato));
        StringAssert.Contains("sua escolha", inv.RotuloDaPista(boato));
        Assert.IsFalse(inv.PistaVerificada(boato), "a escolha não revela a verdade");

        Assert.IsFalse(Deducao.Escolher(gm, caso, f1, out _), "par decidido: não dá para trocar");
        Assert.IsFalse(Deducao.Escolher(gm, caso, boato, out _));
        Assert.AreEqual(1, gm.escolhasDeDeducao.Count);
    }

    [Test]
    public void Escolher_SoComAsDuasAfirmacoes_SoNoCasoEmAndamento_ESoPistasDoConjunto()
    {
        Descobrir(f1, boato);
        Assert.IsFalse(Deducao.Escolher(gm, caso, f1, out _), "sem caso em andamento");

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f2);
        Assert.IsFalse(Deducao.Escolher(gm, caso, f2, out _), "a contrária ainda não foi descoberta");
        Descobrir(alegacao1, alegacao2);
        Assert.IsFalse(Deducao.Escolher(gm, caso, alegacao1, out _), "alegação fica fora do quadro");
        Assert.IsFalse(Deducao.AguardandoEscolha(gm, alegacao1), "e não trava a prensa");

        gm.ConcluirCaso(caso);
        Assert.IsFalse(Deducao.Escolher(gm, caso, f1, out _), "caso concluído");
        CollectionAssert.IsEmpty(gm.escolhasDeDeducao);
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
        List<List<Item>> pares = Deducao.ParesConhecidos(gm, caso);
        Assert.AreEqual(2, pares.Count);
        CollectionAssert.AreEqual(new[] { boato, f1 }, pares[0]);
        CollectionAssert.AreEqual(new[] { calunia }, pares[1]);
    }

    [Test]
    public void NovoCaso_NaoHerdaEscolhas()
    {
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        Escolher(f1);
        Escolher(calunia);
        gm.ConcluirCaso(caso);

        CaseData outro = CasoComDeducao("Outro", out Item of1, out Item ob, out _, out _);
        Assert.IsTrue(gm.ConfirmarCaso(outro));
        Descobrir(of1, ob);
        Assert.IsFalse(Deducao.ParDecidido(gm, of1));
        Assert.IsTrue(Deducao.PodeEscolher(gm, outro, ob));
        StringAssert.Contains("não verificada", Deducao.Rotulo(gm, of1));
        Assert.IsTrue(gm.escolhasDeDeducao.TrueForAll(e => e.caso == caso.name), "as escolhas ficam no caso em que foram feitas");
    }

    [Test]
    public void Tribunal_PistaDescartadaNaoViraProva()
    {
        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        catalogo.itens.AddRange(new[] { f1, boato, f2, calunia });
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        Escolher(boato);

        List<Item> provas = TribunalManager.ProvasDoCaso(gm, caso, catalogo);
        CollectionAssert.DoesNotContain(provas, f1, "descartada no quadro");
        CollectionAssert.IsSubsetOf(new[] { boato, f2, calunia }, provas, "o par 2 ainda não decidido continua inteiro");
    }

    // ===== Save =====

    [Test]
    public void Save_V9_PreservaEscolhas_DescartadaNaoVolta_ESaveAntigoNaoInventaEscolhas()
    {
        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        catalogo.casos.Add(caso);
        catalogo.itens.AddRange(new[] { f1, boato, f2, calunia });

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Descobrir(f1, boato, f2, calunia);
        Escolher(boato);

        string json = JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Fase3", 14));
        GameManager lido = Go("GM_Lido").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(json), lido, catalogo);
        Assert.IsTrue(Deducao.Escolhida(lido, boato), "escolha preservada");
        Assert.IsTrue(Deducao.Descartada(lido, f1));
        Assert.IsFalse(Deducao.ParDecidido(lido, f2), "o par 2 continua aberto");
        Assert.AreEqual(1, lido.escolhasDeDeducao.Count);

        // Um save que traga a descartada no inventário (editado à mão, por exemplo): ela não volta.
        const string jsonComDescartada = @"{ ""versao"": 9, ""faseAtual"": 3, ""casoEscolhido"": ""CasoDeducao"", ""casosJaSelecionados"": [""CasoDeducao""],
            ""inventario"": [{ ""itemID"": ""CasoDeducao_f1"", ""quantidade"": 1 }, { ""itemID"": ""CasoDeducao_boato"", ""quantidade"": 1 }],
            ""escolhasDeDeducao"": [{ ""caso"": ""CasoDeducao"", ""escolhida"": ""CasoDeducao_boato"", ""descartada"": ""CasoDeducao_f1"" }] }";
        GameManager editado = Go("GM_Editado").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonComDescartada), editado, catalogo);
        Assert.IsFalse(editado.inventarioSalvo.Exists(i => i.itemID == f1.itemID), "a descartada não volta ao inventário");
        Assert.IsTrue(editado.inventarioSalvo.Exists(i => i.itemID == boato.itemID));

        // Save v8 (hipóteses e quadro confirmado da regra antiga): nenhuma escolha é inventada.
        const string jsonV8 = @"{ ""versao"": 8, ""faseAtual"": 3, ""casoEscolhido"": ""CasoDeducao"", ""casosJaSelecionados"": [""CasoDeducao""],
            ""pistasVerificadas"": [""CasoDeducao_f1""],
            ""marcacoesDeDeducao"": [{ ""caso"": ""CasoDeducao"", ""pista"": ""CasoDeducao_f1"", ""marca"": 1 }],
            ""deducoesConfirmadas"": [""CasoDeducao""],
            ""escolhasDeDeducao"": [{ ""caso"": ""CasoDeducao"", ""escolhida"": ""CasoDeducao_f1"", ""descartada"": ""CasoDeducao_boato"" }] }";
        GameManager antigo = Go("GM_Antigo").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonV8), antigo, catalogo);
        CollectionAssert.IsEmpty(antigo.escolhasDeDeducao);
        CollectionAssert.Contains(antigo.pistasVerificadas, "CasoDeducao_f1", "a verificação antiga fica para os casos sem dedução");
        Assert.IsFalse(Deducao.ParDecidido(antigo, f1));
    }

    // ===== Conteúdo real =====

    [Test]
    public void CasosReais_FasesDoisATres_TemDoisParesCoerentes_EQuadroDesdeAFaseDois()
    {
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI.prefab");
        Assert.AreEqual(2, ui.GetComponentInChildren<QuadroDeDeducaoUI>(true).faseMinima, "o botão do quadro aparece já na Fase 2");
        int daFase2 = 0;
        foreach (CaseData real in ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases)
        {
            if (real.fase < 2) continue; // o tutorial fica com a receita simples
            if (real.fase == 2) daFase2++;
            Assert.IsTrue(Deducao.Aderente(real), real.name);
            CollectionAssert.IsEmpty(Deducao.Problemas(real), real.name);
            Assert.GreaterOrEqual(real.deducao.pares.Count, 2, real.name); // as regras valem para qualquer número de pares
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
            CaseData real = AssetDatabase.LoadAssetAtPath<CaseData>($"Assets/ScriptableObjects/Cases/{par.Key}.asset");
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
