using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Prompt 2: conteúdo e progressão. Usa os assets reais do projeto (casos, receitas, mesa, catálogo, cenas) e
/// objetos temporários para as regras da prensa e da mesa.
/// </summary>
public class CampanhaTests
{
    private readonly List<Object> criados = new List<Object>();

    [TearDown]
    public void Limpar()
    {
        foreach (Object o in criados) if (o != null) Object.DestroyImmediate(o);
        criados.Clear();
    }

    private static List<CaseData> CasosDaMesa()
    {
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI.prefab");
        return new List<CaseData>(ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases);
    }

    [Test]
    public void Validador_CampanhaSemProblemas()
    {
        ValidadorDaCampanha.Resultado resultado = ValidadorDaCampanha.Validar();
        Assert.IsEmpty(resultado.problemas, resultado.Texto());
    }

    [Test]
    public void CartasDoCliente_SaoSoLeitura_EAPrensaSegueFatoXBoato()
    {
        CaseData caso = ScriptableObject.CreateInstance<CaseData>();
        Item Pista(string id, Confiabilidade c)
        {
            Item i = ScriptableObject.CreateInstance<Item>();
            i.itemID = id; i.caso = caso; i.confiabilidade = c;
            criados.Add(i);
            return i;
        }
        criados.Add(caso);
        // A segunda carta imita um asset antigo, ainda marcado como Boato: continua sendo só leitura.
        Item a1 = Pista("a1", Confiabilidade.NaoEPista), a2 = Pista("a2", Confiabilidade.Boato);
        Item fato = Pista("f", Confiabilidade.Fato), calunia = Pista("c", Confiabilidade.Calunia), boato = Pista("b", Confiabilidade.Boato);
        caso.alegacoesIniciais = new List<Item> { a1, a2 };

        foreach (Item carta in new[] { a1, a2 })
        {
            Assert.IsTrue(carta.EhLeitura, carta.itemID);
            Assert.IsFalse(carta.EhPista, $"{carta.itemID}: carta do cliente não é pista");
        }
        Assert.IsFalse(fato.EhLeitura);
        Assert.IsTrue(fato.EhPista);
        Assert.AreEqual(NivelDoPanfleto.Fatos, ReceitaDeCaso.Classificar(fato, Pista("f2", Confiabilidade.Fato)));
        Assert.AreEqual(NivelDoPanfleto.ComBoato, ReceitaDeCaso.Classificar(fato, boato));
        Assert.AreEqual(NivelDoPanfleto.Calunia, ReceitaDeCaso.Classificar(boato, calunia));
    }

    [Test]
    public void CartasReais_SaoSoLeitura_EmTodosOsCasos()
    {
        foreach (CaseData caso in CasosDaMesa())
        {
            if (caso.fase < 2) continue; // o tutorial não tem cartas
            Assert.IsNotEmpty(caso.alegacoesIniciais, caso.name);
            foreach (Item carta in caso.alegacoesIniciais)
            {
                string onde = $"{caso.name}: {carta.name}";
                Assert.AreEqual(Confiabilidade.NaoEPista, carta.confiabilidade, onde);
                Assert.IsTrue(carta.EhLeitura, onde);
                Assert.IsFalse(carta.EhPista, onde);
                Assert.IsFalse(carta.EhSuporte, onde);
                Assert.AreEqual("Carta do cliente", carta.fonte, onde);
                Assert.IsFalse(string.IsNullOrWhiteSpace(carta.descricao), $"{onde}: a carta precisa ter o que ler");
            }
        }
    }

    // ===== Balanceamento (17/10) =====

    private static int Desnivel(ReceitaDeCaso.Versao v, bool depoisDaRevelacao) =>
        (v.povo + (depoisDaRevelacao ? v.penalidadePovo : 0)) - (v.estado + (depoisDaRevelacao ? v.penalidadeEstado : 0));

    [Test]
    public void Balanceamento_MentiraRendeMaisOuroMasEmpurraMenosARota_EFatosCabemNaBarra()
    {
        foreach (CaseData caso in CasosDaMesa())
        {
            if (caso.fase < 2) continue;
            ReceitaDeCaso r = caso.receitaDoPanfleto;
            Assert.LessOrEqual(Mathf.Abs(r.soFatos.povo), 25, $"{caso.name}: um panfleto não pode encher meia barra");
            Assert.LessOrEqual(Mathf.Abs(r.soFatos.estado), 25, caso.name);
            Assert.AreEqual(r.soFatos.ouro, caso.moneyReward, $"{caso.name}: a Mesa mostra a versão Fatos");
            Assert.AreEqual(r.soFatos.povo, caso.publicOpinionReward, caso.name);
            Assert.AreEqual(r.soFatos.estado, caso.stateOpinionReward, caso.name);
            int fatos = Mathf.Abs(Desnivel(r.soFatos, false));
            int lado = System.Math.Sign(Desnivel(r.soFatos, false));
            int agravamento = r.linhaEditorial.agravamentoPercentual;
            foreach (ReceitaDeCaso.Versao mentira in new[] { r.comBoato, r.calunia })
            {
                Assert.Greater(mentira.ouro, r.soFatos.ouro, $"{caso.name}: mentir rende mais ouro na hora");
                Assert.LessOrEqual(Mathf.Abs(Desnivel(mentira, true)), fatos,
                    $"{caso.name}: depois de desmentida, a mentira não pode empurrar a rota mais que os fatos");

                // Sensacionalista (17/10): agrava a perda, mas não a ponto de virar a mentira contra o próprio lado.
                int pp = mentira.penalidadePovo + CalculadoraDePanfleto.Agravamento(mentira.penalidadePovo, agravamento);
                int pe = mentira.penalidadeEstado + CalculadoraDePanfleto.Agravamento(mentira.penalidadeEstado, agravamento);
                int saldo = (mentira.povo + pp) - (mentira.estado + pe);
                if (lado != 0)
                    Assert.AreNotEqual(-lado, System.Math.Sign(saldo),
                        $"{caso.name}: com o sensacionalista, a mentira desmentida não pode virar contra o próprio lado (saldo {saldo})");
            }
            Assert.LessOrEqual(r.linhaEditorial.sensacionalista.ouro, 10, $"{caso.name}: o sensacionalista paga pouco a mais");
            Assert.LessOrEqual(agravamento, 25, $"{caso.name}: o desmentido sensacionalista pesa no máximo +25%");
        }
    }

    [Test]
    public void Balanceamento_NenhumParDeCasosTravaARota_EOHonestoNaoTerminaAFase3Endividado()
    {
        GameManager padrao = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab").GetComponent<GameManager>();
        Recipe tutorial = AssetDatabase.LoadAssetAtPath<Recipe>("Assets/ScriptableObjects/Items/ReceitaFase1.asset");
        List<CaseData> casos = CasosDaMesa();
        List<CaseData> fase2 = casos.FindAll(c => c.fase == 2), fase3 = casos.FindAll(c => c.fase == 3);
        NivelDoPanfleto[] niveis = { NivelDoPanfleto.Fatos, NivelDoPanfleto.ComBoato, NivelDoPanfleto.Calunia };

        // Mesma ordem do jogo: panfleto (com o tom) → limite 0–100 → revelação no fim da fase → despesas.
        void Publicar(ReceitaDeCaso r, NivelDoPanfleto n, LinhaEditorial t, ref int povo, ref int estado, ref int ouro, int despesa)
        {
            ReceitaDeCaso.Versao v = r.VersaoDe(n);
            ReceitaDeCaso.ModificadorEditorial m = r.linhaEditorial.De(t);
            int pp = v.penalidadePovo, pe = v.penalidadeEstado;
            if (t == LinhaEditorial.Sensacionalista && n != NivelDoPanfleto.Fatos)
            {
                pp += CalculadoraDePanfleto.Agravamento(pp, r.linhaEditorial.agravamentoPercentual);
                pe += CalculadoraDePanfleto.Agravamento(pe, r.linhaEditorial.agravamentoPercentual);
            }
            povo = Mathf.Clamp(Mathf.Clamp(povo + v.povo + m.povo, 0, 100) + pp, 0, 100);
            estado = Mathf.Clamp(Mathf.Clamp(estado + v.estado + m.estado, 0, 100) + pe, 0, 100);
            ouro += v.ouro + m.ouro - despesa;
        }

        foreach (CaseData c2 in fase2)
            foreach (CaseData c3 in fase3)
            {
                var rotas = new HashSet<RotaFinal>();
                foreach (NivelDoPanfleto n2 in niveis) foreach (LinhaEditorial t2 in LinhasEditoriais.Opcoes)
                foreach (NivelDoPanfleto n3 in niveis) foreach (LinhaEditorial t3 in LinhasEditoriais.Opcoes)
                {
                    int povo = Mathf.Clamp(50 + tutorial.publicOpinionImpact, 0, 100);
                    int estado = Mathf.Clamp(50 + tutorial.stateOpinionImpact, 0, 100);
                    int ouro = tutorial.moneyReward;
                    Publicar(c2.receitaDoPanfleto, n2, t2, ref povo, ref estado, ref ouro, padrao.despesasPorFase[1]);
                    Publicar(c3.receitaDoPanfleto, n3, t3, ref povo, ref estado, ref ouro, padrao.despesasPorFase[2]);
                    int desnivel = povo - estado;
                    rotas.Add(desnivel >= padrao.margemDaRota ? RotaFinal.A_Guilhotina
                            : desnivel <= -padrao.margemDaRota ? RotaFinal.B_Tirano : RotaFinal.C_Equilibrio);
                    if (n2 == NivelDoPanfleto.Fatos && n3 == NivelDoPanfleto.Fatos)
                        Assert.GreaterOrEqual(ouro, 0, $"{c2.name} → {c3.name} ({t2}, {t3}): só com fatos e sem biblioteca, sem dívida");
                }
                Assert.GreaterOrEqual(rotas.Count, 2, $"{c2.name} → {c3.name}: a investigação e o tom precisam poder mudar a rota");
            }
    }

    [Test]
    public void Fase4_UmCasoPorRota_EFase3ComTresCasosDistintos()
    {
        List<CaseData> casos = CasosDaMesa();
        Assert.AreEqual(3, casos.FindAll(c => c.fase == 3).Count, "decisão do grupo (27/09): três casos na Fase 3");
        Assert.IsNull(casos.Find(c => c.name == "Caso_Padeiro"), "o caso do Padeiro saiu da campanha");
        foreach (RotaFinal rota in new[] { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio })
            Assert.AreEqual(1, casos.FindAll(c => c.fase == 4 && c.rota == rota).Count, rota.ToString());
    }

    [Test]
    public void Mesa_Fase3_UmCasoEntreTres_OsOutrosFicamBloqueados_EAFase4Comeca()
    {
        var go = new GameObject("GM_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        gm.faseAtual = 3;
        gm.casoEscolhido = null;
        List<CaseData> fase3 = CasosDaMesa().FindAll(c => c.fase == 3);
        Assert.AreEqual(1, gm.CasosNecessariosNaFase, "a Fase 3 pede um caso");

        foreach (CaseData caso in fase3) Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Disponivel, CaseSelectionUI.EstadoDe(caso, gm));
        Assert.IsTrue(gm.ConfirmarCaso(fase3[0]));
        Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.EmAndamento, CaseSelectionUI.EstadoDe(fase3[0], gm));
        for (int i = 1; i < fase3.Count; i++)
        {
            Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Bloqueado, CaseSelectionUI.EstadoDe(fase3[i], gm), "escolhido um, os outros bloqueiam");
            Assert.IsFalse(gm.ConfirmarCaso(fase3[i]));
        }

        Assert.IsTrue(gm.ConcluirCaso(fase3[0]));
        Assert.IsTrue(gm.FaseConcluida, "concluir o escolhido basta");
        Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Concluido, CaseSelectionUI.EstadoDe(fase3[0], gm));
        for (int i = 1; i < fase3.Count; i++)
        {
            Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Bloqueado, CaseSelectionUI.EstadoDe(fase3[i], gm), "continuam bloqueados depois da conclusão");
            Assert.IsFalse(gm.ConfirmarCaso(fase3[i]));
        }

        gm.EncerrarFase(); // o que o FimDeFase faz ao voltar ao escritório
        Assert.AreEqual(4, gm.faseAtual, "a Fase 4 começa");
        Assert.AreNotEqual(RotaFinal.Nenhuma, gm.rotaFinal);
        foreach (CaseData caso in fase3) Assert.IsFalse(CaseSelectionUI.CasoVisivel(caso, gm), "os casos da Fase 3 saem da mesa");
    }

    [Test]
    public void Catalogo_ResolveTodosOsCasosEItensDaCampanha()
    {
        CatalogoDeSave catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>("Assets/Resources/CatalogoDeSave.asset");
        foreach (CaseData caso in CasosDaMesa())
        {
            Assert.AreSame(caso, catalogo.BuscarCaso(caso.name), caso.name);
            foreach (Item alegacao in caso.alegacoesIniciais) Assert.AreSame(alegacao, catalogo.BuscarItem(alegacao.itemID), alegacao.name);
            Assert.IsNotNull(catalogo.BuscarItem(caso.receitaDoPanfleto.panfletoPadrao.itemID), caso.name);
        }
    }
}
