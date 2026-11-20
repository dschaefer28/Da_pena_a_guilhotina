using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class CaseSelectionUI : MonoBehaviour
{
    [Header("Referências Visuais")]
    [Tooltip("Arraste o Prefab do Cartão que está na sua pasta Prefabs")]
    public GameObject cardPrefab;
    [Tooltip("Arraste a AreaDosCartoes da sua cena")]
    public Transform cardsContainer;

    [Header("Dados")]
    [Tooltip("Casos de todas as fases. A mesa mostra só os da fase atual (CaseData.fase).")]
    public List<CaseData> availableCases;

    [Header("Layout na tela (Prompt 7)")]
    [Tooltip("O painel foi desenhado para 2376 de largura: numa tela 16:9 o botão Fechar ficava fora dela e, em telas mais " +
             "estreitas, os cartões eram cortados. Ligado, o painel cobre a tela visível, a área dos cartões encolhe até " +
             "caber (nunca passa da largura original) e o Fechar fica no canto superior direito, dentro da área segura.")]
    public bool ajustarATela = true;
    [Min(0f)] public float margemDaTela = 24f;
    [Tooltip("Cartões lado a lado em cada linha. Os casos de uma fase (até três) ficam todos visíveis de uma vez; antes, " +
             "empilhados na largura toda, só cabia um cartão e meio numa tela 1920 x 1080.")]
    [Min(1)] public int cartoesPorLinha = 3;
    [Tooltip("Escurece o cartão de um caso concluído ou bloqueado, sem deixá-lo translúcido.")]
    public Color corDoVeuDeBloqueio = new Color(0f, 0f, 0f, 0.7f);

    private readonly List<LayoutElement> cartoesNaTela = new List<LayoutElement>();

    public static bool Aberta { get; private set; }
    private static int frameEmQueFechou = -1;
    /// <summary>O Esc que fechou a mesa não pode abrir o pause no mesmo frame.</summary>
    public static bool BloqueiaPausa => Aberta || frameEmQueFechou == Time.frameCount;

    // Play Mode sem domain reload: o estado estático não pode vazar entre sessões.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ZerarEstado()
    {
        Aberta = false;
        frameEmQueFechou = -1;
    }

    private RectTransform areaDosCartoes;
    private RectTransform botaoFechar;
    private Vector2 larguraAlturaOriginalDaArea;
    private bool layoutGuardado;
    private Vector2 ultimoTamanhoDoCanvas;
    private Rect ultimaAreaSegura;

    // Acima dos controles de toque (2), abaixo dos popups (10), Biblioteca/Quadro (30–35) e cutscenes (50).
    private const int OrdemDaMesa = 5;

    void OnEnable()
    {
        Aberta = true;
        GarantirCanvasProprio();
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(false);
        AjustarLayout();
        GerarCartoesNaTela();
        // Foco inicial no Fechar: Enter não aceita um caso sem querer.
        if (botaoFechar != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(botaoFechar.gameObject);
    }

    void OnDisable()
    {
        if (Aberta) frameEmQueFechou = Time.frameCount;
        Aberta = false;
        if (MobileControlsManager.Instance != null) MobileControlsManager.Instance.SetControlsInteractable(true);
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            FecharPainel();
            return;
        }
        // Compara o canvas, não a tela: o CanvasScaler pode atualizar um frame depois da troca de resolução.
        if (TamanhoDoCanvas() != ultimoTamanhoDoCanvas || ultimaAreaSegura != Screen.safeArea) AjustarLayout();
    }

    private Vector2 TamanhoDoCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect.size : Vector2.zero;
    }

    private void GarantirCanvasProprio()
    {
        if (!ajustarATela || GetComponent<Canvas>() != null || GetComponentInParent<Canvas>() == null) return;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = OrdemDaMesa;
        gameObject.AddComponent<GraphicRaycaster>();
    }

    // Painel cobre a tela visível; área dos cartões centrada (no máximo a largura original); Fechar no canto superior direito.
    private void AjustarLayout()
    {
        ultimoTamanhoDoCanvas = TamanhoDoCanvas();
        ultimaAreaSegura = Screen.safeArea;
        if (!ajustarATela) { AjustarLarguraDosCartoes(); return; }

        var painel = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var raiz = (RectTransform)canvas.rootCanvas.transform;
        float escala = canvas.rootCanvas.scaleFactor;
        if (escala <= 0f || raiz.rect.width <= 0f) return;

        if (!layoutGuardado)
        {
            areaDosCartoes = transform.Find("AreaDosCartoes") as RectTransform;
            botaoFechar = transform.Find("BotaoFechar") as RectTransform;
            if (areaDosCartoes != null) larguraAlturaOriginalDaArea = areaDosCartoes.sizeDelta;
            layoutGuardado = true;
        }

        // Recuos da área segura (notch, barras) em unidades do canvas.
        Rect seguro = Screen.safeArea;
        float esquerda = seguro.xMin / escala, direita = (Screen.width - seguro.xMax) / escala;
        float topo = (Screen.height - seguro.yMax) / escala, baixo = seguro.yMin / escala;

        float largura = raiz.rect.width, altura = raiz.rect.height;
        painel.anchorMin = painel.anchorMax = painel.pivot = new Vector2(0.5f, 0.5f);
        painel.anchoredPosition = Vector2.zero;
        painel.sizeDelta = new Vector2(largura, altura);

        float topoDosCartoes = altura * 0.5f - topo - margemDaTela;
        if (botaoFechar != null)
        {
            botaoFechar.anchorMin = botaoFechar.anchorMax = botaoFechar.pivot = new Vector2(0.5f, 0.5f);
            Vector2 tamanho = botaoFechar.sizeDelta;
            botaoFechar.anchoredPosition = new Vector2(largura * 0.5f - direita - margemDaTela - tamanho.x * 0.5f,
                                                       altura * 0.5f - topo - margemDaTela - tamanho.y * 0.5f);
            topoDosCartoes -= tamanho.y + 12f;
        }

        if (areaDosCartoes != null)
        {
            float disponivel = largura - esquerda - direita - 2f * margemDaTela;
            float larguraDaArea = Mathf.Min(larguraAlturaOriginalDaArea.x, disponivel);
            float baseDosCartoes = -altura * 0.5f + baixo + margemDaTela;
            float alturaDaArea = Mathf.Max(200f, topoDosCartoes - baseDosCartoes);
            areaDosCartoes.anchorMin = areaDosCartoes.anchorMax = areaDosCartoes.pivot = new Vector2(0.5f, 0.5f);
            areaDosCartoes.sizeDelta = new Vector2(larguraDaArea, alturaDaArea);
            areaDosCartoes.anchoredPosition = new Vector2((esquerda - direita) * 0.5f, topoDosCartoes - alturaDaArea * 0.5f);
        }
        AjustarLarguraDosCartoes();
    }

    // Largura fixa por coluna: um caso só (Fase 4) fica centrado em vez de esticar na tela toda.
    private void AjustarLarguraDosCartoes()
    {
        if (cardsContainer == null || cartoesNaTela.Count == 0) return;
        var conteudo = (RectTransform)cardsContainer;
        var layout = cardsContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
        float util = conteudo.rect.width - (layout != null ? layout.padding.horizontal : 0);
        float espaco = layout != null ? layout.spacing : 24f;
        float largura = Mathf.Floor((util - espaco * (cartoesPorLinha - 1)) / cartoesPorLinha);
        if (largura <= 0f) return;
        foreach (LayoutElement cartao in cartoesNaTela)
            if (cartao != null) cartao.minWidth = cartao.preferredWidth = largura;
    }

    private Transform NovaLinhaDeCartoes()
    {
        var linha = new GameObject("LinhaDeCartoes", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        linha.layer = cardsContainer.gameObject.layer;
        linha.transform.SetParent(cardsContainer, false);
        var layout = linha.GetComponent<HorizontalLayoutGroup>();
        var colunas = cardsContainer.GetComponent<HorizontalOrVerticalLayoutGroup>();
        layout.spacing = colunas != null ? colunas.spacing : 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true; // mesma altura na linha: botões "Aceitar Caso" alinhados
        return linha.transform;
    }

    // Véu escuro por cima em vez de cartão translúcido.
    private void Escurecer(GameObject cartao)
    {
        var veu = new GameObject("VeuDeBloqueio", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        veu.layer = cartao.layer;
        veu.transform.SetParent(cartao.transform, false);
        veu.GetComponent<LayoutElement>().ignoreLayout = true;
        var rt = (RectTransform)veu.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
        var imagem = veu.GetComponent<Image>();
        imagem.color = corDoVeuDeBloqueio;
        imagem.raycastTarget = false;
    }

    private void GerarCartoesNaTela()
    {
        foreach (Transform child in cardsContainer)
        {
            // Destroy só age no fim do frame; desativar tira o cartão do layout já neste frame.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        GameManager gm = GameManager.Instance;
        // Um caso por vez: até o panfleto do caso aceito ser impresso, a mesa só mostra as cartas.
        bool casoEmAndamento = gm != null && gm.CasoAtualEmAndamento;
        if (casoEmAndamento)
            AvisoNaTela.Mostrar($"Termine o caso atual antes de aceitar outro: {(gm.casoEscolhido.caseTitle ?? gm.casoEscolhido.name).Trim()}");

        cartoesNaTela.Clear();
        Transform linhaAtual = null;
        int naLinha = 0;
        foreach (CaseData caso in availableCases)
        {
            if (!CasoVisivel(caso, gm)) continue;

            if (linhaAtual == null || naLinha >= cartoesPorLinha) { linhaAtual = NovaLinhaDeCartoes(); naLinha = 0; }
            naLinha++;

            GameObject novoCartao = Instantiate(cardPrefab, linhaAtual);
            LayoutElement coluna = novoCartao.GetComponent<LayoutElement>();
            if (coluna == null) coluna = novoCartao.AddComponent<LayoutElement>();
            coluna.flexibleWidth = 0f;
            cartoesNaTela.Add(coluna);

            // Nomes congelados: precisam bater com a hierarquia do prefab do cartão.
            TextMeshProUGUI titulo = novoCartao.transform.Find("TituloTexto").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descricao = novoCartao.transform.Find("DescricaoTexto").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI objetivo = novoCartao.transform.Find("ObjetivoTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI recompensa = novoCartao.transform.Find("RecompensaTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI opiniaoPopular = novoCartao.transform.Find("OpiniaoPopularTexto")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI opiniaoEstado = novoCartao.transform.Find("OpiniaoEstadoTexto")?.GetComponent<TextMeshProUGUI>();
            Button botaoAceitar = novoCartao.transform.Find("ButtonRow/BotaoAceitar").GetComponent<Button>();

            if (titulo != null) titulo.text = caso.caseTitle;
            if (descricao != null) descricao.text = caso.caseDescription;

            // Objetivo é opcional (ex.: Caso_Tutorial): esconde a linha se vazio.
            if (objetivo != null)
            {
                bool temObjetivo = !string.IsNullOrWhiteSpace(caso.objectiveText);
                objetivo.gameObject.SetActive(temObjetivo);
                if (temObjetivo) objetivo.text = $"Objetivo: {caso.objectiveText}";
            }

            // Só estimativa: a recompensa real é aplicada na prensa (Recipe/CraftingPress).
            if (recompensa != null)
            {
                string sinalOuro = caso.moneyReward >= 0 ? "+" : "";
                recompensa.text = $"Recompensa estimada: {sinalOuro}{caso.moneyReward} de ouro";
            }
            if (opiniaoPopular != null) opiniaoPopular.text = FormatarTendencia("Opinião Popular", caso.publicOpinionReward);
            if (opiniaoEstado != null) opiniaoEstado.text = FormatarTendencia("Opinião do Estado", caso.stateOpinionReward);

            // Concluído e em andamento ficam bloqueados; os demais da fase também após uma escolha (Fases 2 e 3: um entre três).
            EstadoDoCaso estado = EstadoDe(caso, gm);
            if (titulo != null && estado != EstadoDoCaso.Disponivel)
                titulo.text += estado == EstadoDoCaso.Concluido ? " (concluído)"
                             : estado == EstadoDoCaso.Bloqueado ? " (bloqueado)" : " (em andamento)";
            if (estado == EstadoDoCaso.Concluido || estado == EstadoDoCaso.Bloqueado) Escurecer(novoCartao);

            if (botaoAceitar != null)
            {
                // Mesma regra do GameManager, para o botão não prometer o que será recusado.
                bool podeAceitar = estado == EstadoDoCaso.Disponivel && (gm == null || gm.PodeAceitarCaso(caso, out _));
                botaoAceitar.interactable = podeAceitar;
                if (podeAceitar) botaoAceitar.onClick.AddListener(() => ConfirmarEscolha(caso));
            }
        }
        AjustarLarguraDosCartoes();
    }

    /// <summary>Bloqueado = outro caso da fase já foi escolhido (em andamento) ou a fase já tem os casos que exige.</summary>
    public enum EstadoDoCaso { Disponivel, EmAndamento, Concluido, Bloqueado }

    public static EstadoDoCaso EstadoDe(CaseData caso, GameManager gm)
    {
        if (gm == null || caso == null) return EstadoDoCaso.Disponivel;
        if (gm.casosConcluidos.Contains(caso)) return EstadoDoCaso.Concluido;
        if (gm.casoEscolhido == caso || gm.CasoJaFoiSelecionado(caso)) return EstadoDoCaso.EmAndamento;
        if (gm.CasoAtualEmAndamento || (caso.fase == gm.faseAtual && gm.FaseConcluida)) return EstadoDoCaso.Bloqueado;
        return EstadoDoCaso.Disponivel;
    }

    // Só casos da fase atual; na Fase 4, só o da rota travada.
    public static bool CasoVisivel(CaseData caso, GameManager gm)
    {
        if (caso == null) return false;
        if (gm == null) return true;
        return caso.fase == gm.faseAtual && gm.CasoDaRotaAtual(caso);
    }

    /// <summary>Falso: a mesa avisa em vez de abrir vazia.</summary>
    public bool TemCasosNaFase()
    {
        GameManager gm = GameManager.Instance;
        foreach (CaseData caso in availableCases)
            if (CasoVisivel(caso, gm)) return true;
        return false;
    }

    private void ConfirmarEscolha(CaseData casoEscolhido)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado na cena!");
            FecharPainel();
            return;
        }

        // Recusado pelo GameManager (que já avisa): a mesa continua aberta.
        if (GameManager.Instance.ConfirmarCaso(casoEscolhido)) FecharPainel();
        else GerarCartoesNaTela();
    }

    public void FecharPainel()
    {
        if (gameObject.activeSelf) frameEmQueFechou = Time.frameCount;
        gameObject.SetActive(false);
    }

    // Só a direção, nunca o valor: o resultado real depende do panfleto impresso.
    private static string FormatarTendencia(string rotulo, int valorEstimado)
    {
        if (valorEstimado > 0) return $"<color=#4CAF50>▲</color> {rotulo}: tendência de aumento";
        if (valorEstimado < 0) return $"<color=#E53935>▼</color> {rotulo}: tendência de queda";
        return $"<color=#9E9E9E>●</color> {rotulo}: sem alteração estimada";
    }
}