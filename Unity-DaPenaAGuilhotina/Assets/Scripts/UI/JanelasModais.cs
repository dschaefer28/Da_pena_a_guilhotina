/// <summary>
/// Ponto único de consulta: com uma janela modal aberta, movimento, interação, inventário e pause não respondem.
/// </summary>
public static class JanelasModais
{
    public static bool AlgumaAberta => BibliotecaUI.Aberta || QuadroDeDeducaoUI.Aberto || CaseSelectionUI.Aberta;

    /// <summary>O Esc que fechou uma janela não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => BibliotecaUI.BloqueiaPausa || QuadroDeDeducaoUI.BloqueiaPausa || CaseSelectionUI.BloqueiaPausa;
}
