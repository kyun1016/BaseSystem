using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStatManager : Singleton<PlayerStatManager>
{
    private int[] baseStats = new int[(int)StatType.MAX_COUNT];
    private int[] buffStats = new int[(int)StatType.MAX_COUNT];
    private int[] currentStats = new int[(int)StatType.MAX_COUNT];
    public event Action<StatType, int> OnStatChanged;

    public void Init()
    {
        currentStats = new int[(int)StatType.MAX_COUNT];
        // 초기 스탯 설정
        for (StatType statType = 0; statType < StatType.MAX_COUNT; statType++)
        {
            StatData stat = DataManager.Instance.GetStat(statType);
            if (stat != null)
            {
                currentStats[(int)statType] = stat.MinValue;
            }
        }
    }

    public void SetStat(StatType statType, int amount)
    {
        int newValue = amount;

        StatData baseData = DataManager.Instance.GetStat(statType);
        if (baseData != null)
        {
            newValue = Mathf.Clamp(newValue, baseData.MinValue, baseData.MaxValue);
        }

        int currentValue = currentStats[(int)statType];
        if (newValue == currentValue) return;

        currentStats[(int)statType] = newValue;
        OnStatChanged?.Invoke(statType, newValue);

        Debug.Log($"[{statType}] 스탯 변동: {currentValue} -> {newValue}");
    }

    public void AddStat(StatType statType, int amount)
    {
        int newValue = currentStats[(int)statType] + amount;
        SetStat(statType, newValue);
    }

    public int GetStatValue(StatType statType)
    {
        return currentStats[(int)statType];
    }
}