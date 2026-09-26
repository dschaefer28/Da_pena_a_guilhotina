using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ficha que aparece ao lado de um slot do inventário/prensa quando o ponteiro passa sobre ele (no celular,
/// enquanto o dedo está pressionado). Mostra nome, fonte, descrição e a situação da pista (Fato x Boato),
/// que é o que o jogador lê para decidir o que imprimir.
/// Não precisa de setup: é montada por código no primeiro uso, no Canvas do próprio slot (mesmo padrão do
/// CutsceneLegendas). Chamada pelo UISlotHandler.
/// </summary>
public class FichaDaPista : MonoBehaviour
{
    private const float Largura = 460f;
    private const float Margem = 12f;

    private static FichaDaPista instancia;

    private RectTransform rect;
    private RectTransform areaDoCanvas;
    private TextMeshProUGUI texto;
    private RectTransform ancoraAtual;
    private Item itemAtual;

    public static void Mostrar(Item item, RectTransform ancora)
    {
        if (item == null || ancora == null) return;
        if (!item.EhPista && string.IsNullOrWhiteSpace(item.descricao)) { Esconder(null); return; }

        Canvas raiz = ancora.GetComponentInParent<Canvas>();
        if (raiz == null) return;
        raiz = raiz.rootCanvas;

        if (instancia == null || instancia.areaDoCanvas != (RectTransform)raiz.transform)
            instancia = Montar(raiz, ancora.GetComponentInChildren<TextMeshProUGUI>(true));

        instancia.Exibir(item, ancora);
    }

    /// <summary>Esconde a ficha. Com uma âncora, só esconde se a ficha estiver mostrando aquele slot.</summary>
    public static void Esconder(RectTransform ancora)
    {
        if (instancia == null) return;
        if (ancora != null && instancia.ancoraAtual != ancora) return;
        instancia.ancoraAtual = null;
        instancia.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Inventário fechado (ou slot escondido) com o ponteiro ainda "em cima": não sobra ficha solta na tela.
        if (ancoraAtual == null || !ancoraAtual.gameObject.activeInHierarchy) { Esconder(null); return; }

        // A ficha vale enquanto o item ainda estiver no slot OU na mão do jogador. No celular não existe
        // "passar o mouse": o toque pega o item, e é segurando a pista que o jogador lê a ficha.
        UISlotHandler slot = ancoraAtual.GetComponent<UISlotHandler>();
        Item naMao = MouseManager.instance != null ? MouseManager.instance.heldItem : null;
        bool noSlot = slot != null && slot.item == itemAtual;
        if (!noSlot && naMao != itemAtual) Esconder(null);
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Exibir(Item item, RectTransform ancora)
    {
        ancoraAtual = ancora;
        itemAtual = item;
        texto.text = MontarTexto(item);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        Posicionar(ancora);
    }

    private static string MontarTexto(Item item)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<b>").Append(item.NomeExibicao).Append("</b>");
        if (!string.IsNullOrWhiteSpace(item.fonte))
            sb.Append("\n<size=85%><color=#C8B89A>Fonte: ").Append(item.fonte).Append("</color></size>");
        if (!string.IsNullOrWhiteSpace(item.descricao))
            sb.Append("\n\n").Append(item.descricao);
        if (item.EhSuporte)
        {
            sb.Append("\n\n<size=85%>Documento de apoio (").Append(item.RotuloDeApoio)
              .Append("). Vai no campo Suporte da prensa e não é gasto na impressão.</size>");
        }
        if (item.EhPista)
        {
            var inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
            bool verificada = inventario != null && inventario.PistaVerificada(item);
            sb.Append("\n\n<size=85%>Situação: ").Append(item.RotuloSituacao(verificada)).Append("</size>");
        }
        return sb.ToString();
    }

    // Ao lado direito do slot; se não couber, do lado esquerdo. Verticalmente, centrada e presa dentro da tela.
    private void Posicionar(RectTransform ancora)
    {
        Canvas canvas = areaDoCanvas.GetComponent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        var cantos = new Vector3[4];
        ancora.GetWorldCorners(cantos); // 0 = baixo-esq, 2 = cima-dir
        Vector2 min = ParaLocal(cantos[0], cam);
        Vector2 max = ParaLocal(cantos[2], cam);

        Rect area = areaDoCanvas.rect;
        Vector2 tamanho = rect.rect.size;

        float x = max.x + Margem;
        if (x + tamanho.x > area.xMax) x = min.x - Margem - tamanho.x;
        x = Mathf.Clamp(x, area.xMin, Mathf.Max(area.xMin, area.xMax - tamanho.x));

        float y = (min.y + max.y) * 0.5f - tamanho.y * 0.5f;
        y = Mathf.Clamp(y, area.yMin, Mathf.Max(area.yMin, area.yMax - tamanho.y));

        rect.localPosition = new Vector3(x, y, 0f); // pivô no canto inferior esquerdo
    }

    private Vector2 ParaLocal(Vector3 mundo, Camera cam)
    {
        Vector2 tela = RectTransformUtility.WorldToScreenPoint(cam, mundo);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(areaDoCanvas, tela, cam, out Vector2 local);
        return local;
    }

    private static FichaDaPista Montar(Canvas raiz, TextMeshProUGUI modeloDeFonte)
    {
        var go = new GameObject("FichaDaPista", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup),
                                typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.layer = raiz.gameObject.layer;
        go.transform.SetParent(raiz.transform, false);

        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = Vector2.zero;
        r.sizeDelta = new Vector2(Largura, 0f);

        // Canvas próprio: fica acima do inventário e da prensa, abaixo das cutscenes (50) e do fade de cena.
        var canvas = go.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 40;

        // Não pode bloquear o ponteiro: senão a ficha "rouba" o hover do slot e fica piscando.
        var grupo = go.GetComponent<CanvasGroup>();
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        var fundo = go.GetComponent<Image>();
        fundo.color = new Color(0.12f, 0.1f, 0.086f, 0.96f);
        fundo.raycastTarget = false;

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var t = new GameObject("Texto", typeof(RectTransform));
        t.transform.SetParent(go.transform, false);
        var tmp = t.AddComponent<TextMeshProUGUI>();
        if (modeloDeFonte != null && modeloDeFonte.font != null) tmp.font = modeloDeFonte.font;
        tmp.fontSize = 30f;
        tmp.color = new Color(0.93f, 0.9f, 0.84f);
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        var ficha = go.AddComponent<FichaDaPista>();
        ficha.rect = r;
        ficha.areaDoCanvas = (RectTransform)raiz.transform;
        ficha.texto = tmp;
        go.SetActive(false);
        return ficha;
    }
}
