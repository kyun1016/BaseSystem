using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject, IGameData
{
    public BaseData Base;

    // IGameData 구현 — Base에 위임
    public int ID => Base?.ID ?? 0;
    public eHeader Header => Base?.Header ?? eHeader.None;
    public int Key => Base?.Key ?? 0;
    public string Alias => Base?.Alias ?? string.Empty;
    public LocalizedString Name;
    public LocalizedString Description;
    public eStatType Type;
    public eStatCategory StatCategory;
    public int MinValue;
    public int MaxValue;
    public string IconPath;
}