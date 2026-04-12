using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int currentTurn;
    public List<int> inventory = new List<int>();
    public Dictionary<StatType, int> currentStats = new Dictionary<StatType, int>();
}