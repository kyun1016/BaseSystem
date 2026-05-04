using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStatManager : Singleton<PlayerStatManager>
{
    private int[] baseStats = new int[0];
    private int[] buffStats = new int[0];
    private int[] currentStats = new int[0];
    public event Action<eStatType, int> OnChanged;
    public static readonly int BASE_KEY = (int)eHeader.Stat * BaseData.HEADER_SIZE;

    public void Init()
    {
        baseStats = new int[(int)eStatType.MAX_COUNT];
        buffStats = new int[(int)eStatType.MAX_COUNT];
        currentStats = new int[(int)eStatType.MAX_COUNT];
        // 초기 스탯 설정
        for (int i=0; i<(int)eStatType.MAX_COUNT; i++)
        {
            StatData stat = DataManager.Instance.GetStat(BASE_KEY + i);
            if (stat != null)
            {
                currentStats[i] = stat.MinValue;
                baseStats[i] = stat.MinValue;
                buffStats[i] = 0;
            }
            else
            {
                Debug.LogWarning($"[PlayerStatManager] 초기화 중 StatData가 누락된 eStatType: {(eStatType)i}");
            }
        }
    }

    public void SetStat(eStatType eStatType, int amount)
    {
        int newValue = amount;

        StatData baseData = DataManager.Instance.GetStat(BASE_KEY + (int)eStatType);
        if (baseData != null)
        {
            newValue = Mathf.Clamp(newValue, baseData.MinValue, baseData.MaxValue);
        }

        int currentValue = currentStats[(int)eStatType];
        if (newValue == currentValue) return;

        currentStats[(int)eStatType] = newValue;
        OnChanged?.Invoke(eStatType, newValue);

        Debug.Log($"[{eStatType}] 스탯 변동: {currentValue} -> {newValue}");
    }

    public void AddStat(int key, int amount)
    {
        int newValue = currentStats[key - BASE_KEY] + amount;
        SetStat((eStatType)(key - BASE_KEY), newValue);
    }
    public void AddStat(eStatType eStatType, int amount)
    {
        int newValue = currentStats[(int)eStatType] + amount;
        SetStat(eStatType, newValue);
    }


    public int GetStatValue(eStatType eStatType)
    {
        return currentStats[(int)eStatType];
    }
}