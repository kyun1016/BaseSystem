using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStatManager : Singleton<PlayerStatManager>
{
    private int[] baseStats = new int[(int)eStatType.MAX_COUNT];
    private int[] buffStats = new int[(int)eStatType.MAX_COUNT];
    private int[] currentStats = new int[(int)eStatType.MAX_COUNT];
    public event Action<eStatType, int> OnStatChanged;

    public void Init()
    {
        currentStats = new int[(int)eStatType.MAX_COUNT];
        // 초기 스탯 설정
        for (eStatType eStatType = 0; eStatType < eStatType.MAX_COUNT; eStatType++)
        {
            StatData stat = DataManager.Instance.GetStat(eStatType);
            if (stat != null)
            {
                currentStats[(int)eStatType] = stat.MinValue;
            }
            else
            {
                Debug.LogWarning($"[PlayerStatManager] 초기화 중 StatData가 누락된 eStatType: {eStatType}");
            }
        }
    }

    public void SetStat(eStatType eStatType, int amount)
    {
        int newValue = amount;

        StatData baseData = DataManager.Instance.GetStat(eStatType);
        if (baseData != null)
        {
            newValue = Mathf.Clamp(newValue, baseData.MinValue, baseData.MaxValue);
        }

        int currentValue = currentStats[(int)eStatType];
        if (newValue == currentValue) return;

        currentStats[(int)eStatType] = newValue;
        OnStatChanged?.Invoke(eStatType, newValue);

        Debug.Log($"[{eStatType}] 스탯 변동: {currentValue} -> {newValue}");
    }

    public void AddStat(int statID, int amount)
    {
        int newValue = currentStats[statID - GameDataHeaders.Stat] + amount;
        SetStat((eStatType)statID, newValue);
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