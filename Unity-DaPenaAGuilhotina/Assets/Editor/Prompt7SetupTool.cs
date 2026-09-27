using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Prompt 7 (tutorial, transições e UI integrada) e os ajustes pedidos pelo grupo em 27/09:
/// - GameManager.prefab: 6h de investigação por caso nas Fases 2, 3 e 4 (horasPorCaso; a conversão continua 1h por
///   interação nova). Só troca o valor antigo (4); outro valor escolhido no Inspector fica e é listado;
/// - UI.prefab: o CanvasScaler passa a "Expand" (continua 1920 x 1080 de referência). Em 16:9 e em telas mais largas
///   (celulares) nada muda; em telas mais estreitas (16:10, 4:3) a UI inteira cabe na largura em vez de cortar a prensa;
/// - cena Jogo: o texto da dica de Fato x Boato do TutorialManager (falava em "procurar outra fonte que confirme") passa a
///   apontar o Quadro de pistas, se ainda for o texto antigo; os "pensamentos" do alçapão, que usavam falas do Operário da
///   Fase 2, passam a ser falas curtas do protagonista (assets novos em Dialogos/Julien Valois).
/// A dedução desde a Fase 2 fica na ferramenta 5 (Ferramentas > Campanha > 5 - Aplicar dedução ativa).
/// Idempotente. Recusa rodar em Play Mode ou com cena suja; restaura a cena que estava aberta.
/// Menu: Ferramentas > Campanha > 7 - Aplicar ajustes do Prompt 7
/// </summary>
public static class Prompt7SetupTool
{
    private const string PrefabGameManager = "Assets/Prefab/GameManager.prefab";
    private const string PrefabUI = "Assets/Prefab/UI.prefab";
    private const string CenaJogo = "Assets/Scenes/Jogo.unity";
    private const string PastaPensamentos = "Assets/Dialogos/Julien Valois";
    private const string Protagonista = "Julien Valois";

    public const int HorasPorCaso = 6;
    private const int HorasAntigas = 4;

    /// <summary>Texto da dica de Fato x Boato até o Prompt 7 (gravado na cena Jogo).</summary>
    public const string DicaFatoBoatoAntiga =
        "Nem toda pista é verdade. No inventário, {apontar} uma pista para ler quem contou. " +
        "Pistas \"não verificadas\" podem ser boatos: procure outra fonte que confirme antes de imprimir.";

    // Falas do Operário que o alçapão usava como "pensamento" (referência errada, anotada desde o Prompt 0).
    private static readonly string[] PensamentosErrados = { "Operario_Dialogo", "resposta_operario" };

    private const string PensamentoSemCaso = PastaPensamentos + "/Pensamento_Alcapao_SemCaso.asset";
    private const string PensamentoFaltamPistas = PastaPensamentos + "/Pensamento_Alcapao_FaltamPistas.asset";
    private static readonly string[] FalaSemCaso = { "Não tenho nada para imprimir agora. Primeiro preciso aceitar um caso na mesa." };
    private static readonly string[] FalaFaltamPistas = { "Ainda não. Preciso do relato de Marie e do decreto de Dupaty antes de descer à prensa." };

    [MenuItem("Ferramentas/Campanha/7 - Aplicar ajustes do Prompt 7")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[Prompt7Setup] Ajustes do Prompt 7\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        var mantidos = new List<string>();
        int prefabs = AjustarGameManager(mantidos) + AjustarEscalaDaUI(mantidos);
        int pensamentosCriados = GarantirPensamento(PensamentoSemCaso, FalaSemCaso) + GarantirPensamento(PensamentoFaltamPistas, FalaFaltamPistas);
        AssetDatabase.SaveAssets();
        int cena = AjustarCenaJogo(mantidos);

        r.AppendLine($"  Alterações nos prefabs: {prefabs}; pensamentos criados: {pensamentosCriados}; alterações na cena Jogo: {cena}.");
        if (mantidos.Count > 0) r.AppendLine("  Mantidos (valor escolhido no Inspector, diferente do antigo): " + string.Join(", ", mantidos) + ".");
        return r.ToString();
    }

    private static int AjustarGameManager(List<string> mantidos)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabGameManager);
        try
        {
            GameManager gm = raiz.GetComponent<GameManager>();
            if (gm == null || gm.horasPorCaso == HorasPorCaso) return 0;
            if (gm.horasPorCaso != HorasAntigas) { mantidos.Add($"GameManager.horasPorCaso ({gm.horasPorCaso}h)"); return 0; }
            gm.horasPorCaso = HorasPorCaso;
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabGameManager);
            return 1;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    // Só troca a configuração antiga exata (MatchWidthOrHeight com peso 1 = altura).
    private static int AjustarEscalaDaUI(List<string> mantidos)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            CanvasScaler escala = raiz.GetComponent<CanvasScaler>();
            if (escala == null || escala.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand) return 0;
            if (escala.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || escala.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight ||
                !Mathf.Approximately(escala.matchWidthOrHeight, 1f))
            {
                mantidos.Add("UI.prefab CanvasScaler");
                return 0;
            }
            escala.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
            return 1;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    private static int GarantirPensamento(string caminho, string[] falas)
    {
        if (AssetDatabase.LoadAssetAtPath<DialogueData>(caminho) != null) return 0;
        if (!AssetDatabase.IsValidFolder(PastaPensamentos)) AssetDatabase.CreateFolder("Assets/Dialogos", "Julien Valois");
        var dialogo = ScriptableObject.CreateInstance<DialogueData>();
        dialogo.talkScript = new List<Dialogue>();
        foreach (string fala in falas) dialogo.talkScript.Add(new Dialogue { name = Protagonista, text = fala, choices = new List<Choice>() });
        AssetDatabase.CreateAsset(dialogo, caminho);
        return 1;
    }

    private static int AjustarCenaJogo(List<string> mantidos)
    {
        string cenaOriginal = SceneManager.GetActiveScene().path;
        Scene cena = EditorSceneManager.OpenScene(CenaJogo, OpenSceneMode.Single);
        int alteracoes = 0;
        try
        {
            TutorialManager tm = Object.FindAnyObjectByType<TutorialManager>(FindObjectsInactive.Include);
            if (tm != null && tm.dicaFatoBoato != TutorialManager.DicaFatoBoatoPadrao)
            {
                if (tm.dicaFatoBoato == DicaFatoBoatoAntiga)
                {
                    Undo.RecordObject(tm, "Dica de Fato x Boato (Prompt 7)");
                    tm.dicaFatoBoato = TutorialManager.DicaFatoBoatoPadrao; // as dicas novas ainda não estão na cena: valem os padrões
                    EditorUtility.SetDirty(tm);
                    alteracoes++;
                }
                else mantidos.Add("TutorialManager.dicaFatoBoato");
            }

            DialogueData semCaso = AssetDatabase.LoadAssetAtPath<DialogueData>(PensamentoSemCaso);
            DialogueData faltamPistas = AssetDatabase.LoadAssetAtPath<DialogueData>(PensamentoFaltamPistas);
            foreach (TrapdoorInteractable alcapao in Object.FindObjectsByType<TrapdoorInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (alcapao.gameObject.scene != cena) continue;
                if (TrocarPensamento(alcapao, ref alcapao.pensamentoSemCaso, semCaso)) alteracoes++;
                if (TrocarPensamento(alcapao, ref alcapao.pensamentoFaltamPistas, faltamPistas)) alteracoes++;
            }

            if (alteracoes > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(cenaOriginal) && cenaOriginal != CenaJogo) EditorSceneManager.OpenScene(cenaOriginal, OpenSceneMode.Single);
        }
        return alteracoes;
    }

    private static bool TrocarPensamento(TrapdoorInteractable alcapao, ref DialogueData campo, DialogueData novo)
    {
        if (novo == null || campo == novo) return false;
        if (campo != null && System.Array.IndexOf(PensamentosErrados, campo.name) < 0) return false; // escolhido no Inspector: fica
        Undo.RecordObject(alcapao, "Pensamentos do alçapão (Prompt 7)");
        campo = novo;
        EditorUtility.SetDirty(alcapao);
        return true;
    }
}
