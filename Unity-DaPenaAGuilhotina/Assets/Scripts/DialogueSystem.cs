using System;
using System.Collections.Generic;
using UnityEngine;

public enum STATE {
    DISABLED,
    WAITING,
    TYPING,
    CHOOSING 
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

    // === NOVO: Guarda a instância do áudio atual para podermos pará-lo se necessário ===
    private FMOD.Studio.EventInstance currentAudioInstance;

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
    }

    public void Next() {
        Debug.Log("PASSO 1: O NPC recebeu o comando de interação!");
        IsDialogueActive = true;

        if (dialogueData.talkScript == null || dialogueData.talkScript.Count == 0) {
            Debug.LogWarning("Cuidado: O ScriptableObject carregado não possui falas!");
            EndDialogue();
            return;
        }

        // === NOVO: Para o áudio da fala anterior antes de tocar a próxima ===
        StopCurrentAudio();

        if(currentText == 0) {
            OnDialogueStarted?.Invoke();
        }

        // Pega as informações do diálogo atual
        Dialogue currentDialogue = dialogueData.talkScript[currentText];
        string speakerName = currentDialogue.name;
        string speakerText = currentDialogue.text;
        
        // === NOVO: Toca o áudio se ele tiver sido configurado no FMOD ===
        if (!currentDialogue.dialogueAudio.IsNull) {
            currentAudioInstance = FMODUnity.RuntimeManager.CreateInstance(currentDialogue.dialogueAudio);
            currentAudioInstance.start();
            currentAudioInstance.release(); // Libera a memória quando o som acabar sozinho
        }

        OnDialogueLineStarted?.Invoke(speakerName, speakerText);

        currentText++;
        if(currentText >= dialogueData.talkScript.Count) finished = true;

        state = STATE.TYPING;
    }

    void OnTypeFinished() {
    StopCurrentAudio();

    Dialogue currentDialogue = dialogueData.talkScript[currentText - 1];

    if (currentDialogue.choices != null && currentDialogue.choices.Count > 0) {
        state = STATE.CHOOSING;
        SetupChoices(currentDialogue);
    } else {
        state = STATE.WAITING;
    }
}

    void EndDialogue() {
        StopCurrentAudio();

        OnDialogueEnded?.Invoke();
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;
    }

    void StopCurrentAudio() {
        if (currentAudioInstance.isValid()) {
            currentAudioInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentAudioInstance.release();
        }
    }

    void SetupChoices(Dialogue dialogue) {
        OnChoicesCleared?.Invoke();
        OnChoicesAvailable?.Invoke(dialogue.choices);
    }

    public void MakeChoice(DialogueData nextTalkData) {
        OnChoicesCleared?.Invoke();

        if (nextTalkData != null) {
            dialogueData = nextTalkData;
            currentText = 0;
            finished = false;
            Next();
        } else {
            EndDialogue();
        }
    }
}