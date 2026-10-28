using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Padrão visual único dos botões: o sprite laranja com contorno claro ("button_0", o do "Aceitar Caso" da Mesa de
/// Casos), fatiado (9-slice) para a moldura não deformar em botões de outras proporções. O asset fica em
/// Resources/EstiloDeBotao para que a UI montada em runtime (Tribunal, prensa, glifos de tecla) e as ferramentas do
/// editor usem o mesmo sprite sem precisar de campo serializado em cada script.
/// </summary>
[CreateAssetMenu(fileName = "EstiloDeBotao", menuName = "Da Pena/Estilo de Botão")]
public class EstiloDeBotao : ScriptableObject
{
    public Sprite sprite;

    // Mesma escala da moldura usada na Biblioteca e no Quadro (1 px do sprite ≈ 1,43 unidades de UI).
    public const float EscalaDaMoldura = 0.7f;

    // A face do sprite não é centrada: moldura de 7 px de cada lado, 9 px de sombra embaixo e 3 px de brilho em cima.
    public static Vector2 RecuoEsquerdaBaixo => new Vector2(7f / EscalaDaMoldura + 4f, 9f / EscalaDaMoldura);
    public static Vector2 RecuoDireitaCima => new Vector2(7f / EscalaDaMoldura + 4f, 3f / EscalaDaMoldura);

    private static EstiloDeBotao instancia;

    public static Sprite Sprite
    {
        get
        {
            if (instancia == null) instancia = Resources.Load<EstiloDeBotao>("EstiloDeBotao");
            return instancia != null ? instancia.sprite : null;
        }
    }

    /// <summary>Recuos (esquerda, direita, cima, baixo) para um layout group ficar dentro da face do sprite.</summary>
    public static RectOffset Recuos(int extraHorizontal = 0, int extraVertical = 0) => new RectOffset(
        Mathf.CeilToInt(RecuoEsquerdaBaixo.x) + extraHorizontal, Mathf.CeilToInt(RecuoDireitaCima.x) + extraHorizontal,
        Mathf.CeilToInt(RecuoDireitaCima.y) + extraVertical, Mathf.CeilToInt(RecuoEsquerdaBaixo.y) + extraVertical);

    /// <summary>Aplica o sprite ao fundo (e as cores de estado ao botão, se houver). Retorna false se o asset sumir.</summary>
    public static bool Aplicar(Image fundo, Button botao = null)
    {
        Sprite s = Sprite;
        if (fundo == null || s == null) return false;
        fundo.sprite = s;
        fundo.type = Image.Type.Sliced;
        fundo.pixelsPerUnitMultiplier = EscalaDaMoldura;
        fundo.color = Color.white;
        if (botao != null)
        {
            botao.targetGraphic = fundo;
            botao.transition = Selectable.Transition.ColorTint;
            // A tinta não clareia o sprite além da cor dele: realce e foco do teclado escurecem um pouco em vez de clarear.
            ColorBlock cores = ColorBlock.defaultColorBlock;
            cores.highlightedColor = cores.selectedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            botao.colors = cores;
        }
        return true;
    }

    // Respiro do texto além da face, igual em todos os botões (antes o texto encostava na moldura em cima e embaixo).
    public const float RespiroHorizontal = 10f;
    public const float RespiroVertical = 5f;

    /// <summary>Padding de um layout group cujo filho é o rótulo: face do sprite mais o respiro padrão.</summary>
    public static RectOffset RecuosDoRotulo() =>
        Recuos(Mathf.RoundToInt(RespiroHorizontal), Mathf.RoundToInt(RespiroVertical));

    /// <summary>
    /// Rótulo esticado no botão: branco, centralizado pela altura real das letras (Midline: caixa-alta não fica
    /// deslocada), dentro da face do sprite com o respiro padrão, e encolhe um pouco se não couber.
    /// </summary>
    public static void AjustarRotulo(TMP_Text rotulo, float tamanho = 0f)
    {
        if (rotulo == null) return;
        rotulo.color = Color.white;
        ConfigurarTexto(rotulo, tamanho);
        RectTransform rt = rotulo.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        Vector2 respiro = new Vector2(RespiroHorizontal, RespiroVertical);
        rt.offsetMin = RecuoEsquerdaBaixo + respiro;
        rt.offsetMax = -(RecuoDireitaCima + respiro);
    }

    /// <summary>Só alinhamento e tamanho (para rótulos posicionados por layout group ou por âncoras próprias).</summary>
    public static void ConfigurarTexto(TMP_Text rotulo, float tamanho = 0f)
    {
        if (rotulo == null) return;
        // Com o auto-size ligado, fontSize é o tamanho calculado; o pedido fica em fontSizeMax.
        float base_ = tamanho > 0f ? tamanho : rotulo.enableAutoSizing ? rotulo.fontSizeMax : rotulo.fontSize;
        rotulo.alignment = TextAlignmentOptions.Midline;
        rotulo.fontSize = base_;
        rotulo.enableAutoSizing = true;
        rotulo.fontSizeMax = base_;
        rotulo.fontSizeMin = Mathf.Round(base_ * 0.7f);
    }
}
