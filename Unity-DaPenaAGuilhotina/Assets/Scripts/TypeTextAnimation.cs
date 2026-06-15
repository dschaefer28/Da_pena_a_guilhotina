using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TypeTextAnimation : MonoBehaviour {

    public Action TypeFinished;

    public float typeDelay = 0.05f;
    public TextMeshProUGUI textObject;

    public string fullText;

    private DialogueSystem dialogueSystem;

    Coroutine coroutine;

    void OnEnable() {
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
        }

        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueLineStarted += HandleDialogueLineStarted;
        }
    }

    void OnDisable() {
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

    public void StartTyping() {
        coroutine = StartCoroutine(TypeText());
    }

    IEnumerator TypeText() {

        textObject.text = fullText;
        textObject.maxVisibleCharacters = 0;
        for(int i = 0; i <= textObject.text.Length; i++) {
            textObject.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typeDelay);
        }

        TypeFinished?.Invoke();

    }

    public void Skip() {

        StopCoroutine(coroutine);
        textObject.maxVisibleCharacters = textObject.text.Length;

    }

}