/// <summary>
/// Janelas modais da HUD montadas por código (Biblioteca e Quadro de dedução). Enquanto uma delas está aberta o jogo fica
/// pausado e movimento, interação, inventário e pause não respondem. Quem precisa dessa trava consulta aqui, em vez de
/// conhecer cada janela.
/// </summary>
public static class JanelasModais
{
    public static bool AlgumaAberta => BibliotecaUI.Aberta || QuadroDeDeducaoUI.Aberto;

    /// <summary>O Esc que fechou uma janela não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => BibliotecaUI.BloqueiaPausa || QuadroDeDeducaoUI.BloqueiaPausa;
}
