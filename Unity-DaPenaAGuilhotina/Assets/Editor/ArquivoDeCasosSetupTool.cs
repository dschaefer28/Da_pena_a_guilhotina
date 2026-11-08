using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cena "menu principal": cria o botão CASOS na coluna do menu (cópia do JOGAR, logo abaixo dele; a coluna sobe meio
/// passo para caber) e o painel "Casos resolvidos" (cópia do painel de CRÉDITOS: mesma moldura fundohud, título, botão
/// VOLTAR e sons). Dentro do painel: lista de casos e setas de partida (cópias do VOLTAR, sprite button), texto do caso
/// com rolagem (fonte do título) e a barra de rolagem no mesmo modelo da Mesa de Casos. O ArquivoDeCasosUI só preenche.
/// Idempotente: o que já existe fica como está (para refazer o painel, apague "painelCasos" e rode de novo).
/// Recusa rodar em Play Mode ou com cena suja; restaura a cena que estava aberta.
/// Menu: Ferramentas > Menu principal > Criar painel Casos resolvidos
/// </summary>
public static class ArquivoDeCasosSetupTool
{
    private const string CenaMenu = "Assets/Scenes/menu principal.unity";
    private const string NomeBotaoMenu = "CasosButton";
    private const string NomePainel = "painelCasos";
    public const string TextoBotaoMenu = "CASOS";
    public const string TituloPainel = "CASOS RESOLVIDOS";
    private const int BotoesDeCaso = 4; // um caso por fase (GameManager.casosPorFase)

    // O sprite fundohud_0 já é recortado na moldura: o tamanho do Image é o tamanho visível (x de -670 a 670).
    private static readonly Vector2 TamanhoDaMoldura = new Vector2(1340f, 820f);
    private static readonly Color CorDaBarra = new Color(0f, 0f, 0f, 0.15f);
    private static readonly Color CorDaAlca = new Color(0.36f, 0.25f, 0.13f, 0.9f);

    [MenuItem("Ferramentas/Menu principal/Criar painel Casos resolvidos")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[ArquivoDeCasosSetup] Casos resolvidos no menu principal\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        string cenaOriginal = SceneManager.GetActiveScene().path;
        Scene cena = EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Single);
        try
        {
            MenuPrincipalManager menu = Object.FindAnyObjectByType<MenuPrincipalManager>(FindObjectsInactive.Include);
            if (menu == null) return r.Append("MenuPrincipalManager não encontrado na cena. Nada foi alterado.").ToString();
            var so = new SerializedObject(menu);

            r.AppendLine("  Painel: " + GarantirPainel(menu, so));
            r.AppendLine("  Botão do menu: " + GarantirBotaoDoMenu(menu));

            if (cena.isDirty) EditorSceneManager.SaveScene(cena);
            else r.AppendLine("  Nenhuma alteração.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaMenu) EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
        return r.ToString();
    }

    // ===== Botão CASOS na coluna do menu =====

    private static string GarantirBotaoDoMenu(MenuPrincipalManager menu)
    {
        Button jogar = BotaoQueChama(menu, nameof(MenuPrincipalManager.Jogar));
        if (jogar == null) return "botão que chama Jogar não encontrado; nada criado.";
        Transform coluna = jogar.transform.parent;
        if (coluna.Find(NomeBotaoMenu) != null)
        {
            Religar(coluna.Find(NomeBotaoMenu).GetComponent<Button>(), menu, nameof(MenuPrincipalManager.AbrirCasos));
            return $"'{NomeBotaoMenu}' já existia; mantido.";
        }

        // Coluna de cima para baixo, com o CASOS logo depois do JOGAR; mesmo passo, meio passo acima para caber na tela.
        var botoes = new List<RectTransform>();
        foreach (Transform filho in coluna)
            if (filho.GetComponent<Button>() != null) botoes.Add((RectTransform)filho);
        botoes.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        float passo = EspacamentoDaColuna(botoes);
        float topo = botoes[0].anchoredPosition.y + passo * 0.5f;

        GameObject novo = Object.Instantiate(jogar.gameObject, coluna);
        novo.name = NomeBotaoMenu;
        Undo.RegisterCreatedObjectUndo(novo, "Botão Casos");
        novo.transform.SetSiblingIndex(jogar.transform.GetSiblingIndex() + 1);
        TMP_Text texto = novo.GetComponentInChildren<TMP_Text>(true);
        if (texto != null) texto.text = TextoBotaoMenu;
        Religar(novo.GetComponent<Button>(), menu, nameof(MenuPrincipalManager.AbrirCasos));

        botoes.Insert(botoes.IndexOf((RectTransform)jogar.transform) + 1, (RectTransform)novo.transform);
        for (int i = 0; i < botoes.Count; i++)
        {
            Undo.RecordObject(botoes[i], "Coluna do menu");
            botoes[i].anchoredPosition = new Vector2(botoes[i].anchoredPosition.x, topo - i * passo);
        }
        return $"criado abaixo do JOGAR (cópia dele); coluna reposicionada com passo de {passo:0} px.";
    }

    // ===== Painel =====

    private static string GarantirPainel(MenuPrincipalManager menu, SerializedObject so)
    {
        SerializedProperty campo = so.FindProperty("painelCasos");
        if (campo.objectReferenceValue != null) return $"já ligado ('{campo.objectReferenceValue.name}'), mantido.";

        GameObject creditos = so.FindProperty("painelCreditos")?.objectReferenceValue as GameObject;
        if (creditos == null) return "painel de CRÉDITOS não encontrado para servir de modelo; nada criado.";

        Transform existente = creditos.transform.parent.Find(NomePainel);
        if (existente != null)
        {
            campo.objectReferenceValue = existente.gameObject;
            so.ApplyModifiedProperties();
            return $"'{NomePainel}' já existia; ligado ao campo.";
        }

        GameObject painel = Object.Instantiate(creditos, creditos.transform.parent);
        painel.name = NomePainel;
        Undo.RegisterCreatedObjectUndo(painel, "Painel Casos resolvidos");
        painel.transform.SetSiblingIndex(creditos.transform.GetSiblingIndex() + 1);
        painel.SetActive(false);

        // Do modelo ficam a moldura, o título e o VOLTAR; os nomes dos créditos saem.
        Image moldura = null;
        Button voltar = null;
        foreach (Transform filho in painel.transform)
        {
            if (moldura == null && filho.GetComponent<Image>() != null && filho.GetComponent<Button>() == null) moldura = filho.GetComponent<Image>();
            else if (voltar == null && filho.GetComponent<Button>() != null) voltar = filho.GetComponent<Button>();
        }
        if (moldura == null || voltar == null) return "moldura ou VOLTAR não encontrados no painel de créditos; apague 'painelCasos' e confira o modelo.";
        TMP_Text titulo = null;
        foreach (TMP_Text t in moldura.GetComponentsInChildren<TMP_Text>(true)) { titulo = t; break; }
        if (titulo == null) return "título não encontrado na moldura dos créditos; apague 'painelCasos' e confira o modelo.";

        var sobras = new List<GameObject>();
        foreach (Transform filho in painel.transform)
            if (filho != moldura.transform && filho != voltar.transform) sobras.Add(filho.gameObject);
        foreach (GameObject sobra in sobras) Object.DestroyImmediate(sobra);

        Posicionar((RectTransform)moldura.transform, Vector2.zero, TamanhoDaMoldura);

        titulo.transform.SetParent(painel.transform, false);
        titulo.name = "Titulo";
        titulo.text = TituloPainel;
        titulo.fontSize = 60f;
        Posicionar(titulo.rectTransform, new Vector2(0f, 335f), new Vector2(1100f, 90f));

        TMP_Text textoDaPartida = Object.Instantiate(titulo.gameObject, painel.transform).GetComponent<TMP_Text>();
        textoDaPartida.name = "TextoDaPartida";
        textoDaPartida.text = "Partida 1 de 1";
        textoDaPartida.fontSize = 28f;
        Posicionar(textoDaPartida.rectTransform, new Vector2(0f, 268f), new Vector2(900f, 50f));

        var ui = painel.AddComponent<ArquivoDeCasosUI>();

        Button anterior = CopiarBotao(voltar, painel.transform, "BotaoPartidaAnterior", "<", new Vector2(-540f, 268f), new Vector2(90f, 60f), 30f);
        Button seguinte = CopiarBotao(voltar, painel.transform, "BotaoPartidaSeguinte", ">", new Vector2(540f, 268f), new Vector2(90f, 60f), 30f);
        Ligar(anterior, ui, nameof(ArquivoDeCasosUI.PartidaAnterior));
        Ligar(seguinte, ui, nameof(ArquivoDeCasosUI.PartidaSeguinte));

        var botoesDosCasos = new Button[BotoesDeCaso];
        var rotulos = new TMP_Text[BotoesDeCaso];
        for (int i = 0; i < BotoesDeCaso; i++)
        {
            Button b = CopiarBotao(voltar, painel.transform, $"BotaoCaso{i + 1}", $"Fase {i + 1}", new Vector2(-420f, 180f - i * 115f), new Vector2(400f, 100f), 28f);
            TMP_Text rotulo = b.GetComponentInChildren<TMP_Text>(true);
            rotulo.enableAutoSizing = true;
            rotulo.fontSizeMin = 18f;
            rotulo.fontSizeMax = 28f;
            rotulo.textWrappingMode = TextWrappingModes.Normal;
            UnityEventTools.AddIntPersistentListener(b.onClick, ui.SelecionarCaso, i);
            botoesDosCasos[i] = b;
            rotulos[i] = rotulo;
        }

        ScrollRect rolagem = CriarDetalhe(painel.transform, titulo, out TMP_Text textoDoCaso);

        voltar.name = "BotãoVoltar";
        Posicionar((RectTransform)voltar.transform, new Vector2(0f, -310f), new Vector2(260f, 70f));
        Religar(voltar, menu, nameof(MenuPrincipalManager.FecharCasos));

        var uiSo = new SerializedObject(ui);
        Preencher(uiSo.FindProperty("botoesDosCasos"), botoesDosCasos);
        Preencher(uiSo.FindProperty("rotulosDosCasos"), rotulos);
        uiSo.FindProperty("textoDoCaso").objectReferenceValue = textoDoCaso;
        uiSo.FindProperty("rolagem").objectReferenceValue = rolagem;
        uiSo.FindProperty("textoDaPartida").objectReferenceValue = textoDaPartida;
        uiSo.FindProperty("botaoPartidaAnterior").objectReferenceValue = anterior;
        uiSo.FindProperty("botaoPartidaSeguinte").objectReferenceValue = seguinte;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        campo.objectReferenceValue = painel;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(menu);
        return "criado a partir do painel de CRÉDITOS (mesma moldura, título, VOLTAR e sons).";
    }

    // Área de leitura à direita da lista: viewport com máscara, texto que cresce para baixo e barra de rolagem.
    private static ScrollRect CriarDetalhe(Transform painel, TMP_Text modeloDeTexto, out TMP_Text texto)
    {
        var area = new GameObject("Detalhe", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        area.layer = painel.gameObject.layer;
        area.transform.SetParent(painel, false);
        Posicionar((RectTransform)area.transform, new Vector2(222f, -17f), new Vector2(810f, 465f));
        var alvo = area.GetComponent<Image>();
        alvo.color = new Color(1f, 1f, 1f, 0f); // só recebe o arraste/roda do mouse

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.layer = area.layer;
        viewport.transform.SetParent(area.transform, false);
        var rtViewport = (RectTransform)viewport.transform;
        rtViewport.anchorMin = Vector2.zero; rtViewport.anchorMax = Vector2.one; rtViewport.pivot = new Vector2(0f, 1f);
        rtViewport.offsetMin = Vector2.zero; rtViewport.offsetMax = new Vector2(-24f, 0f);

        texto = Object.Instantiate(modeloDeTexto.gameObject, viewport.transform).GetComponent<TMP_Text>();
        texto.name = "TextoDoCaso";
        texto.text = "Texto do caso";
        texto.fontSize = 28f;
        texto.enableAutoSizing = false;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.overflowMode = TextOverflowModes.Overflow;
        texto.richText = true;
        texto.raycastTarget = false;
        var rtTexto = texto.rectTransform;
        rtTexto.anchorMin = new Vector2(0f, 1f); rtTexto.anchorMax = Vector2.one; rtTexto.pivot = new Vector2(0.5f, 1f);
        rtTexto.anchoredPosition = Vector2.zero; rtTexto.sizeDelta = new Vector2(0f, 465f);
        var ajuste = texto.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var rolagem = area.GetComponent<ScrollRect>();
        rolagem.horizontal = false;
        rolagem.vertical = true;
        rolagem.movementType = ScrollRect.MovementType.Clamped;
        rolagem.scrollSensitivity = 30f;
        rolagem.viewport = rtViewport;
        rolagem.content = rtTexto;
        rolagem.verticalScrollbar = CriarBarra((RectTransform)area.transform);
        rolagem.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return rolagem;
    }

    // Mesmo modelo da barra da Mesa de Casos (MesaDeCasosLayoutTool), com cores que contrastam com a moldura bege.
    private static Scrollbar CriarBarra(RectTransform area)
    {
        Sprite quadrado = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var barra = new GameObject("BarraDeRolagem", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        barra.layer = area.gameObject.layer;
        barra.transform.SetParent(area, false);
        var rt = (RectTransform)barra.transform;
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = Vector2.one; rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(12f, -16f); rt.anchoredPosition = new Vector2(-4f, 0f);
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
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        return scrollbar;
    }

    // ===== Utilidades =====

    private static Button CopiarBotao(Button modelo, Transform pai, string nome, string rotulo, Vector2 posicao, Vector2 tamanho, float fonte)
    {
        GameObject copia = Object.Instantiate(modelo.gameObject, pai);
        copia.name = nome;
        Posicionar((RectTransform)copia.transform, posicao, tamanho);
        Button b = copia.GetComponent<Button>();
        LimparOnClick(b);
        TMP_Text texto = copia.GetComponentInChildren<TMP_Text>(true);
        if (texto != null) { texto.text = rotulo; texto.fontSize = fonte; }
        return b;
    }

    private static void Posicionar(RectTransform rt, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    private static void Preencher(SerializedProperty lista, Object[] valores)
    {
        lista.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
    }

    private static void LimparOnClick(Button b)
    {
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
    }

    private static void Ligar(Button b, Object alvo, string metodo)
    {
        var acao = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), alvo, metodo);
        UnityEventTools.AddPersistentListener(b.onClick, acao);
    }

    // Deixa o OnClick com uma única chamada persistente ao método do menu.
    private static void Religar(Button b, MenuPrincipalManager menu, string metodo)
    {
        if (b.onClick.GetPersistentEventCount() == 1 && b.onClick.GetPersistentTarget(0) == menu && b.onClick.GetPersistentMethodName(0) == metodo) return;
        Undo.RecordObject(b, "OnClick do menu");
        LimparOnClick(b);
        Ligar(b, menu, metodo);
        EditorUtility.SetDirty(b);
    }

    private static float EspacamentoDaColuna(List<RectTransform> botoesDeCimaParaBaixo)
    {
        float passo = 0f;
        for (int i = 1; i < botoesDeCimaParaBaixo.Count; i++)
        {
            float d = botoesDeCimaParaBaixo[i - 1].anchoredPosition.y - botoesDeCimaParaBaixo[i].anchoredPosition.y;
            if (d > 0f && (passo == 0f || d < passo)) passo = d;
        }
        return passo > 0f ? passo : 110f;
    }

    private static Button BotaoQueChama(MenuPrincipalManager menu, string metodo)
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.gameObject.scene != menu.gameObject.scene) continue;
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentTarget(i) == menu && b.onClick.GetPersistentMethodName(i) == metodo) return b;
        }
        return null;
    }
}
