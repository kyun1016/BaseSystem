using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewItemData", menuName = "ScriptableObjects/ItemData", order = 1)]
public class ItemData : ScriptableObject, IGameData
{
    public BaseData Base;

    // IGameData 구현 — Base에 위임
    public int ID => Base?.ID ?? 0;
    public eHeader Header => Base?.Header ?? eHeader.None;
    public int Key => Base?.Key ?? 0;
    public string Alias => Base?.Alias ?? string.Empty;
    public LocalizedString Name;
    public LocalizedString Description;
    public eItemType ItemType;
    public int BuyPrice;
    public int SellPrice;
    public List<ReferenceData> EffectStats = new List<ReferenceData>();
    public bool IsHidden;
    public string IconPath;
}