using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[Serializable]
public struct Choice {
    public string choiceText;
    public DialogueData nextDialogue;
}

[Serializable]
public struct Dialogue {
    public string name;
    [TextArea(5, 10)]
    public string text;

    public EventReference dialogueAudio;

    public List<Choice> choices;       // vazio = diálogo linear
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "ScriptableObject/TalkScript", order = 1)]
public class DialogueData : ScriptableObject {
    public List<Dialogue> talkScript;
}