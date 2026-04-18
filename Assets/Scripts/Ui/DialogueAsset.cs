using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public string speakerName;
    public Sprite portrait;
    public List<DialogueNode> nodes = new List<DialogueNode>();
}

[Serializable]
public class DialogueNode
{
    [TextArea] public string text;

    // If this list is empty => Space continues
    // If this list has items => show buttons
    public List<DialogueChoice> choices = new List<DialogueChoice>();

    // Used ONLY when there are no choices (Space mode)
    public int nextNodeIndex = -1; // -1 means end dialogue
}

[Serializable]
public class DialogueChoice
{
    public string choiceText;

    // Jump to a node when clicked
    public int nextNodeIndex = -1; // -1 means end dialogue
}
