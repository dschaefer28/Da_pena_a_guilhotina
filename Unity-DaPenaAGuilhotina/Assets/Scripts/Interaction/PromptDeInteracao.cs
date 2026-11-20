using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso flutuante (tecla + nome opcional) sobre o interagível mais próximo do jogador. Canvas world space montado
/// em runtime; vai no mesmo GameObject do PlayerInteraction.
/// </summary>
[RequireComponent(typeof(PlayerInteraction))]
// Depois das câmeras (CameraFollow, CinemachineBrain) no LateUpdate: o desvio do relógio projeta com a câmera do frame atual.
[DefaultExecutionOrder(1000)]
public class PromptDeInteracao : MonoBehaviour
{
    [Tooltip("Fonte do texto. Vazio = fonte padrão do TextMesh Pro.")]
    public TMP_FontAsset fonte;
    [Tooltip("Distância acima do topo do colisor do objeto, em unidades do mundo.")]
    public float alturaAcimaDoAlvo = 0.35f;
    [Tooltip("Escala do aviso no mundo (1 unidade = 100 px do canvas).")]
    public float escala = 0.012f;

    // Mesma cor/sprite do BotaoContinuar (UI.prefab).
    private static readonly Color CorFundo = new Color(0.45f, 0.2f, 0.15f, 1f);
    private static readonly Color CorBorda = new Color(0.29f, 0.12f, 0.09f, 1f);
    private static readonly Color CorFundoDoNome = new Color(0.12f, 0.07f, 0.05f, 0.85f);
    private static readonly Color CorTextoDoNome = new Color(1f, 0.95f, 0.85f, 1f);
    private const float EspacoEntreNomeETecla = 4f; // px do canvas

    private PlayerInteraction jogador;
    private Canvas canvas;
    private RectTransform raiz;
    private TextMeshProUGUI texto;
    private LayoutElement larguraTecla;
    private RectTransform tecla;
    private RectTransform etiquetaDoNome;
    private TextMeshProUGUI textoDoNome;
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

        // Sem largura fixa: forçar um quadrado de 48 fazia o miolo (mín. 66) vazar da borda.
        texto.text = DispositivoDeControle.Glifo(ControleTutorial.Interagir);

        string nome = alvo.TryGetComponent<IInteractable>(out var interagivel) ? interagivel.NomeExibido : null;
        bool comNome = !string.IsNullOrWhiteSpace(nome);
        textoDoNome.text = comNome ? nome : string.Empty;
        etiquetaDoNome.gameObject.SetActive(comNome);

        canvas.gameObject.SetActive(true);
        // Antes do primeiro layout a tecla tem 100x100; o posicionamento precisa do tamanho real.
        LayoutRebuilder.ForceRebuildLayoutImmediate(tecla);
        if (comNome) LayoutRebuilder.ForceRebuildLayoutImmediate(etiquetaDoNome);
        // Nome embaixo, tecla empilhada acima dele.
        tecla.anchoredPosition = new Vector2(0f, comNome ? etiquetaDoNome.rect.height + EspacoEntreNomeETecla : 0f);
        Posicionar();
    }

    private void LateUpdate()
    {
        if (alvoAtual == null) { if (canvas.gameObject.activeSelf) canvas.gameObject.SetActive(false); return; }

        bool visivel = jogador.PodeInteragirAgora;
        if (canvas.gameObject.activeSelf != visivel) canvas.gameObject.SetActive(visivel);
        if (visivel) Posicionar();
    }

    private void Posicionar()
    {
        if (alvoAtual == null) return;

        // Topo da arte, não do colisor: o colisor de NPC cobre só o corpo e o aviso caía sobre a cabeça.
        Vector3 topo = TopoVisual(renderersDoAlvo, alvoAtual);

        // Nunca abaixo da cabeça do jogador (mais alto que porta/mesa).
        Vector3 topoJogador = TopoVisual(renderersDoJogador, jogador.gameObject);
        if (topoJogador.y > topo.y) topo.y = topoJogador.y;

        Vector3 posicao = topo + Vector3.up * alturaAcimaDoAlvo;
        posicao = ManterDentroDaTela(posicao);
        canvas.transform.position = FugirDoRelogio(posicao);
    }

    private const float MargemNoTopoDaTela = 0.2f; // unidades do mundo

    // Alvos altos (murais) jogariam o aviso para fora da tela: desce até caber.
    private Vector3 ManterDentroDaTela(Vector3 posicao)
    {
        Camera cam = Camera.main;
        if (cam == null) return posicao;

        float altura = (tecla.anchoredPosition.y + tecla.rect.height) * escala;
        float topoVisivel = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, Mathf.Abs(cam.transform.position.z))).y - MargemNoTopoDaTela;
        if (posicao.y + altura > topoVisivel) posicao.y = topoVisivel - altura;
        return posicao;
    }

    private const float MargemAbaixoDoRelogio = 10f; // pixels de tela

    // O topo das portas coincide com o relógio de horas: se o aviso cair sobre ele, desce até logo abaixo.
    private Vector3 FugirDoRelogio(Vector3 posicao)
    {
        Camera cam = Camera.main;
        if (cam == null || !HudDoRelogio.RetanguloNaTela(out Rect relogio)) return posicao;

        // Bloco inteiro em px do canvas.
        bool comNome = etiquetaDoNome.gameObject.activeSelf;
        float altura = tecla.anchoredPosition.y + tecla.rect.height;
        float largura = Mathf.Max(tecla.rect.width, comNome ? etiquetaDoNome.rect.width : 0f);

        Vector3 cantoBaixo = cam.WorldToScreenPoint(posicao);
        Vector3 cantoCima = cam.WorldToScreenPoint(posicao + Vector3.up * (altura * escala));
        float meiaLargura = Mathf.Abs(cam.WorldToScreenPoint(posicao + Vector3.right * (largura * 0.5f * escala)).x - cantoBaixo.x);
        Rect naTela = Rect.MinMaxRect(cantoBaixo.x - meiaLargura, cantoBaixo.y, cantoBaixo.x + meiaLargura, cantoCima.y);
        if (!naTela.Overlaps(relogio) || cantoCima.y - cantoBaixo.y <= 0f) return posicao;

        float descerEmPixels = cantoCima.y - (relogio.yMin - MargemAbaixoDoRelogio);
        float unidadesPorPixel = (altura * escala) / (cantoCima.y - cantoBaixo.y);
        return posicao - Vector3.up * (descerEmPixels * unidadesPorPixel);
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
        go.transform.SetParent(null, false); // solto no mundo para não herdar flip/escala do jogador
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Sorting layer mais alta: cenário e personagens ficam acima de "Default".
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
        // Com o sprite dos botões a tecla já tem moldura e face: o miolo vira só layout.
        bool comSprite = EstiloDeBotao.Aplicar(imgBorda);
        tecla = rBorda;
        larguraTecla = borda.GetComponent<LayoutElement>();
        larguraTecla.preferredHeight = 48f;
        larguraTecla.minWidth = 48f;
        larguraTecla.flexibleWidth = 0f;
        larguraTecla.flexibleHeight = 0f;
        var hl = borda.GetComponent<HorizontalLayoutGroup>();
        hl.padding = comSprite ? EstiloDeBotao.Recuos() : new RectOffset(3, 3, 3, 3);
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = true;
        var fitter = borda.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Miolo com layout group para a largura da tecla vir do texto.
        var miolo = new GameObject("Miolo", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        miolo.transform.SetParent(borda.transform, false);
        var imgMiolo = miolo.GetComponent<Image>();
        imgMiolo.sprite = SpriteArredondado();
        imgMiolo.type = Image.Type.Sliced;
        imgMiolo.color = CorFundo;
        imgMiolo.raycastTarget = false;
        imgMiolo.enabled = !comSprite;
        var hlMiolo = miolo.GetComponent<HorizontalLayoutGroup>();
        hlMiolo.padding = comSprite ? new RectOffset(0, 0, 0, 0) : new RectOffset(12, 12, 4, 4);
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

        MontarEtiquetaDoNome();
    }

    private void MontarEtiquetaDoNome()
    {
        var placa = new GameObject("Nome", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        placa.transform.SetParent(raiz, false);
        etiquetaDoNome = (RectTransform)placa.transform;
        etiquetaDoNome.anchorMin = etiquetaDoNome.anchorMax = new Vector2(0.5f, 0.5f);
        etiquetaDoNome.pivot = new Vector2(0.5f, 0f);
        etiquetaDoNome.anchoredPosition = Vector2.zero;
        var img = placa.GetComponent<Image>();
        img.sprite = SpriteArredondado();
        img.type = Image.Type.Sliced;
        img.color = CorFundoDoNome;
        img.raycastTarget = false;
        var hl = placa.GetComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(10, 10, 3, 3);
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childControlWidth = hl.childControlHeight = true;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var fitter = placa.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var t = new GameObject("Texto", typeof(RectTransform));
        t.transform.SetParent(placa.transform, false);
        textoDoNome = t.AddComponent<TextMeshProUGUI>();
        if (fonte != null) textoDoNome.font = fonte;
        textoDoNome.fontSize = 20f;
        textoDoNome.fontStyle = FontStyles.Bold;
        textoDoNome.alignment = TextAlignmentOptions.Center;
        textoDoNome.color = CorTextoDoNome;
        textoDoNome.raycastTarget = false;
        textoDoNome.textWrappingMode = TextWrappingModes.NoWrap;
        textoDoNome.overflowMode = TextOverflowModes.Overflow;

        placa.SetActive(false);
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
