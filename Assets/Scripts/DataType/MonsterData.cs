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
    public int MonsterID;
    public string Name;

    public int ID         => MonsterID;
    public int TypeHeader => GameDataID.GetHeader(MonsterID);
    public int Number     => GameDataID.GetNumber(MonsterID);
    public void SetID(int id) => MonsterID = id;
    public string Name_KR;
    [TextArea]
    public string Description;
    public int HP;
    public int MP;
    public int Damage;
    public List<string> Skills = new List<string>();
    public List<MonsterSoulDrop> DropSoul = new List<MonsterSoulDrop>();
    public List<MonsterItemDrop> DropItems = new List<MonsterItemDrop>();
    public int ObjectID;
    public int SpriteID;
}