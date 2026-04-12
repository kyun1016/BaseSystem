using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject
{
    public int ID;
    public StatType Type;
    public string Name;
    public string Name_KR;
    [TextArea] 
    public string Description;
    public StatCategory StatCategory;
    public int MinValue;
    public int MaxValue;
    public string IconPath;
}