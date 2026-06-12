using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Choice {
    public string choiceText;          // O texto que vai aparecer no botão
    public DialogueData nextDialogue;  // O ScriptableObject que será carregado se essa opção for escolhida
}

[Serializable]
public struct Dialogue {
    public string name;
    [TextArea(5, 10)]
    public string text;
    public List<Choice> choices;       // Lista de escolhas (deixe vazio para diálogo linear)
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "ScriptableObject/TalkScript", order = 1)]
public class DialogueData : ScriptableObject {
    public List<Dialogue> talkScript;
}