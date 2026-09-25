using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta, no prefab UI, a HUD de status com a arte separada em três peças: o retrato do protagonista com os
/// arabescos (iconbar), o trilho de cima (barra1, Opinião do Povo) e o de baixo (barra2, Opinião do Estado).
/// Os sliders ficam dentro do vão transparente de cada trilho, com rótulo, o ouro logo abaixo e os textos de
/// "+X/-X" que o HUDManager anima.
/// Os objetos Slider_OpiniãoPublica, Slider_OpiniãoEstado e Texto_Capital são reaproveitados (só mudam de
/// pai e de layout), então as referências do HUDManager continuam valendo.
/// Tudo é ancorado em proporção dentro do painel, e o painel fica no canto superior esquerdo com o
/// HudAreaSegura: o CanvasScaler cuida da resolução (PC ou celular) e o notch não cobre a HUD.
/// Menu: Ferramentas > HUD > Aplicar moldura das barras de status. Reexecutável.
/// </summary>
public static class HudStatusBuilder
{
    private const string CaminhoPrefabUI = "Assets/Prefab/UI.prefab";
    private const string PastaSprites = "Assets/Sprites/InterfaceButtons/";
    private const string SpriteRetrato = "iconbar_27";   // as outras fatias dessas texturas são só sujeira do corte automático
    private const string SpriteTrilhoPovo = "barra1_1";
    private const string SpriteTrilhoEstado = "barra2_1";

    // Layout em "unidades de desenho" = pixels do iconbar.png original, com origem no canto superior esquerdo
    // do retrato e y crescendo para baixo. O retrato ocupa (0,0)-(2218,2026).
    private const float LarguraPainel = 3993f, AlturaPainel = 2026f;
    private const float RetratoLargura = 2218f, RetratoAltura = 2026f;

    // Os trilhos entram por trás do círculo (o lado inclinado se encaixa na curva) e seus trilhos de fora
    // coincidem com os dois braços do retrato. Escala 1.25 = px do barra*.png para unidades de desenho.
    private const float EscalaTrilho = 1.25f;
    private const float TrilhoX = 1600f;
    private const float TrilhoPovoY = 318f, TrilhoPovoLargura = 1914f, TrilhoPovoAltura = 530f;
    private const float TrilhoEstadoY = 1098f, TrilhoEstadoLargura = 1914f, TrilhoEstadoAltura = 514.4f;

    // Vão interno de cada trilho, em px do próprio png relativos ao canto do sprite (x da esquerda, y do topo).
    // A barra passa um pouco por baixo da moldura para não sobrar fresta.
    private static readonly Rect VaoPovo = new Rect(100f, 91f, 1709f, 348f);    // barra1: x 115..1824, y 170..518
    private static readonly Rect VaoEstado = new Rect(101f, 79.4f, 1708f, 334f); // barra2: x 164..1872, y 102..436

    private const float OuroX = 2250f, OuroTopo = 1745f, OuroBase = 2010f;   // à direita do braço de baixo

    private const float Escala = 0.115f;                            // unidade de desenho -> px da tela de 1920x1080
    private static readonly Vector2 MargemTela = new Vector2(24f, 18f);

    private static readonly Color CorPovo = new Color(0.72f, 0.18f, 0.15f, 1f);
    private static readonly Color CorEstado = new Color(0.22f, 0.38f, 0.66f, 1f);
    private static readonly Color CorFundoTrilho = new Color(0.10f, 0.08f, 0.07f, 0.9f);
    private static readonly Color CorOuro = new Color(0.98f, 0.85f, 0.45f, 1f);

    [MenuItem("Ferramentas/HUD/Aplicar moldura das barras de status (UI.prefab)")]
    public static void Aplicar()
    {
        Sprite retrato = CarregarSprite("iconbar.png", SpriteRetrato);
        Sprite trilhoPovo = CarregarSprite("barra1.png", SpriteTrilhoPovo);
        Sprite trilhoEstado = CarregarSprite("barra2.png", SpriteTrilhoEstado);
        if (retrato == null || trilhoPovo == null || trilhoEstado == null) return;

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

            // Moldura única da arte antiga (barra.png, apagada): substituída pelas três peças abaixo.
            Transform antiga = painel.Find("Moldura");
            if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

            Rect vaoPovo = VaoTela(TrilhoPovoY, VaoPovo), vaoEstado = VaoTela(TrilhoEstadoY, VaoEstado);
            ConfigurarBarra(hud.publicOpinionSlider, painel, vaoPovo, CorPovo);
            ConfigurarBarra(hud.stateOpinionSlider, painel, vaoEstado, CorEstado);
            Image imgPovo = GarantirImagem(painel, "MolduraPovo", trilhoPovo,
                new Rect(TrilhoX, TrilhoPovoY, TrilhoPovoLargura * EscalaTrilho, TrilhoPovoAltura * EscalaTrilho));
            Image imgEstado = GarantirImagem(painel, "MolduraEstado", trilhoEstado,
                new Rect(TrilhoX, TrilhoEstadoY, TrilhoEstadoLargura * EscalaTrilho, TrilhoEstadoAltura * EscalaTrilho));
            Image imgRetrato = GarantirImagem(painel, "Retrato", retrato, new Rect(0f, 0f, RetratoLargura, RetratoAltura));

            // Ordem de desenho: barras (sliders) atrás, trilhos por cima delas, retrato por cima de tudo
            // (esconde a ponta inclinada dos trilhos); os textos abaixo entram depois.
            hud.publicOpinionSlider.transform.SetSiblingIndex(0);
            hud.stateOpinionSlider.transform.SetSiblingIndex(1);
            imgPovo.transform.SetSiblingIndex(2);
            imgEstado.transform.SetSiblingIndex(3);
            imgRetrato.transform.SetSiblingIndex(4);

            GarantirRotulo(painel, "RotuloPovo", "Povo", vaoPovo, fonte);
            GarantirRotulo(painel, "RotuloEstado", "Estado", vaoEstado, fonte);

            ConfigurarOuro(hud.capitalText, painel, fonte);

            hud.deltaPovoText = GarantirDelta(painel, "DeltaPovo", vaoPovo, fonte);
            hud.deltaEstadoText = GarantirDelta(painel, "DeltaEstado", vaoEstado, fonte);
            hud.deltaCapitalText = GarantirDelta(painel, "DeltaOuro", new Rect(OuroX, OuroTopo, 0f, OuroBase - OuroTopo), fonte);
            hud.formatoCapital = "Ouro: {0}";
            EditorUtility.SetDirty(hud);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefabUI);
            Debug.Log("[HudStatusBuilder] HUD de status montada (retrato + trilhos) em " + CaminhoPrefabUI + ".");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static Sprite CarregarSprite(string arquivo, string nome)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PastaSprites + arquivo))
            if (o is Sprite s && s.name == nome) return s;
        Debug.LogError($"[HudStatusBuilder] Sprite '{nome}' não encontrada em {PastaSprites + arquivo}.");
        return null;
    }

    // Vão do trilho convertido para unidades de desenho do painel.
    private static Rect VaoTela(float trilhoY, Rect vao) =>
        new Rect(TrilhoX + vao.x * EscalaTrilho, trilhoY + vao.y * EscalaTrilho, vao.width * EscalaTrilho, vao.height * EscalaTrilho);

    // Painel com a proporção do desenho, preso ao canto superior esquerdo da tela com margem. O HudAreaSegura
    // soma o recuo do notch/cantos do celular a essa margem.
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
        // Valores inteiros: com frações a Unity regravava a posição nas cenas com erro de arredondamento
        // e cada cena ficava com um override falso da HUD.
        painel.sizeDelta = new Vector2(Mathf.Round(LarguraPainel * Escala), Mathf.Round(AlturaPainel * Escala));
        painel.anchoredPosition = new Vector2(MargemTela.x, -MargemTela.y);
        painel.localScale = Vector3.one;
        if (painel.GetComponent<HudAreaSegura>() == null) painel.gameObject.AddComponent<HudAreaSegura>();
        return painel;
    }

    private static Image GarantirImagem(RectTransform painel, string nome, Sprite sprite, Rect area)
    {
        Transform t = painel.Find(nome);
        Image img = t != null ? t.GetComponent<Image>() : null;
        if (img == null) img = InventarioListaBuilder.CriarRect(nome, painel).gameObject.AddComponent<Image>();
        Ancorar(img.rectTransform, area);
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false; // a área já tem a proporção exata do sprite
        img.color = Color.white;
        img.raycastTarget = false;  // não rouba toque/clique do jogo
        return img;
    }

    // Ancoragem normalizada a partir de unidades de desenho (x da esquerda, y do topo): escala junto com o painel.
    private static void Ancorar(RectTransform r, Rect area)
    {
        r.anchorMin = new Vector2(area.xMin / LarguraPainel, 1f - area.yMax / AlturaPainel);
        r.anchorMax = new Vector2(area.xMax / LarguraPainel, 1f - area.yMin / AlturaPainel);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
    }

    private static void ConfigurarBarra(Slider slider, RectTransform painel, Rect vao, Color cor)
    {
        RectTransform r = (RectTransform)slider.transform;
        r.SetParent(painel, false);
        Ancorar(r, vao);

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

    private static void GarantirRotulo(RectTransform painel, string nome, string texto, Rect vao, TMP_FontAsset fonte)
    {
        Transform t = painel.Find(nome);
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null) tmp = InventarioListaBuilder.CriarTexto(nome, painel, texto, 26f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.text = texto;
        InventarioListaBuilder.ConfigurarTexto(tmp, 26f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14f;
        tmp.fontSizeMax = 26f;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        // Começa depois da parte que o retrato cobre e respira um pouco dentro do vão.
        Ancorar(tmp.rectTransform, new Rect(vao.x + 160f, vao.y + 30f, vao.width - 200f, vao.height - 60f));
        tmp.transform.SetAsLastSibling();
    }

    private static void ConfigurarOuro(TextMeshProUGUI capital, RectTransform painel, TMP_FontAsset fonte)
    {
        capital.transform.SetParent(painel, false);
        InventarioListaBuilder.ConfigurarTexto(capital, 28f, TextAlignmentOptions.MidlineLeft, fonte);
        capital.color = CorOuro;
        capital.fontStyle = FontStyles.Bold;
        capital.enableAutoSizing = true;
        capital.fontSizeMin = 16f;
        capital.fontSizeMax = 28f;
        capital.textWrappingMode = TextWrappingModes.NoWrap;
        capital.text = "Ouro: 0";
        Ancorar(capital.rectTransform, new Rect(OuroX, OuroTopo, LarguraPainel - OuroX, OuroBase - OuroTopo));
        capital.transform.SetAsLastSibling();
    }

    // "+15" / "-10" logo à direita dos trilhos, na altura de cada linha. Começa escondido (o HUDManager liga).
    private static TextMeshProUGUI GarantirDelta(RectTransform painel, string nome, Rect linha, TMP_FontAsset fonte)
    {
        Transform t = painel.Find(nome);
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null) tmp = InventarioListaBuilder.CriarTexto(nome, painel, "+0", 28f, TextAlignmentOptions.MidlineLeft, fonte);
        InventarioListaBuilder.ConfigurarTexto(tmp, 28f, TextAlignmentOptions.MidlineLeft, fonte);
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        float centro = linha.center.y;
        Ancorar(tmp.rectTransform, new Rect(LarguraPainel + 40f, centro - 220f, 900f, 440f));
        tmp.transform.SetAsLastSibling();
        tmp.gameObject.SetActive(false);
        return tmp;
    }
}
