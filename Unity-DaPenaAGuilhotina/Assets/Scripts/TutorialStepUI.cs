using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Liga uma etapa do tutorial (pelo ID) aos objetos desta cena que devem ficar visíveis a partir dela.</summary>
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
/// Componente local de cena que mostra o popup do tutorial e revela botões/objetos aos poucos, de acordo
/// com a etapa atual do TutorialManager (persistente entre cenas). Vive no prefab UI (existe em todas as
/// cenas); só a lista "Revelacoes" é configurada por cena, porque aponta para objetos daquela cena.
///
/// Universal (Windows/Android): o texto passa por DispositivoDeControle.Substituir e a etapa pode
/// destacar um controle como "tecla" desenhada (GlifoDeControle). Segue as práticas de tutorial
/// contextual: uma ação por etapa, popup fora do caminho (rodapé), some durante diálogos e cutscenes
/// e volta sozinho, e no PC também avança com Enter/Espaço.
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
    private bool popupDispensado;   // jogador clicou Continuar numa etapa de evento: não reabrir sozinho
    private DialogueSystem dialogoObservado;
    private string dicaPendente;    // dica contextual (fora do roteiro) esperando para aparecer
    private string dicaExibida;
    private bool mostrandoDica;

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
        if (botaoContinuar != null) botaoContinuar.onClick.AddListener(OnContinuarClicado);
        if (botaoPular != null) botaoPular.onClick.AddListener(OnPularClicado);

        // Os botões do popup têm 46 de altura (≈3 mm num celular): a área de toque cresce além do desenho,
        // para os lados e para cima (sobre o texto, que não é clicável), sem mudar o visual.
        AumentarAreaDeToque(botaoContinuar);
        AumentarAreaDeToque(botaoPular);

        // Reforça a inscrição aqui (além do OnEnable) para o caso de o TutorialManager ainda
        // não existir/estar pronto no momento em que este objeto foi habilitado.
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

        // Sincroniza imediatamente com o progresso já salvo (ex: o jogador voltou do porão pro
        // escritório e os botões de inventário/pause já ensinados precisam continuar visíveis).
        HandleEtapaAlterada(TutorialManager.Instance.EtapaAtualId);
    }

    void Update()
    {
        // No PC, Enter/Espaço equivalem ao clique em Continuar (o texto da etapa diz isso).
        if (painelPopup == null || !painelPopup.activeSelf || DispositivoDeControle.EhToque) return;
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

    // ===== Dicas contextuais (fora do roteiro, ex: Fato x Boato) =====

    private void HandleDicaSolicitada(string texto)
    {
        dicaPendente = texto;
        MostrarDicaSePossivel();
    }

    private bool TelaOcupada() =>
        (dialogoObservado != null && dialogoObservado.IsDialogueActive) || CutsceneLegendas.EmExibicao;

    // A dica recebida no fim de uma conversa espera o diálogo/cutscene sair da tela, como as etapas.
    private bool MostrarDicaSePossivel()
    {
        if (string.IsNullOrEmpty(dicaPendente) || painelPopup == null || TelaOcupada()) return false;

        mostrandoDica = true;
        dicaExibida = dicaPendente;
        if (imagemIcone != null) imagemIcone.gameObject.SetActive(false);
        if (textoMensagem != null) textoMensagem.text = DispositivoDeControle.Substituir(dicaPendente);
        DefinirTitulo("Dica");
        dicaPendente = null;
        MontarGlifos(ControleTutorial.Nenhum);
        if (botaoPular != null) botaoPular.gameObject.SetActive(false);
        painelPopup.SetActive(true);
        PosicionarPopup();
        return true;
    }

    // O título ("Titulo", criado pelo TutorialPopupBuilder) diferencia etapa do roteiro de dica avulsa.
    private void DefinirTitulo(string titulo)
    {
        Transform t = painelPopup != null ? painelPopup.transform.Find("Titulo") : null;
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp != null) tmp.text = titulo;
    }

    // Pular pede um segundo toque: no celular um toque sem querer no canto do popup apagaria o tutorial inteiro.
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

    private void DefinirTextoPular(string texto)
    {
        TextMeshProUGUI rotulo = botaoPular != null ? botaoPular.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (rotulo != null) rotulo.text = texto;
    }

    private static void AumentarAreaDeToque(Button botao)
    {
        Graphic alvo = botao != null ? botao.targetGraphic : null;
        if (alvo == null) return;
        // (esquerda, baixo, direita, cima): valores negativos aumentam a área clicável.
        if (alvo.raycastPadding == Vector4.zero) alvo.raycastPadding = new Vector4(-16f, -12f, -16f, -24f);
    }

    // Layout do rodapé como veio do prefab (TutorialPopupBuilder); PosicionarPopup parte sempre dele.
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

        AfastarDoInventario(popup);
        EvitarControlesDeToque();
    }

    // Com a prensa aberta (etapas do porão) o inventário ocupa o centro da tela de cima a baixo, e o popup do
    // rodapé cobria justamente a lista onde a etapa manda clicar na pista. Nesse caso ele vai para a faixa
    // livre à esquerda do inventário (abaixo da HUD) e volta ao rodapé quando o inventário fecha.
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
        if (livre < LarguraMinimaAoLado) return; // tela estreita demais: melhor sobrepor do que espremer o texto

        popup.anchorMin = popup.anchorMax = popup.pivot = Vector2.zero;
        popup.anchoredPosition = new Vector2(MargemTela, MargemTela);
        // Mais estreito, o texto quebra em mais linhas: um pouco mais de altura para não encolher a fonte.
        popup.sizeDelta = new Vector2(Mathf.Min(tamanhoOriginal.x, livre), tamanhoOriginal.y + 60f);
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

    // Em telas mais quadradas (tablets 4:3) o popup do rodapé cobria o botão Inventário — justamente na etapa
    // que pede para tocá-lo. Se o popup encostar num controle de toque visível, ele sobe até ficar acima dele.
    private static readonly string[] ControlesDeToque = { "JoystickBG", "InteractButton", "InventoryButton" };

    private void EvitarControlesDeToque()
    {
        RectTransform popup = painelPopup != null ? painelPopup.transform as RectTransform : null;
        if (popup == null || MobileControlsManager.Instance == null || !DispositivoDeControle.EhToque) return;
        float yBase = popup.anchoredPosition.y; // PosicionarPopup já devolveu o popup à posição de partida

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

    // O popup sai da frente enquanto uma conversa ou cutscene estiver na tela e volta depois,
    // se a etapa ainda for a mesma e o jogador não a tiver dispensado.
    private void HandleDialogoComecou()
    {
        // Dica interrompida por uma conversa volta a ficar pendente (reaparece quando a conversa acabar).
        if (mostrandoDica) { dicaPendente = dicaExibida; mostrandoDica = false; }
        if (painelPopup != null) painelPopup.SetActive(false);
    }
    private void HandleDialogoTerminou() => ReexibirSePrecisar();
    private void HandleCutsceneTerminou() => ReexibirSePrecisar();

    private void ReexibirSePrecisar()
    {
        if (mostrandoDica || MostrarDicaSePossivel()) return;
        if (popupDispensado || TutorialManager.Instance == null || CutsceneLegendas.EmExibicao) return;
        TutorialStep etapa = TutorialManager.Instance.ObterEtapa(etapaExibidaAtualmente);
        if (etapa != null && etapa.etapaId == TutorialManager.Instance.EtapaAtualId) MostrarPopup(etapa);
    }

    // Avisa no Console se algum "Etapa Id" digitado em Revelacoes não bate com nenhuma etapa
    // real do TutorialManager — a causa mais comum de "o botão não aparece" é um erro de digitação aqui.
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

        // Uma dica na tela tem prioridade: a etapa nova aparece quando o jogador fechar a dica.
        if (mostrandoDica) return;

        TutorialStep etapa = TutorialManager.Instance != null ? TutorialManager.Instance.ObterEtapa(etapaId) : null;
        if (etapa == null)
        {
            if (painelPopup != null) painelPopup.SetActive(false);
            return;
        }

        // Numa conversa ou cutscene, espera terminar (ReexibirSePrecisar) em vez de aparecer por cima.
        bool dialogoAberto = dialogoObservado != null && dialogoObservado.IsDialogueActive;
        if (dialogoAberto || CutsceneLegendas.EmExibicao) { if (painelPopup != null) painelPopup.SetActive(false); return; }

        MostrarPopup(etapa);
    }

    // Revela permanentemente (nunca esconde) tudo que pertence a uma etapa já alcançada — não só a etapa
    // exata que acabou de começar — para sobreviver a troca de cena (Jogo -> Porao -> Jogo de novo).
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

        if (imagemIcone != null)
        {
            imagemIcone.sprite = etapa.icone;
            imagemIcone.gameObject.SetActive(etapa.icone != null);
        }

        if (textoMensagem != null) textoMensagem.text = DispositivoDeControle.Substituir(etapa.mensagem);

        MontarGlifos(etapa.controle);
        if (botaoPular != null) botaoPular.gameObject.SetActive(true);
        confirmarPularAte = -1f;
        DefinirTextoPular(TextoPularPadrao);
        DefinirTitulo("Tutorial");

        painelPopup.SetActive(true);
        PosicionarPopup();
    }

    private void MontarGlifos(ControleTutorial controle)
    {
        if (containerGlifos == null) return;

        for (int i = containerGlifos.childCount - 1; i >= 0; i--)
        {
            // Destroy só age no fim do frame; desativar antes evita a linha antiga aparecer junto da nova.
            GameObject antigo = containerGlifos.GetChild(i).gameObject;
            antigo.SetActive(false);
            Destroy(antigo);
        }

        TMP_FontAsset fonte = fonteGlifos != null ? fonteGlifos : (textoMensagem != null ? textoMensagem.font : null);
        bool algum = false;
        if (controle == ControleTutorial.Principais)
        {
            // Três linhas no cartão: teclas menores para caberem na coluna do popup.
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
        containerGlifos.gameObject.SetActive(algum);
    }

    private void OnContinuarClicado()
    {
        if (mostrandoDica)
        {
            mostrandoDica = false;
            if (painelPopup != null) painelPopup.SetActive(false);
            ReexibirSePrecisar(); // se havia uma etapa esperando atrás da dica
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
            // Etapas que avançam por evento/cena: o botão só esconde a mensagem; o TutorialManager
            // continua esperando a ação real acontecer para avançar de fato.
            popupDispensado = true;
            if (painelPopup != null) painelPopup.SetActive(false);
        }
    }
}
