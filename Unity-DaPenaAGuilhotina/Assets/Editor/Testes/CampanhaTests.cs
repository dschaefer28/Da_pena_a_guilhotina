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
        var ui = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI.prefab");
        return new List<CaseData>(ui.GetComponentInChildren<CaseSelectionUI>(true).availableCases);
    }

    [Test]
    public void Validador_CampanhaSemProblemas()
    {
        ValidadorDaCampanha.Resultado resultado = ValidadorDaCampanha.Validar();
        Assert.IsEmpty(resultado.problemas, resultado.Texto());
    }

    [Test]
    public void Prensa_DuasAlegacoesUsamVersaoPropria_MisturadasSeguemFatoXBoato()
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
        Item a1 = Pista("a1", Confiabilidade.Boato), a2 = Pista("a2", Confiabilidade.Boato);
        Item fato = Pista("f", Confiabilidade.Fato), calunia = Pista("c", Confiabilidade.Calunia), boato = Pista("b", Confiabilidade.Boato);
        caso.alegacoesIniciais = new List<Item> { a1, a2 };

        Assert.AreEqual(NivelDoPanfleto.Alegacoes, ReceitaDeCaso.Classificar(a1, a2));
        Assert.AreEqual(NivelDoPanfleto.Alegacoes, ReceitaDeCaso.Classificar(a1, fato), "1 fato + 1 alegação não pode render como a investigação");
        Assert.AreEqual(NivelDoPanfleto.Alegacoes, ReceitaDeCaso.Classificar(boato, a2));
        Assert.AreEqual(NivelDoPanfleto.Calunia, ReceitaDeCaso.Classificar(a2, calunia), "calúnia continua sendo calúnia");
        Assert.AreEqual(NivelDoPanfleto.Fatos, ReceitaDeCaso.Classificar(fato, Pista("f2", Confiabilidade.Fato)));
    }

    [Test]
    public void AtalhoComAlegacao_NuncaRendeMaisQueAInvestigacao_EmNenhumCaso()
    {
        foreach (CaseData caso in CasosDaMesa())
        {
            ReceitaDeCaso r = caso.receitaDoPanfleto;
            Item fato = null;
            foreach (string guid in AssetDatabase.FindAssets("t:Item"))
            {
                Item i = AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid));
                if (i.caso == caso && i.confiabilidade == Confiabilidade.Fato) { fato = i; break; }
            }
            Assert.IsNotNull(fato, caso.name);
            ResultadoDoPanfleto atalho = CalculadoraDePanfleto.Calcular(r, fato, caso.alegacoesIniciais[0], null);
            Assert.AreEqual(NivelDoPanfleto.Alegacoes, atalho.nivel, caso.name);
            Assert.Less(atalho.ouro, r.soFatos.ouro, caso.name);
            Assert.Less(Mathf.Abs(atalho.povo) + Mathf.Abs(atalho.estado), Mathf.Abs(r.soFatos.povo) + Mathf.Abs(r.soFatos.estado), caso.name);
        }
    }

    [Test]
    public void Receitas_SoAlegacoesRendemMenosQueFatos_ETemConsequenciaPropria()
    {
        foreach (CaseData caso in CasosDaMesa())
        {
            ReceitaDeCaso r = caso.receitaDoPanfleto;
            Assert.IsNotNull(r, caso.name);
            Assert.Less(r.soAlegacoes.ouro, r.soFatos.ouro, $"{caso.name}: a saída não pode render mais ouro que a investigação");
            Assert.LessOrEqual(Mathf.Abs(r.soAlegacoes.povo) + Mathf.Abs(r.soAlegacoes.estado),
                               Mathf.Abs(r.soFatos.povo) + Mathf.Abs(r.soFatos.estado), $"{caso.name}: impacto político menor");
            Assert.IsTrue(r.soAlegacoes.TemRevelacao, $"{caso.name}: a saída tem consequência própria");
        }
    }

    [Test]
    public void Fase4_UmCasoPorRota_EFase3ComQuatroCasosDistintos()
    {
        List<CaseData> casos = CasosDaMesa();
        Assert.AreEqual(4, casos.FindAll(c => c.fase == 3).Count);
        foreach (RotaFinal rota in new[] { RotaFinal.A_Guilhotina, RotaFinal.B_Tirano, RotaFinal.C_Equilibrio })
            Assert.AreEqual(1, casos.FindAll(c => c.fase == 4 && (c.rota == rota || c.rota == RotaFinal.Nenhuma)).Count, rota.ToString());
    }

    [Test]
    public void Mesa_TresEstados_EFase3ExigeDoisCasosDistintos()
    {
        var go = new GameObject("GM_Teste");
        criados.Add(go);
        GameManager gm = go.AddComponent<GameManager>();
        gm.faseAtual = 3;
        gm.casoEscolhido = null;
        List<CaseData> fase3 = CasosDaMesa().FindAll(c => c.fase == 3);

        Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Disponivel, CaseSelectionUI.EstadoDe(fase3[0], gm));
        Assert.IsTrue(gm.ConfirmarCaso(fase3[0]));
        Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.EmAndamento, CaseSelectionUI.EstadoDe(fase3[0], gm));
        Assert.IsFalse(gm.ConfirmarCaso(fase3[1]), "só um em andamento");

        Assert.IsTrue(gm.ConcluirCaso(fase3[0]));
        Assert.AreEqual(CaseSelectionUI.EstadoDoCaso.Concluido, CaseSelectionUI.EstadoDe(fase3[0], gm));
        Assert.IsFalse(gm.FaseConcluida, "a Fase 3 pede dois casos");
        Assert.IsFalse(gm.ConfirmarCaso(fase3[0]), "concluído não volta");

        Assert.IsTrue(gm.ConfirmarCaso(fase3[1]));
        Assert.IsTrue(gm.ConcluirCaso(fase3[1]));
        Assert.IsTrue(gm.FaseConcluida);
        Assert.IsFalse(gm.ConfirmarCaso(fase3[2]), "a fase pede dois: um terceiro caso não é aceito");
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
