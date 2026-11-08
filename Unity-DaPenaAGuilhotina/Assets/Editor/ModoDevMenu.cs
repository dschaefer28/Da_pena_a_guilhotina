using UnityEditor;

/// <summary>Ferramentas > Modo dev: abre o painel do modo dev em Play, para quando o F1 não chega ao jogo (ex.: o foco
/// está numa janela do Editor e não na Game view).</summary>
public static class ModoDevMenu
{
    private const string Caminho = "Ferramentas/Modo dev/Abrir ou fechar o painel (F1)";

    [MenuItem(Caminho)]
    private static void Alternar() => ModoDevUI.Alternar();

    [MenuItem(Caminho, true)]
    private static bool PodeAlternar() => EditorApplication.isPlaying;
}
