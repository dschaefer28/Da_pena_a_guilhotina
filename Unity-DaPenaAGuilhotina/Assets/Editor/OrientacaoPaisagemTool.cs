using UnityEditor;
using UnityEngine;

/// <summary>
/// A UI só existe em paisagem (1920x1080): Auto Rotation apenas entre Landscape Left e Right (Android/iOS).
/// Idempotente.
/// Menu: Ferramentas > Build > Android só em paisagem
/// </summary>
public static class OrientacaoPaisagemTool
{
    [MenuItem("Ferramentas/Build/Android só em paisagem")]
    public static void AplicarPeloMenu() => Debug.Log(Aplicar());

    public static bool SoPaisagem =>
        PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation &&
        !PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown &&
        PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight;

    public static string Aplicar()
    {
        const string prefixo = "[OrientacaoPaisagem] ";
        if (SoPaisagem) return prefixo + "Já estava só em paisagem (Auto Rotation, Landscape Left e Right). Nenhuma alteração.";

        string antes = Descrever();
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        Gravar();
        return $"{prefixo}Antes: {antes}. Agora: {Descrever()}.";
    }

    // Os setters do PlayerSettings mudam só a memória: sem o SetDirty, o SaveAssets não grava o ProjectSettings.asset.
    public static void Gravar()
    {
        foreach (PlayerSettings configuracao in Resources.FindObjectsOfTypeAll<PlayerSettings>()) EditorUtility.SetDirty(configuracao);
        AssetDatabase.SaveAssets();
    }

    private static string Descrever() =>
        $"{PlayerSettings.defaultInterfaceOrientation} (Portrait {Sim(PlayerSettings.allowedAutorotateToPortrait)}, " +
        $"Portrait Upside Down {Sim(PlayerSettings.allowedAutorotateToPortraitUpsideDown)}, " +
        $"Landscape Left {Sim(PlayerSettings.allowedAutorotateToLandscapeLeft)}, " +
        $"Landscape Right {Sim(PlayerSettings.allowedAutorotateToLandscapeRight)})";

    private static string Sim(bool valor) => valor ? "sim" : "não";
}
