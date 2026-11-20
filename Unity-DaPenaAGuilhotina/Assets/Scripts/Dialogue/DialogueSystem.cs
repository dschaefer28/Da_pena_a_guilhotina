using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public enum STATE
{
    DISABLED,
    WAITING,
    TYPING,
    AWAITING_REPLY, // fala com escolhas já exibida por inteiro; o próximo avanço passa a vez ao protagonista
    CHOOSING
}

public class DialogueSystem : MonoBehaviour
{
    public DialogueData dialogueData;

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<string, string> OnDialogueLineStarted;
    public event Action<List<Choice>> OnChoicesAvailable;
    public event Action OnChoicesCleared;
    // Avançar durante a escolha pede à HUD que confirme: só ela sabe qual opção está destacada.
    public event Action OnConfirmChoiceRequested;

    public bool IsDialogueActive { get; private set; }

    int currentText = 0;
    bool finished = false;
    TypeTextAnimation typeText;
    STATE state;
    
    private FMOD.Studio.EventInstance currentAudioInstance;

    void Awake() 
    {
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

    void Start() 
    {
        state = STATE.DISABLED;
        IsDialogueActive = false;
    }

    public void AdvanceDialogue()
    {
        // "ButtonAvancar" cobre a tela e fica sempre ativo: sem diálogo, o clique não pode reabrir a conversa.
        if (state == STATE.DISABLED)
        {
            // Destrava se Next() foi interrompido (ex.: exceção de áudio) com a flag ligada.
            if (IsDialogueActive) EndDialogue();
            return;
        }
        if (state == STATE.TYPING) { typeText.Skip(); OnTypeFinished(); return; }
        if (state == STATE.WAITING)
        {
            if (!finished) Next();
            else EndDialogue();
            return;
        }
        if (state == STATE.AWAITING_REPLY)
        {
            state = STATE.CHOOSING;
            SetupChoices(dialogueData.talkScript[currentText - 1]);
            return;
        }
        if (state == STATE.CHOOSING) { OnConfirmChoiceRequested?.Invoke(); return; }
    }

    public void Next()
    {
        if (dialogueData == null || dialogueData.talkScript == null || dialogueData.talkScript.Count == 0)
        {
            Debug.LogWarning("ScriptableObject de diálogo vazio!");
            // Sem EndDialogue(): OnDialogueStarted não disparou, e OnDialogueEnded faria o tutorial avançar sozinho.
            state = STATE.DISABLED;
            IsDialogueActive = false;
            return;
        }

        IsDialogueActive = true;

        if (currentText == 0 && !AudioSeguro.BanksCarregados)
        {
            Debug.LogWarning("[DialogueSystem] FMOD ainda não carregou todos os banks — " +
                             "o áudio da primeira fala pode não tocar.");
        }

        StopCurrentAudio();

        if(currentText == 0) OnDialogueStarted?.Invoke();

        Dialogue currentDialogue = dialogueData.talkScript[currentText];
        string speakerName = currentDialogue.name;
        string speakerText = currentDialogue.text;

        // Áudio opcional: se o FMOD falhar, a conversa segue muda (AudioSeguro nunca lança exceção).
        if (AudioSeguro.TentarCriar(currentDialogue.dialogueAudio, out currentAudioInstance))
        {
            FMOD.RESULT startResult = currentAudioInstance.start();
            if (startResult != FMOD.RESULT.OK)
                Debug.LogWarning($"[DialogueSystem] start() do áudio da fala '{speakerName}' " +
                                 $"retornou {startResult}.");

            currentAudioInstance.release();
        }

        OnDialogueLineStarted?.Invoke(speakerName, speakerText);
        currentText++;
        if(currentText >= dialogueData.talkScript.Count) finished = true;
        state = STATE.TYPING;
    }

    void OnTypeFinished() 
    {
        StopCurrentAudio();
        Dialogue currentDialogue = dialogueData.talkScript[currentText - 1];
        if (currentDialogue.choices != null && currentDialogue.choices.Count > 0)
        {
            // Respostas só no próximo avanço, para o jogador ler a fala inteira.
            state = STATE.AWAITING_REPLY;
        }
        else 
        {
            state = STATE.WAITING;
        }
    }

    void EndDialogue()
    {
        StopCurrentAudio();
        // Estado consistente ANTES de avisar: os ouvintes precisam ver IsDialogueActive == false.
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;

        // Ouvintes isolados: o NPC que entrega a pista se inscreve por último e não pode ser barrado por erro anterior.
        Action fim = OnDialogueEnded;
        if (fim == null) return;
        foreach (Action ouvinte in fim.GetInvocationList())
        {
            try { ouvinte(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    void StopCurrentAudio()
    {
        AudioSeguro.PararELiberar(ref currentAudioInstance);
    }

    void SetupChoices(Dialogue dialogue) 
    {
        OnChoicesCleared?.Invoke();
        OnChoicesAvailable?.Invoke(dialogue.choices);
    }

    public void MakeChoice(DialogueData nextTalkData) 
    {
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