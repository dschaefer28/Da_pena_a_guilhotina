using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Exercita os COMPONENTES reais (não só as regras do GameManager): relógio, NPCMovement, LootInteractable,
/// CraftingPress e compra com o AddItem de verdade, sobre um inventário e uma prensa mínimos montados aqui.
/// Cobre os itens de aceite dos Prompts 1–3 que antes só tinham verificação em Play Mode.
/// </summary>
public class ComponentesTests
{
    private readonly List<Object> criados = new List<Object>();
    private GameManager gm;
    private InventoryManager inv;
    private DialogueSystem ds;
    private CraftingPress prensa;
    private CaseData casoA, casoB;

    // ===== Montagem =====

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

    private UISlotHandler Slot(Transform pai, string nome)
    {
        GameObject s = Go(nome, pai);
        UISlotHandler slot = s.AddComponent<UISlotHandler>();
        slot.slotImg = Go("Img", s.transform).AddComponent<Image>();
        slot.itemCount = Go("Qtd", s.transform).AddComponent<TextMeshProUGUI>();
        return slot;
    }

    private CaseData Caso(string nome, int fase)
    {
        CaseData c = Criar<CaseData>(nome);
        c.caseTitle = nome; c.fase = fase;
        c.nextSceneName = SceneManager.GetActiveScene().name; // o relógio só vale na cena do caso
        return c;
    }

    private Item Pista(string id, CaseData caso, Confiabilidade conf = Confiabilidade.Fato)
    {
        Item i = Criar<Item>(id);
        i.itemID = id; i.caso = caso; i.confiabilidade = conf; i.itemAmt = 1;
        return i;
    }

    private DialogueData Fala(string texto)
    {
        DialogueData d = Criar<DialogueData>("Fala_" + texto);
        d.talkScript = new List<Dialogue> { new Dialogue { name = "NPC", text = texto, choices = new List<Choice>() } };
        return d;
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
        // Estes testes medem a cobrança (1h por interação nova), não o orçamento: fixam 4h para as contas ficarem legíveis.
        // O orçamento real (8h nas Fases 2 a 4) é conferido em TutorialEUiTests e no validador.
        gm.horasPorCaso = 4;

        GameObject grade = Go("Grade");
        inv = Go("Inventario").AddComponent<InventoryManager>();
        inv.inventoryGrid = grade;
        for (int i = 0; i < 4; i++) Slot(grade.transform, "Slot" + i);
        gm.inventoryManager = inv;

        ds = Go("Dialogo").AddComponent<DialogueSystem>();
        gm.dialogueSystem = ds;

        GameObject p = Go("Prensa");
        prensa = p.AddComponent<CraftingPress>();
        prensa.slotInput1 = Slot(p.transform, "E1");
        prensa.slotInput2 = Slot(p.transform, "E2");
        prensa.slotOutput = Slot(p.transform, "Saida");
        prensa.recipes = new List<Recipe>();

        casoA = Caso("CasoA", 3);
        casoB = Caso("CasoB", 3);
    }

    [TearDown]
    public void Desmontar()
    {
        DefinirInstancia(null);
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private void TerminarDialogo()
    {
        if (ds.IsDialogueActive) typeof(DialogueSystem).GetMethod("EndDialogue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ds, null);
    }

    private NPCMovement Npc(string id, CaseData caso, DialogueData fala, params Item[] recompensas)
    {
        NPCMovement npc = Go(id).AddComponent<NPCMovement>();
        npc.idDaInteracao = id;
        npc.disableAfterDialogue = false;
        npc.dialogoPadrao = Fala("pouco útil " + id);
        npc.reacoesDeCaso = new List<CasoReacao>
        {
            new CasoReacao { caso = caso, dialogoInicialDoCaso = fala, recompensasDoDialogo = new List<Item>(recompensas) }
        };
        return npc;
    }

    private LootInteractable Loot(string id, CaseData caso = null, Item item = null)
    {
        LootInteractable loot = Go(id).AddComponent<LootInteractable>();
        loot.idDaInteracao = id;
        loot.pistasPossiveis = new List<LootDeCaso>();
        if (caso != null) loot.pistasPossiveis.Add(new LootDeCaso { caso = caso, itemParaDar = item });
        return loot;
    }

    // Na Fase 4 a mesa só aceita o caso da rota travada (Prompt 6).
    private void NaFase4(CaseData caso)
    {
        caso.fase = 4;
        caso.rota = RotaFinal.A_Guilhotina;
        gm.faseAtual = 4;
        gm.rotaFinal = RotaFinal.A_Guilhotina;
    }

    private void Lotar()
    {
        int n = 0;
        while (inv.AddItem(Pista("lotado_" + n++, null, Confiabilidade.NaoEPista)) && n < 50) { }
    }

    // ===== Relógio =====

    [Test]
    public void Relogio_PrimeiraCobra_RepetirNaoCobra_OutroCasoTemEstadoProprio_ZeroHorasRecusa()
    {
        LootInteractable estante = Loot("Estante");
        Assert.IsTrue(gm.ConfirmarCaso(casoA));
        Assert.IsTrue(RelogioDeInvestigacao.Ativo);
        estante.Interact();
        Assert.AreEqual(3, gm.horasRestantes, "busca vazia também cobra");
        estante.Interact();
        Assert.AreEqual(3, gm.horasRestantes, "repetir no mesmo caso não cobra");

        gm.horasRestantes = 0;
        Loot("Bau").Interact();
        Assert.AreEqual(0, gm.horasRestantes);
        Assert.IsFalse(gm.registroDaInvestigacao.InteracaoPaga(casoA, SceneManager.GetActiveScene().name + "/Bau"), "sem horas nada é registrado");

        gm.ConcluirCaso(casoA);
        Assert.IsTrue(gm.ConfirmarCaso(casoB));
        estante.Interact();
        Assert.AreEqual(3, gm.horasRestantes, "outro caso cobra de novo");
    }

    [Test]
    public void Relogio_ChaveDoSaveV2_ViraChaveNova_SemCobrar()
    {
        LootInteractable estante = Loot("Estante");
        gm.ConfirmarCaso(casoA);
        string antiga = SceneManager.GetActiveScene().name + "/Estante";
        gm.registroDaInvestigacao.interacoesPagasLegadas.Add(RegistroDaInvestigacao.Chave(casoA, antiga));
        estante.Interact();
        Assert.AreEqual(4, gm.horasRestantes);
        Assert.IsTrue(gm.registroDaInvestigacao.InteracaoPaga(casoA, estante.ChaveDaInteracao));
    }

    // ===== NPC =====

    [Test]
    public void Npc_EntregaUmaVez_RevisitaDepoisDeGastarNaoDevolve_DoisCasosSeparados()
    {
        Item pistaA = Pista("pA", casoA), pistaB = Pista("pB", casoB);
        NPCMovement npc = Npc("Testemunha", casoA, Fala("fala A"), pistaA);
        npc.reacoesDeCaso.Add(new CasoReacao { caso = casoB, dialogoInicialDoCaso = Fala("fala B"), recompensasDoDialogo = new List<Item> { pistaB } });

        gm.ConfirmarCaso(casoA);
        npc.Interact(); TerminarDialogo();
        Assert.IsTrue(inv.HasItem("pA"));
        Assert.AreEqual(3, gm.horasRestantes);

        inv.RemoverItens(i => i.itemID == "pA"); // gasta na prensa
        npc.Interact(); TerminarDialogo();
        Assert.IsFalse(inv.HasItem("pA"), "a pista gasta não volta");
        Assert.AreEqual(3, gm.horasRestantes, "revisita não cobra");

        gm.ConcluirCaso(casoA);
        gm.ConfirmarCaso(casoB);
        npc.Interact(); TerminarDialogo();
        Assert.IsTrue(inv.HasItem("pB"), "o mesmo NPC tem estado próprio no outro caso");
        Assert.AreEqual(3, gm.horasRestantes);
    }

    [Test]
    public void Npc_InventarioCheio_NaoCobraNemEntrega_DepoisEntregaSemCobrarDeNovo()
    {
        Item pista = Pista("p1", casoA);
        NPCMovement npc = Npc("Testemunha", casoA, Fala("fala"), pista);
        gm.ConfirmarCaso(casoA);
        Lotar();

        npc.Interact();
        Assert.IsFalse(ds.IsDialogueActive, "nada cabe: nem conversa");
        Assert.AreEqual(4, gm.horasRestantes, "e não cobra");

        inv.RemoverItens(i => i.itemID == "lotado_0");
        npc.Interact(); TerminarDialogo();
        Assert.IsTrue(inv.HasItem("p1"));
        Assert.AreEqual(3, gm.horasRestantes);
    }

    [Test]
    public void Npc_EtapaComplementar_SoDepoisDoRequisito_MesmoGasto_SemRepetir()
    {
        Item contrato = Pista("contrato", casoA), depoimento = Pista("depoimento", casoA);
        Item recibo = Pista("recibo", casoA, Confiabilidade.NaoEPista);
        recibo.documentoDeSuporte = true;
        NPCMovement npc = Npc("Cocheiro", casoA, Fala("contrato"), contrato);
        CasoReacao r = npc.reacoesDeCaso[0];
        r.etapasComplementares = new List<EtapaComplementar>
        {
            new EtapaComplementar { id = "recibo", requisitos = new List<Item> { depoimento }, dialogo = Fala("recibo"), recompensas = new List<Item> { recibo } }
        };
        npc.reacoesDeCaso[0] = r;
        gm.ConfirmarCaso(casoA);

        npc.Interact(); TerminarDialogo();
        Assert.IsTrue(inv.HasItem("contrato"));
        Assert.IsFalse(inv.HasItem("recibo"), "sem o requisito, só a fala normal");

        inv.AddItem(depoimento);
        inv.RemoverItens(i => i.itemID == "depoimento"); // obtido e gasto: o requisito continua cumprido
        npc.Interact(); TerminarDialogo();
        Assert.IsTrue(inv.HasItem("recibo"));

        inv.RemoverItens(i => i.itemID == "recibo");
        npc.Interact(); TerminarDialogo();
        Assert.IsFalse(inv.HasItem("recibo"), "a etapa não repete");
        Assert.AreEqual(3, gm.horasRestantes, "conversas seguintes não cobram");
    }

    // Mesmo arranjo do Kornmann (Varennes): a fala normal entrega o contrato; a etapa "recibo" exige o depoimento.
    private NPCMovement CocheiroComEtapa(out Item depoimento, out DialogueData falaNormal, out DialogueData falaEtapa)
    {
        Item contrato = Pista("contrato", casoA);
        depoimento = Pista("depoimento", casoA);
        Item recibo = Pista("recibo", casoA, Confiabilidade.NaoEPista);
        recibo.documentoDeSuporte = true;
        falaNormal = Fala("contrato");
        falaEtapa = Fala("recibo");
        NPCMovement npc = Npc("Cocheiro", casoA, falaNormal, contrato);
        CasoReacao r = npc.reacoesDeCaso[0];
        r.etapasComplementares = new List<EtapaComplementar>
        {
            new EtapaComplementar { id = "recibo", requisitos = new List<Item> { depoimento }, dialogo = falaEtapa, recompensas = new List<Item> { recibo } }
        };
        npc.reacoesDeCaso[0] = r;
        return npc;
    }

    // Start não roda em Edit Mode; chamá-lo simula a cena abrindo (ou reabrindo) com o NPC.
    private static void IniciarNpc(NPCMovement npc) =>
        typeof(NPCMovement).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(npc, null);

    [Test]
    public void Npc_EtapaComplementar_RequisitoJaObtido_SegueNaMesmaConversa_SemCobrarDeNovo()
    {
        // Playtest: quem achava o depoimento antes de falar com o Kornmann falava uma vez e nunca recebia o recibo.
        NPCMovement npc = CocheiroComEtapa(out Item depoimento, out DialogueData falaNormal, out DialogueData falaEtapa);
        var avisos = new List<string>();
        System.Action<string, Sprite> ouvir = (texto, icone) => { if (texto.Contains("Cocheiro")) avisos.Add(texto); };
        AvisoNaTela.OnAviso += ouvir;
        try
        {
            gm.ConfirmarCaso(casoA);
            IniciarNpc(npc);
            inv.AddItem(depoimento);
            Assert.AreEqual(0, avisos.Count, "ainda não conversou com ele: não há para onde 'voltar'");

            npc.Interact();
            Assert.AreSame(falaNormal, ds.dialogueData, "primeiro a fala normal");
            TerminarDialogo();
            Assert.IsTrue(inv.HasItem("contrato"));
            Assert.IsTrue(ds.IsDialogueActive, "a conversa continua na etapa já liberada");
            Assert.AreSame(falaEtapa, ds.dialogueData);
            TerminarDialogo();
            Assert.IsTrue(inv.HasItem("recibo"), "tudo numa conversa só");
            Assert.IsFalse(ds.IsDialogueActive);
            Assert.AreEqual(3, gm.horasRestantes, "uma conversa, uma cobrança");
            Assert.AreEqual(0, avisos.Count);
        }
        finally { AvisoNaTela.OnAviso -= ouvir; }
    }

    [Test]
    public void Npc_EtapaComplementar_RequisitoAchadoDepoisDaConversa_AvisaParaVoltar()
    {
        NPCMovement npc = CocheiroComEtapa(out Item depoimento, out _, out _);
        var avisos = new List<string>();
        System.Action<string, Sprite> ouvir = (texto, icone) => { if (texto.Contains("Cocheiro")) avisos.Add(texto); };
        AvisoNaTela.OnAviso += ouvir;
        try
        {
            gm.ConfirmarCaso(casoA);
            IniciarNpc(npc);
            npc.Interact(); TerminarDialogo();
            Assert.IsFalse(inv.HasItem("recibo"), "sem o requisito, a conversa termina na fala normal");

            inv.AddItem(Pista("nada_a_ver", casoA));
            Assert.AreEqual(0, avisos.Count, "item que não é requisito não avisa");
            inv.AddItem(depoimento);
            Assert.AreEqual(1, avisos.Count, "achou o requisito depois de conversar: avisa para voltar");

            IniciarNpc(npc); // voltou à cena sem ter falado de novo
            Assert.AreEqual(2, avisos.Count, "o lembrete reaparece enquanto a etapa não for recebida");

            npc.Interact(); TerminarDialogo();
            Assert.IsTrue(inv.HasItem("recibo"));
            Assert.AreEqual(3, gm.horasRestantes, "voltar não cobra");
            IniciarNpc(npc);
            Assert.AreEqual(2, avisos.Count, "etapa recebida: sem lembrete");
        }
        finally { AvisoNaTela.OnAviso -= ouvir; }
    }

    [Test]
    public void Npc_OuvinteDoFimDoDialogoComErro_NaoImpedeAEntrega()
    {
        NPCMovement npc = Npc("Testemunha", casoA, Fala("fala"), Pista("p1", casoA));
        System.Action quebrado = () => throw new System.InvalidOperationException("ouvinte quebrado");
        ds.OnDialogueEnded += quebrado;
        try
        {
            gm.ConfirmarCaso(casoA);
            npc.Interact();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("ouvinte quebrado"));
            TerminarDialogo();
            Assert.IsTrue(inv.HasItem("p1"), "o erro de outro ouvinte não engole a pista");
        }
        finally { ds.OnDialogueEnded -= quebrado; }
    }

    [Test]
    public void Npc_DepoisDaEntrega_TocaFalaCurta_SemEntregarDeNovo_CampoVazioRepeteAConversa()
    {
        // Mesmo arranjo do Dupaty no tutorial: sem pista fala o padrão; com a pista, a conversa que entrega o documento.
        Item relato = Pista("relato", null, Confiabilidade.NaoEPista);
        Item decreto = Pista("decreto", null, Confiabilidade.NaoEPista);
        DialogueData conversa = Fala("conversa com pista"), lembrete = Fala("vá para a prensa");
        NPCMovement mentor = Npc("Mentor", casoA, null, decreto);
        CasoReacao r = mentor.reacoesDeCaso[0];
        r.pistaNecessaria = relato; r.dialogoComPista = conversa; r.dialogoDepoisDaEntrega = lembrete;
        mentor.reacoesDeCaso[0] = r;
        gm.ConfirmarCaso(casoA);

        mentor.Interact();
        Assert.AreSame(mentor.dialogoPadrao, ds.dialogueData, "sem a pista: fala padrão, antes da entrega");
        TerminarDialogo();
        Assert.IsFalse(inv.HasItem("decreto"));

        inv.AddItem(relato);
        mentor.Interact();
        Assert.AreSame(conversa, ds.dialogueData, "com a pista: a conversa da reação");
        TerminarDialogo();
        Assert.AreEqual(1, inv.ItemNaGrade("decreto").itemAmt);

        mentor.Interact();
        Assert.AreSame(lembrete, ds.dialogueData, "tudo entregue: só a fala curta");
        TerminarDialogo();
        Assert.AreEqual(1, inv.ItemNaGrade("decreto").itemAmt, "a fala curta não entrega de novo");

        // Campo vazio: comportamento anterior (repete a conversa, sem entregar).
        NPCMovement outro = Npc("Outro", casoA, null, Pista("carta", null, Confiabilidade.NaoEPista));
        CasoReacao r2 = outro.reacoesDeCaso[0];
        r2.pistaNecessaria = relato; r2.dialogoComPista = conversa;
        outro.reacoesDeCaso[0] = r2;
        outro.Interact(); TerminarDialogo();
        outro.Interact();
        Assert.AreSame(conversa, ds.dialogueData, "sem fala depois da entrega, repete a conversa");
        TerminarDialogo();
        Assert.AreEqual(1, inv.ItemNaGrade("carta").itemAmt);
    }

    [Test]
    public void Npc_CasoConcluido_SoConversa()
    {
        Item pista = Pista("p1", casoA);
        NPCMovement npc = Npc("Testemunha", casoA, Fala("fala"), pista);
        gm.ConfirmarCaso(casoA);
        gm.ConcluirCaso(casoA);
        npc.Interact(); TerminarDialogo();
        Assert.IsFalse(inv.HasItem("p1"), "depois de concluído, nenhuma pista de graça");
    }

    // ===== Loot =====

    [Test]
    public void Loot_UmaVezPorCaso_InventarioCheioNaoCobra()
    {
        Item pista = Pista("p1", casoA);
        LootInteractable gaveta = Loot("Gaveta", casoA, pista);
        gm.ConfirmarCaso(casoA);
        Lotar();
        gaveta.Interact();
        Assert.AreEqual(4, gm.horasRestantes, "sem espaço, não cobra");

        inv.RemoverItens(i => i.itemID == "lotado_0");
        gaveta.Interact();
        Assert.IsTrue(inv.HasItem("p1"));
        Assert.AreEqual(3, gm.horasRestantes);
        Assert.IsFalse(gaveta.PodeInteragir);

        inv.RemoverItens(i => i.itemID == "p1");
        gaveta.Interact();
        Assert.IsFalse(inv.HasItem("p1"), "não reaparece no mesmo caso");
    }

    // ===== Prensa =====

    private void NasEntradas(Item a, Item b, int quantidade = 1)
    {
        Item ca = a.Clone(); ca.itemAmt = quantidade;
        Item cb = b.Clone(); cb.itemAmt = quantidade;
        criados.Add(ca); criados.Add(cb);
        inv.PlaceInInventory(prensa.slotInput1, ca);
        inv.PlaceInInventory(prensa.slotInput2, cb);
    }

    private ReceitaDeCaso Receita(CaseData caso)
    {
        ReceitaDeCaso r = Criar<ReceitaDeCaso>("Receita_" + caso.name);
        r.caso = caso;
        r.panfletoPadrao = Pista("panfleto_" + caso.name, null, Confiabilidade.NaoEPista);
        r.soFatos = new ReceitaDeCaso.Versao { povo = 20, estado = -10, ouro = 30 };
        r.soAlegacoes = new ReceitaDeCaso.Versao { povo = 5, estado = -2, ouro = 5, penalidadePovo = -5 };
        caso.receitaDoPanfleto = r;
        return r;
    }

    [Test]
    public void Prensa_DuploCliqueNaoRepublica_EOutroCasoNaoConsome()
    {
        // Caso da Fase 4: as sobras ficam na prensa ao concluir (antes da Fase 4 seriam arquivadas), então o
        // 2º clique encontra pistas nas entradas e quem barra é a regra de publicação única.
        NaFase4(casoA);
        Receita(casoA);
        Item f1 = Pista("f1", casoA), f2 = Pista("f2", casoA), deB1 = Pista("b1", casoB), deB2 = Pista("b2", casoB);
        gm.ConfirmarCaso(casoA);

        NasEntradas(deB1, deB2);
        prensa.CombineItems();
        Assert.AreEqual(1, prensa.slotInput1.item.itemAmt, "pistas de outro caso não são consumidas");
        Assert.IsEmpty(gm.panfletosPublicados);

        NasEntradas(f1, f2, quantidade: 2);
        prensa.CombineItems();
        prensa.CombineItems();
        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Assert.AreEqual(30, gm.capitalAtual, "ouro aplicado uma vez");
        Assert.AreEqual(1, prensa.slotInput1.item != null ? prensa.slotInput1.item.itemAmt : 0, "o 2º clique não consumiu");
    }

    [Test]
    public void Prensa_CasoAntesDaFase4_ArquivaAsSobrasDasEntradas()
    {
        Receita(casoA);
        gm.ConfirmarCaso(casoA);
        NasEntradas(Pista("f1", casoA), Pista("f2", casoA), quantidade: 2);
        prensa.CombineItems();
        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Assert.IsNull(prensa.slotInput1.item, "sobras do caso concluído saem também das entradas da prensa");
        Assert.IsNotNull(prensa.slotOutput.item, "o panfleto fica na saída");
    }

    [Test]
    public void Prensa_CartaDoClienteNaoEntra_EApoioContinuaValendo()
    {
        ReceitaDeCaso r = Receita(casoA);
        Item apoio = Pista("apoio", casoA, Confiabilidade.NaoEPista);
        apoio.documentoDeSuporte = true; apoio.qualidade = QualidadeDaEvidencia.Superior;
        r.qualificadores.Add(new ReceitaDeCaso.Qualificador { id = "q", suportesAceitos = new List<Item> { apoio }, povo = 1 });
        gm.ConfirmarCaso(casoA);
        // Cartas definidas depois de aceitar: aqui as peças vêm à mão, sem a entrega automática. A primeira imita um asset
        // antigo, ainda marcado como Boato: mesmo assim é só leitura.
        Item carta = Pista("aleg", casoA, Confiabilidade.Boato);
        casoA.alegacoesIniciais = new List<Item> { carta, Pista("aleg2", casoA, Confiabilidade.NaoEPista) };
        Assert.IsTrue(carta.EhLeitura);
        Assert.IsFalse(carta.EhPista, "carta do cliente nunca é pista");

        inv.AddItem(apoio);
        Assert.IsTrue(prensa.AlternarSuporte(inv.ItemNaGrade("apoio")));
        NasEntradas(Pista("fato", casoA), carta);
        prensa.CombineItems();
        Assert.AreEqual(0, gm.panfletosPublicados.Count, "a carta não vai para a prensa");
        Assert.IsNotNull(prensa.slotInput1.item, "nada é consumido");
        Assert.IsNotNull(prensa.slotInput2.item);

        inv.ClearItemSlot(prensa.slotInput1);
        inv.ClearItemSlot(prensa.slotInput2);
        NasEntradas(Pista("fato", casoA), Pista("fato2", casoA));
        prensa.CombineItems();
        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.AreEqual(NivelDoPanfleto.Fatos, p.nivel);
        Assert.AreEqual(21, p.povo, "fatos (20) + apoio (1)");
        CollectionAssert.Contains(p.suportes, "apoio");
    }

    [Test]
    public void Historico_GuardaODeltaEfetivamenteAplicado()
    {
        Receita(casoA);
        gm.ConfirmarCaso(casoA);
        gm.opiniaoPublicaAtual = 95;
        NasEntradas(Pista("f1", casoA), Pista("f2", casoA));
        prensa.CombineItems();
        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.AreEqual(20, p.povo, "calculado");
        Assert.AreEqual(5, p.povoAplicado, "aplicado: 95 → 100");
        Assert.AreEqual(-10, p.estadoAplicado);
    }

    [Test]
    public void ConcluirCaso_ArquivaSobrasAntesDaFase4_EPreservaNaFase4()
    {
        Receita(casoA);
        gm.ConfirmarCaso(casoA);
        inv.AddItem(Pista("sobra", casoA));
        NasEntradas(Pista("f1", casoA), Pista("f2", casoA));
        prensa.CombineItems();
        Assert.IsFalse(inv.HasItem("sobra"), "sobras da Fase 3 saem do inventário");
        CollectionAssert.Contains(gm.evidenciasObtidas, "sobra", "mas continuam no histórico");

        inv.ClearItemSlot(prensa.slotOutput); // o jogador tira o panfleto pronto
        CaseData final = Caso("CasoFinal", 4);
        Receita(final);
        NaFase4(final);
        Assert.IsTrue(gm.ConfirmarCaso(final));
        inv.AddItem(Pista("prova", final));
        NasEntradas(Pista("g1", final), Pista("g2", final));
        prensa.CombineItems();
        Assert.IsTrue(inv.HasItem("prova"), "o caso da Fase 4 leva as provas ao tribunal");
    }

    // ===== Linha editorial (Prompt 5) =====

    private ReceitaDeCaso ReceitaComLinha(CaseData caso)
    {
        ReceitaDeCaso r = Receita(caso);
        r.comBoato = new ReceitaDeCaso.Versao { povo = 30, estado = -15, ouro = 40, penalidadePovo = -10, penalidadeEstado = -5 };
        r.linhaEditorial = new ReceitaDeCaso.ConfiguracaoEditorial
        {
            ativa = true,
            sensacionalista = new ReceitaDeCaso.ModificadorEditorial { povo = -10, ouro = 15 },
            agravamentoPercentual = 50
        };
        return r;
    }

    [Test]
    public void Prensa_LinhaEditorial_PedeAntesDeConsumir_CancelarNaoGasta_DuploCliquePublicaUmaVez()
    {
        // Fase 4: as sobras ficam nas entradas ao concluir, então os cliques repetidos encontram pistas para consumir.
        NaFase4(casoA);
        ReceitaDeCaso receita = ReceitaComLinha(casoA);
        gm.ConfirmarCaso(casoA);
        int pedidos = 0;
        ReceitaDeCaso pedida = null;
        prensa.OnLinhaEditorialPedida += r => { pedidos++; pedida = r; };

        NasEntradas(Pista("f1", casoA), Pista("boato", casoA, Confiabilidade.Boato), quantidade: 2);
        prensa.CombineItems();
        Assert.AreEqual(1, pedidos, "Misturar pede a linha editorial");
        Assert.AreSame(receita, pedida);
        Assert.AreEqual(2, prensa.slotInput1.item.itemAmt, "nada consumido antes da escolha");
        Assert.IsNull(prensa.slotOutput.item);
        Assert.IsEmpty(gm.panfletosPublicados);
        Assert.AreEqual(0, gm.capitalAtual);

        // Cancelar = não responder. Misturar de novo pede de novo, sem gastar nada.
        prensa.CombineItems();
        Assert.AreEqual(2, pedidos);
        Assert.AreEqual(2, prensa.slotInput1.item.itemAmt);

        prensa.ImprimirComLinhaEditorial(LinhaEditorial.Sensacionalista);
        prensa.ImprimirComLinhaEditorial(LinhaEditorial.Sensacionalista); // duplo clique
        prensa.CombineItems();
        Assert.AreEqual(1, gm.panfletosPublicados.Count, "publicado uma vez");
        Assert.AreEqual(2, pedidos, "caso publicado não pede nova escolha");
        Assert.AreEqual(1, prensa.slotInput1.item.itemAmt, "consumido uma vez");
        Assert.AreEqual(55, gm.capitalAtual, "40 da versão + 15 do sensacionalista, uma vez");

        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.AreEqual(LinhaEditorial.Sensacionalista, p.linha);
        Assert.AreEqual(NivelDoPanfleto.ComBoato, p.nivel);
        Assert.AreEqual(-15, p.penalidadePovo);
        Assert.AreEqual(1, gm.revelacoesPendentes.Count);
        Assert.AreEqual(-15, gm.revelacoesPendentes[0].povo, "penalidade agravada vai para o fluxo de revelações");
        Assert.AreEqual(-8, gm.revelacoesPendentes[0].estado);
    }

    [Test]
    public void Prensa_LinhaEditorial_SemJanelaNaoImprime_SaidaOcupadaNaoPedeNemConsome()
    {
        ReceitaComLinha(casoA);
        gm.ConfirmarCaso(casoA);
        NasEntradas(Pista("f1", casoA), Pista("f2", casoA));

        prensa.CombineItems(); // ninguém ouvindo: só avisa
        Assert.IsNotNull(prensa.slotInput1.item);
        Assert.IsEmpty(gm.panfletosPublicados);

        int pedidos = 0;
        prensa.OnLinhaEditorialPedida += _ => pedidos++;
        inv.PlaceInInventory(prensa.slotOutput, Pista("panfleto_antigo", null, Confiabilidade.NaoEPista));
        prensa.CombineItems();
        Assert.AreEqual(0, pedidos, "com a saída ocupada a prensa recusa antes de pedir o tom");
        prensa.ImprimirComLinhaEditorial(LinhaEditorial.Neutro);
        Assert.IsNotNull(prensa.slotInput1.item, "nada consumido");
        Assert.IsEmpty(gm.panfletosPublicados);
        Assert.AreEqual(50, gm.opiniaoPublicaAtual);

        inv.ClearItemSlot(prensa.slotOutput);
        prensa.ImprimirComLinhaEditorial(LinhaEditorial.DefesaDoPovo); // linha legada: não imprime, pede a escolha de novo
        Assert.AreEqual(1, pedidos);
        Assert.IsEmpty(gm.panfletosPublicados);

        prensa.ImprimirComLinhaEditorial(LinhaEditorial.Neutro);
        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Assert.AreEqual(LinhaEditorial.Neutro, gm.panfletosPublicados[0].linha);
        Assert.AreEqual(70, gm.opiniaoPublicaAtual, "50 + 20 (Fatos), Neutro sem ajuste");
        Assert.AreEqual(40, gm.opiniaoEstadoAtual, "50 - 10");
        Assert.AreEqual(30, gm.capitalAtual, "só o ouro da versão");
        Assert.IsEmpty(gm.revelacoesPendentes);
    }

    [Test]
    public void Prensa_ReceitaExataDoTutorial_ImprimeSemEtapaDeLinhaEditorial()
    {
        CaseData tutorial = Caso("CasoTutorial", 1);
        gm.faseAtual = 1;
        Item decreto = Pista("decreto", null, Confiabilidade.NaoEPista), carta = Pista("carta", null, Confiabilidade.NaoEPista);
        Recipe receita = Criar<Recipe>("ReceitaTutorial");
        receita.ingrediente1 = decreto; receita.ingrediente2 = carta;
        receita.resultItem = Pista("panfleto_tutorial", null, Confiabilidade.NaoEPista);
        receita.publicOpinionImpact = 15; receita.stateOpinionImpact = -10; receita.moneyReward = 20;
        prensa.recipes = new List<Recipe> { receita };
        gm.ConfirmarCaso(tutorial);
        int pedidos = 0;
        prensa.OnLinhaEditorialPedida += _ => pedidos++;

        NasEntradas(decreto, carta);
        prensa.CombineItems();
        Assert.AreEqual(0, pedidos, "o tutorial não ganha etapa extra");
        Assert.AreEqual("panfleto_tutorial", prensa.slotOutput.item.itemID);
        CollectionAssert.Contains(gm.casosConcluidos, tutorial);
        Assert.AreEqual(65, gm.opiniaoPublicaAtual);
        Assert.AreEqual(20, gm.capitalAtual);
    }

    // ===== Dedução ativa (Prompt 4) =====

    [Test]
    public void Prensa_CasoComDeducao_ParSemEscolhaNaoImprime_DepoisDaEscolhaAVersaoVemDaVerdade()
    {
        ReceitaDeCaso r = Receita(casoA);
        r.comBoato = new ReceitaDeCaso.Versao { povo = 30, estado = -15, ouro = 40, penalidadePovo = -10 };
        Item fato = Pista("fato", casoA), boato = Pista("boato", casoA, Confiabilidade.Boato);
        Item f2 = Pista("f2", casoA), calunia = Pista("calunia", casoA, Confiabilidade.Calunia);
        casoA.deducao = new ConfiguracaoDeDeducao
        {
            ativa = true,
            pares = new List<ParContraditorio> { new ParContraditorio { a = fato, b = boato }, new ParContraditorio { a = f2, b = calunia } }
        };
        gm.ConfirmarCaso(casoA);
        inv.AddItem(fato);
        inv.AddItem(boato);
        inv.AddItem(f2); // a calúnia não foi achada: o par 2 não tem o que escolher

        NasEntradas(fato, boato);
        prensa.CombineItems();
        Assert.AreEqual(0, gm.panfletosPublicados.Count, "as duas afirmações de um par não vão juntas para a prensa");
        Assert.IsNotNull(prensa.slotInput1.item, "nada é consumido");

        inv.ClearItemSlot(prensa.slotInput1);
        inv.ClearItemSlot(prensa.slotInput2);
        NasEntradas(boato, f2);
        prensa.CombineItems();
        Assert.AreEqual(0, gm.panfletosPublicados.Count, "o par do boato ainda espera a escolha no quadro");

        Assert.IsTrue(Deducao.Escolher(gm, casoA, boato, out Item descartada), "qualquer escolha é aceita, inclusive o boato");
        Assert.AreSame(fato, descartada);
        Assert.IsFalse(inv.PossuiEmQualquerLugar(fato.itemID), "a descartada sai do inventário");
        prensa.CombineItems();
        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Assert.AreEqual(NivelDoPanfleto.ComBoato, gm.panfletosPublicados[0].nivel, "a versão vem da verdade, não da escolha");
        Assert.AreEqual(1, gm.revelacoesPendentes.Count, "o boato continua sendo exposto no fim da fase");
    }

    // ===== Biblioteca com o inventário real =====

    [Test]
    public void Compra_InventarioCheioReal_NaoCobra()
    {
        Item doc = Pista("doc", casoA, Confiabilidade.NaoEPista);
        doc.documentoDeSuporte = true;
        OfertaDaBiblioteca oferta = Criar<OfertaDaBiblioteca>("Oferta");
        oferta.id = "oferta"; oferta.caso = casoA; oferta.item = doc; oferta.preco = 10;
        gm.ConfirmarCaso(casoA);
        gm.capitalAtual = 50;
        Lotar();

        Assert.AreEqual(GameManager.ResultadoDaCompra.InventarioCheio, gm.ComprarNaBiblioteca(oferta));
        Assert.AreEqual(50, gm.capitalAtual);
        inv.RemoverItens(i => i.itemID == "lotado_0");
        Assert.AreEqual(GameManager.ResultadoDaCompra.Comprada, gm.ComprarNaBiblioteca(oferta));
        Assert.AreEqual(40, gm.capitalAtual);
        Assert.IsTrue(inv.HasItem("doc"));
    }

    // ===== Abas do inventário (Pistas / Panfletos) =====

    [Test]
    public void Inventario_AbaPanfletos_SeparaOsPanfletos_EGuardarNaAbaErradaMudaDeAba()
    {
        foreach (Transform linha in inv.inventoryGrid.transform) linha.gameObject.AddComponent<CanvasGroup>();
        Item pista = Pista("p1", casoA);
        Item panfleto = Criar<Item>("Panfleto");
        panfleto.itemID = "panf"; panfleto.ehPanfleto = true; panfleto.itemAmt = 1;

        Assert.IsTrue(inv.AddItem(pista));
        Assert.IsTrue(inv.AddItem(panfleto));
        Assert.AreEqual(AbaDoInventario.Pistas, inv.AbaAtual, "receber item com o inventário fechado não troca a aba");
        Assert.AreEqual(1, inv.QuantidadeNaAba(AbaDoInventario.Pistas));
        Assert.AreEqual(1, inv.QuantidadeNaAba(AbaDoInventario.Panfletos));

        UISlotHandler linhaDa(string id)
        {
            foreach (Transform t in inv.inventoryGrid.transform)
            {
                UISlotHandler s = t.GetComponent<UISlotHandler>();
                if (s.item != null && s.item.itemID == id) return s;
            }
            return null;
        }
        int vaziasVisiveis()
        {
            int n = 0;
            foreach (Transform t in inv.inventoryGrid.transform)
                if (t.GetComponent<UISlotHandler>().item == null && t.GetComponent<CanvasGroup>().alpha > 0f) n++;
            return n;
        }

        Assert.AreEqual(1f, linhaDa("p1").GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(0f, linhaDa("panf").GetComponent<CanvasGroup>().alpha, "panfleto não aparece na aba Pistas");
        Assert.AreEqual(1, vaziasVisiveis(), "uma linha vazia para guardar o que estiver na mão");

        inv.MostrarAba(AbaDoInventario.Panfletos);
        Assert.AreEqual(0f, linhaDa("p1").GetComponent<CanvasGroup>().alpha, "pista não aparece na aba Panfletos");
        Assert.AreEqual(1f, linhaDa("panf").GetComponent<CanvasGroup>().alpha);
        Assert.AreEqual(1, vaziasVisiveis(), "a aba Panfletos também tem a linha vazia");

        // Inventário aberto: guardar o panfleto da prensa com a aba Pistas aberta leva a lista para Panfletos.
        GameObject ui = Go("InventarioUI");
        inv.inventoryUI = ui;
        inv.MostrarAba(AbaDoInventario.Pistas);
        Item outro = Criar<Item>("Panfleto2");
        outro.itemID = "panf2"; outro.ehPanfleto = true; outro.itemAmt = 1;
        inv.PlaceInInventory(prensa.slotOutput, outro.Clone());
        Assert.AreEqual(AbaDoInventario.Pistas, inv.AbaAtual, "a saída da prensa não é linha da lista");

        UISlotHandler vazia = null;
        foreach (Transform t in inv.inventoryGrid.transform)
            if (t.GetComponent<UISlotHandler>().item == null) { vazia = t.GetComponent<UISlotHandler>(); break; }
        inv.PlaceInInventory(vazia, outro.Clone());
        Assert.AreEqual(AbaDoInventario.Panfletos, inv.AbaAtual);
        Assert.AreEqual(1f, linhaDa("panf2").GetComponent<CanvasGroup>().alpha, "o panfleto guardado fica à vista");
    }

    // ===== Ficha do panfleto (passar o mouse no inventário) =====

    [Test]
    public void FichaDoPanfleto_MostraCasoTeorAfirmacoesTomEEfeito_SemRevelarAVersao()
    {
        ReceitaDeCaso receita = ReceitaComLinha(casoA);
        receita.panfletoPadrao.ehPanfleto = true;
        receita.panfletoPadrao.itemName = "O Grito do Povo";
        casoA.caseTitle = "O Caso A";
        casoA.teorDoPanfleto = "Uma defesa do acusado A.";
        Item fato = Pista("f1", casoA), mentira = Pista("b1", casoA, Confiabilidade.Boato);
        fato.afirmacaoNoPanfleto = "A estava em casa naquela noite.";
        mentira.afirmacaoNoPanfleto = "O vizinho mentiu ao juiz.";
        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo");
        catalogo.itens = new List<Item> { fato, mentira };

        gm.ConfirmarCaso(casoA);
        NasEntradas(fato, mentira);
        prensa.ImprimirComLinhaEditorial(LinhaEditorial.Neutro);
        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Item impresso = prensa.slotOutput.item;
        Assert.IsTrue(impresso.EhPanfleto);

        string ficha = FichaDaPista.TextoDoPanfleto(impresso, gm, catalogo);
        StringAssert.Contains("O Grito do Povo", ficha);
        StringAssert.Contains("Caso: O Caso A", ficha);
        StringAssert.Contains("Uma defesa do acusado A.", ficha);
        StringAssert.Contains("A estava em casa naquela noite.", ficha);
        StringAssert.Contains("O vizinho mentiu ao juiz.", ficha);
        StringAssert.Contains("Tom: neutro", ficha);
        StringAssert.Contains("Efeito da publicação: Povo +", ficha);
        foreach (string verdade in new[] { "boato", "Boato", "calúnia", "Calúnia", "ComBoato", "verdadeir", "confirmada" })
            StringAssert.DoesNotContain(verdade, ficha, "a ficha nunca diz qual afirmação era verdadeira");

        // Sem publicação registrada (o panfleto do tutorial sai de receita exata): mostra a descrição do item.
        Item memorial = Criar<Item>("Memorial");
        memorial.itemID = "memorial"; memorial.ehPanfleto = true; memorial.descricao = "Memorial de defesa dos condenados.";
        string fichaDoTutorial = FichaDaPista.TextoDoPanfleto(memorial, gm, catalogo);
        StringAssert.Contains("Memorial de defesa dos condenados.", fichaDoTutorial);
        StringAssert.DoesNotContain("Efeito da publicação", fichaDoTutorial);
    }
}
