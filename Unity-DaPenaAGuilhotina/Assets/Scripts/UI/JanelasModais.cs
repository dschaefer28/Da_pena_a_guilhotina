/// <summary>
/// Janelas modais da HUD: Biblioteca, Quadro de dedução e Mesa de Casos. Enquanto uma delas está aberta, movimento,
/// interação com o mundo, inventário e pause não respondem (a Biblioteca e o Quadro também pausam o jogo). Quem precisa
/// dessa trava consulta aqui, em vez de conhecer cada janela.
/// </summary>
public static class JanelasModais
{
    public static bool AlgumaAberta => BibliotecaUI.Aberta || QuadroDeDeducaoUI.Aberto || CaseSelectionUI.Aberta;

    /// <summary>O Esc que fechou uma janela não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => BibliotecaUI.BloqueiaPausa || QuadroDeDeducaoUI.BloqueiaPausa || CaseSelectionUI.BloqueiaPausa;
}
