using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class RevelacaoDeEtapa
{
    [Tooltip("Deve ser exatamente igual ao 'Etapa Id' de uma das etapas configuradas no TutorialManager.")]
    public string etapaId;

    [Tooltip("Objetos que ficam visíveis (SetActive true) a partir desta etapa. Ex: o botão de inventário, " +
             "o botão de pause, uma seta apontando para o alçapão, o painel da prensa, etc. " +
             "Deixe estes objetos DESATIVADOS na cena por padrão — o tutorial os ativa na hora certa.")]
    public List<GameObject> objetosParaRevelar;
}

/// <summary>
/// Popup do tutorial e revelação progressiva de UI conforme a etapa do TutorialManager. Vive no prefab UI;
/// só "Revelacoes" é configurada por cena. Some com a tela ocupada e volta sozinho.
/// </summary>
public class TutorialStepUI : MonoBehaviour
{
    [Header("Popup (reutilizado para todas as etapas desta cena)")]
    public GameObject painelPopup;
    [Tooltip("Opcional: Image usada para mostrar o ícone de cada etapa (TutorialStep.icone).")]
    public Image imagemIcone;
    public TextMeshProUGUI textoMensagem;
    [Tooltip("Botão que avança etapas de 'Clique Do Jogador' e que apenas esconde o popup nas demais etapas " +
             "(o jogo continua esperando a ação real: falar com o NPC, criar o panfleto, etc.).")]
    public Button botaoContinuar;
    [Tooltip("Opcional: pula o tutorial inteiro (TutorialManager.PularTutorial). Some quando o popup mostra uma dica.")]
    public Button botaoPular;

    [Header("Glifos de controle (opcional)")]
    [Tooltip("Container (com layout vertical) onde as 'teclas' da etapa são desenhadas. Vazio = sem glifos.")]
    public Transform containerGlifos;
    [Tooltip("Fonte das teclas/rótulos. Vazio = fonte do texto da mensagem.")]
    public TMP_FontAsset fonteGlifos;

    [Header("Revelação de UI por Etapa")]
    public List<RevelacaoDeEtapa> revelacoes;

    private string etapaExibidaAtualmente;
    private bool popupDispensado;   // Ocultar numa etapa de evento: não reabrir sozinho
    private DialogueSystem dialogoObservado;
    private bool mostrandoDica;     // popup mostra TutorialManager.DicaPendente, não a etapa
    private int frameEmQueApareceu = -1;

    void OnEnable()
    {
        Inscrever();
        CutsceneLegendas.OnTerminou += HandleCutsceneTerminou;
    }

    void OnDisable()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
            TutorialManager.Instance.OnDicaSolicitada -= HandleDicaSolicitada;
        }
        CutsceneLegendas.OnTerminou -= HandleCutsceneTerminou;
        ObservarDialogo(null);
        ObservarInventario(null);
    }

    void Start()
    {
        if (painelPopup == null) Debug.LogWarning("[TutorialStepUI] 'Painel Popup' não foi atribuído no Inspector.", this);
        if (textoMensagem == null) Debug.LogWarning("[TutorialStepUI] 'Texto Mensagem' não foi atribuído no Inspector.", this);

        if (painelPopup != null) painelPopup.SetActive(false);
        mostrandoDica = false; // dica pedida antes deste Start continua na fila e volta pelo Update
        if (botaoContinuar != null) botaoContinuar.onClick.AddListener(OnContinuarClicado);
        if (botaoPular != null) botaoPular.onClick.AddListener(OnPularClicado);

        // Botões de 46 de altura (~3 mm no celular): amplia só a área de toque.
        AumentarAreaDeToque(botaoContinuar);
        AumentarAreaDeToque(botaoPular);

        // O TutorialManager pode não estar pronto no OnEnable.
        Inscrever();

        var dialogo = GameManager.Instance != null ? GameManager.Instance.dialogueSystem : FindAnyObjectByType<DialogueSystem>();
        ObservarDialogo(dialogo);
        ObservarInventario(GameManager.Instance != null ? GameManager.Instance.inventoryManager : FindAnyObjectByType<InventoryManager>());

        if (TutorialManager.Instance == null)
        {
            Debug.LogWarning("[TutorialStepUI] Nenhum TutorialManager encontrado na cena. Crie um GameObject com " +
                              "o script 'Tutorial Manager' na cena Jogo (ele é persistente e não precisa existir nas outras cenas).", this);
            return;
        }

        ValidarEtapaIds();

        // Reaplica revelações já alcançadas ao voltar para a cena.
        HandleEtapaAlterada(TutorialManager.Instance.EtapaAtualId);
    }

    void Update()
    {
        if (painelPopup == null) return;

        // Com a tela ocupada o popup sai da frente (sob o pause ainda aceitaria o Enter) e volta depois.
        if (TelaOcupada())
        {
            if (painelPopup.activeSelf) EsconderPorOcupacao();
            return;
        }
        if (!painelPopup.activeSelf)
        {
            // Dica pedida antes do nosso Start (que esconde o painel) ficaria presa como "na tela".
            mostrandoDica = false;
            ReexibirSePrecisar();
            return;
        }

        // Enter/Espaço = Continuar, exceto no frame em que o popup apareceu (o Enter que fechou a conversa o pularia)
        // ou com outro controle focado (o mesmo Enter agiria nos dois).
        if (DispositivoDeControle.EhToque || Time.frameCount == frameEmQueApareceu || OutroControleComFoco()) return;
        var teclado = Keyboard.current;
        if (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame))
            OnContinuarClicado();
    }

    private void Inscrever()
    {
        if (TutorialManager.Instance == null) return;
        TutorialManager.Instance.OnEtapaAlterada -= HandleEtapaAlterada;
        TutorialManager.Instance.OnEtapaAlterada += HandleEtapaAlterada;
        TutorialManager.Instance.OnDicaSolicitada -= HandleDicaSolicitada;
        TutorialManager.Instance.OnDicaSolicitada += HandleDicaSolicitada;
    }

    // ===== Dicas contextuais (fila no TutorialManager) =====

    private void HandleDicaSolicitada(string texto)
    {
        if (!mostrandoDica) MostrarDicaSePossivel();
    }

    private bool TelaOcupada() =>
        (dialogoObservado != null && dialogoObservado.IsDialogueActive) || CutsceneLegendas.EmExibicao ||
        JanelasModais.AlgumaAberta || (PauseMenu.Instance != null && PauseMenu.Instance.IsOpen);

    private bool MostrarDicaSePossivel()
    {
        string dica = TutorialManager.Instance != null ? TutorialManager.Instance.DicaPendente : null;
        if (string.IsNullOrEmpty(dica) || painelPopup == null || TelaOcupada()) return false;

        mostrandoDica = true;
        iconeDaEtapa = false;
        if (imagemIcone != null) imagemIcone.gameObject.SetActive(false);
        if (textoMensagem != null) textoMensagem.text = DispositivoDeControle.Substituir(dica);
        DefinirTitulo("Dica");
        DefinirTextoContinuar(TextoContinuar);
        MontarGlifos(ControleTutorial.Nenhum);
        if (botaoPular != null) botaoPular.gameObject.SetActive(false);
        AtivarPopup();
        return true;
    }

    private void AtivarPopup()
    {
        if (!painelPopup.activeSelf) frameEmQueApareceu = Time.frameCount;
        painelPopup.SetActive(true);
        PosicionarPopup();
    }

    private bool OutroControleComFoco()
    {
        GameObject foco = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return foco != null && foco.activeInHierarchy && !foco.transform.IsChildOf(painelPopup.transform);
    }

    // A dica continua a primeira da fila e volta quando a tela ficar livre.
    private void EsconderPorOcupacao()
    {
        mostrandoDica = false;
        painelPopup.SetActive(false);
    }

    // "Titulo" é criado pelo TutorialPopupBuilder.
    private void DefinirTitulo(string titulo)
    {
        Transform t = painelPopup != null ? painelPopup.transform.Find("Titulo") : null;
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp != null) tmp.text = titulo;
    }

    // Pular pede um segundo toque: evita pular o tutorial por toque acidental no celular.
    private const float JanelaConfirmarPular = 3f;
    private float confirmarPularAte = -1f;

    private void OnPularClicado()
    {
        if (Time.unscaledTime > confirmarPularAte)
        {
            confirmarPularAte = Time.unscaledTime + JanelaConfirmarPular;
            DefinirTextoPular(DispositivoDeControle.EhToque ? "<u>Toque de novo para pular</u>" : "<u>Clique de novo para pular</u>");
            return;
        }
        confirmarPularAte = -1f;
        DefinirTextoPular(TextoPularPadrao);
        if (TutorialManager.Instance != null) TutorialManager.Instance.PularTutorial();
    }

    private const string TextoPularPadrao = "<u>Pular tutorial</u>";

    // Nas etapas de ação o botão só esconde a mensagem; "Continuar" daria a entender que avançou.
    private const string TextoContinuar = "Continuar";
    private const string TextoOcultar = "Ocultar";

    private void DefinirTextoContinuar(string texto)
    {
        TextMeshProUGUI rotulo = botaoContinuar != null ? botaoContinuar.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (rotulo != null) rotulo.text = texto;
    }

    private void DefinirTextoPular(string texto)
    {
        TextMeshProUGUI rotulo = botaoPular != null ? botaoPular.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (rotulo != null) rotulo.text = texto;
    }

    private static void AumentarAreaDeToque(Button botao)
    {
        Graphic alvo = botao != null ? botao.targetGraphic : null;
        if (alvo == null) return;
        // (esq, baixo, dir, cima): negativos ampliam a área clicável.
        if (alvo.raycastPadding == Vector4.zero) alvo.raycastPadding = new Vector4(-16f, -12f, -16f, -24f);
    }

    // Layout original do prefab; PosicionarPopup parte sempre dele.
    private bool layoutOriginalSalvo;
    private Vector2 ancoraMinOriginal, ancoraMaxOriginal, pivoOriginal, posicaoOriginal, tamanhoOriginal;

    private const float MargemTela = 16f;
    private const float LarguraMinimaAoLado = 380f;

    private void PosicionarPopup()
    {
        RectTransform popup = painelPopup != null ? painelPopup.transform as RectTransform : null;
        if (popup == null) return;
        if (!layoutOriginalSalvo)
        {
            ancoraMinOriginal = popup.anchorMin; ancoraMaxOriginal = popup.anchorMax; pivoOriginal = popup.pivot;
            posicaoOriginal = popup.anchoredPosition; tamanhoOriginal = popup.sizeDelta;
            layoutOriginalSalvo = true;
        }
        popup.anchorMin = ancoraMinOriginal; popup.anchorMax = ancoraMaxOriginal; popup.pivot = pivoOriginal;
        popup.anchoredPosition = posicaoOriginal; popup.sizeDelta = tamanhoOriginal;
        if (containerGlifos != null) containerGlifos.gameObject.SetActive(glifosDaEtapa);
        if (imagemIcone != null) imagemIcone.gameObject.SetActive(iconeDaEtapa);

        CrescerParaOTexto(popup);
        AfastarDoInventario(popup);
        EvitarControlesDeToque();
    }

    // Cresce para cima até o texto caber sem auto size, limitado pela HUD do topo.
    private void CrescerParaOTexto(RectTransform popup)
    {
        if (textoMensagem == null || popup.pivot.y != 0f || !(popup.parent is RectTransform pai)) return;
        RectTransform corpo = CorpoDoPopup(popup);
        if (corpo == popup) return;
        float margensDoCorpo = tamanhoOriginal.y - corpo.rect.height; // título em cima, botões embaixo
        float larguraDoTexto = corpo.rect.width - LarguraDaColunaEsquerda(corpo);
        float alturaDoConteudo = textoMensagem.GetPreferredValues(textoMensagem.text, larguraDoTexto, 0f).y;
        if (containerGlifos != null && containerGlifos.gameObject.activeSelf)
            alturaDoConteudo = Mathf.Max(alturaDoConteudo, LayoutUtility.GetPreferredHeight((RectTransform)containerGlifos));
        float maxima = pai.rect.height - AlturaDaHudDoTopo - 2f * MargemTela - popup.anchoredPosition.y;
        float altura = Mathf.Clamp(alturaDoConteudo + margensDoCorpo + 12f, tamanhoOriginal.y, Mathf.Max(tamanhoOriginal.y, maxima));
        popup.sizeDelta = new Vector2(popup.sizeDelta.x, altura);
    }

    // Conteúdo da coluna da esquerda na etapa atual.
    private bool glifosDaEtapa;
    private bool iconeDaEtapa;

    private const float AlturaDaHudDoTopo = 270f;

    // Com o inventário aberto, move o popup para a faixa livre à esquerda dele (senão cobriria as pistas).
    private void AfastarDoInventario(RectTransform popup)
    {
        InventoryManager inventario = GameManager.Instance != null ? GameManager.Instance.inventoryManager : FindAnyObjectByType<InventoryManager>();
        if (inventario == null || inventario.inventoryUI == null || !inventario.inventoryUI.activeInHierarchy) return;

        RectTransform fundo = inventario.inventoryUI.transform.Find("InventoryBackGround") as RectTransform;
        if (fundo == null) fundo = inventario.inventoryUI.transform as RectTransform;
        Rect telaInventario = RetanguloNaTela(fundo);
        if (!telaInventario.Overlaps(RetanguloNaTela(popup))) return;

        Canvas canvas = popup.GetComponentInParent<Canvas>();
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        float bordaEsquerda = RetanguloNaTela((RectTransform)popup.parent).xMin;
        float livre = (telaInventario.xMin - bordaEsquerda) / escala - 2f * MargemTela;
        if (livre < LarguraMinimaAoLado) return; // melhor sobrepor do que espremer o texto

        popup.anchorMin = popup.anchorMax = popup.pivot = Vector2.zero;
        popup.anchoredPosition = new Vector2(MargemTela, MargemTela);
        float largura = Mathf.Min(tamanhoOriginal.x, livre);

        // A coluna de glifos só fica se o texto ainda couber na largura mínima ao lado dela.
        float altura = tamanhoOriginal.y;
        if (textoMensagem != null && popup.parent is RectTransform pai)
        {
            RectTransform corpo = CorpoDoPopup(popup);
            float margensDoCorpo = tamanhoOriginal.y - corpo.rect.height; // título em cima, botões embaixo
            float larguraDoCorpo = largura - (tamanhoOriginal.x - corpo.rect.width);
            float colunaEsquerda = LarguraDaColunaEsquerda(corpo);
            bool cabeAoLado = colunaEsquerda <= 0f || larguraDoCorpo - colunaEsquerda >= LayoutUtility.GetMinWidth(textoMensagem.rectTransform);
            if (!cabeAoLado) EsconderColunaEsquerda();
            float larguraDoTexto = larguraDoCorpo - (cabeAoLado ? colunaEsquerda : 0f);

            float alturaDoConteudo = textoMensagem.GetPreferredValues(textoMensagem.text, larguraDoTexto, 0f).y;
            if (cabeAoLado && containerGlifos != null && containerGlifos.gameObject.activeSelf)
                alturaDoConteudo = Mathf.Max(alturaDoConteudo, LayoutUtility.GetPreferredHeight((RectTransform)containerGlifos));
            float maxima = pai.rect.height - AlturaDaHudDoTopo - 2f * MargemTela;
            altura = Mathf.Clamp(alturaDoConteudo + margensDoCorpo + 12f, tamanhoOriginal.y, Mathf.Max(tamanhoOriginal.y, maxima));
        }
        else EsconderColunaEsquerda();
        popup.sizeDelta = new Vector2(largura, altura);

        // Da Fase 3 em diante o painel Suporte ocupa essa faixa: se sobrepor algo do inventário, volta ao rodapé.
        if (SobrepoeOInventario(RetanguloNaTela(popup), inventario.inventoryUI.transform))
        {
            popup.anchorMin = ancoraMinOriginal; popup.anchorMax = ancoraMaxOriginal; popup.pivot = pivoOriginal;
            popup.anchoredPosition = posicaoOriginal; popup.sizeDelta = tamanhoOriginal;
            if (containerGlifos != null) containerGlifos.gameObject.SetActive(glifosDaEtapa);
            if (imagemIcone != null) imagemIcone.gameObject.SetActive(iconeDaEtapa);
        }
    }

    private static bool SobrepoeOInventario(Rect popup, Transform inventario)
    {
        foreach (Graphic g in inventario.GetComponentsInChildren<Graphic>(false))
            if (g.enabled && g.color.a > 0.01f && g.canvasRenderer.GetInheritedAlpha() > 0.01f && RetanguloNaTela(g.rectTransform).Overlaps(popup))
                return true;
        return false;
    }

    private void EsconderColunaEsquerda()
    {
        if (containerGlifos != null) containerGlifos.gameObject.SetActive(false);
        if (imagemIcone != null) imagemIcone.gameObject.SetActive(false);
    }

    private float LarguraDaColunaEsquerda(RectTransform corpo)
    {
        var layout = corpo.GetComponent<HorizontalLayoutGroup>();
        float espaco = layout != null ? layout.spacing : 0f;
        float total = 0f;
        foreach (RectTransform filho in corpo)
        {
            if (filho == textoMensagem.rectTransform || !filho.gameObject.activeSelf) continue;
            total += LayoutUtility.GetPreferredWidth(filho) + espaco;
        }
        return total;
    }

    // "Corpo" do TutorialPopupBuilder; sem ele, o próprio popup.
    private RectTransform CorpoDoPopup(RectTransform popup)
    {
        RectTransform corpo = textoMensagem != null ? textoMensagem.transform.parent as RectTransform : null;
        return corpo != null && corpo != popup && corpo.IsChildOf(popup) ? corpo : popup;
    }

    private void HandleInventarioAlternado(bool aberto)
    {
        if (painelPopup != null && painelPopup.activeSelf) PosicionarPopup();
    }

    private InventoryManager inventarioObservado;

    private void ObservarInventario(InventoryManager inventario)
    {
        if (inventarioObservado != null) inventarioObservado.OnInventoryToggled -= HandleInventarioAlternado;
        inventarioObservado = inventario;
        if (inventarioObservado != null) inventarioObservado.OnInventoryToggled += HandleInventarioAlternado;
    }

    // Em telas 4:3 o popup cobria os controles de toque: sobe até ficar acima deles.
    private static readonly string[] ControlesDeToque = { "JoystickBG", "InteractButton", "InventoryButton" };

    private void EvitarControlesDeToque()
    {
        RectTransform popup = painelPopup != null ? painelPopup.transform as RectTransform : null;
        if (popup == null || MobileControlsManager.Instance == null || !DispositivoDeControle.EhToque) return;
        float yBase = popup.anchoredPosition.y;

        Rect telaPopup = RetanguloNaTela(popup);
        float topoDosControles = float.NegativeInfinity;
        foreach (RectTransform controle in MobileControlsManager.Instance.GetComponentsInChildren<RectTransform>(false))
        {
            if (System.Array.IndexOf(ControlesDeToque, controle.name) < 0) continue;
            Rect telaControle = RetanguloNaTela(controle);
            if (telaControle.Overlaps(telaPopup)) topoDosControles = Mathf.Max(topoDosControles, telaControle.yMax);
        }
        if (float.IsNegativeInfinity(topoDosControles)) return;

        Canvas canvas = popup.GetComponentInParent<Canvas>();
        float escala = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        float subir = (topoDosControles - telaPopup.yMin) / escala + 12f;
        popup.anchoredPosition = new Vector2(popup.anchoredPosition.x, yBase + subir);

        // Depois de subir, o topo não pode passar da HUD (o auto size encolhe o texto).
        if (popup.parent is RectTransform pai && popup.pivot.y == 0f && popup.anchorMin.y == 0f)
        {
            float topoMaximo = pai.rect.height - AlturaDaHudDoTopo - MargemTela;
            float alturaMaxima = topoMaximo - popup.anchoredPosition.y;
            if (popup.sizeDelta.y > alturaMaxima)
                popup.sizeDelta = new Vector2(popup.sizeDelta.x, Mathf.Max(Mathf.Min(tamanhoOriginal.y, popup.sizeDelta.y), alturaMaxima));
        }
    }

    private static Rect RetanguloNaTela(RectTransform rt)
    {
        var cantos = new Vector3[4];
        rt.GetWorldCorners(cantos);
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, cantos[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, cantos[2]);
        return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
    }

    private void ObservarDialogo(DialogueSystem dialogo)
    {
        if (dialogoObservado != null)
        {
            dialogoObservado.OnDialogueStarted -= HandleDialogoComecou;
            dialogoObservado.OnDialogueEnded -= HandleDialogoTerminou;
        }
        dialogoObservado = dialogo;
        if (dialogoObservado != null)
        {
            dialogoObservado.OnDialogueStarted += HandleDialogoComecou;
            dialogoObservado.OnDialogueEnded += HandleDialogoTerminou;
        }
    }

    private void HandleDialogoComecou()
    {
        if (painelPopup != null) EsconderPorOcupacao();
    }
    private void HandleDialogoTerminou() => ReexibirSePrecisar();
    private void HandleCutsceneTerminou() => ReexibirSePrecisar();

    private void ReexibirSePrecisar()
    {
        if (mostrandoDica || MostrarDicaSePossivel()) return;
        if (popupDispensado || TutorialManager.Instance == null || TelaOcupada()) return;
        if (painelPopup != null && painelPopup.activeSelf) return;
        TutorialStep etapa = TutorialManager.Instance.ObterEtapa(etapaExibidaAtualmente);
        if (etapa != null && etapa.etapaId == TutorialManager.Instance.EtapaAtualId) MostrarPopup(etapa);
    }

    private void ValidarEtapaIds()
    {
        if (revelacoes == null) return;
        foreach (var revelacao in revelacoes)
        {
            if (revelacao == null || string.IsNullOrEmpty(revelacao.etapaId)) continue;
            if (TutorialManager.Instance.ObterEtapa(revelacao.etapaId) == null)
            {
                Debug.LogWarning($"[TutorialStepUI] O 'Etapa Id' \"{revelacao.etapaId}\" configurado em Revelacoes não " +
                                  "existe na lista de Etapas do TutorialManager. Confira a grafia (maiúsculas/minúsculas e espaços contam).", this);
            }
        }
    }

    private void HandleEtapaAlterada(string etapaId)
    {
        etapaExibidaAtualmente = etapaId;
        popupDispensado = false;
        AplicarRevelacoes();

        // Dica na tela tem prioridade sobre a etapa nova.
        if (mostrandoDica && painelPopup != null && painelPopup.activeSelf) return;
        mostrandoDica = false;

        TutorialStep etapa = TutorialManager.Instance != null ? TutorialManager.Instance.ObterEtapa(etapaId) : null;
        if (etapa == null)
        {
            if (painelPopup != null) painelPopup.SetActive(false);
            return;
        }

        if (TelaOcupada()) { if (painelPopup != null) painelPopup.SetActive(false); return; }

        MostrarPopup(etapa);
    }

    // Revela (nunca esconde) tudo de etapas já alcançadas, para sobreviver à troca de cena.
    private void AplicarRevelacoes()
    {
        if (revelacoes == null || TutorialManager.Instance == null) return;

        foreach (var revelacao in revelacoes)
        {
            if (revelacao == null || revelacao.objetosParaRevelar == null) continue;
            if (!TutorialManager.Instance.EtapaJaFoiAlcancada(revelacao.etapaId)) continue;

            foreach (var objeto in revelacao.objetosParaRevelar)
            {
                if (objeto != null) objeto.SetActive(true);
            }
        }
    }

    private void MostrarPopup(TutorialStep etapa)
    {
        if (painelPopup == null)
        {
            Debug.LogWarning($"[TutorialStepUI:{gameObject.scene.name}/{name}] Deveria mostrar a etapa '{etapa.etapaId}' mas 'Painel Popup' está vazio no Inspector.", this);
            return;
        }

        iconeDaEtapa = etapa.icone != null;
        if (imagemIcone != null)
        {
            imagemIcone.sprite = etapa.icone;
            imagemIcone.gameObject.SetActive(iconeDaEtapa);
        }

        if (textoMensagem != null) textoMensagem.text = DispositivoDeControle.Substituir(etapa.mensagem);

        MontarGlifos(etapa.controle);
        if (botaoPular != null) botaoPular.gameObject.SetActive(true);
        confirmarPularAte = -1f;
        DefinirTextoPular(TextoPularPadrao);
        DefinirTitulo("Tutorial");
        DefinirTextoContinuar(etapa.tipoDeAvanco == TipoDeAvanco.CliqueDoJogador ? TextoContinuar : TextoOcultar);

        AtivarPopup();
    }

    private void MontarGlifos(ControleTutorial controle)
    {
        if (containerGlifos == null) return;

        for (int i = containerGlifos.childCount - 1; i >= 0; i--)
        {
            // Destroy só age no fim do frame.
            GameObject antigo = containerGlifos.GetChild(i).gameObject;
            antigo.SetActive(false);
            Destroy(antigo);
        }

        TMP_FontAsset fonte = fonteGlifos != null ? fonteGlifos : (textoMensagem != null ? textoMensagem.font : null);
        bool algum = false;
        if (controle == ControleTutorial.Principais)
        {
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Mover, fonte, 36f);
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Interagir, fonte, 36f);
            GlifoDeControle.Criar(containerGlifos, ControleTutorial.Inventario, fonte, 36f);
            algum = true;
        }
        else if (controle != ControleTutorial.Nenhum)
        {
            GlifoDeControle.Criar(containerGlifos, controle, fonte);
            algum = true;
        }
        glifosDaEtapa = algum;
        containerGlifos.gameObject.SetActive(algum);
    }

    private void OnContinuarClicado()
    {
        if (mostrandoDica)
        {
            mostrandoDica = false;
            if (TutorialManager.Instance != null) TutorialManager.Instance.ConcluirDica();
            if (painelPopup != null) painelPopup.SetActive(false);
            ReexibirSePrecisar(); // próxima dica da fila ou a etapa que esperava
            return;
        }

        if (TutorialManager.Instance == null) return;

        TutorialStep etapa = TutorialManager.Instance.ObterEtapa(etapaExibidaAtualmente);
        if (etapa != null && etapa.tipoDeAvanco == TipoDeAvanco.CliqueDoJogador)
        {
            TutorialManager.Instance.CompletarEtapaAtual();
        }
        else
        {
            popupDispensado = true;
            if (painelPopup != null) painelPopup.SetActive(false);
        }
    }
}
