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
    /// <summary>Cartão com os três controles principais (mover, interagir, inventário) — primeira tela do tutorial.</summary>
    Principais
}

/// <summary>
/// Traduz "qual controle" em "como se faz neste aparelho". O tutorial é um só (universal): as mensagens
/// usam marcadores como {interagir} ou {mover}, e aqui eles viram "o botão Interagir" no Android ou
/// "a tecla E" no Windows. A fonte da verdade sobre "é toque?" é a mesma regra que decide se os
/// controles mobile aparecem na tela (MobileUIManager), então texto e botões nunca discordam.
///
/// Marcadores aceitos nas mensagens: {mover} {interagir} {inventario} {pausa} {continuar}
/// {toque} (verbo: "toque"/"clique"), {Toque} (com maiúscula) e {apontar} ("passe o mouse sobre"/"toque em").
/// </summary>
public static class DispositivoDeControle
{
    public static bool EhToque => MobileUIManager.ControlesMobileAtivos;

    /// <summary>Texto curto para desenhar dentro de uma "tecla" (glifo): "E", "I", "Esc", "A  D" / "Interagir"...</summary>
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

    /// <summary>Nome da ação, para o rótulo ao lado do glifo.</summary>
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

    /// <summary>Frase completa usada no lugar do marcador: "{interagir}" -> "o botão Interagir" / "a tecla E".</summary>
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

    /// <summary>Substitui os marcadores de uma mensagem pelas frases do aparelho atual.</summary>
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
        // Como ver a ficha de uma pista no inventário (FichaDaPista): hover no PC, dedo pressionado no celular.
        sb.Replace("{apontar}", EhToque ? "toque em" : "passe o mouse sobre");
        return sb.ToString();
    }

    private static string Capitalizar(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
