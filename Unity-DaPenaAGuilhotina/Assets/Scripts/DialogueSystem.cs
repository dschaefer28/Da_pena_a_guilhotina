using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public enum STATE
{
    DISABLED,
    WAITING,
    TYPING,
    CHOOSING
}

public class DialogueSystem : MonoBehaviour
{
    public DialogueData dialogueData;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action OnDialogueCancelled;
    public event Action<string, string> OnDialogueLineStarted;
    public event Action<List<Choice>> OnChoicesAvailable;
    public event Action OnChoicesCleared;

    public bool IsDialogueActive { get; private set; }

    int currentText = 0;
    bool finished = false;
    TypeTextAnimation typeText;
    STATE state = STATE.DISABLED;
    private Coroutine pendingAudio;
    
    private FMOD.Studio.EventInstance currentAudioInstance;

    void Awake() 
    {
        // ARQUITETURA BLINDADA: Atualiza a referência global para o sistema de diálogos desta cena
        if (GameManager.Instance != null)
        {
            GameManager.Instance.dialogueSystem = this;
        }

        typeText = GetComponent<TypeTextAnimation>();
        if(typeText != null)
        {
            typeText.TypeFinished = OnTypeFinished;
        }
        else
        {
            Debug.LogError("TypeTextAnimation não foi encontrado no GameObject DialogueManager!");
        }
    }

    public void AdvanceDialogue()
    {
        if (state == STATE.DISABLED) { Next(); return; }
        if (state == STATE.TYPING)
        {
            if (typeText != null) typeText.Skip();
            OnTypeFinished();
            return;
        }
        if (state == STATE.WAITING)
        {
            if (!finished) Next();
            else EndDialogue();
            return;
        }
    }

    public void Next() 
    {
        IsDialogueActive = true;
        if (dialogueData == null || dialogueData.talkScript == null || dialogueData.talkScript.Count == 0) 
        {
            Debug.LogWarning("ScriptableObject de diálogo vazio!");
            CancelDialogue();
            return;
        }

        // FMOD: se os banks ainda não terminaram de carregar, a 1ª fala costuma sair muda
        if (Application.isPlaying && currentText == 0 && !FMODUnity.RuntimeManager.HaveAllBanksLoaded)
        {
            Debug.LogWarning("[DialogueSystem] FMOD ainda não carregou todos os banks — " +
                             "o áudio da primeira fala pode não tocar.");
        }

        StopCurrentAudio();

        if(currentText == 0 && state == STATE.DISABLED) OnDialogueStarted?.Invoke();

        Dialogue currentDialogue = dialogueData.talkScript[currentText];
        string speakerName = currentDialogue.name;
        string speakerText = currentDialogue.text;
        
        if (!currentDialogue.dialogueAudio.IsNull)
        {
            pendingAudio = StartCoroutine(PlayLineAudio(currentDialogue.dialogueAudio));
        }

        currentText++;
        if(currentText >= dialogueData.talkScript.Count) finished = true;
        state = STATE.TYPING;
        OnDialogueLineStarted?.Invoke(speakerName, speakerText);

        if (typeText == null)
            OnTypeFinished();
    }

    void OnTypeFinished() 
    {
        if (dialogueData == null || dialogueData.talkScript == null || currentText <= 0 || currentText > dialogueData.talkScript.Count)
        {
            EndDialogue();
            return;
        }

        Dialogue currentDialogue = dialogueData.talkScript[currentText - 1];
        if (currentDialogue.choices != null && currentDialogue.choices.Count > 0) 
        {
            state = STATE.CHOOSING;
            SetupChoices(currentDialogue);
        } 
        else 
        {
            state = STATE.WAITING;
        }
    }

    void EndDialogue() 
    {
        StopCurrentAudio();
        OnChoicesCleared?.Invoke();
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;
        OnDialogueEnded?.Invoke();
    }

    public void CancelDialogue()
    {
        bool wasActive = IsDialogueActive;
        StopCurrentAudio();
        if (typeText != null) typeText.Skip();
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;
        OnChoicesCleared?.Invoke();
        if (wasActive) OnDialogueCancelled?.Invoke();
    }

    private IEnumerator PlayLineAudio(EventReference audio)
    {
        // Keep the request attached to this line; advancing cancels it.
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!RuntimeManager.HaveAllBanksLoaded && Time.realtimeSinceStartup < deadline)
            yield return null;

        try
        {
            currentAudioInstance = RuntimeManager.CreateInstance(audio);
            FMOD.RESULT result = currentAudioInstance.start();
            if (result != FMOD.RESULT.OK)
                Debug.LogWarning($"[DialogueSystem] Áudio não iniciou: {result}.");
        }
        catch (FMODUnity.EventNotFoundException exception)
        {
            Debug.LogWarning($"[DialogueSystem] {exception.Message}");
        }
        pendingAudio = null;
    }

    void StopCurrentAudio() 
    {
        if (pendingAudio != null) { StopCoroutine(pendingAudio); pendingAudio = null; }
        if (currentAudioInstance.isValid()) 
        {
            currentAudioInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentAudioInstance.release();
            currentAudioInstance = default;
        }
    }

    private void OnDestroy()
    {
        CancelDialogue();
    }

    void SetupChoices(Dialogue dialogue) 
    {
        OnChoicesCleared?.Invoke();
        OnChoicesAvailable?.Invoke(dialogue.choices);
    }

    public void MakeChoice(DialogueData nextTalkData) 
    {
        if (state != STATE.CHOOSING) return;
        OnChoicesCleared?.Invoke();
        if (nextTalkData != null) 
        {
            dialogueData = nextTalkData;
            currentText = 0;
            finished = false;
            Next();
        } 
        else 
        {
            EndDialogue();
        }
    }
}
