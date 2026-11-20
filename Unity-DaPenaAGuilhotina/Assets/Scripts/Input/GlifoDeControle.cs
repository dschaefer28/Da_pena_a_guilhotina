using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "[tecla] Rótulo" montado em runtime para o aparelho atual ("[E] Interagir" / "[Interagir]").
/// Linha > Tecla > Miolo > Texto, e Linha > Rotulo; tudo dimensionado pelo texto via layout groups.
/// </summary>
public class GlifoDeControle : MonoBehaviour
{
    // Mesma cor/sprite do BotaoContinuar (UI.prefab).
    private static readonly Color CorTecla = new Color(0.45f, 0.2f, 0.15f, 1f);
    private static readonly Color CorBorda = new Color(0.29f, 0.12f, 0.09f, 1f);
    private static readonly Color CorRotulo = new Color(1f, 1f, 1f, 0.85f);

    public ControleTutorial controle;

    private TextMeshProUGUI textoTecla;
    private TextMeshProUGUI textoRotulo;
    private LayoutElement tamanhoTecla;

    /// <summary><paramref name="pai"/> deve ter um layout vertical.</summary>
    public static GlifoDeControle Criar(Transform pai, ControleTutorial controle, TMP_FontAsset fonte, float altura = 44f)
    {
        var linha = new GameObject("Glifo_" + controle, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        linha.layer = pai.gameObject.layer;
        linha.transform.SetParent(pai, false);
        var hl = linha.GetComponent<HorizontalLayoutGroup>();
        hl.spacing = 12f;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        var leLinha = linha.GetComponent<LayoutElement>();
        leLinha.preferredHeight = altura;
        leLinha.minHeight = altura;
        // Trava em 0: os grupos internos com force expand reportariam flexível e a linha esticaria na coluna.
        leLinha.flexibleHeight = 0f;
        leLinha.flexibleWidth = 0f;

        var borda = new GameObject("Tecla", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        borda.layer = linha.layer;
        borda.transform.SetParent(linha.transform, false);
        var imgBorda = borda.GetComponent<Image>();
        imgBorda.sprite = SpriteArredondado();
        imgBorda.type = Image.Type.Sliced;
        imgBorda.color = CorBorda;
        imgBorda.raycastTarget = false;
        // Com o sprite dos botões a tecla já tem moldura e face: o miolo vira só layout.
        bool comSprite = EstiloDeBotao.Aplicar(imgBorda);
        var hlBorda = borda.GetComponent<HorizontalLayoutGroup>();
        hlBorda.padding = comSprite ? EstiloDeBotao.Recuos() : new RectOffset(3, 3, 3, 3);
        hlBorda.childControlWidth = hlBorda.childControlHeight = true;
        hlBorda.childForceExpandWidth = hlBorda.childForceExpandHeight = true;
        var leBorda = borda.GetComponent<LayoutElement>();
        leBorda.preferredHeight = altura;
        leBorda.minHeight = altura;
        leBorda.minWidth = altura;   // tecla de uma letra fica quadrada
        leBorda.flexibleWidth = 0f;
        leBorda.flexibleHeight = 0f;

        var miolo = new GameObject("Miolo", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        miolo.layer = linha.layer;
        miolo.transform.SetParent(borda.transform, false);
        var imgMiolo = miolo.GetComponent<Image>();
        imgMiolo.sprite = SpriteArredondado();
        imgMiolo.type = Image.Type.Sliced;
        imgMiolo.color = CorTecla;
        imgMiolo.raycastTarget = false;
        imgMiolo.enabled = !comSprite;
        var hlMiolo = miolo.GetComponent<HorizontalLayoutGroup>();
        hlMiolo.padding = comSprite ? new RectOffset(0, 0, 0, 0) : new RectOffset(10, 10, 0, 0);
        hlMiolo.childAlignment = TextAnchor.MiddleCenter;
        hlMiolo.childControlWidth = hlMiolo.childControlHeight = true;
        hlMiolo.childForceExpandWidth = hlMiolo.childForceExpandHeight = true;

        // Rótulo com mínimo de 22 (EB Garamond), mesmo nas teclas menores.
        var tecla = CriarTexto("Texto", miolo.transform, fonte, altura * 0.55f, TextAlignmentOptions.Center, Color.white);
        tecla.fontStyle = FontStyles.Bold;

        var rotulo = CriarTexto("Rotulo", linha.transform, fonte, Mathf.Max(22f, altura * 0.55f), TextAlignmentOptions.MidlineLeft, CorRotulo);

        var glifo = linha.AddComponent<GlifoDeControle>();
        glifo.controle = controle;
        glifo.textoTecla = tecla;
        glifo.textoRotulo = rotulo;
        glifo.tamanhoTecla = leBorda;
        glifo.Atualizar();
        return glifo;
    }

    /// <summary>Chame se o modo toque/teclado puder mudar.</summary>
    public void Atualizar()
    {
        string tecla = DispositivoDeControle.Glifo(controle);
        string rotulo = DispositivoDeControle.NomeDaAcao(controle);
        if (textoTecla != null) textoTecla.text = tecla;
        if (textoRotulo != null)
        {
            textoRotulo.text = rotulo;
            // No toque a "tecla" já é o nome do botão.
            textoRotulo.gameObject.SetActive(!string.Equals(tecla, rotulo, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    private static TextMeshProUGUI CriarTexto(string nome, Transform pai, TMP_FontAsset fonte, float tamanho, TextAlignmentOptions alinhamento, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.layer = pai.gameObject.layer;
        go.transform.SetParent(pai, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (fonte != null) tmp.font = fonte;
        tmp.fontSize = tamanho;
        tmp.alignment = alinhamento;
        tmp.color = cor;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static Sprite spriteArredondado;
    private static Sprite SpriteArredondado()
    {
        if (spriteArredondado != null) return spriteArredondado;
#if UNITY_EDITOR
        spriteArredondado = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        if (spriteArredondado == null) spriteArredondado = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        return spriteArredondado;
    }
}
