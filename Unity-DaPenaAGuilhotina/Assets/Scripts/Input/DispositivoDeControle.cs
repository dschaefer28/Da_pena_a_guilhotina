using System.Text;

/// <summary>Controle do jogo que uma etapa do tutorial (ou um aviso na tela) quer destacar.</summary>
public enum ControleTutorial
{
    Nenhum,
    Mover,
    Interagir,
    Inventario,
    Pausa,
    Continuar,
    /// <summary>Cartão com mover, interagir e inventário (primeira tela do tutorial).</summary>
    Principais
}

/// <summary>
/// Traduz marcadores das mensagens para o aparelho atual ("{interagir}" → "a tecla E" / "o botão Interagir").
/// "É toque?" vem de MobileUIManager.ControlesMobileAtivos, então texto e botões nunca discordam.
/// Marcadores: {mover} {interagir} {inventario} {pausa} {continuar} {Continuar} {toque} {Toque} {apontar}.
/// </summary>
public static class DispositivoDeControle
{
    public static bool EhToque => MobileUIManager.ControlesMobileAtivos;

    /// <summary>Texto curto para dentro da "tecla" do glifo.</summary>
    public static string Glifo(ControleTutorial controle)
    {
        bool toque = EhToque;
        switch (controle)
        {
            case ControleTutorial.Mover:      return toque ? "Joystick" : "A  D";
            case ControleTutorial.Interagir:  return toque ? "Interagir" : "E";
            case ControleTutorial.Inventario: return toque ? "Inventário" : "I";
            case ControleTutorial.Pausa:      return toque ? "Pause" : "Esc";
            case ControleTutorial.Continuar:  return toque ? "Continuar" : "Enter";
            default: return string.Empty;
        }
    }

    public static string NomeDaAcao(ControleTutorial controle)
    {
        switch (controle)
        {
            case ControleTutorial.Mover:      return "Mover";
            case ControleTutorial.Interagir:  return "Interagir";
            case ControleTutorial.Inventario: return "Inventário";
            case ControleTutorial.Pausa:      return "Pausar";
            case ControleTutorial.Continuar:  return "Continuar";
            default: return string.Empty;
        }
    }

    public static string Frase(ControleTutorial controle)
    {
        bool toque = EhToque;
        switch (controle)
        {
            case ControleTutorial.Mover:      return toque ? "o joystick" : "as teclas A e D (ou as setas)";
            case ControleTutorial.Interagir:  return toque ? "o botão Interagir" : "a tecla E";
            case ControleTutorial.Inventario: return toque ? "o botão Inventário" : "a tecla I";
            case ControleTutorial.Pausa:      return toque ? "o botão Pause" : "a tecla Esc";
            case ControleTutorial.Continuar:  return toque ? "toque em Continuar" : "clique em Continuar (ou aperte Enter)";
            default: return string.Empty;
        }
    }

    public static string Substituir(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.IndexOf('{') < 0) return texto;

        var sb = new StringBuilder(texto);
        sb.Replace("{mover}", Frase(ControleTutorial.Mover));
        sb.Replace("{interagir}", Frase(ControleTutorial.Interagir));
        sb.Replace("{inventario}", Frase(ControleTutorial.Inventario));
        sb.Replace("{pausa}", Frase(ControleTutorial.Pausa));
        sb.Replace("{continuar}", Frase(ControleTutorial.Continuar));
        sb.Replace("{Continuar}", Capitalizar(Frase(ControleTutorial.Continuar)));
        sb.Replace("{toque}", EhToque ? "toque" : "clique");
        sb.Replace("{Toque}", EhToque ? "Toque" : "Clique");
        // Ficha da pista: hover no PC, toque no celular.
        sb.Replace("{apontar}", EhToque ? "toque em" : "passe o mouse sobre");
        return sb.ToString();
    }

    private static string Capitalizar(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
