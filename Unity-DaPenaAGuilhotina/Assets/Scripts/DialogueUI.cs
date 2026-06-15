using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour {

    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI talkText;
    
    [Header("Sistema de Escolhas")]
    [SerializeField] private GameObject choiceButtonPrefab; // O prefab do botão
    [SerializeField] private Transform choicesContainer;    // Onde os botões vão ser instanciados

    private DialogueSystem dialogueSystem;

    public float speed = 10f;
    bool open = false;
    
    // Lista para guardar os botões gerados e apagá-los depois
    private List<GameObject> activeButtons = new List<GameObject>();

    void OnEnable() {
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
        }

        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted += HandleDialogueStarted;
            dialogueSystem.OnDialogueLineStarted += HandleDialogueLineStarted;
            dialogueSystem.OnDialogueEnded += HandleDialogueEnded;
            dialogueSystem.OnChoicesAvailable += HandleChoicesAvailable;
            dialogueSystem.OnChoicesCleared += HandleChoicesCleared;
        }
    }

    void OnDisable() {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueStarted -= HandleDialogueStarted;
            dialogueSystem.OnDialogueLineStarted -= HandleDialogueLineStarted;
            dialogueSystem.OnDialogueEnded -= HandleDialogueEnded;
            dialogueSystem.OnChoicesAvailable -= HandleChoicesAvailable;
            dialogueSystem.OnChoicesCleared -= HandleChoicesCleared;
        }
    }

    void Update() {
        if(open) {
            background.fillAmount = Mathf.Lerp(background.fillAmount, 1, speed * Time.deltaTime);
        } else {
            background.fillAmount = Mathf.Lerp(background.fillAmount, 0, speed * Time.deltaTime);
        }
    }

    private void HandleDialogueStarted() {
        gameObject.SetActive(true);
        Enable();
    }

    private void HandleDialogueLineStarted(string name, string text) {
        SetName(name);
    }

    private void HandleDialogueEnded() {
        // Garante que o fillAmount zere imediatamente antes de desativar o objeto,
        // para evitar que o valor permaneça visível quando o GameObject é desativado.
        Disable();
        if (background != null) background.fillAmount = 0f;
        gameObject.SetActive(false);
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

    private void HandleChoicesCleared()
    {
        ClearChoices();
    }

    public void SetName(string name) {
        nameText.text = name;
    }

    public void Enable() {
        background.fillAmount = 0;
        open = true;
    }

    public void Disable() {
        open = false;
        nameText.text = "";
        talkText.text = "";
        ClearChoices();
    }

    // Cria um botão de escolha na tela
    public void CreateChoiceButton(string text, UnityEngine.Events.UnityAction onClickAction) {
        GameObject newButton = Instantiate(choiceButtonPrefab, choicesContainer);
        activeButtons.Add(newButton);

        // Ajusta o texto do botão (assume que o botão tem um TextMeshProUGUI no filho)
        newButton.GetComponentInChildren<TextMeshProUGUI>().text = text;

        // Adiciona a função de clique
        newButton.GetComponent<Button>().onClick.AddListener(onClickAction);
    }

    // Limpa os botões antigos
    public void ClearChoices() {
        foreach (GameObject btn in activeButtons) {
            Destroy(btn);
        }
        activeButtons.Clear();
    }
}