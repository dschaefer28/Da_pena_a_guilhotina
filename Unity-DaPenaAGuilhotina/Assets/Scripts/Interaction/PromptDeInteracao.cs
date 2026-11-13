using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso flutuante que aparece sobre o objeto interagível mais próximo do jogador ("E" no Windows,
/// "Interagir" no Android). Atende ao item do documento "ícone de feedback visual indicando
/// interatividade" (alçapão) e vale para NPCs, portas, mesa e prensa. Coloque no mesmo GameObject
/// do PlayerInteraction; o visual é montado em runtime (canvas em world space), sem arte externa.
/// Quando o alvo tem nome (IInteractable.NomeExibido, ex: NPCs), uma etiqueta com o nome fica logo abaixo da tecla.
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

    // Mesma cor/sprite do BotaoContinuar (UI.prefab), para bater com o resto da UI (GlifoDeControle).
    private static readonly Color CorFundo = new Color(0.45f, 0.2f, 0.15f, 1f);
    private static readonly Color CorBorda = new Color(0.29f, 0.12f, 0.09f, 1f);
    // Etiqueta do nome: placa escura translúcida e texto creme, legível sobre qualquer cenário.
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

        // Sem largura fixa: a borda acompanha o miolo. Forçar a tecla curta a um quadrado (48) deixava o miolo, que não
        // encolhe abaixo de 66 (texto mínimo + margens), vazando para a direita da borda.
        texto.text = DispositivoDeControle.Glifo(ControleTutorial.Interagir);

        string nome = alvo.TryGetComponent<IInteractable>(out var interagivel) ? interagivel.NomeExibido : null;
        bool comNome = !string.IsNullOrWhiteSpace(nome);
        textoDoNome.text = comNome ? nome : string.Empty;
        etiquetaDoNome.gameObject.SetActive(comNome);

        canvas.gameObject.SetActive(true);
        // A tecla nasce com o tamanho padrão (100 x 100) até o primeiro layout: o desvio do relógio e o empilhamento
        // nome/tecla precisam do tamanho real. Cada um é raiz do próprio layout (o canvas não tem layout group).
        LayoutRebuilder.ForceRebuildLayoutImmediate(tecla);
        if (comNome) LayoutRebuilder.ForceRebuildLayoutImmediate(etiquetaDoNome);
        // O nome ocupa o lugar de baixo (logo acima da cabeça) e a tecla sobe para cima dele.
        tecla.anchoredPosition = new Vector2(0f, comNome ? etiquetaDoNome.rect.height + EspacoEntreNomeETecla : 0f);
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

        Vector3 posicao = topo + Vector3.up * alturaAcimaDoAlvo;
        posicao = ManterDentroDaTela(posicao);
        canvas.transform.position = FugirDoRelogio(posicao);
    }

    private const float MargemNoTopoDaTela = 0.2f; // unidades do mundo

    // Alvos altos (quadros/murais) têm o topo colado no limite da câmera: o aviso nascia acima dele, fora da tela, e o jogador
    // nunca via que dava para interagir. Se o bloco (nome + tecla) passaria do topo visível, desce até caber.
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

    // Ao chegar pela porta nas cenas de investigação, o topo da porta fica na altura do relógio de horas (topo central):
    // o "E" aparecia atrás dele. Se a tecla cair sobre o relógio, desce até logo abaixo dele.
    private Vector3 FugirDoRelogio(Vector3 posicao)
    {
        Camera cam = Camera.main;
        if (cam == null || !HudDoRelogio.RetanguloNaTela(out Rect relogio)) return posicao;

        // Bloco inteiro (nome embaixo + tecla em cima), em px do canvas.
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
        // Com o sprite padrão dos botões, a própria tecla tem moldura e face: o miolo vira só layout (sem imagem).
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

        // Miolo também é um layout group: assim a largura da tecla vem do texto (e não fica no mínimo).
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

    // Placa com o nome do alvo, logo abaixo da tecla (base no ponto do aviso; a tecla sobe quando ela aparece).
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
