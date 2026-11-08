using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Modo dev (apresentação): com os casos e pistas reais da campanha, cada atalho monta uma partida que as regras do jogo
/// aceitam — os três finais chegam ao Tribunal com a rota certa, o Dupaty libera a ida e o réu tem o destino pedido; o
/// início de cada fase deixa a mesa com os casos daquela fase (na Fase 4, só o da rota).
/// </summary>
public class ModoDevTests
{
    private static readonly RotaFinal[] Rotas = { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio };

    private readonly List<Object> criados = new List<Object>();
    private CatalogoDeSave catalogo;

    [SetUp]
    public void Montar()
    {
        catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        Assert.IsNotNull(catalogo, "Resources/CatalogoDeSave");
    }

    [TearDown]
    public void Desmontar()
    {
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private GameManager NovoGameManager()
    {
        var go = new GameObject("GM");
        criados.Add(go);
        return go.AddComponent<GameManager>();
    }

    private GameManager Preparado(ModoDev.Destino destino, RotaFinal rota, bool reuAbsolvido)
    {
        GameManager gm = NovoGameManager();
        string erro = ModoDev.Preparar(gm, catalogo, destino, rota, reuAbsolvido);
        Assert.IsNull(erro, $"{destino}/{rota}: {erro}");
        return gm;
    }

    [Test]
    public void Tribunal_CadaFinal_TravaARota_LiberaODupaty_EDaODestinoPedidoAoReu()
    {
        var checkpoint = new GameObject("Dupaty").AddComponent<CheckpointDoTribunal>();
        criados.Add(checkpoint.gameObject);

        foreach (RotaFinal rota in Rotas)
            foreach (bool absolvido in new[] { true, false })
            {
                string onde = $"{rota}, réu {(absolvido ? "absolvido" : "condenado")}";
                GameManager gm = Preparado(ModoDev.Destino.Tribunal, rota, absolvido);

                Assert.AreEqual(GameManager.UltimaFase, gm.faseAtual, onde);
                Assert.AreEqual(rota, gm.rotaFinal, onde);
                CaseData caso = gm.casoEscolhido;
                Assert.IsNotNull(caso, onde);
                Assert.AreEqual(GameManager.UltimaFase, caso.fase, onde);
                Assert.AreEqual(rota, caso.rota, onde);

                Assert.AreEqual(CheckpointDoTribunal.Situacao.Pronto, checkpoint.Avaliar(gm, out CaseData julgado), onde);
                Assert.AreSame(caso, julgado, onde);
                Assert.AreEqual(absolvido, TribunalManager.ReuAbsolvido(gm, caso), onde);

                // Provas do tribunal: as pistas que ficaram no quadro + o panfleto; nenhuma descartada nem carta do cliente.
                List<Item> provas = TribunalManager.ProvasDoCaso(gm, caso, catalogo);
                Assert.GreaterOrEqual(provas.Count, 2, $"{onde}: provas suficientes para encerrar a defesa");
                foreach (Item prova in provas)
                {
                    Assert.IsFalse(Deducao.Descartada(gm, prova), $"{onde}: '{prova.name}' foi descartada no quadro");
                    Assert.IsFalse(prova.EhLeitura, $"{onde}: '{prova.name}' é carta do cliente");
                }
                Assert.AreEqual(Deducao.TotalDeContradicoes(caso), Deducao.ParesDecididos(gm, caso), $"{onde}: todos os pares decididos");

                // Uma fase passada de cada: tutorial, Fase 2 e Fase 3 concluídos, cada uma com o seu caso publicado.
                for (int fase = 1; fase < GameManager.UltimaFase; fase++)
                {
                    int f = fase;
                    Assert.IsTrue(gm.casosConcluidos.Exists(c => c != null && c.fase == f), $"{onde}: caso da Fase {fase} concluído");
                }
            }
    }

    [Test]
    public void InicioDaFase_MesaComOsCasosDaFase_ENaFase4SoODaRota()
    {
        foreach (ModoDev.Destino destino in new[] { ModoDev.Destino.Fase2, ModoDev.Destino.Fase3, ModoDev.Destino.Fase4 })
            foreach (RotaFinal rota in destino == ModoDev.Destino.Fase4 ? Rotas : new[] { RotaFinal.Nenhuma })
            {
                string onde = $"{destino}/{rota}";
                GameManager gm = Preparado(destino, rota, true);
                int fase = ModoDev.FaseDe(destino);

                Assert.AreEqual(fase, gm.faseAtual, onde);
                Assert.IsTrue(gm.TutorialConcluido, onde);
                Assert.IsFalse(gm.CasoAtualEmAndamento, $"{onde}: a mesa fica livre");
                Assert.IsFalse(gm.FaseConcluida, $"{onde}: a fase nova ainda não foi jogada");
                Assert.AreEqual(rota, gm.rotaFinal, onde);
                Assert.GreaterOrEqual(gm.capitalAtual, ModoDev.OuroMinimo, onde);
                Assert.AreEqual(0, gm.revelacoesPendentes.Count, $"{onde}: as fases passadas saíram só com fatos");

                var aceitaveis = catalogo.casos.FindAll(c => c != null && gm.PodeAceitarCaso(c, out _));
                Assert.IsNotEmpty(aceitaveis, $"{onde}: algum caso na mesa");
                foreach (CaseData c in aceitaveis)
                {
                    Assert.AreEqual(fase, c.fase, $"{onde}: '{c.name}' é de outra fase");
                    if (fase == GameManager.UltimaFase) Assert.AreEqual(rota, c.rota, $"{onde}: '{c.name}' é de outra rota");
                }
            }
    }

    [Test]
    public void FasesPassadas_UsamOCasoQueMaisPuxaParaARota()
    {
        GameManager a = Preparado(ModoDev.Destino.Fase4, RotaFinal.A_Guilhotina, true);
        GameManager b = Preparado(ModoDev.Destino.Fase4, RotaFinal.B_Tirano, true);
        Assert.Greater(a.opiniaoPublicaAtual - a.opiniaoEstadoAtual, b.opiniaoPublicaAtual - b.opiniaoEstadoAtual,
            "a rota A chega com o Povo mais alto em relação ao Estado do que a rota B");

        foreach (int fase in new[] { 2, 3 })
        {
            CaseData doA = ModoDev.CasoParaARota(catalogo, fase, RotaFinal.A_Guilhotina);
            CaseData doB = ModoDev.CasoParaARota(catalogo, fase, RotaFinal.B_Tirano);
            ReceitaDeCaso.Versao fa = doA.receitaDoPanfleto.soFatos, fb = doB.receitaDoPanfleto.soFatos;
            Assert.GreaterOrEqual(fa.povo - fa.estado, fb.povo - fb.estado, $"Fase {fase}: o caso da rota A puxa mais para o Povo");
        }
    }

    [Test]
    public void Fase1_NaoMontaNada()
    {
        GameManager gm = Preparado(ModoDev.Destino.Fase1, RotaFinal.Nenhuma, true);
        Assert.AreEqual(1, gm.faseAtual);
        Assert.IsEmpty(gm.casosConcluidos);
        Assert.IsFalse(gm.TutorialConcluido);
    }

    [Test]
    public void Fase4ETribunal_SemRota_Recusam()
    {
        Assert.IsNotNull(ModoDev.Preparar(NovoGameManager(), catalogo, ModoDev.Destino.Fase4, RotaFinal.Nenhuma, true));
        Assert.IsNotNull(ModoDev.Preparar(NovoGameManager(), catalogo, ModoDev.Destino.Tribunal, RotaFinal.Nenhuma, true));
    }
}
