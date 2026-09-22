using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica o layout do painel da Prensa (UI_Inventory/PainelPrensa): mesma moldura e tipografia do
/// inventário, leitura "Entrada 1 + Entrada 2 = Resultado" e botão Misturar grande, que só habilita
/// com as duas entradas cheias (BotaoMisturarEstado). Mantém os GameObjects existentes (Slot_Ent1/2,
/// Slot_Saida, Misturar) para não quebrar as referências do CraftingPress e o onClick do botão.
/// Menu: Ferramentas > Inventário > 3. Reexecutável.
/// </summary>
public static class PrensaPainelBuilder
{
    private const string CaminhoPrefab = "Assets/Prefab/UI_Inventory.prefab";

    // Painel à DIREITA do inventário (760x900 centralizado), alinhado ao topo dele: ocupa x 1380..1820 e
    // y 140..860 do canvas 1920x1080. À esquerda ficam as barras do HUD (que o jogador precisa ver ao
    // imprimir) e o joystick; à direita só o botão Pause (acima de y=130) e os botões mobile (abaixo de 870).
    private const float LarguraPainel = 440f;
    private const float AlturaPainel = 720f;
    private const float PosicaoX = 640f;
    private const float PosicaoY = 40f;
    private const float AlturaCabecalho = 100f;

    private const float TamanhoSlot = 130f;
    private const float TamanhoIcone = 96f;
    private const float DistanciaEntradas = 100f;   // centro de cada entrada em relação ao eixo do painel
    private const float AlturaEntradas = 110f;
    private const float AlturaIgual = -25f;
    private const float AlturaSaida = -115f;
    private const float AlturaBotao = -290f;
    private static readonly Vector2 TamanhoBotao = new Vector2(320f, 84f);

    private static readonly Color CorRotulo = new Color(1f, 1f, 1f, 0.85f);
    private static readonly Color CorDica = new Color(1f, 1f, 1f, 0.6f);

    [MenuItem("Ferramentas/Inventário/3 - Aplicar layout do painel da Prensa")]
    public static void Aplicar()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(CaminhoPrefab);
        try
        {
            RectTransform painel = raiz.transform.Find("PainelPrensa") as RectTransform;
            RectTransform inventario = raiz.transform.Find("InventoryBackGround") as RectTransform;
            if (painel == null || inventario == null)
            {
                Debug.LogError("[PrensaPainelBuilder] PainelPrensa ou InventoryBackGround não encontrado no prefab.");
                return;
            }

            CraftingPress prensa = painel.GetComponent<CraftingPress>();
            if (prensa == null || prensa.slotInput1 == null || prensa.slotInput2 == null || prensa.slotOutput == null)
            {
                Debug.LogError("[PrensaPainelBuilder] CraftingPress sem os três slots atribuídos.");
                return;
            }

            TMP_FontAsset fonte = InventarioListaBuilder.ObterFonte(painel);
            Image fundoInventario = inventario.GetComponent<Image>();
            Sprite quadrado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            ConfigurarPainel(painel, fundoInventario.sprite, fundoInventario.pixelsPerUnitMultiplier);
            ConfigurarCabecalho(painel, fonte);

            ConfigurarSlot(prensa.slotInput1, new Vector2(-DistanciaEntradas, AlturaEntradas), "Entrada 1", "+", fonte, quadrado);
            ConfigurarSlot(prensa.slotInput2, new Vector2(DistanciaEntradas, AlturaEntradas), "Entrada 2", "+", fonte, quadrado);
            ConfigurarSlot(prensa.slotOutput, new Vector2(0f, AlturaSaida), "Resultado", "?", fonte, quadrado);

            ConfigurarSinal(painel, "Sinal", "+", new Vector2(0f, AlturaEntradas), fonte);
            ConfigurarSinal(painel, "Igual", "=", new Vector2(0f, AlturaIgual), fonte);

            ConfigurarBotao(painel, fonte, quadrado);

            PrefabUtility.SaveAsPrefabAsset(raiz, CaminhoPrefab);
            Debug.Log($"[PrensaPainelBuilder] Layout da prensa aplicado em {CaminhoPrefab}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static void ConfigurarPainel(RectTransform painel, Sprite moldura, float ppu)
    {
        painel.anchorMin = painel.anchorMax = new Vector2(0.5f, 0.5f);
        painel.pivot = new Vector2(0.5f, 0.5f);
        painel.anchoredPosition = new Vector2(PosicaoX, PosicaoY);
        painel.sizeDelta = new Vector2(LarguraPainel, AlturaPainel);
        painel.localScale = Vector3.one;

        Image fundo = painel.GetComponent<Image>();
        fundo.sprite = moldura;
        fundo.type = Image.Type.Sliced;
        fundo.fillCenter = true;
        fundo.pixelsPerUnitMultiplier = ppu;
        fundo.color = Color.white;
        fundo.raycastTarget = true; // bloqueia cliques no cenário atrás do painel
    }

    private static void ConfigurarCabecalho(RectTransform painel, TMP_FontAsset fonte)
    {
        Transform antigo = painel.Find("Cabecalho");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);
        Transform dicaAntiga = painel.Find("Dica");
        if (dicaAntiga != null) Object.DestroyImmediate(dicaAntiga.gameObject);

        RectTransform cabecalho = InventarioListaBuilder.CriarRect("Cabecalho", painel);
        cabecalho.anchorMin = new Vector2(0f, 1f);
        cabecalho.anchorMax = new Vector2(1f, 1f);
        cabecalho.pivot = new Vector2(0.5f, 1f);
        cabecalho.anchoredPosition = new Vector2(0f, -InventarioListaBuilder.Margem);
        cabecalho.sizeDelta = new Vector2(-2f * InventarioListaBuilder.Margem, AlturaCabecalho - InventarioListaBuilder.Margem);
        cabecalho.SetAsFirstSibling();

        TextMeshProUGUI titulo = InventarioListaBuilder.CriarTexto("Titulo", cabecalho, "Prensa", 46f, TextAlignmentOptions.MidlineLeft, fonte);
        titulo.fontStyle = FontStyles.Bold;
        InventarioListaBuilder.Esticar(titulo.rectTransform, new Vector2(12f, 0f), new Vector2(-12f, 0f));

        TextMeshProUGUI dica = InventarioListaBuilder.CriarTexto("Dica", painel, "Junte duas pistas e misture para imprimir o panfleto.", 26f, TextAlignmentOptions.Top, fonte);
        dica.color = CorDica;
        dica.textWrappingMode = TextWrappingModes.Normal;
        RectTransform rDica = dica.rectTransform;
        rDica.anchorMin = rDica.anchorMax = new Vector2(0.5f, 1f);
        rDica.pivot = new Vector2(0.5f, 1f);
        rDica.anchoredPosition = new Vector2(0f, -(AlturaCabecalho + 4f));
        rDica.sizeDelta = new Vector2(LarguraPainel - 2f * InventarioListaBuilder.Margem, 64f);
        rDica.SetSiblingIndex(1);
    }

    private static void ConfigurarSlot(UISlotHandler slot, Vector2 posicao, string rotulo, string placeholder, TMP_FontAsset fonte, Sprite quadrado)
    {
        RectTransform rect = (RectTransform)slot.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(TamanhoSlot, TamanhoSlot);
        rect.localScale = Vector3.one;

        Image fundo = slot.GetComponent<Image>();
        if (fundo == null) fundo = slot.gameObject.AddComponent<Image>();
        fundo.sprite = quadrado;
        fundo.type = Image.Type.Sliced;
        fundo.color = InventarioListaBuilder.CorFundoLinha; // mesmo tom das linhas do inventário
        fundo.raycastTarget = true;

        if (slot.slotImg != null)
        {
            RectTransform icone = slot.slotImg.rectTransform;
            icone.anchorMin = icone.anchorMax = new Vector2(0.5f, 0.5f);
            icone.pivot = new Vector2(0.5f, 0.5f);
            icone.anchoredPosition = Vector2.zero;
            icone.sizeDelta = new Vector2(TamanhoIcone, TamanhoIcone);
            icone.localScale = Vector3.one;
            slot.slotImg.preserveAspect = true;
            slot.slotImg.raycastTarget = false;
        }

        if (slot.itemCount != null)
        {
            RectTransform contador = slot.itemCount.rectTransform;
            contador.anchorMin = contador.anchorMax = new Vector2(1f, 0f);
            contador.pivot = new Vector2(1f, 0f);
            contador.anchoredPosition = new Vector2(-10f, 6f);
            contador.sizeDelta = new Vector2(70f, 40f);
            contador.localScale = Vector3.one;
            InventarioListaBuilder.ConfigurarTexto(slot.itemCount, 30f, TextAlignmentOptions.BottomRight, fonte);
            slot.itemCount.enableAutoSizing = false;
            slot.itemCount.textWrappingMode = TextWrappingModes.NoWrap;
            slot.itemCount.overflowMode = TextOverflowModes.Overflow;
            if (slot.item == null) slot.itemCount.text = string.Empty;
        }

        Transform vazioAntigo = slot.transform.Find("TextoVazio");
        if (vazioAntigo != null) Object.DestroyImmediate(vazioAntigo.gameObject);
        TextMeshProUGUI vazio = InventarioListaBuilder.CriarTexto("TextoVazio", slot.transform, placeholder, 72f, TextAlignmentOptions.Center, fonte);
        vazio.color = InventarioListaBuilder.CorTextoVazio;
        InventarioListaBuilder.Esticar(vazio.rectTransform, Vector2.zero, Vector2.zero);
        slot.objetoVazio = vazio.gameObject;
        vazio.gameObject.SetActive(slot.item == null);

        Transform rotuloAntigo = slot.transform.Find("Rotulo");
        if (rotuloAntigo != null) Object.DestroyImmediate(rotuloAntigo.gameObject);
        TextMeshProUGUI texto = InventarioListaBuilder.CriarTexto("Rotulo", slot.transform, rotulo, 26f, TextAlignmentOptions.Top, fonte);
        texto.color = CorRotulo;
        RectTransform rRotulo = texto.rectTransform;
        rRotulo.anchorMin = rRotulo.anchorMax = new Vector2(0.5f, 0f);
        rRotulo.pivot = new Vector2(0.5f, 1f);
        rRotulo.anchoredPosition = new Vector2(0f, -6f);
        rRotulo.sizeDelta = new Vector2(220f, 32f);
    }

    private static void ConfigurarSinal(RectTransform painel, string nome, string simbolo, Vector2 posicao, TMP_FontAsset fonte)
    {
        Transform antigo = painel.Find(nome);
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);

        TextMeshProUGUI sinal = InventarioListaBuilder.CriarTexto(nome, painel, simbolo, 64f, TextAlignmentOptions.Center, fonte);
        sinal.color = CorRotulo;
        RectTransform rect = sinal.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(80f, 80f);
    }

    private static void ConfigurarBotao(RectTransform painel, TMP_FontAsset fonte, Sprite quadrado)
    {
        Transform botaoT = painel.Find("Misturar");
        if (botaoT == null)
        {
            Debug.LogError("[PrensaPainelBuilder] Botão 'Misturar' não encontrado; o onClick dele (CraftingPress.CombineItems) precisa ser preservado.");
            return;
        }

        RectTransform botao = (RectTransform)botaoT;
        botao.anchorMin = botao.anchorMax = new Vector2(0.5f, 0.5f);
        botao.pivot = new Vector2(0.5f, 0.5f);
        botao.anchoredPosition = new Vector2(0f, AlturaBotao);
        botao.sizeDelta = TamanhoBotao;
        botao.localScale = Vector3.one;
        botao.SetAsLastSibling();

        Image fundo = botao.GetComponent<Image>();
        fundo.sprite = quadrado;
        fundo.type = Image.Type.Sliced;
        fundo.color = InventarioListaBuilder.CorBotaoFechar;

        Button button = botao.GetComponent<Button>();
        button.targetGraphic = fundo;

        if (botao.GetComponent<CanvasGroup>() == null) botao.gameObject.AddComponent<CanvasGroup>();
        if (botao.GetComponent<BotaoMisturarEstado>() == null) botao.gameObject.AddComponent<BotaoMisturarEstado>();

        TextMeshProUGUI rotulo = botao.GetComponentInChildren<TextMeshProUGUI>(true);
        if (rotulo == null) rotulo = InventarioListaBuilder.CriarTexto("Texto", botao, "Misturar", 34f, TextAlignmentOptions.Center, fonte);
        rotulo.text = "Misturar";
        InventarioListaBuilder.ConfigurarTexto(rotulo, 34f, TextAlignmentOptions.Center, fonte);
        rotulo.enableAutoSizing = false;
        InventarioListaBuilder.Esticar(rotulo.rectTransform, Vector2.zero, Vector2.zero);
    }
}
