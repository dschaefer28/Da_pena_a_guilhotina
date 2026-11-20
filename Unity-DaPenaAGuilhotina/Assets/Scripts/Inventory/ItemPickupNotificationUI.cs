using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup de "item recebido" e dos avisos do AvisoNaTela, numa fila única. Ícone e nome vêm do próprio Item.
/// </summary>
public class ItemPickupNotificationUI : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("GameObject raiz do popup (o que fica inativo/ativo). Deixe desmarcado no Editor por padrão.")]
    public GameObject painelPopup;
    [Tooltip("Image usada para mostrar o ícone do item recebido (Item.itemImg).")]
    public Image imagemItem;
    [Tooltip("Texto TMP usado para mostrar a mensagem de coleta.")]
    public TextMeshProUGUI textoMensagem;

    [Header("Configurações")]
    [Tooltip("Use {0} no lugar onde o nome do item deve aparecer. Aceita rich text do TMP.")]
    [TextArea(2, 3)]
    public string formatoMensagem = "<size=75%><color=#FAD98C>Você recebeu</color></size>\n{0}";
    [Tooltip("Por quantos segundos o popup fica visível antes de sumir sozinho.")]
    [Min(0.1f)] public float duracaoExibicao = 2.5f;
    [Tooltip("Largura máxima do texto: mensagens curtas deixam o popup estreito, as longas quebram linha aqui.")]
    [Min(100f)] public float larguraMaximaTexto = 520f;
    [Tooltip("Duração do fade de entrada/saída (0 = aparece e some seco).")]
    [Min(0f)] public float duracaoFade = 0.2f;

    [Tooltip("Espaço entre o relógio de investigação (HudDoRelogio) e o popup, quando os dois estão no topo.")]
    [Min(0f)] public float margemAbaixoDoRelogio = 12f;

    private InventoryManager inventoryManagerAtual;
    private float? alturaBasePopup;
    // Itens e avisos dividem a fila para nunca se sobreporem. duracao 0 = duracaoExibicao.
    private readonly Queue<(string texto, Sprite icone, float duracao)> filaDeItens = new Queue<(string, Sprite, float)>();
    private Coroutine exibicaoEmAndamento;

    void Start()
    {
        if (painelPopup != null) painelPopup.SetActive(false);
        VincularInventario();
    }

    void OnEnable()
    {
        VincularInventario();
        AvisoNaTela.OnAviso += HandleAviso;
    }

    void OnDisable()
    {
        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled -= HandleInventarioAlternado;
        }
        // Senão, ao reativar com o mesmo inventário, VincularInventario acharia que já está inscrito.
        inventoryManagerAtual = null;
        AvisoNaTela.OnAviso -= HandleAviso;
        // Desativar mata a corrotina; sem zerar, a fila nunca mais andaria.
        exibicaoEmAndamento = null;
        filaDeItens.Clear();
        if (painelPopup != null) painelPopup.SetActive(false);
    }

    // O InventoryManager é recriado a cada cena: reinscreve quando a referência muda.
    private void VincularInventario()
    {
        InventoryManager atual = GameManager.Instance != null
            ? GameManager.Instance.inventoryManager
            : FindAnyObjectByType<InventoryManager>();

        if (atual == inventoryManagerAtual) return;

        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado -= HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled -= HandleInventarioAlternado;
        }

        inventoryManagerAtual = atual;

        if (inventoryManagerAtual != null)
        {
            inventoryManagerAtual.OnItemAdicionado += HandleItemAdicionado;
            inventoryManagerAtual.OnInventoryToggled += HandleInventarioAlternado;
        }
        else
            Debug.LogWarning("[ItemPickupNotificationUI] Nenhum InventoryManager encontrado na cena.");
    }

    private void HandleItemAdicionado(Item item)
    {
        if (item == null) return;
        Enfileirar(string.Format(formatoMensagem, item.NomeExibicao), item.itemImg);
    }

    private void HandleAviso(string texto, Sprite icone) => Enfileirar(texto, icone, AvisoNaTela.DuracaoPedida);

    private void Enfileirar(string texto, Sprite icone, float duracao = 0f)
    {
        // Objeto inativo não roda corrotina (ex.: UI da cena antiga na troca de cena).
        if (!isActiveAndEnabled) return;

        filaDeItens.Enqueue((texto, icone, duracao));
        if (exibicaoEmAndamento == null)
            exibicaoEmAndamento = StartCoroutine(ExibirFila());
    }

    private IEnumerator ExibirFila()
    {
        while (filaDeItens.Count > 0)
        {
            var (texto, icone, duracao) = filaDeItens.Dequeue();
            MostrarPopup(texto, icone);
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(duracao > 0f ? duracao : duracaoExibicao);
            yield return Fade(1f, 0f);
            if (painelPopup != null) painelPopup.SetActive(false);
        }
        exibicaoEmAndamento = null;
    }

    private IEnumerator Fade(float de, float para)
    {
        CanvasGroup grupo = painelPopup != null ? painelPopup.GetComponent<CanvasGroup>() : null;
        if (grupo == null) yield break;
        for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(de, para, t / duracaoFade);
            yield return null;
        }
        grupo.alpha = para;
    }

    private void MostrarPopup(string texto, Sprite icone)
    {
        if (imagemItem != null)
        {
            imagemItem.sprite = icone;
            imagemItem.gameObject.SetActive(icone != null);
        }

        if (textoMensagem != null)
        {
            textoMensagem.text = texto;
            // Largura da frase até larguraMaximaTexto; sem limite o ContentSizeFitter atravessaria a tela.
            larguraDoTexto = Mathf.Min(textoMensagem.GetPreferredValues(texto).x + 2f, larguraMaximaTexto);
        }

        if (painelPopup != null)
        {
            painelPopup.SetActive(true);
            Posicionar();
        }
    }

    private float larguraDoTexto;
    private float xBasePopup;
    private Transform hudDeStatus;
    private const float MargemAoLadoDoInventario = 16f; // unidades do canvas
    private const float LarguraMinimaDoTexto = 200f;

    // Recalcula já, para o primeiro frame não aparecer com o tamanho da mensagem anterior.
    private void Posicionar()
    {
        var rect = painelPopup != null ? painelPopup.transform as RectTransform : null;
        if (rect == null) return;
        LayoutElement le = textoMensagem != null ? textoMensagem.GetComponent<LayoutElement>() : null;
        if (le != null) le.preferredWidth = larguraDoTexto;

        // Desce para baixo do relógio de investigação, que ocupa o mesmo topo.
        if (!alturaBasePopup.HasValue) { alturaBasePopup = rect.anchoredPosition.y; xBasePopup = rect.anchoredPosition.x; }
        float borda = HudDoRelogio.BordaInferior;
        float y = borda > 0f ? Mathf.Min(alturaBasePopup.Value, -(borda + margemAbaixoDoRelogio)) : alturaBasePopup.Value;
        rect.anchoredPosition = new Vector2(xBasePopup, y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        AfastarDoInventario(rect, le);
    }

    private void HandleInventarioAlternado(bool aberto)
    {
        if (painelPopup != null && painelPopup.activeSelf) Posicionar();
    }

    // Com o inventário aberto, evita cobrir seu conteúdo e a HUD de status (fundos e molduras podem ficar por baixo).
    // Tenta a faixa à esquerda da lista e depois à direita; fica a primeira livre ou, senão, a que cobre menos
    // (incluindo a posição normal). Faixa estreita quebra a mensagem em mais linhas.
    private void AfastarDoInventario(RectTransform rect, LayoutElement le)
    {
        InventoryManager inventario = inventoryManagerAtual;
        if (inventario == null || inventario.inventoryUI == null || !inventario.inventoryUI.activeInHierarchy) return;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        List<Rect> conteudo = ConteudoVisivel(inventario.inventoryUI.transform);
        if (conteudo.Count == 0) return;
        Rect lista = inventario.inventoryUI.transform.Find("InventoryBackGround") is RectTransform fundo && fundo.gameObject.activeInHierarchy
            ? NaTela(fundo) : Uniao(conteudo);
        Rect? hud = HudDeStatusNaTela(canvas, out Rect hudNaTela) ? hudNaTela : (Rect?)null;
        if (hud.HasValue) conteudo.Add(hud.Value);

        float melhorArea = AreaCoberta(NaTela(rect), conteudo);
        if (melhorArea <= 0f) return;
        Vector3 melhorPosicao = rect.position;
        float melhorLargura = larguraDoTexto;

        float escala = canvas != null && canvas.rootCanvas.scaleFactor > 0f ? canvas.rootCanvas.scaleFactor : 1f;
        float margem = MargemAoLadoDoInventario * escala;
        Rect seguro = Screen.safeArea;
        var faixas = new[] { new Vector2(seguro.xMin + margem, lista.xMin - margem), new Vector2(lista.xMax + margem, seguro.xMax - margem) };
        foreach (Vector2 faixa in faixas)
        {
            if (!PosicionarNaFaixa(rect, le, escala, faixa.x, faixa.y, seguro.yMax - margem, hud, margem)) continue;
            Rect naTela = NaTela(rect);
            if (naTela.yMin < seguro.yMin) continue;
            float area = AreaCoberta(naTela, conteudo);
            if (area >= melhorArea) continue;
            melhorArea = area;
            melhorPosicao = rect.position;
            melhorLargura = le != null ? le.preferredWidth : larguraDoTexto;
            if (area <= 0f) break;
        }

        if (le != null) le.preferredWidth = melhorLargura;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        rect.position = melhorPosicao;
    }

    // Topo da faixa [xMin, xMax], abaixo da HUD se ela estiver por cima. Falso se não couber na largura.
    private bool PosicionarNaFaixa(RectTransform rect, LayoutElement le, float escala, float xMin, float xMax, float topo,
                                   Rect? hud, float margem)
    {
        float faixa = xMax - xMin;
        if (faixa <= 0f) return false;
        if (le != null) le.preferredWidth = larguraDoTexto;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        float largura = NaTela(rect).width;
        if (largura > faixa)
        {
            if (le == null) return false;
            le.preferredWidth = Mathf.Max(LarguraMinimaDoTexto, larguraDoTexto - (largura - faixa) / escala);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            largura = NaTela(rect).width;
            if (largura > faixa + 1f) return false;
        }

        float centro = (xMin + xMax) * 0.5f;
        if (hud.HasValue && hud.Value.xMin < centro + largura * 0.5f && hud.Value.xMax > centro - largura * 0.5f)
            topo = Mathf.Min(topo, hud.Value.yMin - margem);
        rect.position = new Vector3(centro, topo, rect.position.z); // pivô no topo central (canvas Screen Space Overlay)
        return true;
    }

    // Gráficos-folha visíveis, em pixels de tela. Fundos/molduras (com gráficos dentro) e imagens transparentes não contam.
    private static List<Rect> ConteudoVisivel(Transform raiz)
    {
        var visiveis = new List<Graphic>();
        foreach (Graphic g in raiz.GetComponentsInChildren<Graphic>(false))
            if (g.enabled && g.color.a > 0.01f && g.canvasRenderer.GetInheritedAlpha() > 0.01f) visiveis.Add(g);

        var rects = new List<Rect>();
        foreach (Graphic g in visiveis)
        {
            bool temConteudoDentro = false;
            foreach (Graphic outro in visiveis)
                if (outro != g && outro.transform.IsChildOf(g.transform)) { temConteudoDentro = true; break; }
            if (!temConteudoDentro) rects.Add(NaTela(g.rectTransform));
        }
        return rects;
    }

    private static float AreaCoberta(Rect r, List<Rect> outros)
    {
        float area = 0f;
        foreach (Rect o in outros)
        {
            float largura = Mathf.Min(r.xMax, o.xMax) - Mathf.Max(r.xMin, o.xMin);
            float altura = Mathf.Min(r.yMax, o.yMax) - Mathf.Max(r.yMin, o.yMin);
            if (largura > 0f && altura > 0f) area += largura * altura;
        }
        return area;
    }

    private static Rect Uniao(List<Rect> rects)
    {
        Rect u = rects[0];
        foreach (Rect r in rects) u = Rect.MinMaxRect(Mathf.Min(u.xMin, r.xMin), Mathf.Min(u.yMin, r.yMin), Mathf.Max(u.xMax, r.xMax), Mathf.Max(u.yMax, r.yMax));
        return u;
    }

    private bool HudDeStatusNaTela(Canvas canvas, out Rect tela)
    {
        tela = default;
        if (hudDeStatus == null && canvas != null)
            foreach (Transform t in canvas.rootCanvas.GetComponentsInChildren<Transform>(true))
                if (t.name == "HUD_Status") { hudDeStatus = t; break; }
        if (hudDeStatus == null || !hudDeStatus.gameObject.activeInHierarchy || !(hudDeStatus is RectTransform rt)) return false;
        tela = NaTela(rt);
        return true;
    }

    private static Rect NaTela(RectTransform rt)
    {
        var cantos = new Vector3[4];
        rt.GetWorldCorners(cantos); // canvas Screen Space Overlay: já em pixels
        return Rect.MinMaxRect(cantos[0].x, cantos[0].y, cantos[2].x, cantos[2].y);
    }
}
