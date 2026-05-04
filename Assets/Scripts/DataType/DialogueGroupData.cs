using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueGroupData", menuName = "ScriptableObjects/DialogueGroupData", order = 2)]
public class DialogueGroupData : ScriptableObject, IGameData
{
    public BaseData Base;
    // IGameData 구현 — Base에 위임
    public int ID => Base?.ID ?? 0;
    public eHeader Header => Base?.Header ?? eHeader.None;
    public int Key => Base?.Key ?? 0;
    public string Alias => Base?.Alias ?? string.Empty;

    public int StartNodeKey;        // 진입 노드 Key
    public List<DialogueData> Nodes = new List<DialogueData>();
}