using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso flutuante que aparece sobre o objeto interagível mais próximo do jogador ("E" no Windows,
/// "Interagir" no Android). Atende ao item do documento "ícone de feedback visual indicando
/// interatividade" (alçapão) e vale para NPCs, portas, mesa e prensa. Coloque no mesmo GameObject
/// do PlayerInteraction; o visual é montado em runtime (canvas em world space), sem arte externa.
/// </summary>
[RequireComponent(typeof(PlayerInteraction))]
public class PromptDeInteracao : MonoBehaviour
{
    [Tooltip("Fonte do texto. Vazio = fonte padrão do TextMesh Pro.")]
    public TMP_FontAsset fonte;
    [Tooltip("Distância acima do topo do colisor do objeto, em unidades do mundo.")]
    public float alturaAcimaDoAlvo = 0.35f;
    [Tooltip("Escala do aviso no mundo (1 unidade = 100 px do canvas).")]
    public float escala = 0.012f;

    // Mesma cor/sprite do BotaoContinuar (UI.prefab), para bater com o resto da UI (GlifoDeControle).
    private static readonly Color CorFundo = new Color(0.45f, 0.2f, 0.15f, 1f);
    private static readonly Color CorBorda = new Color(0.29f, 0.12f, 0.09f, 1f);

    private PlayerInteraction jogador;
    private Canvas canvas;
    private RectTransform raiz;
    private TextMeshProUGUI texto;
    private LayoutElement larguraTecla;
    private GameObject alvoAtual;
    private Renderer[] renderersDoAlvo;
    private Renderer[] renderersDoJogador;

    private void Awake()
    {
        jogador = GetComponent<PlayerInteraction>();
        renderersDoJogador = GetComponentsInChildren<Renderer>();
        Montar();
        canvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        jogador.OnInteragivelMaisProximoAlterado += HandleAlvoAlterado;
        HandleAlvoAlterado(jogador.InteragivelMaisProximo);
    }

    private void OnDisable()
    {
        jogador.OnInteragivelMaisProximoAlterado -= HandleAlvoAlterado;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void HandleAlvoAlterado(GameObject alvo)
    {
        alvoAtual = alvo;
        if (alvo == null) { canvas.gameObject.SetActive(false); return; }

        renderersDoAlvo = alvo.GetComponentsInChildren<Renderer>();

        string tecla = DispositivoDeControle.Glifo(ControleTutorial.Interagir);
        texto.text = tecla;
        larguraTecla.preferredWidth = tecla.Length <= 3 ? larguraTecla.preferredHeight : -1f;
        canvas.gameObject.SetActive(true);
        Posicionar();
    }

    private void LateUpdate()
    {
        if (alvoAtual == null) { if (canvas.gameObject.activeSelf) canvas.gameObject.SetActive(false); return; }

        // Some enquanto um painel/diálogo estiver aberto (nesses momentos o Interagir não faz nada).
        bool visivel = jogador.PodeInteragirAgora;
        if (canvas.gameObject.activeSelf != visivel) canvas.gameObject.SetActive(visivel);
        if (visivel) Posicionar();
    }

    private void Posicionar()
    {
        if (alvoAtual == null) return;

        // Prefere o topo real da arte (sprite) ao do colisor: o colisor de um NPC costuma cobrir só o
        // corpo (menor que o capuz/cabelo), e a etiqueta acabava nascendo em cima do personagem.
        Vector3 topo = TopoVisual(renderersDoAlvo, alvoAtual);

        // O jogador precisa estar bem perto do alvo para interagir, então a etiqueta também não pode
        // ficar abaixo da cabeça DELE (ex: jogador mais alto que a porta/mesa que está acessando).
        Vector3 topoJogador = TopoVisual(renderersDoJogador, jogador.gameObject);
        if (topoJogador.y > topo.y) topo.y = topoJogador.y;

        canvas.transform.position = topo + Vector3.up * alturaAcimaDoAlvo;
    }

    private static Vector3 TopoVisual(Renderer[] renderers, GameObject fallback)
    {
        if (renderers != null && renderers.Length > 0)
        {
            Bounds limites = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) limites.Encapsulate(renderers[i].bounds);
            return new Vector3(limites.center.x, limites.max.y, 0f);
        }
        var colisor = fallback.GetComponent<Collider2D>();
        return colisor != null ? new Vector3(colisor.bounds.center.x, colisor.bounds.max.y, 0f) : fallback.transform.position;
    }

    private void Montar()
    {
        var go = new GameObject("PromptInteracao", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(null, false); // fica solto no mundo para não herdar o flip/escala do jogador
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Cenário e personagens usam sorting layers acima de "Default" (cenario, personagem); o aviso
        // precisa da camada mais alta do projeto para não ficar atrás da parede.
        var camadas = SortingLayer.layers;
        if (camadas.Length > 0) canvas.sortingLayerID = camadas[camadas.Length - 1].id;
        canvas.sortingOrder = 100;
        raiz = (RectTransform)go.transform;
        raiz.sizeDelta = new Vector2(200f, 60f);
        raiz.localScale = Vector3.one * escala;

        var borda = new GameObject("Tecla", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        borda.transform.SetParent(raiz, false);
        var rBorda = (RectTransform)borda.transform;
        rBorda.anchorMin = rBorda.anchorMax = new Vector2(0.5f, 0.5f);
        rBorda.pivot = new Vector2(0.5f, 0f);
        rBorda.anchoredPosition = Vector2.zero;
        var imgBorda = borda.GetComponent<Image>();
        imgBorda.sprite = SpriteArredondado();
        imgBorda.type = Image.Type.Sliced;
        imgBorda.color = CorBorda;
        imgBorda.raycastTarget = false;
        larguraTecla = borda.GetComponent<LayoutElement>();
        larguraTecla.preferredHeight = 48f;
        larguraTecla.minWidth = 48f;
        larguraTecla.flexibleWidth = 0f;
        larguraTecla.flexibleHeight = 0f;
        var hl = borda.GetComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(3, 3, 3, 3);
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = true;
        var fitter = borda.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Miolo também é um layout group: assim a largura da tecla vem do texto (e não fica no mínimo).
        var miolo = new GameObject("Miolo", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        miolo.transform.SetParent(borda.transform, false);
        var imgMiolo = miolo.GetComponent<Image>();
        imgMiolo.sprite = SpriteArredondado();
        imgMiolo.type = Image.Type.Sliced;
        imgMiolo.color = CorFundo;
        imgMiolo.raycastTarget = false;
        var hlMiolo = miolo.GetComponent<HorizontalLayoutGroup>();
        hlMiolo.padding = new RectOffset(12, 12, 4, 4);
        hlMiolo.childAlignment = TextAnchor.MiddleCenter;
        hlMiolo.childControlWidth = hlMiolo.childControlHeight = true;
        hlMiolo.childForceExpandWidth = hlMiolo.childForceExpandHeight = true;

        var t = new GameObject("Texto", typeof(RectTransform));
        t.transform.SetParent(miolo.transform, false);
        texto = t.AddComponent<TextMeshProUGUI>();
        if (fonte != null) texto.font = fonte;
        texto.fontSize = 26f;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Overflow;
        var leTexto = t.AddComponent<LayoutElement>();
        leTexto.minWidth = 42f;
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private static Sprite SpriteArredondado()
    {
#if UNITY_EDITOR
        var s = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        if (s != null) return s;
#endif
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
    }
}
