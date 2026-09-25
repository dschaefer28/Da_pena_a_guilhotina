using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta, no prefab UI, a HUD de status com a arte "barra" (retrato do protagonista + dois trilhos):
/// os sliders de Opinião do Povo (trilho de cima) e do Estado (trilho de baixo) passam a ficar dentro dos
/// trilhos da moldura, com rótulo, o ouro logo abaixo e os textos de "+X/-X" que o HUDManager anima.
/// Os objetos Slider_OpiniãoPublica, Slider_OpiniãoEstado e Texto_Capital são reaproveitados (só mudam de
/// pai e de layout), então as referências do HUDManager continuam valendo.
/// Menu: Ferramentas > HUD > Aplicar moldura das barras de status. Reexecutável.
/// </summary>
public static class HudStatusBuilder
{
    private const string CaminhoPrefabUI = "Assets/Prefab/UI.prefab";
    private const string CaminhoSprite = "Assets/Sprites/InterfaceButtons/barra.png";

    // Medidas em pixels do sprite "barra_0" (459 x 524; y contado a partir do TOPO do sprite).
    private const float LarguraSprite = 459f, AlturaSprite = 524f;
    private const float VisivelX = 4f, VisivelTopo = 137f;          // canto superior esquerdo da parte desenhada
    private const float TrilhoXMin = 219f, TrilhoXMax = 432f;       // interior dos dois trilhos
    private const float Trilho1Topo = 209f, Trilho1Base = 249f;     // trilho de cima (Povo)
    private const float Trilho2Topo = 283f, Trilho2Base = 321f;     // trilho de baixo (Estado)
    private const float OuroTopo = 326f, OuroBase = 382f;           // faixa livre abaixo do trilho de baixo
    private const float OuroXMin = 285f;                            // à direita do arabesco de baixo
    private const float Sangria = 3f;                               // barra passa um pouco por baixo da moldura

    private const float Escala = 0.8f;                              // 1 px do sprite = 0.8 px da tela de 1920x1080
    private static readonly Vector2 MargemTela = new Vector2(24f, 18f);

    private static readonly Color CorPovo = new Color(0.72f, 0.18f, 0.15f, 1f);
    private static readonly Color CorEstado = new Color(0.22f, 0.38f, 0.66f, 1f);
    private static readonly Color CorFundoTrilho = new Color(0.10f, 0.08f, 0.07f, 0.9f);
    private static readonly Color CorOuro = new Color(0.98f, 0.85f, 0.45f, 1f);

    [MenuItem("Ferramentas/HUD/Aplicar moldura das barras de status (UI.prefab)")]
    public static void Aplicar()
    {
        Sprite moldura = AssetDatabase.LoadAssetAtPath<Sprite>(CaminhoSprite);
        if (moldura == null)
        {
            Debug.LogError($"[HudStatusBuilder] Sprite não encontrada em {CaminhoSprite}.");
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefabUI);
        try
        {
            HUDManager hud = raiz.GetComponentInChildren<HUDManager>(true);
            if (hud == null || hud.publicOpinionSlider == null || hud.stateOpinionSlider == null || hud.capitalText == null)
            {
                Debug.LogError("[HudStatusBuilder] HUDManager (com os dois sliders e o texto de capital) não encontrado no UI.prefab.");
                return;
            }
            TMP_FontAsset fonte = InventarioListaBuilder.ObterFonte((RectTransform)raiz.transform);

            RectTransform painel = GarantirPainel(raiz.transform, hud.publicOpinionSlider.transform);

            ConfigurarBarra(hud.publicOpinionSlider, painel, Trilho1Topo, Trilho1Base, CorPovo);
            ConfigurarBarra(hud.stateOpinionSlider, painel, Trilho2Topo, Trilho2Base, CorEstado);
            Image imgMoldura = GarantirMoldura(painel, moldura);
            // Ordem de desenho: trilhos (sliders) atrás, moldura por cima; os textos abaixo entram depois dela.
            hud.publicOpinionSlider.transform.SetSiblingIndex(0);
            hud.stateOpinionSlider.transform.SetSiblingIndex(1);
            imgMoldura.transform.SetSiblingIndex(2);

            GarantirRotulo(painel, "RotuloPovo", "Povo", Trilho1Topo, Trilho1Base, fonte);
            GarantirRotulo(painel, "RotuloEstado", "Estado", Trilho2Topo, Trilho2Base, fonte);

            ConfigurarOuro(hud.capitalText, painel, fonte);

            hud.deltaPovoText = GarantirDelta(painel, "DeltaPovo", Trilho1Topo, Trilho1Base, fonte);
            hud.deltaEstadoText = GarantirDelta(painel, "DeltaEstado", Trilho2Topo, Trilho2Base, fonte);
            hud.deltaCapitalText = GarantirDelta(painel, "DeltaOuro", OuroTopo, OuroBase, fonte);
            hud.formatoCapital = "Ouro: {0}";
            EditorUtility.SetDirty(hud);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefabUI);
            Debug.Log("[HudStatusBuilder] HUD de status montada com a moldura 'barra' em " + CaminhoPrefabUI + ".");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // Retângulo do tamanho do sprite inteiro (com a transparência em volta), posicionado de forma que a parte
    // desenhada fique no canto superior esquerdo da tela, com margem.
    private static RectTransform GarantirPainel(Transform raizUI, Transform sliderAntigo)
    {
        RectTransform painel = raizUI.Find("HUD_Status") as RectTransform;
        if (painel == null)
        {
            // Nasce no lugar da hierarquia onde os sliders ficavam (mesma ordem de desenho em relação aos painéis).
            painel = InventarioListaBuilder.CriarRect("HUD_Status", raizUI);
            if (sliderAntigo.parent == raizUI) painel.SetSiblingIndex(sliderAntigo.GetSiblingIndex());
        }
        painel.anchorMin = painel.anchorMax = new Vector2(0f, 1f);
        painel.pivot = new Vector2(0f, 1f);
        // Valores inteiros: com frações (367.2, 20.8...) a Unity regravava a posição nas cenas com erro de
        // arredondamento (20.800049) e cada cena ficava com um override falso da HUD.
        painel.sizeDelta = new Vector2(Mathf.Round(LarguraSprite * Escala), Mathf.Round(AlturaSprite * Escala));
        painel.anchoredPosition = new Vector2(Mathf.Round(MargemTela.x - VisivelX * Escala), Mathf.Round(-MargemTela.y + VisivelTopo * Escala));
        painel.localScale = Vector3.one;
        return painel;
    }

    private static Image GarantirMoldura(RectTransform painel, Sprite sprite)
    {
        Transform t = painel.Find("Moldura");
        Image img = t != null ? t.GetComponent<Image>() : null;
        if (img == null) img = InventarioListaBuilder.CriarRect("Moldura", painel).gameObject.AddComponent<Image>();
        InventarioListaBuilder.Esticar(img.rectTransform, Vector2.zero, Vector2.zero);
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false; // o painel já tem a proporção exata do sprite
        img.color = Color.white;
        img.raycastTarget = false;
        return img;
    }

    // Ancoragem normalizada a partir de coordenadas do sprite (x da esquerda, y do topo).
    private static void Ancorar(RectTransform r, float xMin, float xMax, float topo, float baseY)
    {
        r.anchorMin = new Vector2(xMin / LarguraSprite, 1f - baseY / AlturaSprite);
        r.anchorMax = new Vector2(xMax / LarguraSprite, 1f - topo / AlturaSprite);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
    }

    private static void ConfigurarBarra(Slider slider, RectTransform painel, float topo, float baseY, Color cor)
    {
        RectTransform r = (RectTransform)slider.transform;
        r.SetParent(painel, false);
        Ancorar(r, TrilhoXMin - Sangria, TrilhoXMax + Sangria, topo - Sangria, baseY + Sangria);

        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;

        Transform fundo = r.Find("Background");
        if (fundo != null)
        {
            InventarioListaBuilder.Esticar((RectTransform)fundo, Vector2.zero, Vector2.zero);
            Image img = fundo.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.type = Image.Type.Simple; img.color = CorFundoTrilho; img.raycastTarget = false; }
        }

        Transform area = r.Find("Fill Area");
        if (area != null) InventarioListaBuilder.Esticar((RectTransform)area, Vector2.zero, Vector2.zero);

        if (slider.fillRect != null)
        {
            slider.fillRect.offsetMin = slider.fillRect.offsetMax = Vector2.zero; // âncoras são do Slider
            Image img = slider.fillRect.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.type = Image.Type.Simple; img.color = cor; img.raycastTarget = false; }
        }
    }

    private static void GarantirRotulo(RectTransform painel, string nome, string texto, float topo, float baseY, TMP_FontAsset fonte)
    {
        Transform t = painel.Find(nome);
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null) tmp = InventarioListaBuilder.CriarTexto(nome, painel, texto, 22f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.text = texto;
        InventarioListaBuilder.ConfigurarTexto(tmp, 22f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 22f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        Ancorar(tmp.rectTransform, TrilhoXMin + 10f, TrilhoXMax - 4f, topo, baseY);
        tmp.transform.SetAsLastSibling();
    }

    private static void ConfigurarOuro(TextMeshProUGUI capital, RectTransform painel, TMP_FontAsset fonte)
    {
        capital.transform.SetParent(painel, false);
        InventarioListaBuilder.ConfigurarTexto(capital, 26f, TextAlignmentOptions.MidlineLeft, fonte);
        capital.color = CorOuro;
        capital.fontStyle = FontStyles.Bold;
        capital.enableAutoSizing = true;
        capital.fontSizeMin = 16f;
        capital.fontSizeMax = 26f;
        capital.textWrappingMode = TextWrappingModes.NoWrap;
        capital.text = "Ouro: 0";
        Ancorar(capital.rectTransform, OuroXMin, TrilhoXMax, OuroTopo, OuroBase);
        capital.transform.SetAsLastSibling();
    }

    // "+15" / "-10" logo à direita da moldura, na altura de cada linha. Começa escondido (o HUDManager liga).
    private static TextMeshProUGUI GarantirDelta(RectTransform painel, string nome, float topo, float baseY, TMP_FontAsset fonte)
    {
        Transform t = painel.Find(nome);
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null) tmp = InventarioListaBuilder.CriarTexto(nome, painel, "+0", 26f, TextAlignmentOptions.MidlineLeft, fonte);
        InventarioListaBuilder.ConfigurarTexto(tmp, 26f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        Ancorar(tmp.rectTransform, LarguraSprite - 8f, LarguraSprite + 110f, topo - 6f, baseY + 6f);
        tmp.transform.SetAsLastSibling();
        tmp.gameObject.SetActive(false);
        return tmp;
    }
}
