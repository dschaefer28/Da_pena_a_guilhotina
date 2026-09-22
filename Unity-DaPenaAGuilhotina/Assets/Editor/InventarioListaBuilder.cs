using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Aplica o layout de lista ao prefab UI_Inventory: painel maior, uma coluna, linhas altas com
/// ícone + nome + quantidade, cabeçalho com botão Fechar e texto "Espaço livre" nas linhas vazias.
/// Menu: Ferramentas > Inventário. Pode rodar de novo sempre que linhas forem adicionadas ao prefab.
/// </summary>
public static class InventarioListaBuilder
{
    private const string CaminhoPrefab = "Assets/Prefab/UI_Inventory.prefab";
    private const string CaminhoPrefabUI = "Assets/Prefab/UI.prefab";
    private static readonly string[] CenasParaSincronizar = { "Assets/Scenes/Jogo.unity", "Assets/Scenes/Porao.unity" };

    private const int LinhasDesejadas = 24;

    // Medidas em pixels do canvas de referência (1920x1080, CanvasScaler casando pela altura).
    private const float LarguraPainel = 760f;
    private const float AlturaPainel = 900f;
    private const float Margem = 28f;
    private const float AlturaCabecalho = 120f;
    private const float AlturaLinha = 112f;
    private const float EspacoEntreLinhas = 12f;
    private const float TamanhoIcone = 84f;
    private const float LarguraContador = 100f;
    private const float RespiroInterno = 16f;
    private const float LarguraBotaoFechar = 200f;

    private static readonly Color CorFundoLinha = new Color(0f, 0f, 0f, 0.32f);
    private static readonly Color CorBotaoFechar = new Color(0.45f, 0.20f, 0.15f, 1f);
    private static readonly Color CorTextoVazio = new Color(1f, 1f, 1f, 0.45f);

    [MenuItem("Ferramentas/Inventário/1 - Aplicar layout de lista ao UI_Inventory")]
    public static void Aplicar()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
        try
        {
            RectTransform painel = raiz.transform.Find("InventoryBackGround") as RectTransform;
            RectTransform viewport = painel != null ? painel.Find("Viewport") as RectTransform : null;
            RectTransform conteudo = viewport != null ? viewport.Find("ContentContainer") as RectTransform : null;
            if (conteudo == null)
            {
                Debug.LogError("[InventarioListaBuilder] Estrutura InventoryBackGround/Viewport/ContentContainer não encontrada no prefab.");
                return;
            }

            TMP_FontAsset fonte = ObterFonte(conteudo);

            ConfigurarPainel(painel);
            ConfigurarCabecalho(painel, fonte);
            ConfigurarViewport(viewport);
            ConfigurarGrade(conteudo);
            GarantirQuantidadeDeLinhas(conteudo);

            int linhas = 0;
            foreach (Transform filho in conteudo)
            {
                UISlotHandler slot = filho.GetComponent<UISlotHandler>();
                if (slot == null) continue;
                ConfigurarLinha(slot, fonte);
                linhas++;
            }

            ConfigurarIconeDeArrasto(raiz.transform.Find("DragIcon") as RectTransform);

            InventoryLayoutPreview preview = painel.GetComponent<InventoryLayoutPreview>();
            if (preview != null) preview.showPreview = false;

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Debug.Log($"[InventarioListaBuilder] Layout de lista aplicado a {linhas} linhas de {CaminhoPrefab}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    /// <summary>
    /// As cenas Jogo/Porao (e o prefab UI) acumularam linhas extras e overrides de tamanho/posição por cima
    /// do UI_Inventory. Este passo remove essas linhas locais e reverte só os componentes de layout/visual
    /// do painel, para que o prefab volte a mandar. Salva as cenas; rode com o Play Mode desligado.
    /// </summary>
    [MenuItem("Ferramentas/Inventário/2 - Sincronizar UI.prefab e cenas com o UI_Inventory")]
    public static void SincronizarCenas()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[InventarioListaBuilder] Saia do Play Mode antes de sincronizar as cenas.");
            return;
        }

        Scene cenaAtual = SceneManager.GetActiveScene();
        if (cenaAtual.isDirty)
        {
            Debug.LogError($"[InventarioListaBuilder] A cena '{cenaAtual.name}' tem alterações não salvas. Salve (ou descarte) antes de sincronizar.");
            return;
        }
        string caminhoCenaOriginal = cenaAtual.path;

        SincronizarPrefabUI();

        foreach (string caminho in CenasParaSincronizar)
        {
            Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
            int removidos = 0, revertidos = 0;

            foreach (GameObject raiz in cena.GetRootGameObjects())
            {
                Transform painel = raiz.transform.Find("UI_Inventory/InventoryBackGround");
                if (painel == null) continue;
                removidos += RemoverLinhasAdicionadas(painel);
                revertidos += ReverterOverridesDeLayout(painel);
            }

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log($"[InventarioListaBuilder] {caminho}: {removidos} linha(s) locais removida(s), overrides revertidos em {revertidos} componente(s).");
        }

        if (!string.IsNullOrEmpty(caminhoCenaOriginal))
            EditorSceneManager.OpenScene(caminhoCenaOriginal, OpenSceneMode.Single);
    }

    private static void SincronizarPrefabUI()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefabUI);
        try
        {
            Transform painel = raiz.transform.Find("UI_Inventory/InventoryBackGround");
            if (painel == null)
            {
                Debug.LogWarning($"[InventarioListaBuilder] UI_Inventory/InventoryBackGround não encontrado em {CaminhoPrefabUI}.");
                return;
            }
            int removidos = RemoverLinhasAdicionadas(painel);
            int revertidos = ReverterOverridesDeLayout(painel);
            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefabUI);
            Debug.Log($"[InventarioListaBuilder] {CaminhoPrefabUI}: {removidos} linha(s) locais removida(s), overrides revertidos em {revertidos} componente(s).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static int RemoverLinhasAdicionadas(Transform painel)
    {
        Transform conteudo = painel.Find("Viewport/ContentContainer");
        if (conteudo == null) return 0;

        int removidos = 0;
        for (int i = conteudo.childCount - 1; i >= 0; i--)
        {
            GameObject filho = conteudo.GetChild(i).gameObject;
            if (!PrefabUtility.IsAddedGameObjectOverride(filho)) continue;
            Object.DestroyImmediate(filho);
            removidos++;
        }
        return removidos;
    }

    // Só layout/visual: referências (UISlotHandler.inventoryManager, eventos FMOD, ícones do preview)
    // podem viver como override legítimo do prefab pai e são preservadas.
    private static int ReverterOverridesDeLayout(Transform painel)
    {
        int revertidos = 0;
        foreach (Component c in painel.GetComponentsInChildren<Component>(true))
        {
            if (c == null || !EhComponenteDeLayout(c) || !PrefabUtility.IsPartOfPrefabInstance(c)) continue;
            PrefabUtility.RevertObjectOverride(c, InteractionMode.AutomatedAction);
            revertidos++;
        }
        return revertidos;
    }

    private static bool EhComponenteDeLayout(Component c) =>
        c is RectTransform || c is Image || c is TextMeshProUGUI || c is GridLayoutGroup ||
        c is ContentSizeFitter || c is ScrollRect || c is RectMask2D || c is LayoutElement || c is CanvasGroup;

    // O atlas "Cinzel-VariableFont_wght SDF" (512px, 97 caracteres) não tem á/é/ç etc., e o TMP cai na
    // LiberationSans para esses glifos ("Inventário" saía com o "á" em outra fonte). O Cinzel-Regular
    // tem a tabela Latin-1 completa.
    private const string CaminhoFonte = "Assets/Fonts/Cinzel-Regular SDF.asset";

    private static TMP_FontAsset ObterFonte(RectTransform conteudo)
    {
        TMP_FontAsset fonte = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoFonte);
        if (fonte == null)
        {
            Debug.LogWarning($"[InventarioListaBuilder] Fonte {CaminhoFonte} não encontrada; mantendo a fonte atual das linhas.");
            fonte = conteudo.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
        }
        return fonte;
    }

    private static void GarantirQuantidadeDeLinhas(RectTransform conteudo)
    {
        var linhas = new List<UISlotHandler>();
        foreach (Transform filho in conteudo)
        {
            UISlotHandler slot = filho.GetComponent<UISlotHandler>();
            if (slot != null) linhas.Add(slot);
        }
        if (linhas.Count == 0) return;

        GameObject modelo = linhas[0].gameObject;
        for (int i = linhas.Count; i < LinhasDesejadas; i++)
        {
            GameObject copia = Object.Instantiate(modelo, conteudo);
            copia.name = $"InventorySlot ({i})";
            copia.GetComponent<UISlotHandler>().item = null;
        }
    }

    private static void ConfigurarPainel(RectTransform painel)
    {
        painel.anchorMin = painel.anchorMax = new Vector2(0.5f, 0.5f);
        painel.pivot = new Vector2(0.5f, 0.5f);
        painel.anchoredPosition = Vector2.zero;
        painel.sizeDelta = new Vector2(LarguraPainel, AlturaPainel);

        ScrollRect scroll = painel.GetComponent<ScrollRect>();
        if (scroll != null)
        {
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.scrollSensitivity = 40f;
        }
    }

    private static void ConfigurarCabecalho(RectTransform painel, TMP_FontAsset fonte)
    {
        Transform antigo = painel.Find("Cabecalho");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        RectTransform cabecalho = CriarRect("Cabecalho", painel);
        cabecalho.anchorMin = new Vector2(0f, 1f);
        cabecalho.anchorMax = new Vector2(1f, 1f);
        cabecalho.pivot = new Vector2(0.5f, 1f);
        cabecalho.anchoredPosition = new Vector2(0f, -Margem);
        cabecalho.sizeDelta = new Vector2(-2f * Margem, AlturaCabecalho - Margem);
        cabecalho.SetAsFirstSibling();

        TextMeshProUGUI titulo = CriarTexto("Titulo", cabecalho, "Inventário", 46f, TextAlignmentOptions.MidlineLeft, fonte);
        titulo.fontStyle = FontStyles.Bold;
        Esticar(titulo.rectTransform, new Vector2(12f, 0f), new Vector2(-(LarguraBotaoFechar + 12f), 0f));

        RectTransform botao = CriarRect("BotaoFechar", cabecalho);
        botao.anchorMin = botao.anchorMax = new Vector2(1f, 0.5f);
        botao.pivot = new Vector2(1f, 0.5f);
        botao.anchoredPosition = new Vector2(-4f, 0f);
        botao.sizeDelta = new Vector2(LarguraBotaoFechar, 72f);

        Image fundoBotao = botao.gameObject.AddComponent<Image>();
        fundoBotao.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fundoBotao.type = Image.Type.Sliced;
        fundoBotao.color = CorBotaoFechar;

        Button button = botao.gameObject.AddComponent<Button>();
        button.targetGraphic = fundoBotao;
        botao.gameObject.AddComponent<BotaoFecharInventario>();

        TextMeshProUGUI rotulo = CriarTexto("Texto", botao, "Fechar", 32f, TextAlignmentOptions.Center, fonte);
        Esticar(rotulo.rectTransform, Vector2.zero, Vector2.zero);
    }

    private static void ConfigurarViewport(RectTransform viewport)
    {
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.offsetMin = new Vector2(Margem, Margem);
        viewport.offsetMax = new Vector2(-Margem, -AlturaCabecalho);
    }

    private static void ConfigurarGrade(RectTransform conteudo)
    {
        conteudo.anchorMin = new Vector2(0f, 1f);
        conteudo.anchorMax = new Vector2(1f, 1f);
        conteudo.pivot = new Vector2(0.5f, 1f);
        conteudo.anchoredPosition = Vector2.zero;
        conteudo.sizeDelta = new Vector2(0f, conteudo.sizeDelta.y);

        GridLayoutGroup grade = conteudo.GetComponent<GridLayoutGroup>();
        if (grade == null) grade = conteudo.gameObject.AddComponent<GridLayoutGroup>();
        grade.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grade.startAxis = GridLayoutGroup.Axis.Horizontal;
        grade.childAlignment = TextAnchor.UpperCenter;
        grade.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grade.constraintCount = 1;
        grade.cellSize = new Vector2(LarguraPainel - 2f * Margem, AlturaLinha);
        grade.spacing = new Vector2(0f, EspacoEntreLinhas);
        grade.padding = new RectOffset(0, 0, 4, 4);

        ContentSizeFitter fitter = conteudo.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = conteudo.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void ConfigurarLinha(UISlotHandler slot, TMP_FontAsset fonte)
    {
        GameObject go = slot.gameObject;

        Image fundo = go.GetComponent<Image>();
        if (fundo != null)
        {
            fundo.color = CorFundoLinha;
            fundo.raycastTarget = true;
        }
        if (go.GetComponent<LayoutElement>() == null) go.AddComponent<LayoutElement>();
        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();

        if (slot.slotImg != null)
        {
            RectTransform icone = slot.slotImg.rectTransform;
            icone.anchorMin = icone.anchorMax = new Vector2(0f, 0.5f);
            icone.pivot = new Vector2(0f, 0.5f);
            icone.anchoredPosition = new Vector2(RespiroInterno, 0f);
            icone.sizeDelta = new Vector2(TamanhoIcone, TamanhoIcone);
            slot.slotImg.preserveAspect = true;
            slot.slotImg.raycastTarget = false;
        }

        if (slot.itemNameText != null)
        {
            float esquerda = RespiroInterno + TamanhoIcone + RespiroInterno;
            float direita = LarguraContador + RespiroInterno;
            Esticar(slot.itemNameText.rectTransform, new Vector2(esquerda, 8f), new Vector2(-direita, -8f));
            ConfigurarTexto(slot.itemNameText, 30f, TextAlignmentOptions.MidlineLeft, fonte);
            slot.itemNameText.enableAutoSizing = true;
            slot.itemNameText.fontSizeMin = 20f;
            slot.itemNameText.fontSizeMax = 32f;
            slot.itemNameText.textWrappingMode = TextWrappingModes.Normal;
            slot.itemNameText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (slot.itemCount != null)
        {
            RectTransform contador = slot.itemCount.rectTransform;
            contador.anchorMin = new Vector2(1f, 0f);
            contador.anchorMax = new Vector2(1f, 1f);
            contador.pivot = new Vector2(1f, 0.5f);
            contador.anchoredPosition = new Vector2(-RespiroInterno, 0f);
            contador.sizeDelta = new Vector2(LarguraContador, -16f);
            ConfigurarTexto(slot.itemCount, 34f, TextAlignmentOptions.MidlineRight, fonte);
            slot.itemCount.enableAutoSizing = false;
            slot.itemCount.textWrappingMode = TextWrappingModes.NoWrap;
            slot.itemCount.overflowMode = TextOverflowModes.Overflow;
        }

        Transform vazioAntigo = go.transform.Find("TextoVazio");
        if (vazioAntigo != null) Object.DestroyImmediate(vazioAntigo.gameObject);
        TextMeshProUGUI vazio = CriarTexto("TextoVazio", go.transform, "Espaço livre", 28f, TextAlignmentOptions.Center, fonte);
        vazio.color = CorTextoVazio;
        Esticar(vazio.rectTransform, new Vector2(RespiroInterno, 0f), new Vector2(-RespiroInterno, 0f));
        slot.objetoVazio = vazio.gameObject;
        vazio.gameObject.SetActive(slot.item == null);
    }

    private static void ConfigurarIconeDeArrasto(RectTransform icone)
    {
        if (icone == null) return;
        icone.localScale = Vector3.one;
        icone.sizeDelta = new Vector2(96f, 96f);
        Image imagem = icone.GetComponent<Image>();
        if (imagem != null) imagem.preserveAspect = true;
    }

    private static RectTransform CriarRect(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.layer = pai.gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(pai, false);
        return rect;
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, string texto, float tamanho,
        TextAlignmentOptions alinhamento, TMP_FontAsset fonte)
    {
        var tmp = CriarRect(nome, pai).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = texto;
        ConfigurarTexto(tmp, tamanho, alinhamento, fonte);
        return tmp;
    }

    private static void ConfigurarTexto(TextMeshProUGUI tmp, float tamanho, TextAlignmentOptions alinhamento, TMP_FontAsset fonte)
    {
        if (fonte != null) tmp.font = fonte;
        tmp.fontSize = tamanho;
        tmp.alignment = alinhamento;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private static void Esticar(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
