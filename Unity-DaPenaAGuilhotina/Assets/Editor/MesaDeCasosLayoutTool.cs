using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mesa de Casos legível (relatório de testes de 28/09, item 4). Nos prefabs, não nas cenas:
/// - CartaoPrefab: fundo escuro e opaco (era cinza-claro 0,56 com texto bege/branco, pouco contraste), sem o
///   ContentSizeFitter (a linha de cartões controla o tamanho), com um espaçador antes do "Aceitar Caso", para os
///   botões dos cartões da mesma linha ficarem alinhados embaixo, e a descrição alinhada à esquerda (justificada, abria
///   buracos na coluna). Os cartões ficam em colunas (CaseSelectionUI).
/// - UI.prefab, Painel_MesaDeCasos: barra de rolagem (aparece só quando os cartões não cabem, indicando que há mais
///   para ver; fora da navegação do teclado), margem direita para ela e o "Fechar" na fonte Cinzel dos outros textos da
///   mesa (estava em LiberationSans).
/// Idempotente: só troca o que ainda está com o valor antigo; o resto fica como está.
/// Menu: Ferramentas > UI > Aplicar layout da Mesa de Casos
/// </summary>
public static class MesaDeCasosLayoutTool
{
    private const string PrefabUI = "Assets/_Project/Prefabs/UI.prefab";
    private const string PrefabCartao = "Assets/_Project/Prefabs/CartaoPrefab.prefab";
    private const string CaminhoDaMesa = "Painel_MesaDeCasos";

    private static readonly Color CorAntigaDoCartao = new Color(0.557f, 0.528f, 0.528f, 1f);
    public static readonly Color CorDoCartao = new Color(0.18f, 0.13f, 0.11f, 1f); // a das linhas da Biblioteca e do Quadro
    private static readonly Color CorDaBarra = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color CorDaAlca = new Color(0.98f, 0.85f, 0.55f, 0.9f);
    private const float LarguraDaBarra = 12f;
    private const int MargemDireitaComBarra = 28;

    [MenuItem("Ferramentas/UI/Aplicar layout da Mesa de Casos")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[MesaDeCasosLayout]\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        r.AppendLine("  Cartão: " + AjustarCartao());
        r.AppendLine("  Mesa: " + AjustarMesa());
        return r.ToString();
    }

    private static string AjustarCartao()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabCartao);
        try
        {
            var feito = new StringBuilder();
            Image fundo = raiz.GetComponent<Image>();
            if (fundo != null && Parecida(fundo.color, CorAntigaDoCartao)) { fundo.color = CorDoCartao; feito.Append("fundo escuro e opaco; "); }

            ContentSizeFitter ajuste = raiz.GetComponent<ContentSizeFitter>();
            if (ajuste != null) { Object.DestroyImmediate(ajuste); feito.Append("sem ContentSizeFitter; "); }

            if (raiz.GetComponent<LayoutElement>() == null) { raiz.AddComponent<LayoutElement>().flexibleWidth = 0f; feito.Append("LayoutElement; "); }

            // Na coluna (~600 de largura) o texto justificado abria buracos entre as palavras; alinhado à esquerda, como o
            // objetivo e as consequências do mesmo cartão.
            TextMeshProUGUI descricao = raiz.transform.Find("DescricaoTexto")?.GetComponent<TextMeshProUGUI>();
            if (descricao != null && descricao.alignment == TextAlignmentOptions.TopJustified)
            {
                descricao.alignment = TextAlignmentOptions.TopLeft;
                feito.Append("descrição alinhada à esquerda; ");
            }

            Transform botoes = raiz.transform.Find("ButtonRow");
            if (botoes != null && raiz.transform.Find("Espacador") == null)
            {
                var espacador = new GameObject("Espacador", typeof(RectTransform), typeof(LayoutElement));
                espacador.layer = raiz.layer;
                espacador.transform.SetParent(raiz.transform, false);
                espacador.transform.SetSiblingIndex(botoes.GetSiblingIndex());
                var le = espacador.GetComponent<LayoutElement>();
                le.flexibleHeight = 1f; le.minHeight = 0f;
                feito.Append("espaçador antes do Aceitar Caso; ");
            }

            if (feito.Length == 0) return "já ajustado.";
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabCartao);
            return feito.ToString().TrimEnd(' ', ';') + ".";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static string AjustarMesa()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            Transform mesa = raiz.transform.Find(CaminhoDaMesa);
            if (mesa == null) return $"'{CaminhoDaMesa}' não existe no UI.prefab; nada alterado.";
            var feito = new StringBuilder();

            // Fechar na fonte dos cartões (Cinzel-Regular SDF).
            TextMeshProUGUI rotuloFechar = mesa.Find("BotaoFechar")?.GetComponentInChildren<TextMeshProUGUI>(true);
            TMP_FontAsset cinzel = FonteDosCartoes();
            if (rotuloFechar != null && cinzel != null && rotuloFechar.font != cinzel)
            {
                rotuloFechar.font = cinzel;
                rotuloFechar.text = rotuloFechar.text.Trim();
                feito.Append("Fechar em Cinzel; ");
            }

            var area = mesa.Find("AreaDosCartoes") as RectTransform;
            ScrollRect rolagem = area != null ? area.GetComponent<ScrollRect>() : null;
            if (rolagem != null)
            {
                if (rolagem.verticalScrollbar == null) { rolagem.verticalScrollbar = CriarBarra(area); feito.Append("barra de rolagem; "); }
                // Fora da navegação do teclado: com "Automatic", a seta para baixo saindo do Fechar caía na barra (logo
                // abaixo dele) e não nos cartões. A barra continua para mouse, arrasto e toque.
                if (rolagem.verticalScrollbar.navigation.mode != Navigation.Mode.None)
                {
                    rolagem.verticalScrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
                    feito.Append("barra fora da navegação do teclado; ");
                }
                if (rolagem.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.AutoHide)
                {
                    rolagem.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    feito.Append("barra só quando há rolagem; ");
                }
                var colunas = rolagem.content != null ? rolagem.content.GetComponent<HorizontalOrVerticalLayoutGroup>() : null;
                if (colunas != null && colunas.padding.right < MargemDireitaComBarra)
                {
                    colunas.padding = new RectOffset(colunas.padding.left, MargemDireitaComBarra, colunas.padding.top, colunas.padding.bottom);
                    feito.Append("margem direita para a barra; ");
                }
            }

            if (feito.Length == 0) return "já ajustada.";
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
            return feito.ToString().TrimEnd(' ', ';') + ".";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static Scrollbar CriarBarra(RectTransform area)
    {
        Sprite quadrado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var barra = new GameObject("BarraDeRolagem", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        barra.layer = area.gameObject.layer;
        barra.transform.SetParent(area, false);
        var rt = (RectTransform)barra.transform;
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(LarguraDaBarra, -16f); rt.anchoredPosition = new Vector2(-4f, 0f);
        var fundo = barra.GetComponent<Image>();
        fundo.sprite = quadrado; fundo.type = Image.Type.Sliced; fundo.color = CorDaBarra;

        var deslize = new GameObject("AreaDeDeslize", typeof(RectTransform));
        deslize.layer = barra.layer;
        deslize.transform.SetParent(barra.transform, false);
        var rtDeslize = (RectTransform)deslize.transform;
        rtDeslize.anchorMin = Vector2.zero; rtDeslize.anchorMax = Vector2.one; rtDeslize.offsetMin = rtDeslize.offsetMax = Vector2.zero;

        var alca = new GameObject("Alca", typeof(RectTransform), typeof(Image));
        alca.layer = barra.layer;
        alca.transform.SetParent(deslize.transform, false);
        var rtAlca = (RectTransform)alca.transform;
        rtAlca.offsetMin = rtAlca.offsetMax = Vector2.zero;
        var imagemAlca = alca.GetComponent<Image>();
        imagemAlca.sprite = quadrado; imagemAlca.type = Image.Type.Sliced; imagemAlca.color = CorDaAlca;

        var scrollbar = barra.GetComponent<Scrollbar>();
        scrollbar.handleRect = rtAlca;
        scrollbar.targetGraphic = imagemAlca;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private static TMP_FontAsset FonteDosCartoes()
    {
        GameObject cartao = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabCartao);
        TextMeshProUGUI titulo = cartao != null ? cartao.transform.Find("TituloTexto")?.GetComponent<TextMeshProUGUI>() : null;
        return titulo != null ? titulo.font : null;
    }

    private static bool Parecida(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;
}
