using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta, no prefab UI, o popup do tutorial no rodapé da tela (fora do caminho do inventário, da prensa,
/// do HUD e dos botões mobile), com título, coluna de glifos de controle, texto e botão Continuar no
/// mesmo estilo dos painéis. Também deixa no prefab o Canvas próprio do popup (antes era um override
/// só da cena Jogo), liga os campos do TutorialStepUI e cria o objeto das cutscenes de legenda.
/// Menu: Ferramentas > Tutorial > 1. Reexecutável.
/// </summary>
public static class TutorialPopupBuilder
{
    private const string CaminhoPrefabUI = "Assets/Prefab/UI.prefab";
    private const string CaminhoPrefabInventario = "Assets/Prefab/UI_Inventory.prefab";

    // Rodapé centralizado: entre o joystick (x < 440) e os botões mobile (x > 1365), abaixo da prensa (y > 860).
    private static readonly Vector2 TamanhoPopup = new Vector2(780f, 260f);
    private const float MargemInferior = 16f;
    private const float Margem = 28f;
    private const float LarguraGlifos = 230f;
    private static readonly Vector2 TamanhoBotao = new Vector2(200f, 46f);

    [MenuItem("Ferramentas/Tutorial/1 - Aplicar layout do popup do tutorial (UI.prefab)")]
    public static void Aplicar()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefabUI);
        try
        {
            Transform itens = raiz.transform.Find("ItensPoupUp");
            RectTransform popup = itens != null ? itens.Find("PopupTutorial") as RectTransform : null;
            TutorialStepUI stepUI = itens != null ? itens.GetComponentInChildren<TutorialStepUI>(true) : null;
            if (popup == null || stepUI == null)
            {
                Debug.LogError("[TutorialPopupBuilder] ItensPoupUp/PopupTutorial ou o TutorialStepUI (TutorialUI) não existem no UI.prefab.");
                return;
            }

            TMP_FontAsset fonteTitulo = InventarioListaBuilder.ObterFonte(popup);
            Sprite moldura = ObterMoldura();
            Sprite quadrado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // O pai dos popups era um retângulo de 100x100 perdido no meio da tela; os popups se ancoram
            // nele, então ele precisa cobrir a tela inteira para "rodapé" e "canto" fazerem sentido.
            RectTransform rItens = (RectTransform)itens;
            rItens.anchorMin = Vector2.zero;
            rItens.anchorMax = Vector2.one;
            rItens.pivot = new Vector2(0.5f, 0.5f);
            rItens.offsetMin = Vector2.zero;
            rItens.offsetMax = Vector2.zero;
            rItens.localScale = Vector3.one;

            ConfigurarPainel(popup, moldura);
            GarantirCanvasProprio(popup.gameObject);

            RectTransform popupItem = itens.Find("PopupItemRecebido") as RectTransform;
            if (popupItem != null)
            {
                GarantirCanvasProprio(popupItem.gameObject);
                // Aviso "Você recebeu" fica à esquerda, no meio da altura (onde já aparecia na cena Jogo).
                popupItem.anchorMin = popupItem.anchorMax = new Vector2(0f, 0.5f);
                popupItem.pivot = new Vector2(0f, 0.5f);
                popupItem.anchoredPosition = new Vector2(16f, 0f);
                popupItem.localScale = Vector3.one;
            }

            // Busca em profundidade: depois da primeira execução, TextoEtapa/IconeEtapa passam a viver dentro de "Corpo".
            TextMeshProUGUI texto = Achar(popup, "TextoEtapa")?.GetComponent<TextMeshProUGUI>();
            Image icone = Achar(popup, "IconeEtapa")?.GetComponent<Image>();
            Button botao = Achar(popup, "BotaoContinuar")?.GetComponent<Button>();
            if (texto == null || botao == null)
            {
                Debug.LogError("[TutorialPopupBuilder] PopupTutorial precisa ter os filhos TextoEtapa e BotaoContinuar.");
                return;
            }

            ConfigurarTitulo(popup, fonteTitulo);
            RectTransform corpo = ConfigurarCorpo(popup);
            RectTransform glifos = ConfigurarColunaDeGlifos(corpo);
            ConfigurarTexto(texto, corpo);
            ConfigurarIcone(icone, glifos);
            ConfigurarBotao(botao, fonteTitulo, quadrado);

            stepUI.painelPopup = popup.gameObject;
            stepUI.textoMensagem = texto;
            stepUI.imagemIcone = icone;
            stepUI.botaoContinuar = botao;
            stepUI.containerGlifos = glifos;
            stepUI.fonteGlifos = fonteTitulo;
            // Revelações apontam para objetos de cena (botões mobile): ficam como override em cada cena,
            // nunca no prefab. A lista antiga do prefab tinha IDs que já não existem.
            stepUI.revelacoes = new System.Collections.Generic.List<RevelacaoDeEtapa>();

            ConfigurarCutscenes(raiz.transform, fonteTitulo);

            popup.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefabUI);
            Debug.Log("[TutorialPopupBuilder] Popup do tutorial, canvas próprio, TutorialStepUI e cutscenes aplicados em " + CaminhoPrefabUI + ".");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static Transform Achar(Transform raiz, string nome)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            if (t != raiz && t.name == nome) return t;
        return null;
    }

    private static Sprite ObterMoldura()
    {
        GameObject inventario = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoPrefabInventario);
        Image fundo = inventario != null ? inventario.transform.Find("InventoryBackGround")?.GetComponent<Image>() : null;
        return fundo != null ? fundo.sprite : null;
    }

    private static void ConfigurarPainel(RectTransform popup, Sprite moldura)
    {
        popup.anchorMin = popup.anchorMax = new Vector2(0.5f, 0f);
        popup.pivot = new Vector2(0.5f, 0f);
        popup.anchoredPosition = new Vector2(0f, MargemInferior);
        popup.sizeDelta = TamanhoPopup;
        popup.localScale = Vector3.one;

        Image fundo = popup.GetComponent<Image>();
        if (fundo == null) fundo = popup.gameObject.AddComponent<Image>();
        if (moldura != null)
        {
            fundo.sprite = moldura;
            fundo.type = Image.Type.Sliced;
            fundo.pixelsPerUnitMultiplier = 0.32f;
            fundo.color = Color.white;
        }
        else
        {
            fundo.color = new Color(0.13f, 0.11f, 0.12f, 0.95f);
        }
        fundo.raycastTarget = true;
    }

    private static void GarantirCanvasProprio(GameObject go)
    {
        Canvas canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;
        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
    }

    private static void ConfigurarTitulo(RectTransform popup, TMP_FontAsset fonte)
    {
        Transform antigo = popup.Find("Titulo");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        TextMeshProUGUI titulo = InventarioListaBuilder.CriarTexto("Titulo", popup, "Tutorial", 28f, TextAlignmentOptions.MidlineLeft, fonte);
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = new Color(0.98f, 0.85f, 0.55f, 1f);
        RectTransform r = titulo.rectTransform;
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -14f);
        r.sizeDelta = new Vector2(-2f * Margem, 36f);
        r.SetAsFirstSibling();
    }

    private static RectTransform ConfigurarCorpo(RectTransform popup)
    {
        Transform antigo = popup.Find("Corpo");
        RectTransform corpo = antigo as RectTransform;
        if (corpo == null) corpo = InventarioListaBuilder.CriarRect("Corpo", popup);
        corpo.anchorMin = Vector2.zero;
        corpo.anchorMax = Vector2.one;
        corpo.pivot = new Vector2(0.5f, 0.5f);
        corpo.offsetMin = new Vector2(Margem, MargemInferior + TamanhoBotao.y + 10f);
        corpo.offsetMax = new Vector2(-Margem, -54f);

        HorizontalLayoutGroup hl = corpo.GetComponent<HorizontalLayoutGroup>();
        if (hl == null) hl = corpo.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 18f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        return corpo;
    }

    private static RectTransform ConfigurarColunaDeGlifos(RectTransform corpo)
    {
        Transform antigo = corpo.Find("Glifos");
        RectTransform glifos = antigo as RectTransform;
        if (glifos == null) glifos = InventarioListaBuilder.CriarRect("Glifos", corpo);
        glifos.SetAsFirstSibling();

        LayoutElement le = glifos.GetComponent<LayoutElement>();
        if (le == null) le = glifos.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = LarguraGlifos;
        le.minWidth = LarguraGlifos;   // sem isso o texto (largura preferida enorme) esmaga a coluna
        le.flexibleWidth = 0f;

        VerticalLayoutGroup vl = glifos.GetComponent<VerticalLayoutGroup>();
        if (vl == null) vl = glifos.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 6f;
        vl.childAlignment = TextAnchor.MiddleLeft;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;

        // As linhas de glifo são criadas em runtime (GlifoDeControle); no prefab a coluna fica vazia.
        for (int i = glifos.childCount - 1; i >= 0; i--) Object.DestroyImmediate(glifos.GetChild(i).gameObject);
        return glifos;
    }

    private static void ConfigurarTexto(TextMeshProUGUI texto, RectTransform corpo)
    {
        texto.transform.SetParent(corpo, false);
        texto.transform.SetAsLastSibling();
        LayoutElement le = texto.GetComponent<LayoutElement>();
        if (le == null) le = texto.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.minWidth = 300f;

        texto.fontSize = 24f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 17f;
        texto.fontSizeMax = 25f;
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.overflowMode = TextOverflowModes.Overflow;
        texto.color = Color.white;
        texto.raycastTarget = false;
    }

    private static void ConfigurarIcone(Image icone, RectTransform glifos)
    {
        if (icone == null) return;
        // Ícone opcional da etapa fica na coluna da esquerda, junto dos glifos; some quando a etapa não tem imagem.
        icone.transform.SetParent(glifos.parent, false);
        icone.transform.SetSiblingIndex(1);
        LayoutElement le = icone.GetComponent<LayoutElement>();
        if (le == null) le = icone.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 96f;
        le.preferredHeight = 96f;
        le.flexibleWidth = 0f;
        icone.preserveAspect = true;
        icone.color = Color.white;
        icone.raycastTarget = false;
        icone.gameObject.SetActive(false);
    }

    private static void ConfigurarBotao(Button botao, TMP_FontAsset fonte, Sprite quadrado)
    {
        RectTransform r = (RectTransform)botao.transform;
        r.SetParent(botao.transform.parent, false);
        r.SetAsLastSibling();
        r.anchorMin = r.anchorMax = new Vector2(1f, 0f);
        r.pivot = new Vector2(1f, 0f);
        r.anchoredPosition = new Vector2(-Margem, MargemInferior);
        r.sizeDelta = TamanhoBotao;
        r.localScale = Vector3.one;

        Image fundo = botao.GetComponent<Image>();
        if (fundo == null) fundo = botao.gameObject.AddComponent<Image>();
        fundo.sprite = quadrado;
        fundo.type = Image.Type.Sliced;
        fundo.color = InventarioListaBuilder.CorBotaoFechar;
        botao.targetGraphic = fundo;

        TextMeshProUGUI rotulo = botao.GetComponentInChildren<TextMeshProUGUI>(true);
        if (rotulo == null) rotulo = InventarioListaBuilder.CriarTexto("Texto", botao.transform, "Continuar", 24f, TextAlignmentOptions.Center, fonte);
        rotulo.text = "Continuar";
        InventarioListaBuilder.ConfigurarTexto(rotulo, 24f, TextAlignmentOptions.Center, fonte);
        rotulo.enableAutoSizing = false;
        InventarioListaBuilder.Esticar(rotulo.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void ConfigurarCutscenes(Transform raizUI, TMP_FontAsset fonte)
    {
        Transform t = raizUI.Find("Cutscenes");
        GameObject go = t != null ? t.gameObject : new GameObject("Cutscenes", typeof(RectTransform));
        if (t == null) { go.layer = raizUI.gameObject.layer; go.transform.SetParent(raizUI, false); }

        CutsceneLegendas legendas = go.GetComponent<CutsceneLegendas>();
        if (legendas == null) legendas = go.AddComponent<CutsceneLegendas>();
        legendas.fonte = fonte;

        if (go.GetComponent<Fase1Desfecho>() == null) go.AddComponent<Fase1Desfecho>();
    }
}
