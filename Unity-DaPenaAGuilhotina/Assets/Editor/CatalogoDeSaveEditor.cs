using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Mantém o Resources/CatalogoDeSave com todos os Items e Casos do projeto. Roda sozinho ao dar Play e antes
/// do build, então um item novo já entra no save sem ninguém precisar lembrar de registrar.
/// </summary>
[InitializeOnLoad]
public class CatalogoDeSaveEditor : IPreprocessBuildWithReport
{
    private const string Pasta = "Assets/Resources";
    private const string Caminho = Pasta + "/" + CatalogoDeSave.CaminhoNoResources + ".asset";

    static CatalogoDeSaveEditor()
    {
        EditorApplication.playModeStateChanged += estado =>
        {
            if (estado == PlayModeStateChange.ExitingEditMode) Atualizar();
        };
    }

    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => Atualizar();

    [MenuItem("Ferramentas/Save/Atualizar catálogo")]
    public static void Atualizar()
    {
        CatalogoDeSave catalogo = AssetDatabase.LoadAssetAtPath<CatalogoDeSave>(Caminho);
        if (catalogo == null)
        {
            if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets", "Resources");
            catalogo = ScriptableObject.CreateInstance<CatalogoDeSave>();
            AssetDatabase.CreateAsset(catalogo, Caminho);
        }

        List<Item> itens = Buscar<Item>();
        List<CaseData> casos = Buscar<CaseData>();
        AvisarIdsRepetidos(itens);

        if (MesmoConteudo(catalogo.itens, itens) && MesmoConteudo(catalogo.casos, casos)) return;
        catalogo.itens = itens;
        catalogo.casos = casos;
        EditorUtility.SetDirty(catalogo);
        AssetDatabase.SaveAssetIfDirty(catalogo);
        Debug.Log($"[CatalogoDeSave] Atualizado: {itens.Count} itens, {casos.Count} casos.");
    }

    [MenuItem("Ferramentas/Save/Apagar save")]
    public static void ApagarSave()
    {
        if (File.Exists(SistemaDeSave.CaminhoDoArquivo)) File.Delete(SistemaDeSave.CaminhoDoArquivo);
        Debug.Log($"[SistemaDeSave] Save apagado ({SistemaDeSave.CaminhoDoArquivo}).");
    }

    [MenuItem("Ferramentas/Save/Abrir pasta do save")]
    public static void AbrirPasta() => EditorUtility.RevealInFinder(Application.persistentDataPath);

    private static List<T> Buscar<T>() where T : Object
    {
        var lista = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" }))
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) lista.Add(asset);
        }
        lista.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return lista;
    }

    private static bool MesmoConteudo<T>(List<T> atual, List<T> novo) where T : Object
    {
        if (atual == null || atual.Count != novo.Count) return false;
        for (int i = 0; i < novo.Count; i++)
            if (atual[i] != novo[i]) return false;
        return true;
    }

    // O save guarda o item pelo itemID: dois itens com o mesmo ID voltariam como o primeiro deles.
    private static void AvisarIdsRepetidos(List<Item> itens)
    {
        var vistos = new Dictionary<string, Item>();
        foreach (Item item in itens)
        {
            if (string.IsNullOrEmpty(item.itemID)) continue;
            if (vistos.TryGetValue(item.itemID, out Item outro))
                Debug.LogWarning($"[CatalogoDeSave] '{item.name}' e '{outro.name}' têm o mesmo itemID '{item.itemID}': o save não distingue os dois.", item);
            else
                vistos.Add(item.itemID, item);
        }
    }
}
