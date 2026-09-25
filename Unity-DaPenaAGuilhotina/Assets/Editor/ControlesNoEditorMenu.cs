using UnityEditor;

/// <summary>
/// Um único ajuste, para o projeto inteiro, de como o jogo se comporta no Editor: como PC (tecla E, A/D,
/// sem botões na tela) ou como celular (joystick e botões Interagir/Inventário/Pause). Vale para todas as
/// cenas ao mesmo tempo, então o tutorial nunca mais fica diferente entre Jogo e Porão. Nos builds o
/// aparelho decide sozinho (Windows = teclado, Android = toque). Com o Device Simulator aberto, o Editor
/// já se comporta como celular mesmo com esta opção desmarcada.
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
