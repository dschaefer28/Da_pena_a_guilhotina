using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desenha, em runtime, uma "tecla" (glifo) com o texto do controle no aparelho atual e um rótulo ao
/// lado: [ E ] Interagir no Windows, [ Interagir ] no Android (o rótulo some quando repetiria a tecla).
/// Substitui as artes dos controles pedidas no documento de tarefas sem depender de imagens; quando a
/// arte final existir, basta trocar o fundo/ícone aqui num lugar só.
/// Estrutura: Linha(HorizontalLayout) > Tecla(Image borda, HorizontalLayout) > Miolo(Image, HorizontalLayout) > Texto(TMP)
///                                    > Rotulo(TMP). Tudo dimensionado pelo texto via layout groups.
/// </summary>
public class GlifoDeControle : MonoBehaviour
{
    // Mesma cor/sprite do BotaoContinuar (UI.prefab) para o popup do tutorial não destoar do resto da UI.
    private static readonly Color CorTecla = new Color(0.45f, 0.2f, 0.15f, 1f);
    private static readonly Color CorBorda = new Color(0.29f, 0.12f, 0.09f, 1f);  // sombra: mesmo tom, mais escuro
    private static readonly Color CorRotulo = new Color(1f, 1f, 1f, 0.85f);

    public ControleTutorial controle;

    private TextMeshProUGUI textoTecla;
    private TextMeshProUGUI textoRotulo;
    private LayoutElement tamanhoTecla;

    /// <summary>Cria uma linha "[tecla] Rótulo" dentro de <paramref name="pai"/> (que deve ter um layout vertical).</summary>
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
        // Os grupos internos (com "force expand") reportariam altura/largura flexíveis e a linha esticaria
        // para preencher a coluna inteira; o LayoutElement explícito tem prioridade e trava em 0.
        leLinha.flexibleHeight = 0f;
        leLinha.flexibleWidth = 0f;

        // Tecla: borda dourada (padding 3) > miolo escuro (padding lateral) > texto. Largura vem do texto.
        var borda = new GameObject("Tecla", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        borda.layer = linha.layer;
        borda.transform.SetParent(linha.transform, false);
        var imgBorda = borda.GetComponent<Image>();
        imgBorda.sprite = SpriteArredondado();
        imgBorda.type = Image.Type.Sliced;
        imgBorda.color = CorBorda;
        imgBorda.raycastTarget = false;
        var hlBorda = borda.GetComponent<HorizontalLayoutGroup>();
        hlBorda.padding = new RectOffset(3, 3, 3, 3);
        hlBorda.childControlWidth = hlBorda.childControlHeight = true;
        hlBorda.childForceExpandWidth = hlBorda.childForceExpandHeight = true;
        var leBorda = borda.GetComponent<LayoutElement>();
        leBorda.preferredHeight = altura;
        leBorda.minHeight = altura;
        leBorda.minWidth = altura;   // tecla de uma letra fica quadrada
        leBorda.flexibleWidth = 0f;  // largura só do texto, sem tomar o resto da linha
        leBorda.flexibleHeight = 0f;

        var miolo = new GameObject("Miolo", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        miolo.layer = linha.layer;
        miolo.transform.SetParent(borda.transform, false);
        var imgMiolo = miolo.GetComponent<Image>();
        imgMiolo.sprite = SpriteArredondado();
        imgMiolo.type = Image.Type.Sliced;
        imgMiolo.color = CorTecla;
        imgMiolo.raycastTarget = false;
        var hlMiolo = miolo.GetComponent<HorizontalLayoutGroup>();
        hlMiolo.padding = new RectOffset(10, 10, 0, 0);
        hlMiolo.childAlignment = TextAnchor.MiddleCenter;
        hlMiolo.childControlWidth = hlMiolo.childControlHeight = true;
        hlMiolo.childForceExpandWidth = hlMiolo.childForceExpandHeight = true;

        var tecla = CriarTexto("Texto", miolo.transform, fonte, altura * 0.5f, TextAlignmentOptions.Center, Color.white);
        tecla.fontStyle = FontStyles.Bold;

        var rotulo = CriarTexto("Rotulo", linha.transform, fonte, altura * 0.5f, TextAlignmentOptions.MidlineLeft, CorRotulo);

        var glifo = linha.AddComponent<GlifoDeControle>();
        glifo.controle = controle;
        glifo.textoTecla = tecla;
        glifo.textoRotulo = rotulo;
        glifo.tamanhoTecla = leBorda;
        glifo.Atualizar();
        return glifo;
    }

    /// <summary>Reaplica o texto para o aparelho atual (chame se o modo toque/teclado puder mudar).</summary>
    public void Atualizar()
    {
        string tecla = DispositivoDeControle.Glifo(controle);
        string rotulo = DispositivoDeControle.NomeDaAcao(controle);
        if (textoTecla != null) textoTecla.text = tecla;
        if (textoRotulo != null)
        {
            textoRotulo.text = rotulo;
            // No toque a "tecla" já é o nome do botão ("Interagir"); repetir ao lado só ocupa espaço.
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
        // Mesmo sprite (Background) usado pelo BotaoContinuar no UI.prefab.
#if UNITY_EDITOR
        spriteArredondado = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
#endif
        if (spriteArredondado == null) spriteArredondado = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        return spriteArredondado;
    }
}
