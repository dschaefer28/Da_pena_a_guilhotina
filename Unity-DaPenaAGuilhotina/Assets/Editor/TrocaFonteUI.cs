using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca, em todos os prefabs e cenas do projeto, a fonte "Cinzel-VariableFont_wght SDF" (atlas de 512px
/// com 97 caracteres, sem á/é/ç...) pela "Cinzel-Regular SDF" (Latin-1 completo). Sem ela, o TextMesh Pro
/// desenhava os acentos em LiberationSans no meio das palavras.
/// Só altera valores que pertencem ao próprio objeto: em instâncias de prefab, um valor herdado é corrigido
/// no prefab de origem, não como override. Rode com o Play Mode desligado e a cena atual salva.
/// </summary>
public static class TrocaFonteUI
{
    private const string CaminhoFonteVelha = "Assets/Fonts/Cinzel-VariableFont_wght SDF.asset";
    private const string CaminhoFonteNova = "Assets/Fonts/Cinzel-Regular SDF.asset";

    [MenuItem("Ferramentas/Fontes/Trocar Cinzel Variable (sem acentos) por Cinzel-Regular")]
    public static void Trocar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[TrocaFonteUI] Saia do Play Mode antes de trocar as fontes.");
            return;
        }

        TMP_FontAsset velha = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoFonteVelha);
        TMP_FontAsset nova = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CaminhoFonteNova);
        if (velha == null || nova == null)
        {
            Debug.LogError($"[TrocaFonteUI] Fonte não encontrada: velha={(velha != null)} nova={(nova != null)}.");
            return;
        }

        Scene cenaAtual = SceneManager.GetActiveScene();
        if (cenaAtual.isDirty)
        {
            Debug.LogError($"[TrocaFonteUI] A cena '{cenaAtual.name}' tem alterações não salvas. Salve (ou descarte) antes.");
            return;
        }
        string caminhoCenaOriginal = cenaAtual.path;

        int prefabsAlterados = 0, textosEmPrefabs = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            GameObject raiz = PrefabUtility.LoadPrefabContents(caminho);
            try
            {
                int n = TrocarEm(raiz, velha, nova, caminho);
                if (n == 0) continue;
                PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
                prefabsAlterados++;
                textosEmPrefabs += n;
                Debug.Log($"[TrocaFonteUI] {caminho}: {n} texto(s) trocado(s).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }

        int cenasAlteradas = 0, textosEmCenas = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            Scene cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Single);
            int n = 0;
            foreach (GameObject raiz in cena.GetRootGameObjects())
                n += TrocarEm(raiz, velha, nova, caminho);
            if (n == 0) continue;
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            cenasAlteradas++;
            textosEmCenas += n;
            Debug.Log($"[TrocaFonteUI] {caminho}: {n} texto(s) trocado(s).");
        }

        if (!string.IsNullOrEmpty(caminhoCenaOriginal))
            EditorSceneManager.OpenScene(caminhoCenaOriginal, OpenSceneMode.Single);

        Debug.Log($"[TrocaFonteUI] Concluído: {textosEmPrefabs} texto(s) em {prefabsAlterados} prefab(s) e {textosEmCenas} texto(s) em {cenasAlteradas} cena(s) agora usam '{nova.name}'.");
    }

    private static int TrocarEm(GameObject raiz, TMP_FontAsset velha, TMP_FontAsset nova, string origem)
    {
        int trocados = 0;
        foreach (TMP_Text texto in raiz.GetComponentsInChildren<TMP_Text>(true))
        {
            if (texto.font != velha) continue;

            // Valor herdado de outro prefab: será corrigido lá, não vira override aqui.
            if (PrefabUtility.IsPartOfPrefabInstance(texto))
            {
                SerializedProperty prop = new SerializedObject(texto).FindProperty("m_fontAsset");
                if (prop != null && !prop.prefabOverride) continue;
            }

            Material materialAntes = texto.fontSharedMaterial;
            texto.font = nova; // o TMP troca o material para o padrão da fonte nova quando o atlas muda
            if (materialAntes != null && materialAntes != velha.material)
                Debug.LogWarning($"[TrocaFonteUI] {origem} > {texto.name}: usava o preset de material '{materialAntes.name}'; voltou ao material padrão da fonte nova.", texto);

            EditorUtility.SetDirty(texto);
            trocados++;
        }
        return trocados;
    }
}
