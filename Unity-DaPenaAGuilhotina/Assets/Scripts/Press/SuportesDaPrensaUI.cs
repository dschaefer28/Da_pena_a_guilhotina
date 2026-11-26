using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Campo "Suporte" da prensa: marca documentos de apoio do caso (não consumidos) e mostra uma prévia que nunca revela
/// a classificação oculta das pistas. Vai no PainelPrensa (UI.prefab) e se monta por código a partir de faseMinima.
/// </summary>
[RequireComponent(typeof(CraftingPress))]
public class SuportesDaPrensaUI : MonoBehaviour
{
    [Range(1, GameManager.UltimaFase)] public int faseMinima = 3;
    public TMP_FontAsset fonte;

    private static readonly Color CorTexto = new Color(0.93f, 0.9f, 0.84f);

    private CraftingPress prensa;
    private InventoryManager inventario;
    private RectTransform painel;
    private RectTransform lista;
    private TextMeshProUGUI previa;

    private void Awake() => prensa = GetComponent<CraftingPress>();

    private void OnEnable()
    {
        if (painel == null) Montar();
        inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : null;
        if (inventario != null) inventario.OnSlotAlterado += AoMudarSlot;
        prensa.OnSuportesAlterados += Atualizar;
        Atualizar();
    }

    private void OnDisable()
    {
        if (inventario != null) inventario.OnSlotAlterado -= AoMudarSlot;
        if (prensa != null) prensa.OnSuportesAlterados -= Atualizar;
    }

    private void AoMudarSlot(UISlotHandler _) => Atualizar();

    private void Update()
    {
        if (painel != null) AjustarLargura();
    }

    private void Atualizar()
    {
        GameManager gm = GameManager.Instance;
        bool ativo = gm != null && gm.faseAtual >= faseMinima && gm.CasoAtualEmAndamento;
        painel.gameObject.SetActive(ativo);
        if (!ativo) return;

        CaseData caso = gm.casoEscolhido;
        ReceitaDeCaso receita = caso.receitaDoPanfleto;

        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            GameObject velho = lista.GetChild(i).gameObject;
            velho.SetActive(false);
            Destroy(velho);
        }

        var documentos = new List<Item>();
        if (inventario != null)
            foreach (Item item in inventario.ItensDaGrade())
                if (item.EhSuporte && item.caso == caso && !documentos.Exists(d => d.itemID == item.itemID)) documentos.Add(item);

        if (documentos.Count == 0)
            Texto("Nenhum", lista, 26f, "<color=#9E9E9E>Nenhum documento de apoio deste caso no inventário.</color>");
        foreach (Item doc in documentos)
        {
            Item d = doc;
            bool marcado = prensa.SuporteSelecionado(d);
            bool aceito = receita != null && receita.AceitaComoSuporte(d);
            Button b = Botao(lista, $"{(marcado ? "[x]" : "[  ]")} {d.NomeExibicao}\n<size=85%>{(aceito ? "aceito como apoio neste caso" : "<color=#9E9E9E>sem efeito neste caso</color>")}</size>");
            b.onClick.AddListener(() => prensa.AlternarSuporte(d));
        }

        previa.text = MontarPrevia(gm, caso, receita);
    }

    // Nada aqui pode depender da confiabilidade real das pistas: a prévia não pode virar detector de boatos.
    private string MontarPrevia(GameManager gm, CaseData caso, ReceitaDeCaso receita)
    {
        var sb = new StringBuilder("<b>Prévia</b>\n");
        Item a = prensa.slotInput1 != null ? prensa.slotInput1.item : null;
        Item b = prensa.slotInput2 != null ? prensa.slotInput2.item : null;
        sb.Append(a != null && b != null ? $"Entradas: {a.NomeExibicao} + {b.NomeExibicao}\n" : "Coloque duas pistas nas entradas.\n");
        sb.Append($"<size=90%>Tendência estimada do caso: {Seta(caso.publicOpinionReward)} Povo  {Seta(caso.stateOpinionReward)} Estado</size>\n");

        List<Item> selecionados = prensa.SuportesSelecionadosNoInventario();
        int reforcos = CalculadoraDePanfleto.ReforcosPossiveis(receita, selecionados);
        if (selecionados.Count == 0) sb.Append("<size=90%>Sem apoio selecionado.</size>\n");
        else if (reforcos == 0) sb.Append("<size=90%>Os documentos marcados não reforçam este caso.</size>\n");
        else
            sb.Append($"<size=90%>Apoio: {reforcos} reforço(s) possível(is)" +
                      (selecionados.Count > reforcos ? " (documentos equivalentes não somam)" : string.Empty) + ".</size>\n");
        sb.Append("<size=85%><color=#B0A48C>O resultado real depende da verdade das pistas, que a prensa não revela.</color></size>");
        return sb.ToString();
    }

    private static string Seta(int estimativa) =>
        estimativa > 0 ? "<color=#4CAF50>▲</color>" : estimativa < 0 ? "<color=#E53935>▼</color>" : "<color=#9E9E9E>●</color>";

    // ===== Montagem =====

    private void Montar()
    {
        if (fonte == null)
        {
            var modelo = GetComponentInChildren<TextMeshProUGUI>(true);
            if (modelo != null) fonte = modelo.font;
        }

        var go = new GameObject("PainelSuporte", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(transform, false);
        painel = (RectTransform)go.transform;
        painel.anchorMin = painel.anchorMax = painel.pivot = new Vector2(0.5f, 0.5f);
        go.GetComponent<Image>().color = new Color(0.11f, 0.08f, 0.07f, 0.96f);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(20, 20, 20, 20); v.spacing = 12f;
        v.childControlWidth = v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // Mínimo de ~22 na referência de 1080 (EB Garamond).
        Texto("Titulo", painel, 34f, "<b>Suporte (opcional)</b>");
        Texto("Dica", painel, 25f, "<color=#B0A48C>Documentos de apoio do caso. Não são gastos na impressão.</color>");

        var area = new GameObject("Lista", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
        area.transform.SetParent(painel, false);
        area.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        var le = area.GetComponent<LayoutElement>(); le.flexibleHeight = 1f; le.minHeight = 160f;
        var conteudo = new GameObject("Conteudo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        conteudo.transform.SetParent(area.transform, false);
        lista = (RectTransform)conteudo.transform;
        lista.anchorMin = new Vector2(0f, 1f); lista.anchorMax = new Vector2(1f, 1f); lista.pivot = new Vector2(0.5f, 1f);
        lista.offsetMin = lista.offsetMax = Vector2.zero;
        var vl = conteudo.GetComponent<VerticalLayoutGroup>();
        vl.padding = new RectOffset(8, 8, 8, 8); vl.spacing = 8f;
        vl.childControlWidth = vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        conteudo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var rolagem = area.GetComponent<ScrollRect>();
        rolagem.content = lista; rolagem.horizontal = false; rolagem.movementType = ScrollRect.MovementType.Clamped;

        previa = Texto("Previa", painel, 26f, string.Empty);
        previa.GetComponent<LayoutElement>().minHeight = 190f;
        AjustarLargura();
    }

    // Entre a borda esquerda e o inventário (encolhe em telas estreitas); base alinhada à prensa, topo abaixo da HUD_Status.
    private void AjustarLargura()
    {
        var pai = (RectTransform)transform;           // PainelPrensa (à direita do inventário)
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var raiz = (RectTransform)canvas.rootCanvas.transform;
        float meiaTela = raiz.rect.width * 0.5f;
        const float bordaInventario = 380f, margem = 24f, topoMaximo = 260f;
        float largura = Mathf.Clamp(meiaTela - bordaInventario - 2f * margem, 300f, 560f);
        float centroX = -(bordaInventario + margem + largura * 0.5f);   // em coordenadas do canvas

        float baseY = pai.anchoredPosition.y - pai.rect.height * 0.5f;  // base da prensa, no canvas
        var hud = raiz.Find("HUD_Status") as RectTransform;
        float topoY = topoMaximo;
        if (hud != null)
        {
            var cantos = new Vector3[4];
            hud.GetWorldCorners(cantos);
            topoY = Mathf.Min(topoMaximo, raiz.InverseTransformPoint(cantos[0]).y - margem);
        }
        float altura = Mathf.Max(420f, topoY - baseY);
        painel.sizeDelta = new Vector2(largura, altura);
        painel.anchoredPosition = new Vector2(centroX - pai.anchoredPosition.x, baseY + altura * 0.5f - pai.anchoredPosition.y);
    }

    private TextMeshProUGUI Texto(string nome, Transform pai, float tamanho, string conteudo)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (fonte != null) tmp.font = fonte;
        tmp.text = conteudo; tmp.fontSize = tamanho; tmp.color = CorTexto; tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal; tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        return tmp;
    }

    private Button Botao(Transform pai, string rotulo)
    {
        var go = new GameObject("Documento", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(pai, false);
        bool comSprite = EstiloDeBotao.Aplicar(go.GetComponent<Image>(), go.GetComponent<Button>());
        if (!comSprite) go.GetComponent<Image>().color = new Color(0.25f, 0.18f, 0.13f);
        go.GetComponent<LayoutElement>().minHeight = 84f;
        var texto = Texto("Rotulo", go.transform, 26f, rotulo);
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        var rt = (RectTransform)texto.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(14f, 4f); rt.offsetMax = new Vector2(-14f, -4f);
        if (comSprite) EstiloDeBotao.AjustarRotulo(texto);
        var botao = go.GetComponent<Button>();
        SomDeCliqueDoBotao.Garantir(botao);
        return botao;
    }
}
