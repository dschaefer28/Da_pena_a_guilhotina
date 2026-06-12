using UnityEngine;

public enum STATE {
    DISABLED,
    WAITING,
    TYPING,
    CHOOSING // Novo estado adicionado!
}

public class DialogueSystem : MonoBehaviour {

    public DialogueData dialogueData;

    int currentText = 0;
    bool finished = false;

    TypeTextAnimation typeText;
    DialogueUI dialogueUI;

    STATE state;

    void Awake() {
        typeText = FindObjectOfType<TypeTextAnimation>();
        dialogueUI = FindObjectOfType<DialogueUI>();
        typeText.TypeFinished = OnTypeFinished;
    }

    void Start() {
        state = STATE.DISABLED;
    }

    void Update() {
        if(state == STATE.DISABLED) return;

        switch(state) {
            case STATE.WAITING:
                Waiting();
                break;
            case STATE.TYPING:
                Typing();
                break;
            // No estado CHOOSING, não fazemos nada no Update, esperamos o clique do botão.
        }
    }

    public void Next() {

        // --- TRAVA DE SEGURANÇA: Se não tiver fala na lista, encerra o diálogo. ---
        if (dialogueData.talkScript == null || dialogueData.talkScript.Count == 0) {
            Debug.LogWarning("Cuidado: O ScriptableObject carregado não possui falas!");
            EndDialogue();
            return;
        }
        // ------------------------------------------------------------------------------

        if(currentText == 0) {
            dialogueUI.Enable();
        }

        dialogueUI.SetName(dialogueData.talkScript[currentText].name);
        typeText.fullText = dialogueData.talkScript[currentText].text;

        currentText++;
        if(currentText >= dialogueData.talkScript.Count) finished = true;

        typeText.StartTyping();
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
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E)) {
            if(!finished) {
                Next();
            } else {
                EndDialogue();
            }
        }
    }

    void Typing() {
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E)) {
            typeText.Skip();
            OnTypeFinished(); // Chama a verificação direto para não pular as escolhas acidentalmente
        }
    }

    void EndDialogue() {
        dialogueUI.Disable();
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
    }

    // --- NOVA LÓGICA DE ESCOLHAS ---

    void SetupChoices(Dialogue dialogue) {
        dialogueUI.ClearChoices(); // Limpa botões velhos, por garantia

        foreach (Choice choice in dialogue.choices) {
            // Guarda a referência da próxima conversa em uma variável local para o evento do botão
            DialogueData nextTalk = choice.nextDialogue; 
            
            // Cria o botão passando o texto e o que acontece quando clica
            dialogueUI.CreateChoiceButton(choice.choiceText, () => MakeChoice(nextTalk));
        }
    }

    public void MakeChoice(DialogueData nextTalkData) {
        dialogueUI.ClearChoices(); // Limpa os botões da tela

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