using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prompt 6 (o que restava em 27/09): limites e margem da rota, rota travada, filtro da Fase 4, mesa e porta pela
/// regra de progresso do tutorial, inventário cheio ao restaurar sem perder itens, o checkpoint do Dupaty e o mínimo de
/// provas para encerrar a defesa.
/// </summary>
public class ProgressaoETribunalTests
{
    private readonly List<Object> criados = new List<Object>();
    private GameManager gm;

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

    private CaseData Caso(string nome, int fase, RotaFinal rota = RotaFinal.Nenhuma)
    {
        CaseData c = Criar<CaseData>(nome);
        c.caseTitle = nome; c.fase = fase; c.rota = rota;
        return c;
    }

    private Item ItemDe(string id, CaseData caso = null, Confiabilidade conf = Confiabilidade.NaoEPista)
    {
        Item i = Criar<Item>(id);
        i.itemID = id; i.caso = caso; i.confiabilidade = conf; i.itemAmt = 1;
        return i;
    }

    private static void DefinirInstancia(GameManager valor) =>
        typeof(GameManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new object[] { valor });

    [SetUp]
    public void Montar()
    {
        gm = Go("GM").AddComponent<GameManager>();
        DefinirInstancia(gm);
        gm.casoEscolhido = null;
    }

    [TearDown]
    public void Desmontar()
    {
        DefinirInstancia(null);
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    // ===== Rota =====

    [Test]
    public void Rota_LimitesDaMargemPadrao()
    {
        Assert.AreEqual(20, gm.margemDaRota);
        var esperado = new Dictionary<int, RotaFinal>
        {
            { -21, RotaFinal.B_Tirano }, { -20, RotaFinal.B_Tirano }, { -19, RotaFinal.C_Equilibrio }, { 0, RotaFinal.C_Equilibrio },
            { 19, RotaFinal.C_Equilibrio }, { 20, RotaFinal.A_Guilhotina }, { 21, RotaFinal.A_Guilhotina },
        };
        foreach (var par in esperado)
        {
            gm.opiniaoPublicaAtual = 50 + par.Key;
            gm.opiniaoEstadoAtual = 50;
            Assert.AreEqual(par.Value, gm.CalcularRota(), $"desnível {par.Key}");
        }
    }

    [Test]
    public void Rota_MargemInvalida_EmpateNaoPendeParaNenhumLado()
    {
        foreach (int margem in new[] { 0, -5 })
        {
            gm.margemDaRota = margem; // só por código: no Inspector o mínimo é 1
            gm.opiniaoPublicaAtual = gm.opiniaoEstadoAtual = 50;
            Assert.AreEqual(RotaFinal.C_Equilibrio, gm.CalcularRota(), $"margem {margem}: empate");
            gm.opiniaoPublicaAtual = 51;
            Assert.AreEqual(RotaFinal.A_Guilhotina, gm.CalcularRota(), $"margem {margem}: +1");
            gm.opiniaoPublicaAtual = 49;
            Assert.AreEqual(RotaFinal.B_Tirano, gm.CalcularRota(), $"margem {margem}: -1");
        }
    }

    [Test]
    public void Rota_TravadaNaFase4_NaoMudaComAsBarrasNemComOSave()
    {
        gm.faseAtual = 3;
        gm.opiniaoPublicaAtual = 80; gm.opiniaoEstadoAtual = 50;
        gm.EncerrarFase();
        Assert.AreEqual(4, gm.faseAtual);
        Assert.AreEqual(RotaFinal.A_Guilhotina, gm.rotaFinal);

        gm.AplicarImpactoPanfleto(-60, 50, 0); // o panfleto da Fase 4 inverte as barras
        Assert.AreEqual(RotaFinal.A_Guilhotina, gm.rotaFinal, "a Fase 4 mexe nas barras sem recalcular o final");

        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo");
        GameManager lido = Go("GM_Lido").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Jogo", 14))), lido, catalogo);
        Assert.AreEqual(RotaFinal.A_Guilhotina, lido.rotaFinal, "salvar e carregar mantém a rota");
        Assert.AreEqual(RotaFinal.B_Tirano, lido.CalcularRota(), "mesmo com as barras apontando para outra");
    }

    [Test]
    public void Fase4_MostraSoOCasoDaRota_ECasoSemRotaNuncaApareceJunto()
    {
        CaseData daRotaA = Caso("RotaA", 4, RotaFinal.A_Guilhotina), daRotaB = Caso("RotaB", 4, RotaFinal.B_Tirano);
        CaseData semRota = Caso("SemRota", 4), daFase3 = Caso("Fase3", 3);
        gm.faseAtual = 4; gm.rotaFinal = RotaFinal.B_Tirano;

        Assert.IsTrue(CaseSelectionUI.CasoVisivel(daRotaB, gm));
        Assert.IsFalse(CaseSelectionUI.CasoVisivel(daRotaA, gm));
        Assert.IsFalse(CaseSelectionUI.CasoVisivel(semRota, gm), "caso sem rota não aparece na Fase 4");
        Assert.IsFalse(gm.PodeAceitarCaso(semRota, out _));
        Assert.IsFalse(gm.PodeAceitarCaso(daRotaA, out _));
        Assert.IsTrue(gm.PodeAceitarCaso(daRotaB, out _));

        gm.faseAtual = 3; gm.rotaFinal = RotaFinal.Nenhuma;
        Assert.IsTrue(CaseSelectionUI.CasoVisivel(daFase3, gm), "antes da Fase 4 os casos sem rota continuam valendo");
    }

    // ===== Mesa e porta: regra de progresso =====

    [Test]
    public void MesaEPortaDoEscritorio_SeguemOTutorial_NaoOPanfletoNoInventario()
    {
        CaseData tutorial = Caso("Tutorial", 1);
        gm.faseAtual = 1;
        gm.casosPorFase = new[] { 1, 1, 1, 1 };
        Assert.IsTrue(gm.ConfirmarCaso(tutorial));

        TableInteractable mesa = Go("Mesa").AddComponent<TableInteractable>();
        DoorInteractable porta = Go("Porta").AddComponent<DoorInteractable>();
        porta.exigeTutorialConcluido = true;
        DoorInteractable portaDaRua = Go("PortaDaRua").AddComponent<DoorInteractable>();

        Assert.IsTrue(mesa.exigeTutorialConcluido, "a mesa exige o tutorial por padrão");
        Assert.IsFalse(mesa.PodeInteragir, "tutorial em andamento: mesa trancada");
        Assert.IsFalse(porta.PodeInteragir, "e a porta do escritório também");
        Assert.IsTrue(portaDaRua.PodeInteragir, "portas sem a regra continuam livres");

        gm.ConcluirCaso(tutorial); // o panfleto saiu da prensa; nenhum item no inventário é exigido
        Assert.IsTrue(mesa.PodeInteragir);
        Assert.IsTrue(porta.PodeInteragir);

        gm.casosConcluidos.Clear(); gm.faseAtual = 2; // ex.: save sem o registro do tutorial, já na Fase 2
        Assert.IsTrue(mesa.PodeInteragir, "a fase já basta");
    }

    // ===== Inventário cheio ao restaurar =====

    private InventoryManager Inventario(int slots)
    {
        GameObject grade = Go("Grade");
        for (int i = 0; i < slots; i++)
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

    [Test]
    public void Restaurar_ComAGradeCheia_GuardaOQueNaoCabe_ENadaSePerde()
    {
        InventoryManager inv = Inventario(2);
        Item a = ItemDe("a"), b = ItemDe("b"), panfleto = ItemDe("panfleto_do_caso");
        gm.inventarioSalvo = new List<Item> { a.Clone(), b.Clone(), panfleto.Clone() };
        foreach (Item i in gm.inventarioSalvo) criados.Add(i);

        inv.RestaurarInventario();
        Assert.IsTrue(inv.HasItem("a") && inv.HasItem("b"));
        Assert.IsFalse(inv.HasItem("panfleto_do_caso"), "não coube na grade");
        Assert.AreEqual(1, gm.itensForaDaGrade.Count, "mas ficou guardado");
        Assert.IsTrue(inv.PossuiEmQualquerLugar("panfleto_do_caso"));

        inv.SalvarEstadoAtual(); // trocar de cena ou salvar
        Assert.AreEqual(3, gm.inventarioSalvo.Count, "o item guardado vai junto");
        Assert.AreEqual(3, SistemaDeSave.CapturarDados(gm, "Jogo", 0).inventario.Count, "e entra no save");

        inv.RemoverItens(i => i.itemID == "a"); // a grade ganha espaço (ex.: arquivamento ao concluir um caso)
        Assert.IsTrue(inv.HasItem("panfleto_do_caso"), "o espaço liberado recebe o item guardado");
        Assert.AreEqual(0, gm.itensForaDaGrade.Count);
    }

    // ===== Checkpoint do tribunal (Dupaty) =====

    [Test]
    public void Checkpoint_SoLiberaComOCasoDaRotaPublicado_EPistasComprovadas()
    {
        CheckpointDoTribunal dupaty = Go("Checkpoint").AddComponent<CheckpointDoTribunal>();
        CaseData julgado = Caso("Julgado", 4, RotaFinal.A_Guilhotina), deOutraRota = Caso("OutraRota", 4, RotaFinal.B_Tirano);
        ReceitaDeCaso receita = Criar<ReceitaDeCaso>("Receita");
        receita.caso = julgado;
        receita.panfletoPadrao = ItemDe("panfleto_julgado");
        julgado.receitaDoPanfleto = receita;
        Item p1 = ItemDe("p1", julgado, Confiabilidade.Fato), p2 = ItemDe("p2", julgado, Confiabilidade.Fato);

        gm.faseAtual = 3;
        Assert.AreEqual(CheckpointDoTribunal.Situacao.ForaDaFase4, dupaty.Avaliar(gm, out _));

        gm.faseAtual = 4; gm.rotaFinal = RotaFinal.A_Guilhotina;
        Assert.IsTrue(gm.ConfirmarCaso(julgado));
        Assert.AreEqual(CheckpointDoTribunal.Situacao.CasoNaoConcluido, dupaty.Avaliar(gm, out _), "em andamento");

        gm.casosConcluidos.Add(deOutraRota);
        Assert.AreEqual(CheckpointDoTribunal.Situacao.CasoNaoConcluido, dupaty.Avaliar(gm, out _), "caso de outra rota não conta");

        gm.casosConcluidos.Add(julgado);
        Assert.AreEqual(CheckpointDoTribunal.Situacao.SemPanfleto, dupaty.Avaliar(gm, out _), "concluído sem publicação registrada");

        gm.panfletosPublicados.Add(new GameManager.PanfletoPublicado { caso = julgado, nivel = NivelDoPanfleto.Fatos, pistas = new List<string> { "p1", "p2" } });
        gm.RegistrarEvidencia(p1);
        Assert.AreEqual(CheckpointDoTribunal.Situacao.PistasSemRegistro, dupaty.Avaliar(gm, out _), "uma pista impressa sem histórico");

        gm.RegistrarEvidencia(p2);
        Assert.AreEqual(CheckpointDoTribunal.Situacao.Pronto, dupaty.Avaliar(gm, out CaseData caso));
        Assert.AreSame(julgado, caso);

        Item exigida = ItemDe("exigida", julgado);
        dupaty.evidenciasExigidas.Add(new CheckpointDoTribunal.EvidenciasDoCaso { caso = julgado, evidencias = new List<Item> { exigida } });
        Assert.AreEqual(CheckpointDoTribunal.Situacao.FaltaEvidencia, dupaty.Avaliar(gm, out _));
        gm.RegistrarEvidencia(exigida);
        Assert.AreEqual(CheckpointDoTribunal.Situacao.Pronto, dupaty.Avaliar(gm, out _));

        gm.panfletosPublicados[0].legado = true; // publicação de save antigo, sem o registro das pistas
        gm.evidenciasObtidas.Remove("p2");
        Assert.AreEqual(CheckpointDoTribunal.Situacao.Pronto, dupaty.Avaliar(gm, out _), "save antigo não é barrado");
    }

    // ===== Consequências aplicadas uma vez =====

    [Test]
    public void EncerrarFase_AplicaUmaVez_ESalvarECarregarNaoReaplica()
    {
        gm.faseAtual = 3;
        gm.opiniaoPublicaAtual = 60;
        CaseData caso = Caso("Boato", 3);
        gm.revelacoesPendentes.Add(new GameManager.RevelacaoPendente { caso = caso, texto = "Desmentido.", povo = -10 });
        gm.EncerrarFase();
        Assert.AreEqual(50, gm.opiniaoPublicaAtual);

        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo");
        catalogo.casos.Add(caso);
        GameManager lido = Go("GM_Lido").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Jogo", 14))), lido, catalogo);
        CollectionAssert.IsEmpty(lido.revelacoesPendentes, "a revelação já cobrada não volta");
        Assert.AreEqual(50, lido.opiniaoPublicaAtual);
        Assert.IsFalse(lido.FaseConcluida, "na fase nova nada está concluído: o FimDeFase não encerra de novo ao reabrir a cena");
    }

    // ===== Tribunal =====

    [Test]
    public void Tribunal_EncerrarADefesa_ExigeUmMinimoDeProvas()
    {
        Assert.IsFalse(TribunalManager.PodeEncerrar(0, 5, 2));
        Assert.IsFalse(TribunalManager.PodeEncerrar(1, 5, 2), "uma prova não basta para encerrar");
        Assert.IsTrue(TribunalManager.PodeEncerrar(2, 5, 2));
        Assert.IsTrue(TribunalManager.PodeEncerrar(1, 1, 2), "com uma prova só, o mínimo acompanha o total");
        Assert.IsFalse(TribunalManager.PodeEncerrar(0, 3, 0), "nunca sem nenhuma prova");
    }
}
