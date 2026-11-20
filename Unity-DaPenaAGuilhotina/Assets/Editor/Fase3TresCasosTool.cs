using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fase 3 com três casos (o jogador conclui um): retira o Caso_Padeiro do UI.prefab (mesa e biblioteca), da cena
/// Fase3 (reações e objetos) e apaga os assets dele. Os três restantes cobrem as três tendências das barras
/// (Champ de Mars: Povo; Varennes: Estado; Assignats: ambos).
/// Idempotente: sem o caso, não faz nada. Para desfazer, recupere os arquivos pelo git.
/// Menu: Ferramentas > Campanha > 6 - Retirar o caso do Padeiro (Fase 3 com três casos)
/// </summary>
public static class Fase3TresCasosTool
{
    private const string CaminhoDoCaso = "Assets/ScriptableObjects/Cases/Caso_Padeiro.asset";
    private const string PrefabUI = "Assets/Prefabs/UI.prefab";
    private const string CenaFase3 = "Assets/Scenes/Fase3.unity";

    private static readonly string[] Assets =
    {
        CaminhoDoCaso,
        "Assets/ScriptableObjects/Items/Campanha/Fase3/ReceitaDeCaso_Padeiro.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Panfleto_Padeiro.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Pista_Padeiro_Fornadas.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Pista_Padeiro_Encomenda.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Pista_Padeiro_Boato.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Pista_Padeiro_Calunia.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Alegacao_Padeiro_1.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Alegacao_Padeiro_2.asset",
        "Assets/ScriptableObjects/Items/Campanha/Fase3/Apoio_Padeiro_Biblioteca.asset",
        "Assets/ScriptableObjects/Items/Campanha/Biblioteca/Oferta_Padeiro.asset",
        "Assets/ScriptableObjects/Dialogues/Campanha/Fase3/ViuvaFrancois_Caso_Padeiro.asset",
        "Assets/ScriptableObjects/Dialogues/Campanha/Fase3/CocheiroJoubert_Caso_Padeiro.asset",
    };

    [MenuItem("Ferramentas/Campanha/6 - Retirar o caso do Padeiro (Fase 3 com três casos)")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static string Aplicar()
    {
        var r = new StringBuilder("[Fase3TresCasos] Retirar o caso do Padeiro\n");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return r.Append("Saia do Play Mode antes.").ToString();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                return r.Append($"A cena '{SceneManager.GetSceneAt(i).name}' tem alterações não salvas. Nada foi alterado.").ToString();

        CaseData padeiro = AssetDatabase.LoadAssetAtPath<CaseData>(CaminhoDoCaso);
        if (padeiro == null) return r.Append("O caso do Padeiro já foi retirado. Nada a fazer.").ToString();
        OfertaDaBiblioteca oferta = AssetDatabase.LoadAssetAtPath<OfertaDaBiblioteca>("Assets/ScriptableObjects/Items/Campanha/Biblioteca/Oferta_Padeiro.asset");

        GameObject raiz = PrefabUtility.LoadPrefabContents(PrefabUI);
        try
        {
            int antes = 0;
            CaseSelectionUI mesa = raiz.GetComponentInChildren<CaseSelectionUI>(true);
            if (mesa != null && mesa.availableCases != null) antes += mesa.availableCases.RemoveAll(c => c == padeiro);
            BibliotecaUI biblioteca = raiz.GetComponentInChildren<BibliotecaUI>(true);
            if (biblioteca != null && oferta != null) antes += biblioteca.ofertas.RemoveAll(o => o == oferta);
            if (antes > 0) PrefabUtility.SaveAsPrefabAsset(raiz, PrefabUI);
            r.AppendLine($"  UI.prefab: {antes} referência(s) removida(s) (mesa e biblioteca).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        SceneSetup[] configuracao = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene cena = EditorSceneManager.OpenScene(CenaFase3, OpenSceneMode.Single);
            int removidas = 0;
            foreach (var par in IdDeInteracao.InteracoesDaCena(cena))
            {
                if (par.Key is NPCMovement npc && npc.reacoesDeCaso != null)
                {
                    int n = npc.reacoesDeCaso.RemoveAll(x => x.caso == padeiro);
                    if (n > 0) { removidas += n; EditorUtility.SetDirty(npc); PrefabUtility.RecordPrefabInstancePropertyModifications(npc); }
                }
                else if (par.Key is LootInteractable loot && loot.pistasPossiveis != null)
                {
                    int n = loot.pistasPossiveis.RemoveAll(x => x.caso == padeiro);
                    if (n > 0) { removidas += n; EditorUtility.SetDirty(loot); }
                }
            }
            if (removidas > 0)
            {
                EditorSceneManager.MarkSceneDirty(cena);
                EditorSceneManager.SaveScene(cena);
            }
            r.AppendLine($"  Fase3: {removidas} reação(ões)/entrada(s) do Padeiro removida(s).");
        }
        finally
        {
            if (configuracao != null && configuracao.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(configuracao);
        }

        var apagados = new List<string>();
        foreach (string caminho in Assets)
            if (AssetDatabase.LoadMainAssetAtPath(caminho) != null && AssetDatabase.DeleteAsset(caminho)) apagados.Add(System.IO.Path.GetFileNameWithoutExtension(caminho));
        AssetDatabase.SaveAssets();
        CatalogoDeSaveEditor.Atualizar();
        r.Append($"  Assets apagados ({apagados.Count}): {string.Join(", ", apagados)}.");
        return r.ToString();
    }
}
