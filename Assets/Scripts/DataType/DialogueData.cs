using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "ScriptableObjects/DialogueData", order = 1)]
public class DialogueData : ScriptableObject, IGameData
{
    public BaseData Base;

    // IGameData 구현 — Base에 위임
    public int ID => Base?.ID ?? 0;
    public eHeader Header => Base?.Header ?? eHeader.None;
    public int Key => Base?.Key ?? 0;
    public string Alias => Base?.Alias ?? string.Empty;

    public List<LocalizedString> Texts = new List<LocalizedString>(); // 노드가 표시할 텍스트 목록 (Line: 순차 출력, Choice: 선택지 텍스트)
    public string Speakder;
    public eDialogueType DialogueType;
    public eDialogueAction ActionType;
    public string ActionTarget;
    public List<ReferenceData> StatCondition = new List<ReferenceData>();
    public List<ReferenceData> ItemCondition = new List<ReferenceData>();
    public List<ReferenceData> QuestCondition = new List<ReferenceData>();
    public List<int> NextNodeKeys = new List<int>(); // 다음 노드 Key 목록 (Choice: Texts 인덱스와 1:1 대응, Line/Action: 단일 항목)
}