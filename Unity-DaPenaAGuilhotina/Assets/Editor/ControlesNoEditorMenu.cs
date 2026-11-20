using UnityEditor;

/// <summary>
/// Ajuste global (EditorPrefs) de controles no Editor: PC (teclado) ou celular (joystick e botões na tela).
/// Não afeta builds (a plataforma decide); com o Device Simulator aberto o Editor já se comporta como celular.
/// </summary>
public static class ControlesNoEditorMenu
{
    private const string Caminho = "Ferramentas/Controles/Simular celular no Editor";

    [MenuItem(Caminho)]
    private static void Alternar()
    {
        bool novo = !EditorPrefs.GetBool(MobileUIManager.ChaveSimularCelularNoEditor, false);
        EditorPrefs.SetBool(MobileUIManager.ChaveSimularCelularNoEditor, novo);
        UnityEngine.Debug.Log("[Controles] Editor agora simula " + (novo ? "CELULAR (toque)." : "PC (teclado).") +
                              (EditorApplication.isPlaying ? " Reinicie o Play para aplicar." : ""));
    }

    [MenuItem(Caminho, true)]
    private static bool Validar()
    {
        Menu.SetChecked(Caminho, EditorPrefs.GetBool(MobileUIManager.ChaveSimularCelularNoEditor, false));
        return true;
    }
}
