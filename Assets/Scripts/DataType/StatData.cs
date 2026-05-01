using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewStatData", menuName = "ScriptableObjects/StatData", order = 1)]
public class StatData : ScriptableObject
{
    public BaseData Base;
    [TextArea] 
    public LocalizedString Description;
    public eStatCategory StatCategory;
    public int MinValue;
    public int MaxValue;
    public string IconPath;
}