using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prompt 7: dicas de primeira vez (fila, persistência e Novo Jogo), Marie ausente pelo progresso (também depois de
/// carregar um save), 6h de investigação nas Fases 2 a 4, pause que não abre sob uma cutscene, Mesa de Casos como janela
/// modal e o Quadro de pistas desde a Fase 2.
/// As PlayerPrefs de progresso de quem está testando são guardadas no SetUp e devolvidas no TearDown.
/// </summary>
public class TutorialEUiTests
{
    private readonly List<Object> criados = new List<Object>();
    private readonly Dictionary<string, int?> prefsGuardadas = new Dictionary<string, int?>();
    private float? volumeGuardado;
    private const string ChaveVolume = "opt_volume";

    private GameManager gm;
    private TutorialManager tm;

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

    private static void DefinirEstatico(System.Type tipo, string propriedade, object valor) =>
        tipo.GetProperty(propriedade, BindingFlags.Public | BindingFlags.Static).GetSetMethod(true).Invoke(null, new[] { valor });

    [SetUp]
    public void Montar()
    {
        foreach (string chave in ProgressoDoJogo.ChavesDaPartida)
            prefsGuardadas[chave] = PlayerPrefs.HasKey(chave) ? PlayerPrefs.GetInt(chave) : (int?)null;
        volumeGuardado = PlayerPrefs.HasKey(ChaveVolume) ? PlayerPrefs.GetFloat(ChaveVolume) : (float?)null;

        gm = Go("GM").AddComponent<GameManager>();
        DefinirEstatico(typeof(GameManager), "Instance", gm);
        tm = Go("TM").AddComponent<TutorialManager>();
        DefinirEstatico(typeof(TutorialManager), "Instance", tm);
    }

    [TearDown]
    public void Desmontar()
    {
        DefinirEstatico(typeof(GameManager), "Instance", null);
        DefinirEstatico(typeof(TutorialManager), "Instance", null);
        DefinirEstatico(typeof(CutsceneLegendas), "EmExibicao", false);
        DefinirEstatico(typeof(CaseSelectionUI), "Aberta", false);
        Time.timeScale = 1f;
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();

        foreach (var par in prefsGuardadas)
        {
            if (par.Value.HasValue) PlayerPrefs.SetInt(par.Key, par.Value.Value);
            else PlayerPrefs.DeleteKey(par.Key);
        }
        if (volumeGuardado.HasValue) PlayerPrefs.SetFloat(ChaveVolume, volumeGuardado.Value);
        else PlayerPrefs.DeleteKey(ChaveVolume);
        PlayerPrefs.Save();
    }

    // ===== Dicas =====

    [Test]
    public void Dicas_PedidasJuntas_ViramFila_UmaDeCadaVez_ENaoSeRepetem()
    {
        PlayerPrefs.DeleteKey(TutorialManager.CHAVE_DICA_DESPESAS);
        PlayerPrefs.DeleteKey(TutorialManager.CHAVE_DICA_BIBLIOTECA);

        // Virada da Fase 3: a dica das despesas (fim da fase) e a da Biblioteca (botão novo) chegam juntas.
        tm.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_DESPESAS, tm.dicaDespesas);
        tm.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_BIBLIOTECA, tm.dicaBiblioteca);
        Assert.AreEqual(tm.dicaDespesas, tm.DicaPendente, "a primeira pedida aparece primeiro");
        tm.ConcluirDica();
        Assert.AreEqual(tm.dicaBiblioteca, tm.DicaPendente, "a segunda não se perde");
        tm.ConcluirDica();
        Assert.IsNull(tm.DicaPendente);

        tm.SolicitarDicaUmaVez(TutorialManager.CHAVE_DICA_DESPESAS, tm.dicaDespesas);
        Assert.IsNull(tm.DicaPendente, "uma dica já vista não volta");
        Assert.AreEqual(1, PlayerPrefs.GetInt(TutorialManager.CHAVE_DICA_DESPESAS), "marcada no pedido: vai no próximo save");
    }

    [Test]
    public void Dicas_PrimeiraPistaDoQuadro_PedeADicaDaDeducao_DepoisDaDeFatoEBoato()
    {
        PlayerPrefs.DeleteKey(TutorialManager.CHAVE_DICA_FATO_BOATO);
        PlayerPrefs.DeleteKey(TutorialManager.CHAVE_DICA_DEDUCAO);
        CaseData caso = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_Joalheiro.asset");
        MethodInfo aoReceber = typeof(TutorialManager).GetMethod("HandleItemAdicionado", BindingFlags.NonPublic | BindingFlags.Instance);

        aoReceber.Invoke(tm, new object[] { caso.alegacoesIniciais[0] }); // a carta do cliente, ao aceitar o caso
        Assert.AreEqual(tm.dicaFatoBoato, tm.DicaPendente);
        tm.ConcluirDica();
        Assert.IsNull(tm.DicaPendente, "a alegação fica fora do quadro: ainda sem a dica da dedução");

        aoReceber.Invoke(tm, new object[] { caso.deducao.pares[0].a });
        Assert.AreEqual(tm.dicaDeducao, tm.DicaPendente, "primeira pista de um par: a dica do Quadro de pistas");
        StringAssert.Contains("Quadro de pistas", tm.dicaFatoBoato, "a dica de fato/boato aponta o quadro");
        StringAssert.DoesNotContain("outra fonte que confirme", tm.dicaFatoBoato);
    }

    [Test]
    public void NovoJogo_ZeraTodasAsDicas_PreservaAsOpcoes()
    {
        string[] novas =
        {
            TutorialManager.CHAVE_DICA_DESPESAS, TutorialManager.CHAVE_DICA_BIBLIOTECA,
            TutorialManager.CHAVE_DICA_DEDUCAO, TutorialManager.CHAVE_DICA_LINHA_EDITORIAL
        };
        foreach (string chave in novas) CollectionAssert.Contains(ProgressoDoJogo.ChavesDaPartida, chave, "vai no save e zera no Novo Jogo");

        foreach (string chave in ProgressoDoJogo.ChavesDaPartida) PlayerPrefs.SetInt(chave, 1);
        PlayerPrefs.SetFloat(ChaveVolume, 0.3f);
        ProgressoDoJogo.ApagarProgresso();
        foreach (string chave in ProgressoDoJogo.ChavesDaPartida) Assert.IsFalse(PlayerPrefs.HasKey(chave), chave);
        Assert.AreEqual(0.3f, PlayerPrefs.GetFloat(ChaveVolume), 1e-4f, "opção do jogador continua");
    }

    // ===== Etapa "fale com Dupaty" =====

    [Test]
    public void EtapaDeConversa_SoAvancaComConversaDeNpc_NaoComPensamentoDoAlcapao()
    {
        DialogueSystem ds = Go("Dialogo").AddComponent<DialogueSystem>();
        gm.dialogueSystem = ds;
        tm.etapas = new List<TutorialStep>
        {
            new TutorialStep { etapaId = "falar", tipoDeAvanco = TipoDeAvanco.EventoDeJogo, nomeDoEvento = TutorialManager.EVENTO_DIALOGO_FINALIZADO },
            new TutorialStep { etapaId = "depois", tipoDeAvanco = TipoDeAvanco.CliqueDoJogador },
        };
        typeof(TutorialManager).GetField("indiceAtual", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tm, 0);
        MethodInfo terminar = typeof(DialogueSystem).GetMethod("EndDialogue", BindingFlags.NonPublic | BindingFlags.Instance);
        DialogueData Fala(string texto)
        {
            DialogueData d = Criar<DialogueData>("Fala_" + texto);
            d.talkScript = new List<Dialogue> { new Dialogue { name = "Julien Valois", text = texto, choices = new List<Choice>() } };
            return d;
        }

        // Pensamento do protagonista no alçapão (sem caso): fim do diálogo não conclui a etapa.
        TrapdoorInteractable alcapao = Go("Alcapao").AddComponent<TrapdoorInteractable>();
        alcapao.pensamentoSemCaso = Fala("ainda não");
        gm.casoEscolhido = null;
        alcapao.Interact();
        Assert.IsTrue(ds.IsDialogueActive);
        terminar.Invoke(ds, null);
        Assert.AreEqual("falar", tm.EtapaAtualId, "pensamento não é conversa");

        // Conversa com um NPC: conclui.
        NPCMovement dupaty = Go("Dupaty").AddComponent<NPCMovement>();
        dupaty.idDaInteracao = "Dupaty";
        dupaty.disableAfterDialogue = false;
        dupaty.dialogoPadrao = Fala("bom dia");
        dupaty.Interact();
        terminar.Invoke(ds, null);
        Assert.AreEqual("depois", tm.EtapaAtualId);
    }

    // ===== Marie depois do tutorial =====

    [Test]
    public void Marie_SomePeloProgresso_SemDependerDoPanfleto_TambemDepoisDeCarregar()
    {
        CaseData tutorial = Criar<CaseData>("Caso_Tutorial"); tutorial.fase = 1;
        Item panfleto = Criar<Item>("Panfleto_Tutorial"); panfleto.itemID = "panfleto_tutorial";

        Assert.IsFalse(GerenciadorCena1.TutorialEncerrado(gm, tutorial, panfleto), "jogo novo: Marie fica");
        gm.inventarioSalvo.Add(panfleto);
        Assert.IsTrue(GerenciadorCena1.TutorialEncerrado(gm, tutorial, panfleto), "reserva dos saves antigos");
        gm.inventarioSalvo.Clear();
        gm.casosConcluidos.Add(tutorial);
        Assert.IsTrue(GerenciadorCena1.TutorialEncerrado(gm, tutorial, panfleto), "panfleto impresso, mas fora do inventário");

        // Salvar e carregar: a regra lê o progresso que o save leva (fase), não o inventário.
        gm.faseAtual = 2;
        var catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        catalogo.casos.Add(tutorial);
        GameManager lido = Go("GM_Lido").AddComponent<GameManager>();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(JsonUtility.ToJson(SistemaDeSave.CapturarDados(gm, "Jogo", 15))), lido, catalogo);
        Assert.IsEmpty(lido.inventarioSalvo);
        Assert.IsTrue(GerenciadorCena1.TutorialEncerrado(lido, tutorial, panfleto));

        // A cena: Marie desativada e Dupaty sem conversa (na Fase 4 ele atende pelo CheckpointDoTribunal).
        GameObject marie = Go("Marie Bradier");
        NPCMovement dupaty = Go("Charles Dupaty").AddComponent<NPCMovement>();
        GerenciadorCena1 cena = Go("Sc").AddComponent<GerenciadorCena1>();
        cena.casoTutorial = tutorial; cena.panfletoCraftado = panfleto;
        cena.npcsParaSumir = new[] { marie };
        cena.npcsParaSilenciar = new[] { dupaty };
        DefinirEstatico(typeof(GameManager), "Instance", lido);
        typeof(GerenciadorCena1).GetMethod("VerificarEstadoDaCena", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(cena, null);
        Assert.IsFalse(marie.activeSelf);
        Assert.IsFalse(dupaty.canInteract);
    }

    // ===== Orçamento de horas =====

    [Test]
    public void Orcamento_SeisHorasNasFasesDoisATres_CincoEndividado()
    {
        GameManager prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/GameManager.prefab").GetComponent<GameManager>();
        Assert.AreEqual(6, prefab.horasPorCaso);
        Assert.AreEqual(6, gm.horasPorCaso, "o padrão do código é o mesmo do prefab");

        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI.prefab");
        foreach (CaseData caso in ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases)
            if (caso.fase >= 2) Assert.AreEqual(6, ValidadorDaCampanha.HorasDoCaso(caso, prefab), caso.name);

        // Um caso real da Fase 2 no relógio: 6h; endividado, 5h (a conversão continua 1h por interação nova).
        gm.faseAtual = 2;
        CaseData joias = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_Joalheiro.asset");
        Assert.IsTrue(gm.ConfirmarCaso(joias));
        Assert.AreEqual(6, gm.horasRestantes);

        GameManager devendo = Go("GM_Devendo").AddComponent<GameManager>();
        devendo.faseAtual = 2; devendo.capitalAtual = -5;
        Assert.IsTrue(devendo.ConfirmarCaso(joias));
        Assert.AreEqual(5, devendo.horasDoCaso);
    }

    // ===== Janelas modais =====

    private PauseMenu Pause()
    {
        PauseMenu pause = Go("PauseMenu").AddComponent<PauseMenu>();
        pause.container = Go("Container", pause.transform);
        pause.container.SetActive(false);
        return pause;
    }

    [Test]
    public void Pause_NaoAbrePorBaixoDeUmaCutscene()
    {
        PauseMenu pause = Pause();
        DefinirEstatico(typeof(CutsceneLegendas), "EmExibicao", true);
        pause.TogglePauseMobile();
        Assert.IsFalse(pause.IsOpen, "a cutscene devolveria o timeScale com o pause aberto");

        DefinirEstatico(typeof(CutsceneLegendas), "EmExibicao", false);
        pause.TogglePauseMobile();
        Assert.IsTrue(pause.IsOpen);
        Assert.AreEqual(0f, Time.timeScale);
        pause.ResumirButton();
        Assert.AreEqual(1f, Time.timeScale);
    }

    [Test]
    public void MesaDeCasosAberta_EhModal_SemPauseNemInventarioPorCima()
    {
        PauseMenu pause = Pause();
        GameObject grade = Go("Grade");
        var slot = Go("Slot", grade.transform).AddComponent<UISlotHandler>();
        slot.slotImg = Go("Img", slot.transform).AddComponent<Image>();
        slot.itemCount = Go("Qtd", slot.transform).AddComponent<TextMeshProUGUI>();
        InventoryManager inv = Go("Inventario").AddComponent<InventoryManager>();
        inv.inventoryGrid = grade;
        inv.inventoryUI = Go("InventarioUI");
        inv.inventoryUI.SetActive(false);
        gm.inventoryManager = inv;

        DefinirEstatico(typeof(CaseSelectionUI), "Aberta", true);
        Assert.IsTrue(JanelasModais.AlgumaAberta, "movimento e interação com o mundo travam");
        Assert.IsTrue(JanelasModais.BloqueiaPausa);
        pause.TogglePauseMobile();
        Assert.IsFalse(pause.IsOpen, "o Esc/pause da mesa não abre o pause por cima");
        inv.ToggleInventory();
        Assert.IsFalse(inv.inventoryUI.activeSelf, "nem o inventário");

        DefinirEstatico(typeof(CaseSelectionUI), "Aberta", false);
        Assert.IsFalse(JanelasModais.AlgumaAberta);
    }

    // ===== Quadro de pistas desde a Fase 2 =====

    [Test]
    public void Quadro_DisponivelNaFaseDois_ComCasoRealEmAndamento()
    {
        QuadroDeDeducaoUI quadro = Go("Quadro").AddComponent<QuadroDeDeducaoUI>();
        Assert.AreEqual(2, quadro.faseMinima, "padrão do código");
        gm.faseAtual = 2;
        CaseData operario = AssetDatabase.LoadAssetAtPath<CaseData>("Assets/Casos/Caso_Operario.asset");
        Assert.IsFalse(quadro.Disponivel, "sem caso em andamento");
        Assert.IsTrue(gm.ConfirmarCaso(operario));
        Assert.IsTrue(quadro.Disponivel);
        gm.ConcluirCaso(operario);
        Assert.IsFalse(quadro.Disponivel, "depois de publicar, some");
    }
}
