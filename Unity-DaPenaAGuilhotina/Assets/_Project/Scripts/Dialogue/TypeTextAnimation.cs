using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TypeTextAnimation : MonoBehaviour 
{
    public Action TypeFinished;
    public float typeDelay = 0.05f;
    public TextMeshProUGUI textObject;
    public string fullText;
    
    private DialogueSystem dialogueSystem;
    private Coroutine coroutine;

    void Awake()
    {
        // Busca automática do componente irmão
        dialogueSystem = GetComponent<DialogueSystem>();

        // Aplica a velocidade de texto salva no slider de Opções (chave/intervalo espelham
        // PauseMenu.OnTextSpeedSliderChanged — 0 = mais lento, 1 = mais rápido).
        if (PlayerPrefs.HasKey("opt_textspeed"))
        {
            float valor01 = PlayerPrefs.GetFloat("opt_textspeed");
            typeDelay = Mathf.Lerp(0.12f, 0.01f, valor01);
        }
    }

    void OnEnable() 
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueLineStarted += HandleDialogueLineStarted;
        }
    }

    void OnDisable() 
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueLineStarted -= HandleDialogueLineStarted;
        }
    }

    private void HandleDialogueLineStarted(string name, string text)
    {
        fullText = text;
        StartTyping();
    }

    public void StartTyping() 
    {
        coroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText() 
    {
        if (textObject == null) yield break;

        textObject.text = fullText;
        textObject.maxVisibleCharacters = 0;
        
        for(int i = 0; i <= textObject.text.Length; i++) 
        {
            textObject.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typeDelay);
        }
        TypeFinished?.Invoke();
    }

    public void Skip() 
    {
        if (coroutine != null) StopCoroutine(coroutine);
        if (textObject != null) textObject.maxVisibleCharacters = textObject.text.Length;
    }
}