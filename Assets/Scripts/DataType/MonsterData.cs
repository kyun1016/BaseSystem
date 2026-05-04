using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MonsterSoulDrop
{
    public int SoulID;
    public int Weight;
}

[System.Serializable]
public class MonsterItemDrop
{
    public int ItemID;
    public int Weight;
    public int Count;
}

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "ScriptableObjects/MonsterData", order = 1)]
public class MonsterData : ScriptableObject, IGameData
{
    public BaseData Base;

    // IGameData 구현 — Base에 위임
    public int ID => Base?.ID ?? 0;
    public eHeader Header => Base?.Header ?? eHeader.None;
    public int Key => Base?.Key ?? 0;
    public string Alias => Base?.Alias ?? string.Empty;
    public LocalizedString Name;
    public LocalizedString Description;
    public int HP;
    public int MP;
    public int Damage;
    public List<string> Skills = new List<string>();
    public List<ReferenceData> DropSoul = new List<ReferenceData>();
    public List<ReferenceData> DropItems = new List<ReferenceData>();
    public int ObjectID;
    public int SpriteID;
}