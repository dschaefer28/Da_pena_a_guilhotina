using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI talkText;

    [Header("Sistema de Escolhas")]
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choicesContainer;

    private DialogueSystem dialogueSystem;
    public float speed = 10f;
    private bool open = false;
    private List<GameObject> activeButtons = new List<GameObject>();

    void Awake()
    {
        // ARQUITETURA BLINDADA: Busca o script irmão no mesmo GameObject.
        // Isso previne qualquer falha de "Race Condition" com Singletons.
        dialogueSystem = GetComponent<DialogueSystem>();

        if (dialogueSystem == null)
        {
            Debug.LogError("DialogueSystem não encontrado no mesmo GameObject que o DialogueUI!");
        }
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
    }

    private void HandleDialogueStarted()
    {
        Enable();

        // Desliga o raycast dos controles mobile enquanto o diálogo está aberto,
        // para que o toque chegue nos botões de escolha em vez de ser interceptado.
        if (MobileControlsManager.Instance != null)
            MobileControlsManager.Instance.SetControlsInteractable(false);
    }

    private void HandleDialogueLineStarted(string name, string text)
    {
        SetName(name);
    }

    private void HandleDialogueEnded()
    {
        Disable();
        if (background != null) background.fillAmount = 0f;

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
            CreateChoiceButton(choice.choiceText, () => dialogueSystem.MakeChoice(nextTalk));
        }
    }

    private void HandleChoicesCleared() { ClearChoices(); }
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
    }
}