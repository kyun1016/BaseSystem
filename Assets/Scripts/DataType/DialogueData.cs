using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "ScriptableObjects/DialogueData", order = 1)]
public class DialogueData : ScriptableObject
{
    public BaseData Base;
    public LocalizedString Description;
    public eItemType ItemType;
    public int BuyPrice;
    public int SellPrice;
    public List<ReferenceData> EffectStats = new List<ReferenceData>();
    public bool IsHidden;
    public string IconPath;
}