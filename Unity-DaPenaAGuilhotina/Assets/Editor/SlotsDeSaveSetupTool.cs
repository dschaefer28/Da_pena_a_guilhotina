using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cena "menu principal": cria a tela de saves (SlotsDeSaveUI) que o JOGAR abre, no mesmo padrão dos outros painéis do
/// menu. A tela é uma cópia do painel de CRÉDITOS (moldura fundohud, título, VOLTAR e sons); cada slot é um cartão com
/// o mesmo sprite fundohud em 9-slice, título e detalhe na fonte do título e botões copiados do VOLTAR (sprite button).
/// A pergunta "Excluir o save?" é uma cópia da pergunta do Novo Jogo (mesma moldura, SIM e NÃO).
/// Nenhum visual é criado em runtime: o SlotsDeSaveUI só preenche textos e liga/desliga botões.
/// Idempotente: o que já existe fica como está (para refazer, apague "painelSaves" e "painelConfirmarExclusaoSave").
/// Recusa rodar em Play Mode ou com cena suja; restaura a cena que estava aberta.
/// Menu: Ferramentas > Menu principal > Criar tela de Saves
/// </summary>
public static class SlotsDeSaveSetupTool
{
    private const string CenaMenu = "Assets/Scenes/menu principal.unity";
    private const string NomePainel = "painelSaves";
    private const string NomePergunta = "painelConfirmarExclusaoSave";
    public const string TituloPainel = "SAVES";
    private const string SpriteDaMoldura = "Assets/Art/UI/Interface/fundohud.png";

    private static readonly Vector2 TamanhoDaMoldura = new Vector2(1340f, 820f);
    private static readonly Vector2 TamanhoDoCartao = new Vector2(1140f, 150f);
    private const float PrimeiroCartaoY = 185f;
    private const float PassoDosCartoes = 168f;
    // Borda do 9-slice do fundohud_0 (px do sprite): cobre o contorno e os cantos arredondados.
    private const int BordaDaMoldura = 48;
    private const float EscalaDaBordaNoCartao = 1.5f; // contorno do cartão com a mesma espessura visual da moldura grande
    private static readonly Color CorDoCartao = new Color(0.86f, 0.86f, 0.86f, 1f);

    [MenuItem("Ferramentas/Menu principal/Criar tela de Saves")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[SlotsDeSaveSetup] Tela de saves no menu principal\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        r.AppendLine("  Sprite da moldura: " + GarantirBordaDaMoldura());

        string cenaOriginal = SceneManager.GetActiveScene().path;
        Scene cena = EditorSceneManager.OpenScene(CenaMenu, OpenSceneMode.Single);
        try
        {
            MenuPrincipalManager menu = Object.FindAnyObjectByType<MenuPrincipalManager>(FindObjectsInactive.Include);
            if (menu == null) return r.Append("MenuPrincipalManager não encontrado na cena. Nada foi alterado.").ToString();

            r.AppendLine("  Painel: " + GarantirPainel(menu));

            if (cena.isDirty) EditorSceneManager.SaveScene(cena);
            else r.AppendLine("  Nenhuma alteração.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaMenu) EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
        return r.ToString();
    }

    // O fundohud_0 não tinha borda de 9-slice (todos os usos são Simple, que ignoram a borda): o cartão precisa dela para
    // esticar sem deformar o contorno.
    private static string GarantirBordaDaMoldura()
    {
        var importador = AssetImporter.GetAtPath(SpriteDaMoldura) as TextureImporter;
        if (importador == null) return "fundohud.png não encontrado.";
        var fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        ISpriteEditorDataProvider dados = fabrica.GetSpriteEditorDataProviderFromObject(importador);
        dados.InitSpriteEditorDataProvider();
        SpriteRect[] sprites = dados.GetSpriteRects();
        var borda = new Vector4(BordaDaMoldura, BordaDaMoldura, BordaDaMoldura, BordaDaMoldura);
        bool mudou = false;
        foreach (SpriteRect s in sprites)
        {
            if (s.name != "fundohud_0" || s.border == borda) continue;
            s.border = borda;
            mudou = true;
        }
        if (!mudou) return "borda de 9-slice já definida.";
        dados.SetSpriteRects(sprites);
        dados.Apply();
        importador.SaveAndReimport();
        return $"borda de 9-slice {BordaDaMoldura} px definida no fundohud_0.";
    }

    private static string GarantirPainel(MenuPrincipalManager menu)
    {
        var so = new SerializedObject(menu);
        SerializedProperty campo = so.FindProperty("painelSaves");
        if (campo.objectReferenceValue != null) return $"já ligado ('{campo.objectReferenceValue.name}'), mantido.";

        GameObject creditos = so.FindProperty("painelCreditos")?.objectReferenceValue as GameObject;
        GameObject perguntaNovoJogo = so.FindProperty("painelConfirmarNovoJogo")?.objectReferenceValue as GameObject;
        if (creditos == null) return "painel de CRÉDITOS não encontrado para servir de modelo; nada criado.";
        if (perguntaNovoJogo == null) return "pergunta do Novo Jogo não encontrada para servir de modelo; nada criado.";

        Transform existente = creditos.transform.parent.Find(NomePainel);
        if (existente != null)
        {
            campo.objectReferenceValue = existente.gameObject;
            so.ApplyModifiedProperties();
            return $"'{NomePainel}' já existia; ligado ao campo.";
        }

        GameObject painel = Object.Instantiate(creditos, creditos.transform.parent);
        painel.name = NomePainel;
        Undo.RegisterCreatedObjectUndo(painel, "Tela de saves");
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
        if (moldura == null || voltar == null) return "moldura ou VOLTAR não encontrados no painel de créditos; apague 'painelSaves' e confira o modelo.";
        TMP_Text titulo = moldura.GetComponentInChildren<TMP_Text>(true);
        if (titulo == null) return "título não encontrado na moldura dos créditos; apague 'painelSaves' e confira o modelo.";

        var sobras = new List<GameObject>();
        foreach (Transform filho in painel.transform)
            if (filho != moldura.transform && filho != voltar.transform) sobras.Add(filho.gameObject);
        foreach (GameObject sobra in sobras) Object.DestroyImmediate(sobra);

        Posicionar((RectTransform)moldura.transform, Vector2.zero, TamanhoDaMoldura);

        titulo.transform.SetParent(painel.transform, false);
        titulo.name = "Titulo";
        titulo.text = TituloPainel;
        titulo.fontSize = 60f;
        Posicionar(titulo.rectTransform, new Vector2(0f, 330f), new Vector2(1100f, 90f));

        var ui = painel.AddComponent<SlotsDeSaveUI>();

        var cartoes = new List<(TMP_Text titulo, TMP_Text detalhe, Button carregar, Button novo, Button excluir)>();
        for (int i = 0; i < SistemaDeSave.NumeroDeSlots; i++)
            cartoes.Add(CriarCartao(painel.transform, moldura, titulo, voltar, ui, i));

        voltar.name = "BotãoVoltar";
        Posicionar((RectTransform)voltar.transform, new Vector2(0f, -330f), new Vector2(260f, 70f));
        Religar(voltar, menu, nameof(MenuPrincipalManager.FecharSaves));
        voltar.transform.SetAsLastSibling();

        GameObject pergunta = CriarPergunta(perguntaNovoJogo, ui, out TMP_Text tituloPergunta, out Button nao);
        if (pergunta == null) return "pergunta do Novo Jogo sem SIM/NÃO ou sem título; apague 'painelSaves' e confira o modelo.";
        pergunta.transform.SetSiblingIndex(painel.transform.GetSiblingIndex() + 1);

        var uiSo = new SerializedObject(ui);
        uiSo.FindProperty("menu").objectReferenceValue = menu;
        SerializedProperty lista = uiSo.FindProperty("cartoes");
        lista.arraySize = cartoes.Count;
        for (int i = 0; i < cartoes.Count; i++)
        {
            SerializedProperty c = lista.GetArrayElementAtIndex(i);
            c.FindPropertyRelative("titulo").objectReferenceValue = cartoes[i].titulo;
            c.FindPropertyRelative("detalhe").objectReferenceValue = cartoes[i].detalhe;
            c.FindPropertyRelative("botaoCarregar").objectReferenceValue = cartoes[i].carregar;
            c.FindPropertyRelative("botaoNovoJogo").objectReferenceValue = cartoes[i].novo;
            c.FindPropertyRelative("botaoExcluir").objectReferenceValue = cartoes[i].excluir;
        }
        uiSo.FindProperty("painelConfirmarExclusao").objectReferenceValue = pergunta;
        uiSo.FindProperty("tituloConfirmarExclusao").objectReferenceValue = tituloPergunta;
        uiSo.FindProperty("botaoNaoExcluir").objectReferenceValue = nao.gameObject;
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        campo.objectReferenceValue = painel;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(menu);

        // JOGAR continua chamando MenuPrincipalManager.Jogar, que agora abre esta tela.
        return $"criada a partir do painel de CRÉDITOS com {cartoes.Count} slots e a pergunta de exclusão (cópia da do Novo Jogo).";
    }

    private static (TMP_Text, TMP_Text, Button, Button, Button) CriarCartao(Transform painel, Image moldura, TMP_Text modeloDeTexto,
        Button modeloDeBotao, SlotsDeSaveUI ui, int indice)
    {
        GameObject cartao = Object.Instantiate(moldura.gameObject, painel);
        cartao.name = $"Slot{indice + 1}";
        foreach (Transform filho in cartao.transform) Object.DestroyImmediate(filho.gameObject); // a moldura não tem filhos, mas por garantia
        var imagem = cartao.GetComponent<Image>();
        imagem.type = Image.Type.Sliced;
        imagem.pixelsPerUnitMultiplier = EscalaDaBordaNoCartao;
        imagem.color = CorDoCartao;
        imagem.raycastTarget = false;
        Posicionar((RectTransform)cartao.transform, new Vector2(0f, PrimeiroCartaoY - indice * PassoDosCartoes), TamanhoDoCartao);

        TMP_Text titulo = CopiarTexto(modeloDeTexto, cartao.transform, "Titulo", $"SAVE {indice + 1}", 40f,
            new Vector2(-285f, 30f), new Vector2(510f, 55f));
        TMP_Text detalhe = CopiarTexto(modeloDeTexto, cartao.transform, "Detalhe", "Vazio", 28f,
            new Vector2(-285f, -27f), new Vector2(510f, 70f));
        detalhe.alignment = TextAlignmentOptions.TopLeft;
        detalhe.textWrappingMode = TextWrappingModes.Normal;
        detalhe.richText = true;

        Button carregar = CopiarBotao(modeloDeBotao, cartao.transform, "BotaoCarregar", "CARREGAR", new Vector2(165f, 0f), new Vector2(250f, 75f));
        Button excluir = CopiarBotao(modeloDeBotao, cartao.transform, "BotaoExcluir", "EXCLUIR", new Vector2(415f, 0f), new Vector2(210f, 75f));
        Button novo = CopiarBotao(modeloDeBotao, cartao.transform, "BotaoNovoJogo", "NOVO JOGO", new Vector2(370f, 0f), new Vector2(300f, 75f));
        UnityEventTools.AddIntPersistentListener(carregar.onClick, ui.Carregar, indice);
        UnityEventTools.AddIntPersistentListener(excluir.onClick, ui.PedirExclusao, indice);
        UnityEventTools.AddIntPersistentListener(novo.onClick, ui.NovoJogo, indice);
        novo.gameObject.SetActive(false);
        return (titulo, detalhe, carregar, novo, excluir);
    }

    // Cópia da pergunta do Novo Jogo, com os textos e o SIM/NÃO religados ao SlotsDeSaveUI.
    private static GameObject CriarPergunta(GameObject modelo, SlotsDeSaveUI ui, out TMP_Text titulo, out Button nao)
    {
        titulo = null;
        nao = null;
        GameObject pergunta = Object.Instantiate(modelo, modelo.transform.parent);
        pergunta.name = NomePergunta;
        Undo.RegisterCreatedObjectUndo(pergunta, "Pergunta excluir save");
        pergunta.SetActive(false);

        Button sim = null;
        foreach (Button b in pergunta.GetComponentsInChildren<Button>(true))
        {
            if (b.name.Contains("Sim")) sim = b;
            else if (b.name.Contains("Não") || b.name.Contains("Nao")) nao = b;
        }
        TMP_Text aviso = null;
        foreach (TMP_Text t in pergunta.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>() != null) continue;
            if (t.name == "titulo") titulo = t;
            else if (t.name == "aviso") aviso = t;
        }
        if (sim == null || nao == null || titulo == null)
        {
            Object.DestroyImmediate(pergunta);
            return null;
        }

        titulo.text = "EXCLUIR O SAVE 1?";
        if (aviso != null) aviso.text = "Essa ação não pode ser desfeita.";
        LimparOnClick(sim);
        LimparOnClick(nao);
        Ligar(sim, ui, nameof(SlotsDeSaveUI.ConfirmarExclusao));
        Ligar(nao, ui, nameof(SlotsDeSaveUI.CancelarExclusao));
        return pergunta;
    }

    // ===== Utilidades (mesmo padrão do ArquivoDeCasosSetupTool) =====

    private static TMP_Text CopiarTexto(TMP_Text modelo, Transform pai, string nome, string texto, float fonte, Vector2 posicao, Vector2 tamanho)
    {
        TMP_Text t = Object.Instantiate(modelo.gameObject, pai).GetComponent<TMP_Text>();
        t.name = nome;
        t.text = texto;
        t.fontSize = fonte;
        t.enableAutoSizing = false;
        t.alignment = TextAlignmentOptions.Left;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        Posicionar(t.rectTransform, posicao, tamanho);
        return t;
    }

    private static Button CopiarBotao(Button modelo, Transform pai, string nome, string rotulo, Vector2 posicao, Vector2 tamanho)
    {
        GameObject copia = Object.Instantiate(modelo.gameObject, pai);
        copia.name = nome;
        Posicionar((RectTransform)copia.transform, posicao, tamanho);
        Button b = copia.GetComponent<Button>();
        LimparOnClick(b);
        TMP_Text texto = copia.GetComponentInChildren<TMP_Text>(true);
        if (texto != null) texto.text = rotulo;
        return b;
    }

    private static void Posicionar(RectTransform rt, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
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

    private static void Religar(Button b, MenuPrincipalManager menu, string metodo)
    {
        Undo.RecordObject(b, "OnClick do menu");
        LimparOnClick(b);
        Ligar(b, menu, metodo);
        EditorUtility.SetDirty(b);
    }
}
