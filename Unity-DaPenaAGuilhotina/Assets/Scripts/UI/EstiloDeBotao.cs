using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sprite único dos botões (9-slice). Fica em Resources/EstiloDeBotao para a UI montada por código e as ferramentas
/// de Editor usarem sem campo serializado.
/// </summary>
[CreateAssetMenu(fileName = "EstiloDeBotao", menuName = "Da Pena/Estilo de Botão")]
public class EstiloDeBotao : ScriptableObject
{
    public Sprite sprite;

    // Mesma escala da moldura da Biblioteca e do Quadro.
    public const float EscalaDaMoldura = 0.7f;

    // Face do sprite descentrada: moldura de 7 px nos lados, 9 px de sombra embaixo e 3 px de brilho em cima.
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
            // ColorTint não clareia além da cor do sprite: realce escurece.
            ColorBlock cores = ColorBlock.defaultColorBlock;
            cores.highlightedColor = cores.selectedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            botao.colors = cores;
        }
        return true;
    }

    // Respiro do texto além da face.
    public const float RespiroHorizontal = 10f;
    public const float RespiroVertical = 5f;

    /// <summary>Padding de um layout group cujo filho é o rótulo: face do sprite mais o respiro padrão.</summary>
    public static RectOffset RecuosDoRotulo() =>
        Recuos(Mathf.RoundToInt(RespiroHorizontal), Mathf.RoundToInt(RespiroVertical));

    /// <summary>Rótulo esticado na face do botão. Midline para caixa-alta não ficar deslocada.</summary>
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

    /// <summary>Só alinhamento e tamanho, para rótulos posicionados por layout group.</summary>
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
