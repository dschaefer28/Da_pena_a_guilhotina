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
    // Avançar durante a escolha (tecla Interagir, botão virtual...) pede à HUD que confirme a opção
    // selecionada: só ela sabe qual resposta está destacada.
    public event Action OnConfirmChoiceRequested;

    public bool IsDialogueActive { get; private set; }

    int currentText = 0;
    bool finished = false;
    TypeTextAnimation typeText;
    STATE state;
    
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

    void Start() 
    {
        state = STATE.DISABLED;
        IsDialogueActive = false;
    }

    public void AdvanceDialogue()
    {
        // Não faz nada quando não há diálogo em andamento: o botão "ButtonAvancar" do Canvas cobre a
        // tela inteira e fica sempre ativo (mesmo sem diálogo aberto), então qualquer clique na tela
        // chegava aqui. Antes disso chamava Next() e reabria a última conversa do zero a cada clique.
        if (state == STATE.DISABLED)
        {
            // Segurança: se algo interrompeu Next() no meio (exceção de áudio, por exemplo), o
            // diálogo nunca chegou a abrir mas a flag ficou ligada — e toda interação do jogador
            // passava a cair aqui sem fazer nada. Destrava.
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
            // Não chama EndDialogue() aqui: como o diálogo nunca chegou a começar de verdade
            // (OnDialogueStarted nunca disparou), avisar "terminou" (OnDialogueEnded) confundia
            // quem escuta esse evento — o tutorial, por exemplo, avançava sozinho com essa
            // interação vazia antes da conversa real acontecer.
            state = STATE.DISABLED;
            IsDialogueActive = false;
            return;
        }

        IsDialogueActive = true;

        // FMOD: se os banks ainda não terminaram de carregar, a 1ª fala costuma sair muda
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

        // Áudio da fala é opcional: se o FMOD falhar (banks ausentes, evento renomeado, sistema não
        // iniciado), a conversa segue muda em vez de travar o jogo (AudioSeguro nunca lança exceção).
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
            // As respostas só aparecem no próximo avanço: primeiro o jogador lê a fala inteira.
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
        // Estado fica consistente ANTES de avisar os ouvintes: quem reage ao fim do diálogo (tutorial,
        // popups) precisa ver IsDialogueActive == false, senão acha que ainda há conversa na tela.
        state = STATE.DISABLED;
        currentText = 0;
        finished = false;
        IsDialogueActive = false;
        OnDialogueEnded?.Invoke();
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