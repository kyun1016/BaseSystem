using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/ItemData", order = 1)]
public class ItemData : ScriptableObject
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