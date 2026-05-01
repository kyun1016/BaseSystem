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
public class MonsterData : ScriptableObject
{
    public BaseData Base;
    [TextArea]
    public string Description;
    public int HP;
    public int MP;
    public int Damage;
    public List<string> Skills = new List<string>();
    public List<ReferenceData> DropSoul = new List<ReferenceData>();
    public List<ReferenceData> DropItems = new List<ReferenceData>();
    public int ObjectID;
    public int SpriteID;
}