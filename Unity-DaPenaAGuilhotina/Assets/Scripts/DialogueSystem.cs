using System;
using System.Collections.Generic;
using UnityEngine;

public enum STATE {
    DISABLED,
    WAITING,
    TYPING,
    CHOOSING // Novo estado adicionado!
}

public class DialogueSystem : MonoBehaviour {

    public DialogueData dialogueData;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<string, string> OnDialogueLineStarted;
    public event Action<List<Choice>> OnChoicesAvailable;
    public event Action OnChoicesCleared;

    public bool IsDialogueActive { get; private set; }

    int currentText = 0;
    bool finished = false;

    TypeTextAnimation typeText;

    STATE state;

    void Awake() {
        typeText = FindObjectOfType<TypeTextAnimation>();
        typeText.TypeFinished = OnTypeFinished;
    }

    void Start() {
        state = STATE.DISABLED;
        IsDialogueActive = false;
    }

    public void AdvanceDialogue()
    {
        if (state == STATE.DISABLED)
        {
            Next();
            return;
        }

        if (state == STATE.TYPING)
        {
            typeText.Skip();
            OnTypeFinished();
            return;
        }

        if (state == STATE.WAITING)
        {
            if (!finished)
            {
                Next();
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        // No estado CHOOSING, as escolhas devem ser tomadas pelos botões da UI.
    }

    public void Next() {
        IsDialogueActive = true;

        // --- TRAVA DE SEGURANÇA: Se não tiver fala na lista, encerra o diálogo. ---
        if (dialogueData.talkScript == null || dialogueData.talkScript.Count == 0) {
            Debug.LogWarning("Cuidado: O ScriptableObject carregado não possui falas!");
            EndDialogue();
            return;
        }
        // ------------------------------------------------------------------------------

        if(currentText == 0) {
            OnDialogueStarted?.Invoke();
        }

        string speakerName = dialogueData.talkScript[currentText].name;
        string speakerText = dialogueData.talkScript[currentText].text;
        OnDialogueLineStarted?.Invoke(speakerName, speakerText);

        currentText++;
        if(currentText >= dialogueData.talkScript.Count) finished = true;

        state = STATE.TYPING;
    }

    void OnTypeFinished() {
        // Verifica qual foi o último diálogo que acabou de ser digitado
        Dialogue currentDialogue = dialogueData.talkScript[currentText - 1];

        // Se esse diálogo tiver opções de escolha, vamos para o estado de escolher
        if (currentDialogue.choices != null && currentDialogue.choices.Count > 0) {
            state = STATE.CHOOSING;
            SetupChoices(currentDialogue);
        } else {
            state = STATE.WAITING; // Se não, espera o "Enter" normal
        }
    }

    void Waiting() {
        // Input direto removido. Use AdvanceDialogue() para avançar o diálogo reativamente.
    }

    void Typing() {
        // Input direto removido. Use AdvanceDialogue() para pular a digitação.
    }

    void EndDialogue() {
        OnDialogueEnded?.Invoke();
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;
    }

    // --- NOVA LÓGICA DE ESCOLHAS ---

    void SetupChoices(Dialogue dialogue) {
        OnChoicesCleared?.Invoke();
        OnChoicesAvailable?.Invoke(dialogue.choices);
    }

    public void MakeChoice(DialogueData nextTalkData) {
        OnChoicesCleared?.Invoke();

        if (nextTalkData != null) {
            // Se tiver uma próxima conversa, troca o ScriptableObject e reseta a leitura
            dialogueData = nextTalkData;
            currentText = 0;
            finished = false;
            Next();
        } else {
            // Se a opção não levar a lugar nenhum, apenas encerra o diálogo
            EndDialogue();
        }
    }
}