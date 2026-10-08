using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Decisão do grupo (27/09): a Fase 3 passa a ter três casos, e o jogador escolhe e conclui um (como na Fase 2).
/// Sai "O Padeiro de Notre-Dame" (Caso_Padeiro): os três que ficam cobrem as três tendências das barras
/// (Champ de Mars puxa para o Povo, Varennes para o Estado, Assignats sobe os dois), e Champ de Mars guarda o exemplo
/// numérico do Prompt 3 e Varennes a etapa complementar do Cocheiro.
///
/// Esta ferramenta tira o caso da Mesa de Casos e a oferta da Biblioteca (UI.prefab), remove as reações e as entradas
/// dele nos NPCs e objetos da cena Fase3 e apaga os assets do caso (caso, receita, panfleto, pistas, alegações, oferta,
/// documento de apoio e diálogos). A cota da fase (1 caso) está em GameManager.casosPorFase.
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

        // 1. Referências no prefab UI (mesa e biblioteca).
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

        // 2. Reações dos NPCs e entradas dos objetos na cena Fase3.
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

        // 3. Assets do caso.
        var apagados = new List<string>();
        foreach (string caminho in Assets)
            if (AssetDatabase.LoadMainAssetAtPath(caminho) != null && AssetDatabase.DeleteAsset(caminho)) apagados.Add(System.IO.Path.GetFileNameWithoutExtension(caminho));
        AssetDatabase.SaveAssets();
        CatalogoDeSaveEditor.Atualizar();
        r.Append($"  Assets apagados ({apagados.Count}): {string.Join(", ", apagados)}.");
        return r.ToString();
    }
}
