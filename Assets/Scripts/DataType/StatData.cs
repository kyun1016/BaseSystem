using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject, IGameData
{
    public int StatID;
    public StatType Type;
    public string Name;

    public int ID         => StatID;
    public int TypeHeader => GameDataID.GetHeader(StatID);
    public int Number     => GameDataID.GetNumber(StatID);
    public void SetID(int id) => StatID = id;
    public string Name_KR;
    [TextArea] 
    public string Description;
    public StatCategory StatCategory;
    public int MinValue;
    public int MaxValue;
    public string IconPath;
}