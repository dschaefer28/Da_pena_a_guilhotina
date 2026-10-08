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
/// Cena "menu principal" (relatório de testes de 28/09, P0 1):
/// - recria o botão CONTINUAR (removido no commit 27cfe24): cópia do JOGAR (mesma imagem, fonte, tamanho e som de
///   clique), logo acima dele na coluna, OnClick em MenuPrincipalManager.Continuar e ligado ao campo "Botao Continuar";
///   o MenuPrincipalManager só o mostra quando existe save;
/// - cria a pergunta "Começar um novo jogo?" do JOGAR: cópia do painel de confirmação do SAIR, com SIM em
///   ConfirmarNovoJogo e NÃO em FecharConfirmacaoNovoJogo, ligada aos campos do MenuPrincipalManager.
/// Idempotente: o que já está ligado nos campos fica como está. Recusa rodar em Play Mode ou com cena suja;
/// restaura a cena que estava aberta.
/// Menu: Ferramentas > Menu principal > Recriar Continuar e confirmação do Novo Jogo
/// </summary>
public static class MenuPrincipalSetupTool
{
    private const string CenaMenu = "Assets/_Project/Scenes/menu principal.unity";
    private const string NomeContinuar = "ContinuarButton";
    private const string NomePainelNovoJogo = "painelConfirmarNovoJogo";
    public const string TextoContinuar = "CONTINUAR";
    public const string TituloNovoJogo = "COMEÇAR UM NOVO JOGO?";
    public const string AvisoNovoJogo = "O progresso salvo será substituído.";

    [MenuItem("Ferramentas/Menu principal/Recriar Continuar e confirmação do Novo Jogo")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[MenuPrincipalSetup] Continuar e confirmação do Novo Jogo\n");
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

            string continuar = GarantirContinuar(menu, so);
            string novoJogo = GarantirConfirmacaoNovoJogo(menu, so);
            r.AppendLine("  Continuar: " + continuar);
            r.AppendLine("  Confirmação do Novo Jogo: " + novoJogo);

            if (cena.isDirty) EditorSceneManager.SaveScene(cena);
            else r.AppendLine("  Nenhuma alteração.");
        }
        finally
        {
            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaMenu) EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
        return r.ToString();
    }

    private static string GarantirContinuar(MenuPrincipalManager menu, SerializedObject so)
    {
        SerializedProperty campo = so.FindProperty("botaoContinuar");
        if (campo.objectReferenceValue != null) return $"já ligado ('{campo.objectReferenceValue.name}'), mantido.";

        Button jogar = BotaoQueChama(menu, nameof(MenuPrincipalManager.Jogar));
        if (jogar == null) return "botão que chama Jogar não encontrado; nada criado.";
        Transform coluna = jogar.transform.parent;

        Transform existente = coluna.Find(NomeContinuar);
        GameObject botao;
        string acao;
        if (existente != null)
        {
            botao = existente.gameObject;
            acao = $"'{NomeContinuar}' já existia; ligado ao campo.";
        }
        else
        {
            botao = Object.Instantiate(jogar.gameObject, coluna);
            botao.name = NomeContinuar;
            Undo.RegisterCreatedObjectUndo(botao, "Botão Continuar");
            botao.transform.SetSiblingIndex(jogar.transform.GetSiblingIndex());

            // Mesmo espaçamento da coluna (JOGAR → CONFIGURAÇÃO), uma posição acima do JOGAR.
            var rtJogar = (RectTransform)jogar.transform;
            float passo = EspacamentoDaColuna(jogar);
            ((RectTransform)botao.transform).anchoredPosition = rtJogar.anchoredPosition + new Vector2(0f, passo);

            TMP_Text texto = botao.GetComponentInChildren<TMP_Text>(true);
            if (texto != null) texto.text = TextoContinuar;
            acao = $"criado acima do JOGAR ({passo:0} px), cópia do visual dele.";
        }

        Religar(botao.GetComponent<Button>(), menu, nameof(MenuPrincipalManager.Continuar));
        campo.objectReferenceValue = botao;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(menu);
        return acao;
    }

    private static string GarantirConfirmacaoNovoJogo(MenuPrincipalManager menu, SerializedObject so)
    {
        SerializedProperty campoPainel = so.FindProperty("painelConfirmarNovoJogo");
        SerializedProperty campoCancelar = so.FindProperty("botaoCancelarNovoJogo");
        if (campoPainel.objectReferenceValue != null && campoCancelar.objectReferenceValue != null)
            return $"já ligada ('{campoPainel.objectReferenceValue.name}'), mantida.";

        GameObject painelSair = so.FindProperty("painelDeConfirmação")?.objectReferenceValue as GameObject;
        GameObject painel = campoPainel.objectReferenceValue as GameObject;
        string acao;
        if (painel != null) acao = "painel já ligado; botões religados.";
        else
        {
            Transform existente = painelSair != null ? painelSair.transform.parent.Find(NomePainelNovoJogo) : null;
            if (existente != null)
            {
                painel = existente.gameObject;
                acao = $"'{NomePainelNovoJogo}' já existia; ligado aos campos.";
            }
            else
            {
                if (painelSair == null) return "painel de confirmação do SAIR não encontrado para servir de modelo; nada criado.";
                painel = Object.Instantiate(painelSair, painelSair.transform.parent);
                painel.name = NomePainelNovoJogo;
                Undo.RegisterCreatedObjectUndo(painel, "Confirmação do Novo Jogo");
                painel.transform.SetSiblingIndex(painelSair.transform.GetSiblingIndex() + 1);
                painel.SetActive(false);
                EscreverTextos(painel);
                acao = "criada a partir do painel do SAIR (mesmo visual), com título e aviso próprios.";
            }
        }

        Button sim = null, nao = null;
        foreach (Button b in painel.GetComponentsInChildren<Button>(true))
        {
            if (TemChamada(b, menu, "SairJogo") || TemChamada(b, menu, nameof(MenuPrincipalManager.ConfirmarNovoJogo))) sim = b;
            else if (TemChamada(b, menu, "FecharConfirmação") || TemChamada(b, menu, nameof(MenuPrincipalManager.FecharConfirmacaoNovoJogo))) nao = b;
        }
        if (sim == null || nao == null) return acao + " Botões SIM/NÃO não encontrados no painel; religue-os no Inspector.";

        Religar(sim, menu, nameof(MenuPrincipalManager.ConfirmarNovoJogo));
        Religar(nao, menu, nameof(MenuPrincipalManager.FecharConfirmacaoNovoJogo));
        campoPainel.objectReferenceValue = painel;
        campoCancelar.objectReferenceValue = nao.gameObject;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(menu);
        return acao;
    }

    // Título na moldura do painel copiado; o aviso é uma cópia menor do título, entre ele e os botões.
    private static void EscreverTextos(GameObject painel)
    {
        TMP_Text titulo = null;
        foreach (TMP_Text t in painel.GetComponentsInChildren<TMP_Text>(true))
            if (t.GetComponentInParent<Button>(true) == null) { titulo = t; break; }
        if (titulo == null) return;

        titulo.name = "titulo";
        titulo.text = TituloNovoJogo;

        GameObject aviso = Object.Instantiate(titulo.gameObject, titulo.transform.parent);
        aviso.name = "aviso";
        TMP_Text textoAviso = aviso.GetComponent<TMP_Text>();
        textoAviso.text = AvisoNovoJogo;
        textoAviso.fontSize = 36f;
        var rt = (RectTransform)aviso.transform;
        var rtTitulo = (RectTransform)titulo.transform;
        rt.sizeDelta = new Vector2(800f, 80f);
        rt.anchoredPosition = rtTitulo.anchoredPosition - new Vector2(0f, rtTitulo.sizeDelta.y * 0.5f + 60f);
    }

    private static float EspacamentoDaColuna(Button jogar)
    {
        Transform coluna = jogar.transform.parent;
        float y = ((RectTransform)jogar.transform).anchoredPosition.y;
        float passo = 0f;
        foreach (Transform irmao in coluna)
        {
            if (irmao == jogar.transform || irmao.GetComponent<Button>() == null) continue;
            float d = y - ((RectTransform)irmao).anchoredPosition.y;
            if (d > 0f && (passo == 0f || d < passo)) passo = d;
        }
        return passo > 0f ? passo : 110f;
    }

    private static Button BotaoQueChama(MenuPrincipalManager menu, string metodo)
    {
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b.gameObject.scene == menu.gameObject.scene && TemChamada(b, menu, metodo)) return b;
        return null;
    }

    private static bool TemChamada(Button b, MenuPrincipalManager menu, string metodo)
    {
        for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            if (b.onClick.GetPersistentTarget(i) == menu && b.onClick.GetPersistentMethodName(i) == metodo) return true;
        return false;
    }

    // Deixa o OnClick com uma única chamada persistente ao método do menu.
    private static void Religar(Button b, MenuPrincipalManager menu, string metodo)
    {
        if (b.onClick.GetPersistentEventCount() == 1 && TemChamada(b, menu, metodo)) return;
        Undo.RecordObject(b, "OnClick do menu");
        for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(b.onClick, i);
        var acao = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), menu, metodo);
        UnityEventTools.AddPersistentListener(b.onClick, acao);
        EditorUtility.SetDirty(b);
    }
}
