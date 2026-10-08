using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI talkText;
    [Tooltip("ScrollRect que envolve talkText. Usado só para voltar a rolagem ao topo a cada nova fala.")]
    [SerializeField] private ScrollRect talkScrollRect;
    [Tooltip("Botão invisível sobre a área de rolagem do texto: replica o clique-para-avançar do " +
             "ButtonAvancar (que fica coberto ali), já que arrastar/rolar ali precisa ficar livre pro ScrollRect.")]
    [SerializeField] private Button talkAdvanceButton;
    [Tooltip("ButtonAvancar (tela inteira). Fica selecionado no EventSystem durante a fala para o Enter " +
             "(UI/Submit) avançar, e é desligado durante a escolha para um clique fora das opções não confirmar nada.")]
    [SerializeField] private Button advanceButton;

    [Header("Sistema de Escolhas")]
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choicesContainer;
    [Tooltip("ScrollRect que envolve choicesContainer (mesma área da fala, usada na vez do protagonista).")]
    [SerializeField] private ScrollRect choicesScrollRect;

    [Header("Troca de turno")]
    [Tooltip("Nome exibido na aba enquanto o jogador escolhe a resposta. Falas cujo falante tenha esse " +
             "nome (ou o primeiro nome) também aparecem do lado do protagonista.")]
    [SerializeField] private string protagonistName = "Julien Valois";
    [Tooltip("Duração total do flip da arte do painel (metade fechando, metade abrindo).")]
    [SerializeField] private float flipDuration = 0.22f;

    private DialogueSystem dialogueSystem;
    public float speed = 10f;
    private bool open = false;
    private List<GameObject> activeButtons = new List<GameObject>();

    // Arte e aba do nome: o lado do protagonista é o espelho horizontal do lado do NPC.
    private Vector3 backgroundBaseScale;
    private Vector2 nameBasePosition;
    private HorizontalAlignmentOptions nameBaseAlignment;
    private CanvasGroup nameGroup, talkGroup, choicesGroup;

    private bool protagonistSide;  // lado exibido agora
    private bool showingChoices;   // conteúdo exibido agora: respostas (true) ou fala (false)
    private bool firstLinePending; // 1ª fala da conversa entra direto, sem flip
    private string lineName;
    private bool lineIsProtagonist;
    private Coroutine transition;
    private GameObject lastSelectedChoice;

    public bool IsTransitioning => transition != null;

    void Awake()
    {
        // ARQUITETURA BLINDADA: Busca o script irmão no mesmo GameObject.
        // Isso previne qualquer falha de "Race Condition" com Singletons.
        dialogueSystem = GetComponent<DialogueSystem>();

        if (dialogueSystem == null)
        {
            Debug.LogError("DialogueSystem não encontrado no mesmo GameObject que o DialogueUI!");
        }

        // Igual ao onClick do ButtonAvancar (clicar avança/pula a fala) — feito em código em vez de
        // ligação no Inspector porque esse botão fica por cima do ButtonAvancar só na área do texto
        // (precisa estar acima para o ScrollRect receber o scroll do mouse ali).
        if (talkAdvanceButton != null && dialogueSystem != null)
            talkAdvanceButton.onClick.AddListener(dialogueSystem.AdvanceDialogue);

        if (background != null)
        {
            backgroundBaseScale = background.rectTransform.localScale;
            backgroundBaseScale.x = Mathf.Abs(backgroundBaseScale.x);
        }
        if (nameText != null)
        {
            nameBasePosition = nameText.rectTransform.anchoredPosition;
            nameBaseAlignment = nameText.horizontalAlignment;
        }
        nameGroup = GetOrAddGroup(nameText);
        talkGroup = GetOrAddGroup(talkScrollRect);
        choicesGroup = GetOrAddGroup(choicesScrollRect);
        ApplySide(false);
        ShowContent(false);
        SetContentAlpha(1f);
    }

    void OnEnable()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted += HandleDialogueStarted;
            dialogueSystem.OnDialogueLineStarted += HandleDialogueLineStarted;
            dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
            dialogueSystem.OnChoicesAvailable += HandleChoicesAvailable;
            dialogueSystem.OnChoicesCleared += HandleChoicesCleared;
            dialogueSystem.OnConfirmChoiceRequested += ConfirmarResposta;
        }
    }

    void OnDisable()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted -= HandleDialogueStarted;
            dialogueSystem.OnDialogueLineStarted -= HandleDialogueLineStarted;
            dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
            dialogueSystem.OnChoicesAvailable -= HandleChoicesAvailable;
            dialogueSystem.OnChoicesCleared -= HandleChoicesCleared;
            dialogueSystem.OnConfirmChoiceRequested -= ConfirmarResposta;
        }
    }

    void Update()
    {
        if (background == null) return; // Segurança caso a imagem não esteja no Inspector

        if (open)
        {
            background.fillAmount = Mathf.Lerp(background.fillAmount, 1, speed * Time.deltaTime);
        }
        else
        {
            background.fillAmount = Mathf.Lerp(background.fillAmount, 0, speed * Time.deltaTime);
        }

        if (open && !IsTransitioning) KeepSelection();
    }

    // ===== Eventos do DialogueSystem =====

    private void HandleDialogueStarted()
    {
        // MakeChoice reinicia o DialogueData e dispara OnDialogueStarted de novo no meio da conversa:
        // com o painel já aberto isso é continuação, não reabre a caixa.
        if (open) return;

        Enable();
        firstLinePending = true;

        // Desliga o raycast dos controles mobile enquanto o diálogo está aberto,
        // para que o toque chegue nos botões de escolha em vez de ser interceptado.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    private void HandleDialogueLineStarted(string name, string text)
    {
        lineName = name;
        lineIsProtagonist = IsProtagonist(name);

        // No meio de um flip, quem aplica a fala é a própria transição (no ponto em que o painel está fechado).
        if (IsTransitioning) return;

        if (firstLinePending || lineIsProtagonist == protagonistSide)
        {
            firstLinePending = false;
            ShowLine();
            SelectForCurrentState();
        }
        else
        {
            // Falas seguidas de falantes de lados diferentes (ex: pensamento do protagonista) também viram o painel.
            StartTransition(lineIsProtagonist, ShowLine);
        }
    }

    private void HandleDialogueEnded()
    {
        if (transition != null) { StopCoroutine(transition); transition = null; }

        DeselectOwnObjects(); // antes de Disable(), que limpa a lista de opções usada para reconhecê-las
        Disable();
        if (background != null) background.fillAmount = 0f;
        ApplySide(false);
        ShowContent(false);
        SetContentAlpha(1f);
        firstLinePending = false;

        // Devolve o raycast pros controles mobile assim que o diálogo fecha.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(true);
    }

    private void HandleChoicesAvailable(List<Choice> choices)
    {
        if (dialogueSystem == null) return;
        ClearChoices();
        foreach (Choice choice in choices)
        {
            DialogueData nextTalk = choice.nextDialogue;
            CreateChoiceButton(choice.choiceText, () => OnChoiceClicked(nextTalk));
        }
        LinkChoiceNavigation();

        // Vez do protagonista: o painel vira e as respostas ocupam o lugar da fala.
        StartTransition(true, ShowChoices);
    }

    private void HandleChoicesCleared() { ClearChoices(); }

    // ===== Ações comuns (teclado, mouse, toque ou botão virtual chamam estas) =====

    /// <summary>Confirma a resposta destacada. Chamado pelo avanço do DialogueSystem durante a escolha
    /// (tecla Interagir) e disponível para um botão virtual no futuro.</summary>
    public void ConfirmarResposta()
    {
        if (!open || !showingChoices || IsTransitioning || activeButtons.Count == 0) return;

        GameObject alvo = SelectedChoice() ?? (IsValidChoice(lastSelectedChoice) ? lastSelectedChoice : activeButtons[0]);
        Button botao = alvo.GetComponent<Button>();
        if (botao != null && botao.interactable) botao.onClick.Invoke();
    }

    private void OnChoiceClicked(DialogueData nextTalk)
    {
        if (!showingChoices || IsTransitioning || dialogueSystem == null) return;

        // Sem próxima fala a conversa termina: fecha direto, sem flip.
        if (nextTalk == null || nextTalk.talkScript == null || nextTalk.talkScript.Count == 0)
        {
            dialogueSystem.MakeChoice(nextTalk);
            return;
        }

        // MakeChoice só roda com o painel fechado no meio do flip: a digitação e o áudio da próxima fala
        // começam quando a caixa já está do lado de quem fala.
        bool proximoLado = IsProtagonist(nextTalk.talkScript[0].name);
        StartTransition(proximoLado, () =>
        {
            dialogueSystem.MakeChoice(nextTalk);
            ShowLine();
        });
    }

    // ===== Estados visuais =====

    private void ShowLine()
    {
        ApplySide(lineIsProtagonist);
        SetName(lineName);
        ShowContent(false);
        // Cada fala nova começa lida a partir do topo, mesmo que a anterior tenha ficado rolada
        // para baixo. verticalNormalizedPosition é proporcional (0-1), então isso vale tanto para
        // falas curtas quanto para falas longas, independente de quando o layout for recalculado.
        if (talkScrollRect != null) talkScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ShowChoices()
    {
        ApplySide(true);
        SetName(protagonistName);
        ShowContent(true);
        lastSelectedChoice = null;
        if (choicesScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            choicesScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ApplySide(bool protagonista)
    {
        protagonistSide = protagonista;
        if (background != null)
        {
            Vector3 escala = backgroundBaseScale;
            escala.x *= protagonista ? -1f : 1f;
            background.rectTransform.localScale = escala;
        }
        if (nameText != null)
        {
            // Só a posição e o alinhamento do nome acompanham a aba espelhada; o texto nunca é espelhado.
            nameText.rectTransform.anchoredPosition = protagonista
                ? new Vector2(-nameBasePosition.x, nameBasePosition.y)
                : nameBasePosition;
            nameText.horizontalAlignment = protagonista ? MirrorAlignment(nameBaseAlignment) : nameBaseAlignment;
        }
    }

    private void ShowContent(bool respostas)
    {
        showingChoices = respostas;
        SetGroupVisible(talkGroup, !respostas);
        SetGroupVisible(choicesGroup, respostas);
        // Durante a escolha o clique na tela não avança: só as opções respondem.
        if (advanceButton != null) advanceButton.interactable = !respostas;
    }

    private void StartTransition(bool ladoFinal, Action noMeio)
    {
        if (transition != null) StopCoroutine(transition);
        transition = StartCoroutine(Transition(ladoFinal, noMeio));
    }

    private IEnumerator Transition(bool ladoFinal, Action noMeio)
    {
        bool vira = ladoFinal != protagonistSide;
        float metade = Mathf.Max(0.01f, flipDuration * 0.5f);
        SetInteractable(false);

        // 1ª metade: conteúdo some (antes da arte ficar estreita) e, se trocar de lado, a arte fecha no eixo X.
        for (float t = 0f; t < 1f; t += Time.deltaTime / metade)
        {
            SetContentAlpha(1f - Mathf.Clamp01(t / 0.6f));
            if (vira) SetBackgroundWidth(1f - Ease(t));
            yield return null;
        }
        SetContentAlpha(0f);
        if (vira) SetBackgroundWidth(0f);

        noMeio?.Invoke();
        if (!open) { transition = null; yield break; } // a ação encerrou a conversa
        SetInteractable(false); // ShowContent reativa o grupo visível; continua travado até o fim do flip
        SetContentAlpha(0f);

        // 2ª metade: a arte abre já espelhada para o novo lado e o conteúdo novo aparece com ela quase aberta.
        for (float t = 0f; t < 1f; t += Time.deltaTime / metade)
        {
            SetContentAlpha(Mathf.Clamp01((t - 0.4f) / 0.6f));
            if (vira) SetBackgroundWidth(Ease(t));
            yield return null;
        }
        SetContentAlpha(1f);
        ApplySide(protagonistSide);

        transition = null;
        SetInteractable(true);
        SelectForCurrentState();
    }

    // Largura relativa (0-1) da arte, preservando o lado atual (sinal da escala).
    private void SetBackgroundWidth(float fator)
    {
        if (background == null) return;
        Vector3 escala = backgroundBaseScale;
        escala.x *= fator * (protagonistSide ? -1f : 1f);
        background.rectTransform.localScale = escala;
    }

    private void SetContentAlpha(float alpha)
    {
        if (nameGroup != null) nameGroup.alpha = alpha;
        if (talkGroup != null && !showingChoices) talkGroup.alpha = alpha;
        if (choicesGroup != null && showingChoices) choicesGroup.alpha = alpha;
    }

    // Trava cliques e confirmações enquanto o painel vira (evita escolher duas vezes).
    private void SetInteractable(bool ativo)
    {
        if (talkGroup != null) talkGroup.interactable = ativo && !showingChoices;
        if (choicesGroup != null) choicesGroup.interactable = ativo && showingChoices;
        if (advanceButton != null) advanceButton.interactable = ativo && !showingChoices;
    }

    // ===== Seleção (Enter = UI/Submit e setas = UI/Navigate do Input System agem sobre o selecionado) =====

    private void SelectForCurrentState()
    {
        EventSystem es = EventSystem.current;
        if (es == null || !open || PauseAberto()) return;

        if (showingChoices)
        {
            if (activeButtons.Count > 0) es.SetSelectedGameObject(activeButtons[0]);
        }
        else if (advanceButton != null)
        {
            es.SetSelectedGameObject(advanceButton.gameObject);
        }
    }

    // Um clique no vazio tira a seleção; sem ela o Enter pararia de funcionar. Com o pause aberto, os
    // botões do diálogo não podem ficar selecionados, senão o Enter do menu avançaria a conversa por trás.
    private void KeepSelection()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        if (PauseAberto()) { DeselectOwnObjects(); return; }

        GameObject selecionado = es.currentSelectedGameObject;
        if (showingChoices && IsValidChoice(selecionado))
        {
            if (selecionado != lastSelectedChoice) { lastSelectedChoice = selecionado; ScrollToChoice(selecionado); }
            return;
        }
        if (selecionado != null) return;

        if (showingChoices && IsValidChoice(lastSelectedChoice)) es.SetSelectedGameObject(lastSelectedChoice);
        else SelectForCurrentState();
    }

    private void DeselectOwnObjects()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;
        GameObject selecionado = es.currentSelectedGameObject;
        if (selecionado == null) return;
        if ((advanceButton != null && selecionado == advanceButton.gameObject) || activeButtons.Contains(selecionado))
            es.SetSelectedGameObject(null);
    }

    // Mantém a opção destacada pelo teclado dentro da área visível quando as respostas não cabem.
    private void ScrollToChoice(GameObject opcao)
    {
        if (choicesScrollRect == null || choicesScrollRect.content == null) return;
        RectTransform content = choicesScrollRect.content;
        RectTransform viewport = choicesScrollRect.viewport != null ? choicesScrollRect.viewport : (RectTransform)choicesScrollRect.transform;
        float sobra = content.rect.height - viewport.rect.height;
        if (sobra <= 0f) return;

        RectTransform alvo = (RectTransform)opcao.transform;
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(content, alvo);
        float topo = -b.max.y;    // distância do topo do content até o topo da opção
        float base_ = -b.min.y;   // ... até a base da opção
        float rolado = (1f - choicesScrollRect.verticalNormalizedPosition) * sobra;
        if (topo < rolado) rolado = topo;
        else if (base_ > rolado + viewport.rect.height) rolado = base_ - viewport.rect.height;
        choicesScrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(rolado / sobra);
    }

    private GameObject SelectedChoice()
    {
        EventSystem es = EventSystem.current;
        GameObject selecionado = es != null ? es.currentSelectedGameObject : null;
        return IsValidChoice(selecionado) ? selecionado : null;
    }

    private bool IsValidChoice(GameObject go) => go != null && activeButtons.Contains(go);

    private static bool PauseAberto() => PauseMenu.Instance != null && PauseMenu.Instance.IsOpen;

    // ===== API existente =====

    public void SetName(string name) { nameText.text = name; }

    public void Enable()
    {
        if (background != null) background.fillAmount = 0;
        open = true;
    }

    public void Disable()
    {
        open = false;
        nameText.text = "";
        talkText.text = "";
        ClearChoices();
    }

    public void CreateChoiceButton(string text, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject newButton = Instantiate(choiceButtonPrefab, choicesContainer);
        activeButtons.Add(newButton);
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
        newButton.GetComponent<Button>().onClick.AddListener(onClickAction);
    }

    public void ClearChoices()
    {
        foreach (GameObject btn in activeButtons) { Destroy(btn); }
        activeButtons.Clear();
        lastSelectedChoice = null;
    }

    // ===== Utilidades =====

    // Setas navegam só entre as respostas (a navegação automática pularia para botões de outras HUDs).
    private void LinkChoiceNavigation()
    {
        for (int i = 0; i < activeButtons.Count; i++)
        {
            Button botao = activeButtons[i].GetComponent<Button>();
            if (botao == null) continue;
            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
            if (i > 0) nav.selectOnUp = activeButtons[i - 1].GetComponent<Button>();
            if (i < activeButtons.Count - 1) nav.selectOnDown = activeButtons[i + 1].GetComponent<Button>();
            botao.navigation = nav;
        }
    }

    private bool IsProtagonist(string falante)
    {
        if (string.IsNullOrWhiteSpace(falante) || string.IsNullOrWhiteSpace(protagonistName)) return false;
        string nome = falante.Trim();
        string completo = protagonistName.Trim();
        string primeiro = completo.Split(' ')[0];
        return string.Equals(nome, completo, StringComparison.OrdinalIgnoreCase)
            || string.Equals(nome, primeiro, StringComparison.OrdinalIgnoreCase);
    }

    private static HorizontalAlignmentOptions MirrorAlignment(HorizontalAlignmentOptions a)
    {
        if (a == HorizontalAlignmentOptions.Left) return HorizontalAlignmentOptions.Right;
        if (a == HorizontalAlignmentOptions.Right) return HorizontalAlignmentOptions.Left;
        return a;
    }

    private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    private static CanvasGroup GetOrAddGroup(Component alvo)
    {
        if (alvo == null) return null;
        CanvasGroup g = alvo.GetComponent<CanvasGroup>();
        return g != null ? g : alvo.gameObject.AddComponent<CanvasGroup>();
    }

    private static void SetGroupVisible(CanvasGroup g, bool visivel)
    {
        if (g == null) return;
        g.alpha = visivel ? 1f : 0f;
        g.interactable = visivel;
        g.blocksRaycasts = visivel;
    }
}
