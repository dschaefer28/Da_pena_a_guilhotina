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
        casoA.fase = 4;
        gm.faseAtual = 4;
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
    public void Prensa_FatoMaisAlegacao_UsaVersaoInferior_EApoioNaoEhGasto()
    {
        ReceitaDeCaso r = Receita(casoA);
        Item apoio = Pista("apoio", casoA, Confiabilidade.NaoEPista);
        apoio.documentoDeSuporte = true; apoio.qualidade = QualidadeDaEvidencia.Superior;
        r.qualificadores.Add(new ReceitaDeCaso.Qualificador { id = "q", suportesAceitos = new List<Item> { apoio }, povo = 1 });
        gm.ConfirmarCaso(casoA);
        // Alegações definidas depois de aceitar: aqui as peças vêm à mão, sem a entrega automática.
        Item alegacao = Pista("aleg", casoA, Confiabilidade.Boato);
        casoA.alegacoesIniciais = new List<Item> { alegacao, Pista("aleg2", casoA, Confiabilidade.Boato) };

        inv.AddItem(apoio);
        Assert.IsTrue(prensa.AlternarSuporte(inv.ItemNaGrade("apoio")));
        NasEntradas(Pista("fato", casoA), alegacao);
        prensa.CombineItems();

        GameManager.PanfletoPublicado p = gm.panfletosPublicados[0];
        Assert.AreEqual(NivelDoPanfleto.Alegacoes, p.nivel);
        Assert.AreEqual(6, p.povo, "versão inferior (5) + apoio (1)");
        Assert.IsTrue(p.aplicadoConhecido);
        Assert.AreEqual(1, gm.revelacoesPendentes.Count, "a versão inferior tem consequência própria");
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
        gm.faseAtual = 4;
        gm.ConfirmarCaso(final);
        inv.AddItem(Pista("prova", final));
        NasEntradas(Pista("g1", final), Pista("g2", final));
        prensa.CombineItems();
        Assert.IsTrue(inv.HasItem("prova"), "o caso da Fase 4 leva as provas ao tribunal");
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
}
