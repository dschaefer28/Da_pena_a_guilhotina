using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Relatório de testes de 28/09, item 4 (Ferramentas > UI > Aplicar layout da Mesa de Casos). Confere nos prefabs reais o
/// que deixa a Mesa legível: cartão escuro e opaco, dimensionado pela linha de cartões, com o "Aceitar Caso" alinhado
/// embaixo; barra de rolagem que aparece quando os cartões não cabem; "Fechar" na mesma fonte dos cartões.
/// </summary>
public class MesaDeCasosTests
{
    private const string PrefabUI = "Assets/_Project/Prefabs/UI.prefab";
    private const string PrefabCartao = "Assets/_Project/Prefabs/CartaoPrefab.prefab";

    [Test]
    public void Cartao_EscuroOpacoEDimensionadoPelaLinha()
    {
        GameObject cartao = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCartao);
        Assert.IsNotNull(cartao, PrefabCartao);

        Color fundo = cartao.GetComponent<Image>().color;
        Assert.AreEqual(1f, fundo.a, 1e-3f, "Cartão translúcido.");
        Assert.Less(fundo.grayscale, 0.25f, "Cartão claro: o texto claro perde contraste.");
        Assert.IsNull(cartao.GetComponent<ContentSizeFitter>(), "Com ContentSizeFitter o cartão brigaria com a linha de cartões.");

        Transform espacador = cartao.transform.Find("Espacador");
        Transform botoes = cartao.transform.Find("ButtonRow");
        Assert.IsNotNull(espacador, "Sem espaçador, os \"Aceitar Caso\" de uma linha ficam desalinhados.");
        Assert.AreEqual(botoes.GetSiblingIndex() - 1, espacador.GetSiblingIndex(), "O espaçador fica logo antes da linha do botão.");
        Assert.Greater(espacador.GetComponent<LayoutElement>().flexibleHeight, 0f);

        var descricao = cartao.transform.Find("DescricaoTexto").GetComponent<TextMeshProUGUI>();
        Assert.AreNotEqual(TextAlignmentOptions.TopJustified, descricao.alignment, "Justificado abre buracos na coluna.");
    }

    [Test]
    public void Mesa_ColunasBarraDeRolagemEFonteDoFechar()
    {
        GameObject ui = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabUI);
        CaseSelectionUI mesa = ui.GetComponentInChildren<CaseSelectionUI>(true);
        Assert.IsNotNull(mesa, "Mesa de Casos não está no UI.prefab.");
        Assert.GreaterOrEqual(mesa.cartoesPorLinha, 3, "Os casos de uma fase (três) devem caber lado a lado.");
        Assert.Greater(mesa.corDoVeuDeBloqueio.a, 0.5f, "Caso bloqueado precisa ficar claramente mais escuro.");

        ScrollRect rolagem = mesa.transform.Find("AreaDosCartoes").GetComponent<ScrollRect>();
        Assert.IsNotNull(rolagem.verticalScrollbar, "Sem barra, nada indica que há mais cartões.");
        Assert.AreEqual(ScrollRect.ScrollbarVisibility.AutoHide, rolagem.verticalScrollbarVisibility);
        Assert.AreEqual(Navigation.Mode.None, rolagem.verticalScrollbar.navigation.mode, "A barra roubaria a seta do teclado dos cartões.");

        TMP_FontAsset fonteDosCartoes = mesa.cardPrefab.transform.Find("TituloTexto").GetComponent<TextMeshProUGUI>().font;
        TextMeshProUGUI fechar = mesa.transform.Find("BotaoFechar").GetComponentInChildren<TextMeshProUGUI>(true);
        Assert.AreEqual(fonteDosCartoes, fechar.font, "O Fechar usa outra fonte.");
    }
}
