using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Regras do Prompt 1 (estabilidade da investigação, publicação e save), sem cena: registro por caso/interação,
/// IDs estáveis, guardas de domínio do GameManager e migração do save v2 → v3.
/// Rodar em Window > General > Test Runner > EditMode.
/// </summary>
public class InvestigacaoESaveTests
{
    private readonly List<Object> criados = new List<Object>();

    private T Criar<T>(string nome) where T : ScriptableObject
    {
        T objeto = ScriptableObject.CreateInstance<T>();
        objeto.name = nome;
        criados.Add(objeto);
        return objeto;
    }

    private CaseData CriarCaso(string nome, int fase = 2)
    {
        CaseData caso = Criar<CaseData>(nome);
        caso.caseTitle = nome;
        caso.fase = fase;
        return caso;
    }

    private GameManager CriarGameManager()
    {
        var go = new GameObject("GameManager_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>(); // em Edit Mode o Awake não roda: nada de singleton/DontDestroyOnLoad
        gm.faseAtual = 2;
        gm.casoEscolhido = null;
        return gm;
    }

    [TearDown]
    public void Limpar()
    {
        foreach (Object objeto in criados)
            if (objeto != null) Object.DestroyImmediate(objeto);
        criados.Clear();
    }

    // ===== Registro por caso e interação =====

    [Test]
    public void Registro_PagamentoValeSoParaOMesmoCasoEInteracao()
    {
        CaseData casoA = CriarCaso("CasoA"), casoB = CriarCaso("CasoB");
        var registro = new RegistroDaInvestigacao();

        registro.RegistrarPagamento(casoA, "Fase2/Joalheiro");
        registro.RegistrarPagamento(casoA, "Fase2/Joalheiro");

        Assert.IsTrue(registro.InteracaoPaga(casoA, "Fase2/Joalheiro"));
        Assert.IsFalse(registro.InteracaoPaga(casoB, "Fase2/Joalheiro"), "outro caso tem estado próprio");
        Assert.IsFalse(registro.InteracaoPaga(casoA, "Fase2/Operario"));
        Assert.AreEqual(1, registro.interacoesPagas.Count, "registrar de novo não duplica");
    }

    [Test]
    public void Registro_RecompensaNaoDependeDoInventario_ESeparaCasos()
    {
        CaseData casoA = CriarCaso("CasoA"), casoB = CriarCaso("CasoB");
        var registro = new RegistroDaInvestigacao();

        registro.RegistrarRecompensa(casoA, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_1");

        Assert.IsTrue(registro.RecompensaEntregue(casoA, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_1"));
        Assert.IsFalse(registro.RecompensaEntregue(casoA, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_2"));
        Assert.IsFalse(registro.RecompensaEntregue(casoB, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_1"));
    }

    [Test]
    public void Registro_LootPorCaso()
    {
        CaseData casoA = CriarCaso("CasoA"), casoB = CriarCaso("CasoB");
        var registro = new RegistroDaInvestigacao();

        registro.RegistrarLoot(casoA, "Fase3/Estante");

        Assert.IsTrue(registro.LootColetado(casoA, "Fase3/Estante"));
        Assert.IsFalse(registro.LootColetado(casoB, "Fase3/Estante"), "o mesmo objeto pode servir a outro caso");
        Assert.IsFalse(registro.LootColetado(null, "Fase3/Estante"));
    }

    // ===== IDs estáveis =====

    [Test]
    public void Id_HomonimosTemChavesDistintas_EIdRepetidoEhApontado()
    {
        var sala1 = new GameObject("Sala1"); var sala2 = new GameObject("Sala2");
        var estante1 = new GameObject("Estante"); var estante2 = new GameObject("Estante");
        criados.AddRange(new Object[] { sala1, sala2, estante1, estante2 });
        estante1.transform.SetParent(sala1.transform);
        estante2.transform.SetParent(sala2.transform);
        LootInteractable loot1 = estante1.AddComponent<LootInteractable>();
        LootInteractable loot2 = estante2.AddComponent<LootInteractable>();

        Assert.AreEqual(IdDeInteracao.ChaveAntiga(loot1), IdDeInteracao.ChaveAntiga(loot2), "a chave antiga (nome) colidia");
        Assert.AreNotEqual(IdDeInteracao.ChaveDaCena(loot1, ""), IdDeInteracao.ChaveDaCena(loot2, ""));

        var repetidos = new List<KeyValuePair<Component, string>>
        {
            new KeyValuePair<Component, string>(loot1, "estante"),
            new KeyValuePair<Component, string>(loot2, "estante"),
        };
        Assert.AreEqual(1, IdDeInteracao.Problemas(repetidos).Count);

        var vazios = new List<KeyValuePair<Component, string>>
        {
            new KeyValuePair<Component, string>(loot1, ""),
            new KeyValuePair<Component, string>(loot2, "  "),
        };
        Assert.AreEqual(2, IdDeInteracao.Problemas(vazios).Count, "IDs vazios são avisados (mas os caminhos não colidem)");
    }

    // ===== Mesa de Casos (domínio) =====

    [Test]
    public void ConfirmarCaso_MesmoCasoNaoReiniciaRelogio()
    {
        GameManager gm = CriarGameManager();
        CaseData caso = CriarCaso("CasoA");

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Assert.AreEqual(gm.horasPorCaso, gm.horasRestantes);

        gm.horasRestantes = 0;
        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Assert.AreEqual(0, gm.horasRestantes, "confirmar de novo não devolve horas");
    }

    [Test]
    public void ConfirmarCaso_RecusaTrocaDeCasoEmAndamento_ECasoConcluido()
    {
        GameManager gm = CriarGameManager();
        CaseData casoA = CriarCaso("CasoA"), casoB = CriarCaso("CasoB"), casoC = CriarCaso("CasoC");

        Assert.IsTrue(gm.ConfirmarCaso(casoA));
        Assert.IsFalse(gm.ConfirmarCaso(casoB), "só um caso em andamento");
        Assert.AreSame(casoA, gm.casoEscolhido);

        Assert.IsTrue(gm.ConcluirCaso(casoA));
        Assert.IsFalse(gm.ConfirmarCaso(casoA), "caso concluído não volta");
        Assert.IsTrue(gm.ConfirmarCaso(casoB));
        Assert.IsFalse(gm.ConfirmarCaso(CriarCaso("CasoFase3", fase: 3)), "caso de outra fase");
        Assert.IsTrue(gm.ConcluirCaso(casoB));
        Assert.IsTrue(gm.ConfirmarCaso(casoC));
    }

    [Test]
    public void ConfirmarCaso_EndividadoPerdeUmaHora_SemRecarregarNoMesmoCaso()
    {
        GameManager gm = CriarGameManager();
        gm.capitalAtual = -10;
        CaseData caso = CriarCaso("CasoA");

        Assert.IsTrue(gm.ConfirmarCaso(caso));
        Assert.AreEqual(gm.horasPorCaso - gm.horasPerdidasPorDivida, gm.horasDoCaso);

        gm.capitalAtual = 100;
        gm.horasRestantes = 1;
        gm.ConfirmarCaso(caso);
        Assert.AreEqual(1, gm.horasRestantes, "quitar a dívida não recarrega o caso já iniciado");
    }

    // ===== Publicação única =====

    [Test]
    public void Publicacao_UmaVezPorCaso_SoDoCasoEmAndamento()
    {
        GameManager gm = CriarGameManager();
        CaseData casoA = CriarCaso("CasoA"), casoB = CriarCaso("CasoB");
        gm.ConfirmarCaso(casoA);

        Assert.IsFalse(gm.PodePublicarCaso(casoB, out _), "pistas de outro caso");
        Assert.IsFalse(gm.PodePublicarCaso(null, out _));
        Assert.IsTrue(gm.PodePublicarCaso(casoA, out _));

        var versao = new ReceitaDeCaso.Versao { penalidadePovo = -10 };
        gm.RegistrarPanfletoDeCaso(casoA, NivelDoPanfleto.ComBoato, versao);
        gm.RegistrarPanfletoDeCaso(casoA, NivelDoPanfleto.ComBoato, versao);
        Assert.IsTrue(gm.ConcluirCaso(casoA));
        Assert.IsFalse(gm.ConcluirCaso(casoA));

        Assert.AreEqual(1, gm.panfletosPublicados.Count);
        Assert.AreEqual(1, gm.revelacoesPendentes.Count, "a penalidade do boato não é agendada duas vezes");
        Assert.IsFalse(gm.PodePublicarCaso(casoA, out string motivo));
        Assert.IsNotEmpty(motivo);
    }

    // ===== Save =====

    private CatalogoDeSave CriarCatalogo(params Object[] conteudo)
    {
        CatalogoDeSave catalogo = Criar<CatalogoDeSave>("Catalogo_Teste");
        foreach (Object o in conteudo)
        {
            if (o is CaseData caso) catalogo.casos.Add(caso);
            if (o is Item item) catalogo.itens.Add(item);
        }
        return catalogo;
    }

    private void RegistrarClones(GameManager gm) => criados.AddRange(gm.inventarioSalvo);

    [Test]
    public void Save_V2ComCasoEmAndamento_MigraSemApagarProgresso()
    {
        CaseData tutorial = CriarCaso("Caso_Tut", fase: 1), casoA = CriarCaso("CasoA");
        Item pista = Criar<Item>("Pista_A");
        pista.itemID = "pista_a";
        CatalogoDeSave catalogo = CriarCatalogo(tutorial, casoA, pista);

        const string jsonV2 = @"{
            ""versao"": 2, ""cena"": ""Fase2"", ""etapaTutorial"": 14,
            ""capital"": -30, ""opiniaoPublica"": 61, ""opiniaoEstado"": 40,
            ""casoEscolhido"": ""CasoA"", ""casosJaSelecionados"": [""CasoA""], ""faseAtual"": 2,
            ""horasDoCaso"": 3, ""horasRestantes"": 0,
            ""interacoesPagas"": [""Fase2/Joalheiro"", ""Fase2/Estante""],
            ""casosConcluidos"": [""Caso_Tut""],
            ""inventario"": [{ ""itemID"": ""pista_a"", ""quantidade"": 2 }],
            ""pistasVerificadas"": [""pista_a""]
        }";

        SistemaDeSave.DadosDeSave dados = SistemaDeSave.DesserializarDados(jsonV2);
        GameManager gm = CriarGameManager();
        SistemaDeSave.AplicarDados(dados, gm, catalogo);
        RegistrarClones(gm);

        Assert.AreEqual(-30, gm.capitalAtual, "capital negativo preservado");
        Assert.AreEqual(61, gm.opiniaoPublicaAtual);
        Assert.AreEqual(2, gm.faseAtual);
        Assert.AreEqual(0, gm.horasRestantes, "zero horas continua zero");
        Assert.AreEqual(3, gm.horasDoCaso);
        Assert.AreSame(casoA, gm.casoEscolhido);
        Assert.IsTrue(gm.CasoAtualEmAndamento);
        Assert.AreEqual(1, gm.inventarioSalvo.Count);
        Assert.AreEqual(2, gm.inventarioSalvo[0].itemAmt);
        CollectionAssert.Contains(gm.pistasVerificadas, "pista_a");

        RegistroDaInvestigacao registro = gm.registroDaInvestigacao;
        Assert.IsTrue(registro.PagaNoSaveAntigo(casoA, "Fase2/Joalheiro"));
        Assert.IsTrue(registro.PagaNoSaveAntigo(casoA, "Fase2/Estante"));
        Assert.IsFalse(registro.PagaNoSaveAntigo(casoA, "Fase2/Operario"));
        Assert.IsTrue(registro.CasoComEstadoLegado(casoA));
        Assert.IsEmpty(registro.interacoesPagas, "nenhuma chave nova inventada: a conversão acontece na interação");
    }

    [Test]
    public void Save_V2SemCasoEmAndamento_DescartaSoAsChavesDoCasoEncerrado()
    {
        CaseData casoA = CriarCaso("CasoA");
        CatalogoDeSave catalogo = CriarCatalogo(casoA);
        const string jsonV2 = @"{ ""versao"": 2, ""capital"": 15, ""casoEscolhido"": ""CasoA"", ""casosJaSelecionados"": [""CasoA""],
            ""casosConcluidos"": [""CasoA""], ""faseAtual"": 3, ""interacoesPagas"": [""Fase2/Joalheiro""] }";

        GameManager gm = CriarGameManager();
        SistemaDeSave.AplicarDados(SistemaDeSave.DesserializarDados(jsonV2), gm, catalogo);

        Assert.AreEqual(15, gm.capitalAtual);
        Assert.AreEqual(3, gm.faseAtual);
        CollectionAssert.Contains(gm.casosConcluidos, casoA);
        Assert.IsEmpty(gm.registroDaInvestigacao.interacoesPagasLegadas);
        Assert.IsEmpty(gm.registroDaInvestigacao.casosComEstadoLegado);
    }

    [Test]
    public void Save_V3IdaEVolta_PreservaRegistroEProgresso()
    {
        CaseData casoA = CriarCaso("CasoA");
        CatalogoDeSave catalogo = CriarCatalogo(casoA);

        GameManager origem = CriarGameManager();
        origem.ConfirmarCaso(casoA);
        origem.capitalAtual = -5;
        origem.horasRestantes = 0;
        origem.registroDaInvestigacao.RegistrarPagamento(casoA, "Fase2/Joalheiro");
        origem.registroDaInvestigacao.RegistrarLoot(casoA, "Fase2/Estante");
        origem.registroDaInvestigacao.RegistrarRecompensa(casoA, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_1");

        string json = JsonUtility.ToJson(SistemaDeSave.CapturarDados(origem, "Fase2", 14));
        SistemaDeSave.DadosDeSave lido = SistemaDeSave.DesserializarDados(json);
        Assert.AreEqual(SistemaDeSave.VersaoAtual, lido.versao);

        GameManager destino = CriarGameManager();
        SistemaDeSave.AplicarDados(lido, destino, catalogo);

        Assert.AreEqual(-5, destino.capitalAtual);
        Assert.AreEqual(0, destino.horasRestantes);
        Assert.AreSame(casoA, destino.casoEscolhido);
        RegistroDaInvestigacao registro = destino.registroDaInvestigacao;
        Assert.IsTrue(registro.InteracaoPaga(casoA, "Fase2/Joalheiro"));
        Assert.IsTrue(registro.LootColetado(casoA, "Fase2/Estante"));
        Assert.IsTrue(registro.RecompensaEntregue(casoA, "Fase2/Joalheiro", RegistroDaInvestigacao.EtapaReacao, "pista_joias_1"));
        Assert.IsFalse(registro.CasoComEstadoLegado(casoA), "save novo não entra no modo legado");
    }
}
